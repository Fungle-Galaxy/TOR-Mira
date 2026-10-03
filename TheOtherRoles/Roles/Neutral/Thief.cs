using System;
using MiraAPI.GameOptions.OptionTypes;
using System.Collections.Generic;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheOtherRoles.Buttons;
using TheOtherRoles.Networking;
using TheOtherRoles.Patches;
using UnityEngine;
using MiraAPI.Utilities;

namespace TheOtherRoles.Roles.Neutral;

public class Thief(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = new Color32(71, 99, 45, byte.MaxValue);

    public static RoleInfo Info = new(color, RoleId.Thief, isNeutral: true);

    public static PlayerControl thief;
    public static PlayerControl currentTarget;
    public static PlayerControl formerThief;

    public static float cooldown = 30f;

    public static bool suicideFlag; // Used as a flag for suicide

    public static bool hasImpostorVision;
    public static bool canUseVents;
    public static bool canKillSheriff;
    public static bool canStealWithGuess;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Thief;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<ThiefOptions>.Instance.SpawnRate;

    public static void clearAndReload()
    {
        thief = null;
        suicideFlag = false;
        currentTarget = null;
        formerThief = null;
        hasImpostorVision = OptionGroupSingleton<ThiefOptions>.Instance.HasImpVision.Value;
        cooldown = OptionGroupSingleton<ThiefOptions>.Instance.Cooldown.Value;
        canUseVents = OptionGroupSingleton<ThiefOptions>.Instance.CanUseVents.Value;
        canKillSheriff = OptionGroupSingleton<ThiefOptions>.Instance.CanKillSheriff.Value;
        canStealWithGuess = OptionGroupSingleton<ThiefOptions>.Instance.CanStealWithGuess.Value;
    }

    public static bool isFailedThiefKill(PlayerControl target, PlayerControl killer, RoleInfo targetRole)
    {
        return killer == thief && !target.Data.Role.IsImpostor && !new List<RoleInfo>
            { Jackal.Info, canKillSheriff ? Sheriff.Info : null, Sidekick.Info }.Contains(targetRole);
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        if (Thief.thief == null || Thief.thief != player) return;
        var untargetables = new List<PlayerControl>();
        if (Mini.mini != null && !Mini.isGrownUp()) untargetables.Add(Mini.mini);
        Thief.currentTarget = PlayerControlFixedUpdatePatch.setTarget(untargetablePlayers: untargetables);
        PlayerControlFixedUpdatePatch.setPlayerOutline(Thief.currentTarget, Thief.color);
    }
}

/// <summary>
/// The settings of ThiefOptions. They live next to the role on purpose: the group is bound
/// to Thief, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class ThiefOptions : TorRoleOptionGroup<Thief>
{
    public override uint GroupPriority => 360;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Thief));
    public override Color GroupColor => TorOptionColors.Group(Thief.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.Thief), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedNumberOption Cooldown { get; } =
        new ModdedNumberOption("Opt-Thief,1", 30f, 5f, 120f, 5f, MiraNumberSuffixes.None, "0.##");

    public ModdedToggleOption CanKillSheriff { get; } =
        new ModdedToggleOption("Opt-Thief,2", true);

    public ModdedToggleOption HasImpVision { get; } =
        new ModdedToggleOption("Opt-Thief,3", true);

    public ModdedToggleOption CanUseVents { get; } =
        new ModdedToggleOption("Opt-Thief,4", true);

    public ModdedToggleOption CanStealWithGuess { get; } =
        new ModdedToggleOption("Opt-Thief,5", false);
}

/// <summary>
/// The Thief's kill button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>thiefKillButton</c>.
/// </summary>
public sealed class ThiefKillButton : TorButton
{
    private static ThiefKillButton thiefKillButton;

    public ThiefKillButton()
    {
        thiefKillButton = this;

        PositionOffset = TorButtonPositions.UpperRowRight;
        Hotkey = KeyCode.Q;
        ShowButtonText = true;
        ButtonText = new ButtonText(StringNames.KillLabel);

        RealOnClick = () =>
        {
            var thief = Thief.thief;
            var target = Thief.currentTarget;
            var result = Helpers.checkMuderAttempt(thief, target);
            if (result == MurderAttemptResult.BlankKill)
            {
                thiefKillButton.Timer = thiefKillButton.MaxTimer;
                return;
            }

            if (Thief.suicideFlag)
            {
                // Suicide
                PlayerControl.LocalPlayer.RpcUncheckedMurderPlayer(thief.PlayerId, thief.PlayerId, 0);
                Thief.thief.clearAllTasks();
            }

            // Steal role if survived.
            if (!Thief.thief.Data.IsDead && result == MurderAttemptResult.PerformKill)
            {
                PlayerControl.LocalPlayer.RpcThiefStealsRole(target.PlayerId);
            }

            // Kill the victim (after becoming their role - so that no win is triggered for other teams)
            if (result == MurderAttemptResult.PerformKill)
            {
                PlayerControl.LocalPlayer.RpcUncheckedMurderPlayer(thief.PlayerId, target.PlayerId, byte.MaxValue);
            }
        };
        HasButton = () =>
        {
            return Thief.thief != null && PlayerControl.LocalPlayer == Thief.thief &&
                   !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () => { return Thief.currentTarget != null && PlayerControl.LocalPlayer.CanMove; };
        OnMeetingEnds = () => { thiefKillButton.Timer = thiefKillButton.MaxTimer; };
    }

    public override float Cooldown => Thief.cooldown;

    public override void CreateButton(Transform parent)
    {
        // The old constructor took the kill button's sprite straight off the HUD.
        SetSprite(HudManager.Instance.KillButton.graphic.sprite);
        base.CreateButton(parent);
    }
}

public static class ThiefRpcs
{
    [MethodRpc((uint)TorRpc.ThiefStealsRole, LocalHandling = RpcLocalHandling.After)]
    public static void RpcThiefStealsRole(this PlayerControl player, byte playerId)
    {
        RPCProcedure.thiefStealsRole(playerId);
    }
}
