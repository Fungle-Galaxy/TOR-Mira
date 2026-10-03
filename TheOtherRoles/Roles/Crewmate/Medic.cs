using System;
using MiraAPI.GameOptions.OptionTypes;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheOtherRoles.Buttons;
using TheOtherRoles.Patches;
using UnityEngine;
using TheOtherRoles.Utilities;

namespace TheOtherRoles.Roles.Crewmate;

public class Medic(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = new Color32(126, 251, 194, byte.MaxValue);

    public static RoleInfo Info = new(color, RoleId.Medic);

    public static PlayerControl medic;
    public static PlayerControl shielded;
    public static PlayerControl futureShielded;

    public static bool usedShield;

    public static int showShielded;
    public static bool showAttemptToShielded;
    public static bool showAttemptToMedic;
    public static bool setShieldAfterMeeting;
    public static bool showShieldAfterMeeting;
    public static bool meetingAfterShielding;

    public static Color shieldedColor = new Color32(0, 221, 255, byte.MaxValue);
    public static PlayerControl currentTarget;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Medic;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<MedicOptions>.Instance.SpawnRate;
    
    public static bool shieldVisible(PlayerControl target)
    {
        var hasVisibleShield = false;

        var isMorphedMorphling =
            target == Morphling.morphling && Morphling.morphTarget != null && Morphling.morphTimer > 0f;
        if (shielded != null && ((target == shielded && !isMorphedMorphling) ||
                                 (isMorphedMorphling && Morphling.morphTarget == shielded)))
        {
            hasVisibleShield = showShielded == 0 || Helpers.shouldShowGhostInfo()
                                                 || (showShielded == 1 && (PlayerControl.LocalPlayer == shielded ||
                                                                           PlayerControl.LocalPlayer == medic))
                                                 || (showShielded == 2 && PlayerControl.LocalPlayer == medic);
            hasVisibleShield = hasVisibleShield && (meetingAfterShielding || !showShieldAfterMeeting ||
                                                    PlayerControl.LocalPlayer == medic ||
                                                    Helpers.shouldShowGhostInfo());
        }

        return hasVisibleShield;
    }

    public static void clearAndReload()
    {
        medic = null;
        shielded = null;
        futureShielded = null;
        currentTarget = null;
        usedShield = false;
        showShielded = OptionGroupSingleton<MedicOptions>.Instance.ShowShielded.Selection();
        showAttemptToShielded = OptionGroupSingleton<MedicOptions>.Instance.ShowAttemptToShielded.Value;
        showAttemptToMedic = OptionGroupSingleton<MedicOptions>.Instance.ShowAttemptToMedic.Value;
        setShieldAfterMeeting = OptionGroupSingleton<MedicOptions>.Instance.SetOrShowShieldAfterMeeting.Selection() == 2;
        showShieldAfterMeeting = OptionGroupSingleton<MedicOptions>.Instance.SetOrShowShieldAfterMeeting.Selection() == 1;
        meetingAfterShielding = false;
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        if (Medic.medic == null || Medic.medic != player) return;
        Medic.currentTarget = PlayerControlFixedUpdatePatch.setTarget();
        if (!Medic.usedShield) PlayerControlFixedUpdatePatch.setPlayerOutline(Medic.currentTarget, Medic.shieldedColor);
    }
}

/// <summary>
/// The settings of MedicOptions. They live next to the role on purpose: the group is bound
/// to Medic, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class MedicOptions : TorRoleOptionGroup<Medic>
{
    public override uint GroupPriority => 460;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Medic));
    public override Color GroupColor => TorOptionColors.Group(Medic.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.Medic), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedStringOption ShowShielded { get; } =
        new ModdedStringOption("Opt-Medic,1", "Opt-Medic,100", ["Opt-Medic,100", "Opt-Medic,101", "Opt-Medic,102"]);

    public ModdedToggleOption ShowAttemptToShielded { get; } =
        new ModdedToggleOption("Opt-Medic,2", false);

    public ModdedStringOption SetOrShowShieldAfterMeeting { get; } =
        new ModdedStringOption("Opt-Medic,3", "Opt-Medic,110", ["Opt-Medic,110", "Opt-Medic,111", "Opt-Medic,112"]);

    public ModdedToggleOption ShowAttemptToMedic { get; } =
        new ModdedToggleOption("Opt-Medic,4", false);
}

/// <summary>
/// The Medic's "give out a shield" button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>medicShieldButton</c>.
/// </summary>
public sealed class MedicShieldButton : TorButton
{
    private static MedicShieldButton medicShieldButton;

    public MedicShieldButton()
    {
        medicShieldButton = this;

        SetSprite(TorAssets.ShieldButton);
        PositionOffset = TorButtonPositions.LowerRowRight;
        Hotkey = KeyCode.F;
        ShowButtonText = true;

        RealOnClick = () =>
        {
            medicShieldButton.Timer = 0f;

            if (Medic.setShieldAfterMeeting)
                PlayerControl.LocalPlayer.RpcSetFutureShielded(Medic.currentTarget.PlayerId);
            else
                PlayerControl.LocalPlayer.RpcMedicSetShielded(Medic.currentTarget.PlayerId);
            Medic.meetingAfterShielding = false;

            SoundEffectsManager.play("medicShield");
        };
        HasButton = () =>
        {
            return Medic.medic != null && Medic.medic == PlayerControl.LocalPlayer &&
                   !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () => { return !Medic.usedShield && Medic.currentTarget && PlayerControl.LocalPlayer.CanMove; };
        OnMeetingEnds = () => { };
    }

    public override float Cooldown => 0f;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        ButtonText = new ButtonText(5,
            FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.UseButton]
                .FontMaterial);
    }
}

public static class MedicRpcs
{
    [MethodRpc((uint)TorRpc.MedicSetShielded, LocalHandling = RpcLocalHandling.After)]
    public static void RpcMedicSetShielded(this PlayerControl player, byte shieldedId)
    {
        Medic.usedShield = true;
        Medic.shielded = Helpers.playerById(shieldedId);
        Medic.futureShielded = null;
    }

    [MethodRpc((uint)TorRpc.SetFutureShielded, LocalHandling = RpcLocalHandling.After)]
    public static void RpcSetFutureShielded(this PlayerControl player, byte playerId)
    {
        Medic.futureShielded = Helpers.playerById(playerId);
        Medic.usedShield = true;
    }

    // Shows the shielded murder attempt to the Medic and the shielded player
    [MethodRpc((uint)TorRpc.ShieldedMurderAttempt, LocalHandling = RpcLocalHandling.After)]
    public static void RpcShieldedMurderAttempt(this PlayerControl player)
    {
        if (Medic.shielded == null || Medic.medic == null) return;

        var isShieldedAndShow = Medic.shielded == PlayerControl.LocalPlayer && Medic.showAttemptToShielded;
        isShieldedAndShow =
            isShieldedAndShow &&
            (Medic.meetingAfterShielding ||
             !Medic.showShieldAfterMeeting); // Dont show attempt, if shield is not shown yet
        var isMedicAndShow = Medic.medic == PlayerControl.LocalPlayer && Medic.showAttemptToMedic;

        if (isShieldedAndShow || isMedicAndShow || Helpers.shouldShowGhostInfo())
            Helpers.showFlash(Palette.ImpostorRed, 0.5f, ModTranslation.GetString("Game-Normal", 3));
    }
}
