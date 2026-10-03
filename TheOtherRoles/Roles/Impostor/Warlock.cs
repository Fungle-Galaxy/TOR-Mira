using System;
using MiraAPI.GameOptions.OptionTypes;
using TheOtherRoles.Patches;
using UnityEngine;
using MiraAPI.Hud;
using MiraAPI.Utilities;
using TheOtherRoles.Buttons;
using TheOtherRoles.Networking;
using TheOtherRoles.Utilities;

namespace TheOtherRoles.Roles.Impostor;

public class Warlock(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = Palette.ImpostorRed;

    public static RoleInfo Info = new(color, RoleId.Warlock);

    public static PlayerControl warlock;
    public static PlayerControl currentTarget;
    public static PlayerControl curseVictim;
    public static PlayerControl curseVictimTarget;
    public static float cooldown = 30f;
    public static float rootTime = 5f;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Warlock;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<WarlockOptions>.Instance.SpawnRate;
    
    public static void clearAndReload()
    {
        warlock = null;
        currentTarget = null;
        curseVictim = null;
        curseVictimTarget = null;
        cooldown = OptionGroupSingleton<WarlockOptions>.Instance.Cooldown.Value;
        rootTime = OptionGroupSingleton<WarlockOptions>.Instance.RootTime.Value;
    }

    public static void resetCurse()
    {
        var button = CustomButtonSingleton<WarlockCurseButton>.Instance;
        button.Timer = button.MaxTimer;
        button.SetSprite(TorAssets.CurseButton);
        if (button.actionButton != null) button.actionButton.cooldownTimerText.color = Palette.EnabledColor;
        currentTarget = null;
        curseVictim = null;
        curseVictimTarget = null;
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        if (Warlock.warlock == null || Warlock.warlock != player) return;
        if (Warlock.curseVictim != null && (Warlock.curseVictim.Data.Disconnected || Warlock.curseVictim.Data.IsDead))
            Warlock.resetCurse();
        if (Warlock.curseVictim == null)
        {
            Warlock.currentTarget = PlayerControlFixedUpdatePatch.setTarget();
            PlayerControlFixedUpdatePatch.setPlayerOutline(Warlock.currentTarget, Warlock.color);
        }
        else
        {
            Warlock.curseVictimTarget = PlayerControlFixedUpdatePatch.setTarget(targetingPlayer: Warlock.curseVictim);
            PlayerControlFixedUpdatePatch.setPlayerOutline(Warlock.curseVictimTarget, Warlock.color);
        }
    }
}

/// <summary>
/// The settings of WarlockOptions. They live next to the role on purpose: the group is bound
/// to Warlock, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class WarlockOptions : TorRoleOptionGroup<Warlock>
{
    public override uint GroupPriority => 170;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Warlock));
    public override Color GroupColor => TorOptionColors.Group(Warlock.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.Warlock), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedNumberOption Cooldown { get; } =
        new ModdedNumberOption("Opt-Warlock,1", 30f, 10f, 60f, 2.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption RootTime { get; } =
        new ModdedNumberOption("Opt-Warlock,2", 5f, 0f, 15f, 1f, MiraNumberSuffixes.None, "0.##");
}

/// <summary>
/// The Warlock's curse button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>warlockCurseButton</c>.
/// </summary>
public sealed class WarlockCurseButton : TorButton
{
    private static WarlockCurseButton warlockCurseButton;

    public WarlockCurseButton()
    {
        warlockCurseButton = this;

        SetSprite(TorAssets.CurseButton);
        PositionOffset = TorButtonPositions.UpperRowLeft;
        Hotkey = KeyCode.F;
        ShowButtonText = true;
        ButtonText = new ButtonText(20);

        RealOnClick = () =>
        {
            if (Warlock.curseVictim == null)
            {
                // Apply Curse
                Warlock.curseVictim = Warlock.currentTarget;
                warlockCurseButton.SetSprite(TorAssets.CurseKillButton);
                warlockCurseButton.ButtonText = new ButtonText(StringNames.KillLabel);
                warlockCurseButton.Timer = 1f;
                SoundEffectsManager.play("warlockCurse");

                // Ghost Info
                PlayerControl.LocalPlayer.RpcGhostWarlockTarget(PlayerControl.LocalPlayer.PlayerId,
                    Warlock.curseVictim.PlayerId);
            }
            else if (Warlock.curseVictim != null && Warlock.curseVictimTarget != null)
            {
                var murder = Helpers.checkMurderAttemptAndKill(Warlock.warlock, Warlock.curseVictimTarget,
                    showAnimation: false);
                if (murder == MurderAttemptResult.SuppressKill) return;

                // If blanked or killed
                if (Warlock.rootTime > 0)
                {
                    AntiTeleport.position = PlayerControl.LocalPlayer.transform.position;
                    PlayerControl.LocalPlayer.moveable = false;
                    PlayerControl.LocalPlayer.NetTransform
                        .Halt(); // Stop current movement so the warlock is not just running straight into the next object
                    FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(Warlock.rootTime,
                        new Action<float>(p =>
                        {
                            // Delayed action
                            if (p == 1f) PlayerControl.LocalPlayer.moveable = true;
                        })));
                }

                Warlock.curseVictim = null;
                Warlock.curseVictimTarget = null;
                warlockCurseButton.SetSprite(TorAssets.CurseButton);
                warlockCurseButton.ButtonText = new ButtonText(20);
                Warlock.warlock.killTimer = warlockCurseButton.Timer = warlockCurseButton.MaxTimer;

                PlayerControl.LocalPlayer.RpcGhostWarlockTarget(PlayerControl.LocalPlayer.PlayerId,
                    byte.MaxValue); // This will set it to null!
            }
        };
        HasButton = () =>
        {
            return Warlock.warlock != null && Warlock.warlock == PlayerControl.LocalPlayer &&
                   !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () =>
        {
            return ((Warlock.curseVictim == null && Warlock.currentTarget != null) ||
                    (Warlock.curseVictim != null && Warlock.curseVictimTarget != null)) &&
                   PlayerControl.LocalPlayer.CanMove;
        };
        OnMeetingEnds = () =>
        {
            warlockCurseButton.Timer = warlockCurseButton.MaxTimer;
            warlockCurseButton.SetSprite(TorAssets.CurseButton);
            Warlock.curseVictim = null;
            Warlock.curseVictimTarget = null;
        };
    }

    public override float Cooldown => Warlock.cooldown;
}
