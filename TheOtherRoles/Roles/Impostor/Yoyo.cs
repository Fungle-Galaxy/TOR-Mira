using System;
using MiraAPI.GameOptions.OptionTypes;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using UnityEngine;
using TheOtherRoles.Objects;
using MiraAPI.Utilities;
using TheOtherRoles.Buttons;
using TheOtherRoles.Patches;

namespace TheOtherRoles.Roles.Impostor;

public class Yoyo(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = Palette.ImpostorRed;

    public static RoleInfo Info = new(color, RoleId.Yoyo);

    public static PlayerControl yoyo = null;
    public static float blinkDuration;
    public static float markCooldown;
    public static bool markStaysOverMeeting;
    public static bool hasAdminTable;
    public static float adminCooldown;
    public static float silhouetteVisibility;

    public static Vector3? markedLocation;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Yoyo;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<YoyoOptions>.Instance.SpawnRate;

    public static float SilhouetteVisibility =>
        silhouetteVisibility == 0 && (PlayerControl.LocalPlayer == yoyo || PlayerControl.LocalPlayer.Data.IsDead)
            ? 0.1f
            : silhouetteVisibility;
    
    public static void markLocation(Vector3 position)
    {
        markedLocation = position;
    }

    public static void clearAndReload()
    {
        blinkDuration = OptionGroupSingleton<YoyoOptions>.Instance.BlinkDuration.Value;
        markCooldown = OptionGroupSingleton<YoyoOptions>.Instance.MarkCooldown.Value;
        markStaysOverMeeting = OptionGroupSingleton<YoyoOptions>.Instance.MarkStaysOverMeeting.Value;
        hasAdminTable = OptionGroupSingleton<YoyoOptions>.Instance.HasAdminTable.Value;
        adminCooldown = OptionGroupSingleton<YoyoOptions>.Instance.AdminTableCooldown.Value;
        silhouetteVisibility = OptionGroupSingleton<YoyoOptions>.Instance.SilhouetteVisibility.Selection() / 10f;

        markedLocation = null;
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;
}

/// <summary>
/// The settings of YoyoOptions. They live next to the role on purpose: the group is bound
/// to Yoyo, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class YoyoOptions : TorRoleOptionGroup<Yoyo>
{
    public override uint GroupPriority => 220;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Yoyo));
    public override Color GroupColor => TorOptionColors.Group(Yoyo.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.Yoyo), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedNumberOption BlinkDuration { get; } =
        new ModdedNumberOption("Opt-YoYo,1", 20f, 2.5f, 120f, 2.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption MarkCooldown { get; } =
        new ModdedNumberOption("Opt-YoYo,2", 20f, 2.5f, 120f, 2.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedToggleOption MarkStaysOverMeeting { get; } =
        new ModdedToggleOption("Opt-YoYo,3", true);

    public ModdedToggleOption HasAdminTable { get; } =
        new ModdedToggleOption("Opt-YoYo,4", true);

    public ModdedNumberOption AdminTableCooldown { get; } =
        new ModdedNumberOption("Opt-YoYo,5", 20f, 2.5f, 120f, 2.5f, MiraNumberSuffixes.None, "0.##")
        {
            Visible = () => OptionGroupSingleton<YoyoOptions>.Instance.HasAdminTable.Value
        };

    public ModdedStringOption SilhouetteVisibility { get; } =
        new ModdedStringOption("Opt-YoYo,6", "0%", ["0%", "10%", "20%", "30%", "40%", "50%"]);
}

/// <summary>
/// The Yoyo's mark/blink button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>yoyoButton</c>.
/// </summary>
public sealed class YoyoButton : TorButton
{
    private static YoyoButton yoyoButton;

    public YoyoButton()
    {
        yoyoButton = this;

        SetSprite(TorAssets.YoyoMarkButtonSprite);
        PositionOffset = TorButtonPositions.UpperRowLeft;
        Hotkey = KeyCode.F;
        EffectLength = Yoyo.blinkDuration;
        ShowButtonText = true;
        ButtonText = new ButtonText(37);

        RealOnClick = () =>
        {
            var pos = PlayerControl.LocalPlayer.transform.position;
            var buff = new byte[sizeof(float) * 2];
            Buffer.BlockCopy(BitConverter.GetBytes(pos.x), 0, buff, 0 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(pos.y), 0, buff, 1 * sizeof(float), sizeof(float));

            if (Yoyo.markedLocation == null)
            {
                PlayerControl.LocalPlayer.RpcYoyoMarkLocation(buff);
                SoundEffectsManager.play("tricksterPlaceBox");
                yoyoButton.SetSprite(TorAssets.YoyoBlinkButtonSprite);
                yoyoButton.Timer = 10f;
                yoyoButton.EffectEnabled = false;
                yoyoButton.ButtonText = new ButtonText(35);
            }
            else
            {
                // Jump to location
                var exit = (Vector3)Yoyo.markedLocation;
                if (SubmergedCompatibility.IsSubmerged) SubmergedCompatibility.ChangeFloor(exit.y > -7);
                PlayerControl.LocalPlayer.RpcYoyoBlink(byte.MaxValue, buff);
                yoyoButton.EffectLength = Yoyo.blinkDuration;
                yoyoButton.Timer = 10f;
                yoyoButton.EffectEnabled = true;
                yoyoButton.ButtonText = new ButtonText(36);
                SoundEffectsManager.play("morphlingMorph");
            }
        };
        HasButton = () =>
        {
            return Yoyo.yoyo != null && Yoyo.yoyo == PlayerControl.LocalPlayer &&
                   !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () => { return PlayerControl.LocalPlayer.CanMove; };
        OnMeetingEnds = () =>
        {
            if (Yoyo.markStaysOverMeeting)
            {
                yoyoButton.Timer = 10f;
            }
            else
            {
                Yoyo.markedLocation = null;
                yoyoButton.Timer = yoyoButton.MaxTimer;
                yoyoButton.SetSprite(TorAssets.YoyoMarkButtonSprite);
                yoyoButton.ButtonText = new ButtonText(37);
            }
        };
    }

    public override float Cooldown => Yoyo.markCooldown;

    public override void OnEffectEnd()
    {
        if (TransportationToolPatches.isUsingTransportation(Yoyo.yoyo))
        {
            yoyoButton.Timer = 0.5f;
            yoyoButton.DeputyTimer = 0.5f;
            yoyoButton.isEffectActive = true;
            yoyoButton.actionButton.cooldownTimerText.color = new Color(0F, 0.8F, 0F);
            return;
        }

        if (Yoyo.yoyo.inVent) HudManager.Instance.ImpostorVentButton.DoClick();

        // jump back!
        var pos = PlayerControl.LocalPlayer.transform.position;
        var buff = new byte[sizeof(float) * 2];
        Buffer.BlockCopy(BitConverter.GetBytes(pos.x), 0, buff, 0 * sizeof(float), sizeof(float));
        Buffer.BlockCopy(BitConverter.GetBytes(pos.y), 0, buff, 1 * sizeof(float), sizeof(float));
        var exit = (Vector3)Yoyo.markedLocation;
        if (SubmergedCompatibility.IsSubmerged) SubmergedCompatibility.ChangeFloor(exit.y > -7);
        PlayerControl.LocalPlayer.RpcYoyoBlink(0, buff);

        yoyoButton.Timer = yoyoButton.MaxTimer;
        yoyoButton.isEffectActive = false;
        yoyoButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
        yoyoButton.EffectEnabled = false;
        yoyoButton.SetSprite(TorAssets.YoyoMarkButtonSprite);
        yoyoButton.ButtonText = new ButtonText(37);
        SoundEffectsManager.play("morphlingMorph");
        if (Minigame.Instance) Minigame.Instance.Close();
    }
}

/// <summary>
/// The Yoyo's admin table button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>yoyoAdminTableButton</c>.
/// </summary>
public sealed class YoyoAdminTableButton : TorButton
{
    private static YoyoAdminTableButton yoyoAdminTableButton;

    public YoyoAdminTableButton()
    {
        yoyoAdminTableButton = this;

        PositionOffset = TorButtonPositions.LowerRowCenter;
        Hotkey = KeyCode.G;
        EffectEnabled = true;
        ShowButtonText = true;
        ButtonText = new ButtonText(StringNames.Admin);

        RealOnClick = () =>
        {
            if (!MapBehaviour.Instance || !MapBehaviour.Instance.isActiveAndEnabled)
            {
                HudManager.Instance.InitMap();
                MapBehaviour.Instance.ShowCountOverlay(true, true, true);
            }
        };
        HasButton = () =>
        {
            return Yoyo.yoyo != null && Yoyo.yoyo == PlayerControl.LocalPlayer && Yoyo.hasAdminTable &&
                   !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () => { return true; };
        OnMeetingEnds = () =>
        {
            yoyoAdminTableButton.Timer = yoyoAdminTableButton.MaxTimer;
            yoyoAdminTableButton.isEffectActive = false;
            yoyoAdminTableButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
        };
    }

    public override float Cooldown => Yoyo.adminCooldown;

    public override float EffectDuration => 10f;

    public override void OnEffectEnd()
    {
        yoyoAdminTableButton.Timer = yoyoAdminTableButton.MaxTimer;
        if (MapBehaviour.Instance && MapBehaviour.Instance.isActiveAndEnabled) MapBehaviour.Instance.Close();
    }

    public override void CreateButton(Transform parent)
    {
        // The admin sprite only exists once the HUD is up.
        SetSprite(Hacker.getAdminSprite());
        base.CreateButton(parent);
        Mirror = GameOptionsManager.Instance.currentNormalGameOptions.MapId == 3;
    }
}

public static class YoyoRpcs
{
    [MethodRpc((uint)TorRpc.YoyoMarkLocation, LocalHandling = RpcLocalHandling.After)]
    public static void RpcYoyoMarkLocation(this PlayerControl player, byte[] buff)
    {
        if (Yoyo.yoyo == null) return;
        var position = Vector3.zero;
        position.x = BitConverter.ToSingle(buff, 0 * sizeof(float));
        position.y = BitConverter.ToSingle(buff, 1 * sizeof(float));
        Yoyo.markLocation(position);
        new Silhouette(position, -1, false);
    }

    [MethodRpc((uint)TorRpc.YoyoBlink, LocalHandling = RpcLocalHandling.After)]
    public static void RpcYoyoBlink(this PlayerControl player, byte flag, byte[] buff)
    {
        RPCProcedure.yoyoBlink(flag == byte.MaxValue, buff);
    }
}
