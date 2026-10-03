using System;
using Object = UnityEngine.Object;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Hud;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheOtherRoles.Buttons;
using TheOtherRoles.Objects;
using UnityEngine;
using MiraAPI.Utilities;

namespace TheOtherRoles.Roles.Impostor;

public class Bomber(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = Palette.ImpostorRed;
    public static RoleInfo Info = new(color, RoleId.Bomber);

    public static PlayerControl bomber;
    public static Bomb bomb;
    public static bool isPlanted;
    public static bool isActive;
    public static float destructionTime = 20f;
    public static float destructionRange = 2f;
    public static float hearRange = 30f;
    public static float defuseDuration = 3f;
    public static float bombCooldown = 15f;
    public static float bombActiveAfter = 3f;
    
    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Bomber;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<BomberOptions>.Instance.SpawnRate;
    
    public static void clearBomb(bool flag = true)
    {
        TheOtherRolesPlugin.Logger.LogDebug("Clearing Bomb!");
        if (bomb != null)
        {
            Object.Destroy(bomb.bomb);
            Object.Destroy(bomb.background);
            bomb = null;
        }

        isPlanted = false;
        isActive = false;
        Bomb.clearBackgroundSprite();
        if (flag) SoundEffectsManager.stop("bombFuseBurning");
    }

    public static void clearAndReload()
    {
        clearBomb(false);
        bomber = null;
        bomb = null;
        isPlanted = false;
        isActive = false;
        destructionTime = OptionGroupSingleton<BomberOptions>.Instance.BombDestructionTime.Value;
        destructionRange = OptionGroupSingleton<BomberOptions>.Instance.BombDestructionRange.Value / 10;
        hearRange = OptionGroupSingleton<BomberOptions>.Instance.BombHearRange.Value / 10;
        defuseDuration = OptionGroupSingleton<BomberOptions>.Instance.DefuseDuration.Value;
        bombCooldown = OptionGroupSingleton<BomberOptions>.Instance.BombCooldown.Value;
        bombActiveAfter = OptionGroupSingleton<BomberOptions>.Instance.BombActiveAfter.Value;
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;
}

/// <summary>
/// The settings of BomberOptions. They live next to the role on purpose: the group is bound
/// to Bomber, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class BomberOptions : TorRoleOptionGroup<Bomber>
{
    public override uint GroupPriority => 210;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Bomber));
    public override Color GroupColor => TorOptionColors.Group(Bomber.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.Bomber), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedNumberOption BombDestructionTime { get; } =
        new ModdedNumberOption("Opt-Bomber,1", 20f, 2.5f, 120f, 2.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption BombDestructionRange { get; } =
        new ModdedNumberOption("Opt-Bomber,2", 50f, 5f, 150f, 5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption BombHearRange { get; } =
        new ModdedNumberOption("Opt-Bomber,3", 60f, 5f, 150f, 5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption DefuseDuration { get; } =
        new ModdedNumberOption("Opt-Bomber,4", 3f, 0.5f, 30f, 0.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption BombCooldown { get; } =
        new ModdedNumberOption("Opt-Bomber,5", 15f, 2.5f, 30f, 2.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption BombActiveAfter { get; } =
        new ModdedNumberOption("Opt-Bomber,6", 3f, 0.5f, 15f, 0.5f, MiraNumberSuffixes.None, "0.##");
}

/// <summary>
/// The Bomber's "plant bomb" button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>bomberButton</c>.
/// </summary>
public sealed class BomberButton : TorButton
{
    private static BomberButton bomberButton;

    public BomberButton()
    {
        bomberButton = this;

        SetSprite(TorAssets.BombButtonPlant);
        PositionOffset = TorButtonPositions.UpperRowLeft;
        Hotkey = KeyCode.F;
        EffectEnabled = true;
        ShowButtonText = true;
        ButtonText = new ButtonText(33);

        RealOnClick = () =>
        {
            if (Helpers.checkMuderAttempt(Bomber.bomber, Bomber.bomber, ignoreMedic: true) !=
                MurderAttemptResult.BlankKill)
            {
                var pos = PlayerControl.LocalPlayer.transform.position;
                var buff = new byte[sizeof(float) * 2];
                Buffer.BlockCopy(BitConverter.GetBytes(pos.x), 0, buff, 0 * sizeof(float), sizeof(float));
                Buffer.BlockCopy(BitConverter.GetBytes(pos.y), 0, buff, 1 * sizeof(float), sizeof(float));

                PlayerControl.LocalPlayer.RpcPlaceBomb(buff);

                SoundEffectsManager.play("trapperTrap");
            }

            bomberButton.Timer = bomberButton.MaxTimer;
            Bomber.isPlanted = true;
        };
        HasButton = () =>
        {
            return Bomber.bomber != null && Bomber.bomber == PlayerControl.LocalPlayer &&
                   !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () => { return PlayerControl.LocalPlayer.CanMove && !Bomber.isPlanted; };
        OnMeetingEnds = () => { bomberButton.Timer = bomberButton.MaxTimer; };
    }

    public override float Cooldown => Bomber.bombCooldown;

    public override float EffectDuration => Bomber.destructionTime + Bomber.bombActiveAfter;

    public override void OnEffectEnd()
    {
        bomberButton.Timer = bomberButton.MaxTimer;
        bomberButton.isEffectActive = false;
        bomberButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
    }
}

/// <summary>
/// The defuse button for a planted Bomber bomb. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>defuseButton</c>.
/// </summary>
public sealed class DefuseButton : TorButton
{
    private static DefuseButton defuseButton;

    public DefuseButton()
    {
        defuseButton = this;

        SetSprite(TorAssets.BombButtonDefuse);
        PositionOffset = new Vector3(0f, 1f, 0);
        Hotkey = KeyCode.I;
        Mirror = true;
        EffectEnabled = true;
        ShowButtonText = true;
        ButtonText = new ButtonText(34);

        RealOnClick = () => { defuseButton.EffectEnabled = true; };
        HasButton = () =>
        {
            if (CustomButtonSingleton<ShifterShiftButton>.Instance.HasButton())
                defuseButton.PositionOffset = new Vector3(0f, 2f, 0f);
            else
                defuseButton.PositionOffset = new Vector3(0f, 1f, 0f);
            return Bomber.bomb != null && Bomb.canDefuse && !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () =>
        {
            if (defuseButton.isEffectActive && !Bomb.canDefuse)
            {
                defuseButton.Timer = 0f;
                defuseButton.isEffectActive = false;
            }

            return PlayerControl.LocalPlayer.CanMove;
        };
        OnMeetingEnds = () =>
        {
            defuseButton.Timer = 0f;
            defuseButton.isEffectActive = false;
        };
    }

    public override float Cooldown => 0f;

    public override float EffectDuration => Bomber.defuseDuration;

    public override void OnEffectEnd()
    {
        PlayerControl.LocalPlayer.RpcDefuseBomb();

        defuseButton.Timer = 0f;
        Bomb.canDefuse = false;
    }

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        Timer = 0f;
    }
}

public static class BomberRpcs
{
    [MethodRpc((uint)TorRpc.PlaceBomb, LocalHandling = RpcLocalHandling.After)]
    public static void RpcPlaceBomb(this PlayerControl player, byte[] buff)
    {
        if (Bomber.bomber == null) return;
        var position = Vector3.zero;
        position.x = BitConverter.ToSingle(buff, 0 * sizeof(float));
        position.y = BitConverter.ToSingle(buff, 1 * sizeof(float));
        new Bomb(position);
    }

    [MethodRpc((uint)TorRpc.DefuseBomb, LocalHandling = RpcLocalHandling.After)]
    public static void RpcDefuseBomb(this PlayerControl player)
    {
        try
        {
            SoundEffectsManager.playAtPosition("bombDefused", Bomber.bomb.bomb.transform.position,
                range: Bomber.hearRange);
        }
        catch
        {
        }

        Bomber.clearBomb();
        var bomber = CustomButtonSingleton<BomberButton>.Instance;
        bomber.Timer = bomber.MaxTimer;
        bomber.isEffectActive = false;
        if (bomber.actionButton != null) bomber.actionButton.cooldownTimerText.color = Palette.EnabledColor;
    }
}
