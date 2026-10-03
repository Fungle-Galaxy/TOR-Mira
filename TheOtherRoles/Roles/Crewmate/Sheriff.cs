using System;
using MiraAPI.GameOptions.OptionTypes;
using TheOtherRoles.Buttons;
using TheOtherRoles.Networking;
using TheOtherRoles.Patches;
using UnityEngine;
using MiraAPI.Utilities;
using TheOtherRoles.Utilities;

namespace TheOtherRoles.Roles.Crewmate;

public class Sheriff(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = new Color32(248, 205, 70, byte.MaxValue);

    public static RoleInfo Info = new(color, RoleId.Sheriff);

    public static PlayerControl sheriff;
    public static float cooldown = 30f;
    public static bool canKillNeutrals;
    public static bool spyCanDieToSheriff;

    public static PlayerControl currentTarget;
    public static PlayerControl formerDeputy;
    public static PlayerControl formerSheriff;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Sheriff;

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<SheriffOptions>.Instance.SpawnRate;

    public static void replaceCurrentSheriff(PlayerControl deputy)
    {
        if (!formerSheriff) formerSheriff = sheriff;
        sheriff = deputy;
        currentTarget = null;
        cooldown = OptionGroupSingleton<SheriffOptions>.Instance.Cooldown.Value;
    }

    public static void clearAndReload()
    {
        sheriff = null;
        currentTarget = null;
        formerDeputy = null;
        formerSheriff = null;
        cooldown = OptionGroupSingleton<SheriffOptions>.Instance.Cooldown.Value;
        canKillNeutrals = OptionGroupSingleton<SheriffOptions>.Instance.CanKillNeutrals.Value;
        spyCanDieToSheriff = OptionGroupSingleton<SpyOptions>.Instance.CanDieToSheriff.Value;
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        if (Sheriff.sheriff == null || Sheriff.sheriff != player) return;
        Sheriff.currentTarget = PlayerControlFixedUpdatePatch.setTarget();
        PlayerControlFixedUpdatePatch.setPlayerOutline(Sheriff.currentTarget, Sheriff.color);
    }
}

/// <summary>
/// The settings of SheriffOptions. They live next to the role on purpose: the group is bound
/// to Sheriff, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class SheriffOptions : TorRoleOptionGroup<Sheriff>
{
    public override uint GroupPriority => 420;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Sheriff));
    public override Color GroupColor => TorOptionColors.Group(Sheriff.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.Sheriff), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedNumberOption Cooldown { get; } =
        new ModdedNumberOption("Opt-Sheriff,1", 30f, 10f, 60f, 2.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedToggleOption CanKillNeutrals { get; } =
        new ModdedToggleOption("Opt-Sheriff,2", false);

    // The Deputy's own page lives behind the cog on the Deputy's row; all this switch decides is
    // whether the seat exists at all. The Deputy's chance is the +/- spinner on that row.
    public ModdedToggleOption DeputyEnabled { get; } =
        new ModdedToggleOption("Opt-Sheriff,3", false);
}

/// <summary>
/// The Sheriff's kill button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>sheriffKillButton</c>.
/// </summary>
public sealed class SheriffKillButton : TorButton
{
    private static SheriffKillButton sheriffKillButton;

    public SheriffKillButton()
    {
        sheriffKillButton = this;

        PositionOffset = TorButtonPositions.UpperRowRight;
        Hotkey = KeyCode.Q;
        ShowButtonText = true;

        RealOnClick = () =>
        {
            var murderAttemptResult = Helpers.checkMuderAttempt(Sheriff.sheriff, Sheriff.currentTarget);
            if (murderAttemptResult == MurderAttemptResult.SuppressKill) return;

            if (murderAttemptResult == MurderAttemptResult.PerformKill)
            {
                byte targetId = 0;
                if ((Sheriff.currentTarget.Data.Role.IsImpostor &&
                     (Sheriff.currentTarget != Mini.mini || Mini.isGrownUp())) ||
                    (Sheriff.spyCanDieToSheriff && Spy.spy == Sheriff.currentTarget) ||
                    (Sheriff.canKillNeutrals && Helpers.isNeutral(Sheriff.currentTarget)) ||
                    Jackal.jackal == Sheriff.currentTarget || Sidekick.sidekick == Sheriff.currentTarget)
                    targetId = Sheriff.currentTarget.PlayerId;
                else
                    targetId = PlayerControl.LocalPlayer.PlayerId;

                // Armored sheriff shot doesnt kill if backfired
                if (targetId == Sheriff.sheriff.PlayerId && Helpers.checkArmored(Sheriff.sheriff, true, true))
                    return;
                PlayerControl.LocalPlayer.RpcUncheckedMurderPlayer(Sheriff.sheriff.Data.PlayerId, targetId,
                    byte.MaxValue);
            }

            sheriffKillButton.Timer = sheriffKillButton.MaxTimer;
            Sheriff.currentTarget = null;
        };
        HasButton = () =>
        {
            return Sheriff.sheriff != null && Sheriff.sheriff == PlayerControl.LocalPlayer &&
                   !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () => { return Sheriff.currentTarget && PlayerControl.LocalPlayer.CanMove; };
        OnMeetingEnds = () => { sheriffKillButton.Timer = sheriffKillButton.MaxTimer; };
    }

    public override float Cooldown => Sheriff.cooldown;

    public override void CreateButton(Transform parent)
    {
        // The old constructor took the kill button's sprite straight off the HUD.
        SetSprite(HudManager.Instance.KillButton.graphic.sprite);
        base.CreateButton(parent);
        ButtonText = new ButtonText(StringNames.KillLabel,
            FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.UseButton]
                .FontMaterial);
    }
}
