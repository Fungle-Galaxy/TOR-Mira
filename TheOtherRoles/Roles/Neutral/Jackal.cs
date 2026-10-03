using System;
using MiraAPI.GameOptions.OptionTypes;
using System.Collections.Generic;
using System.Linq;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheOtherRoles.Buttons;
using TheOtherRoles.Patches;
using UnityEngine;
using MiraAPI.Utilities;

namespace TheOtherRoles.Roles.Neutral;

public class Jackal(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = new Color32(0, 180, 235, byte.MaxValue);

    public static RoleInfo Info = new(color, RoleId.Jackal, isNeutral: true);

    public static PlayerControl jackal;
    public static PlayerControl fakeSidekick;
    public static PlayerControl currentTarget;
    public static List<PlayerControl> formerJackals = new();

    public static float cooldown = 30f;
    public static float createSidekickCooldown = 30f;
    public static bool canUseVents = true;
    public static bool canCreateSidekick = true;
    public static bool jackalPromotedFromSidekickCanCreateSidekick = true;
    public static bool canCreateSidekickFromImpostor = true;
    public static bool hasImpostorVision;
    public static bool wasTeamRed;
    public static bool wasImpostor;
    public static bool wasSpy;
    public static bool canSabotageLights;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Jackal;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<JackalOptions>.Instance.SpawnRate;

    public static void removeCurrentJackal()
    {
        if (!formerJackals.Any(x => x.PlayerId == jackal.PlayerId)) formerJackals.Add(jackal);
        jackal = null;
        currentTarget = null;
        fakeSidekick = null;
        cooldown = OptionGroupSingleton<JackalOptions>.Instance.KillCooldown.Value;
        createSidekickCooldown = OptionGroupSingleton<JackalOptions>.Instance.CreateSidekickCooldown.Value;
    }

    public static void clearAndReload()
    {
        jackal = null;
        currentTarget = null;
        fakeSidekick = null;
        cooldown = OptionGroupSingleton<JackalOptions>.Instance.KillCooldown.Value;
        createSidekickCooldown = OptionGroupSingleton<JackalOptions>.Instance.CreateSidekickCooldown.Value;
        canUseVents = OptionGroupSingleton<JackalOptions>.Instance.CanUseVents.Value;
        canCreateSidekick = OptionGroupSingleton<JackalOptions>.Instance.CanCreateSidekick.Value;
        jackalPromotedFromSidekickCanCreateSidekick =
            OptionGroupSingleton<JackalOptions>.Instance.PromotedFromSidekickCanCreateSidekick.Value;
        canCreateSidekickFromImpostor = OptionGroupSingleton<JackalOptions>.Instance.CanCreateSidekickFromImpostor.Value;
        formerJackals.Clear();
        hasImpostorVision = OptionGroupSingleton<JackalOptions>.Instance.AndSidekickHaveImpostorVision.Value;
        wasTeamRed = wasImpostor = wasSpy = false;
        canSabotageLights = OptionGroupSingleton<JackalOptions>.Instance.CanSabotageLights.Value;
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        if (Jackal.jackal == null || Jackal.jackal != player) return;
        var untargetablePlayers = new List<PlayerControl>();
        if (Jackal.canCreateSidekickFromImpostor)
            if (Sidekick.sidekick != null)
                untargetablePlayers.Add(Sidekick.sidekick);
        if (Mini.mini != null && !Mini.isGrownUp())
            untargetablePlayers.Add(Mini.mini);
        Jackal.currentTarget = PlayerControlFixedUpdatePatch.setTarget(untargetablePlayers: untargetablePlayers);
        PlayerControlFixedUpdatePatch.setPlayerOutline(Jackal.currentTarget, Palette.ImpostorRed);
    }
}

/// <summary>
/// The settings of JackalOptions. They live next to the role on purpose: the group is bound
/// to Jackal, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class JackalOptions : TorRoleOptionGroup<Jackal>
{
    public override uint GroupPriority => 330;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Jackal));
    public override Color GroupColor => TorOptionColors.Group(Jackal.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.Jackal), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedNumberOption KillCooldown { get; } =
        new ModdedNumberOption("Opt-Jackal,1", 30f, 10f, 60f, 2.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedToggleOption CanUseVents { get; } =
        new ModdedToggleOption("Opt-Jackal,3", true);

    public ModdedToggleOption CanSabotageLights { get; } =
        new ModdedToggleOption("Opt-Jackal,4", true);

    public ModdedToggleOption CanCreateSidekick { get; } =
        new ModdedToggleOption("Opt-Jackal,5", false);

    public ModdedNumberOption CreateSidekickCooldown { get; } =
        new ModdedNumberOption("Opt-Jackal,2", 30f, 10f, 60f, 2.5f, MiraNumberSuffixes.None, "0.##")
        {
            Visible = () => OptionGroupSingleton<JackalOptions>.Instance.CanCreateSidekick.Value
        };

    public ModdedToggleOption SidekickPromotesToJackal { get; } =
        new ModdedToggleOption("Opt-Jackal,6", false)
        {
            Visible = () => OptionGroupSingleton<JackalOptions>.Instance.CanCreateSidekick.Value
        };

    public ModdedToggleOption SidekickCanKill { get; } =
        new ModdedToggleOption("Opt-Jackal,7", false)
        {
            Visible = () => OptionGroupSingleton<JackalOptions>.Instance.CanCreateSidekick.Value
        };

    public ModdedToggleOption SidekickCanUseVents { get; } =
        new ModdedToggleOption("Opt-Jackal,8", true)
        {
            Visible = () => OptionGroupSingleton<JackalOptions>.Instance.CanCreateSidekick.Value
        };

    public ModdedToggleOption SidekickCanSabotageLights { get; } =
        new ModdedToggleOption("Opt-Jackal,9", true)
        {
            Visible = () => OptionGroupSingleton<JackalOptions>.Instance.CanCreateSidekick.Value
        };

    public ModdedToggleOption PromotedFromSidekickCanCreateSidekick { get; } =
        new ModdedToggleOption("Opt-Jackal,10", true)
        {
            Visible = () => OptionGroupSingleton<JackalOptions>.Instance.SidekickPromotesToJackal.Value
        };

    public ModdedToggleOption CanCreateSidekickFromImpostor { get; } =
        new ModdedToggleOption("Opt-Jackal,11", true)
        {
            Visible = () => OptionGroupSingleton<JackalOptions>.Instance.CanCreateSidekick.Value
        };

    public ModdedToggleOption AndSidekickHaveImpostorVision { get; } =
        new ModdedToggleOption("Opt-Jackal,12", false);
}

/// <summary>
/// The Jackal's "create sidekick" button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>jackalSidekickButton</c>.
/// </summary>
public sealed class JackalSidekickButton : TorButton
{
    private static JackalSidekickButton jackalSidekickButton;

    public JackalSidekickButton()
    {
        jackalSidekickButton = this;

        SetSprite(TorAssets.SidekickButton);
        PositionOffset = TorButtonPositions.LowerRowCenter;
        Hotkey = KeyCode.F;
        ShowButtonText = true;
        ButtonText = new ButtonText(16);

        RealOnClick = () =>
        {
            PlayerControl.LocalPlayer.RpcJackalCreatesSidekick(Jackal.currentTarget.PlayerId);
            SoundEffectsManager.play("jackalSidekick");
        };
        HasButton = () =>
        {
            return Jackal.canCreateSidekick && Jackal.jackal != null &&
                   Jackal.jackal == PlayerControl.LocalPlayer && !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () =>
        {
            return Jackal.canCreateSidekick && Jackal.currentTarget != null && PlayerControl.LocalPlayer.CanMove;
        };
        OnMeetingEnds = () => { jackalSidekickButton.Timer = jackalSidekickButton.MaxTimer; };
    }

    public override float Cooldown => Jackal.createSidekickCooldown;
}

/// <summary>
/// The Jackal's kill button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>jackalKillButton</c>.
/// </summary>
public sealed class JackalKillButton : TorButton
{
    private static JackalKillButton jackalKillButton;

    public JackalKillButton()
    {
        jackalKillButton = this;

        PositionOffset = TorButtonPositions.UpperRowRight;
        Hotkey = KeyCode.Q;
        ShowButtonText = true;
        ButtonText = new ButtonText(StringNames.KillLabel);

        RealOnClick = () =>
        {
            if (Helpers.checkMurderAttemptAndKill(Jackal.jackal, Jackal.currentTarget) ==
                MurderAttemptResult.SuppressKill) return;

            jackalKillButton.Timer = jackalKillButton.MaxTimer;
            Jackal.currentTarget = null;
        };
        HasButton = () =>
        {
            return Jackal.jackal != null && Jackal.jackal == PlayerControl.LocalPlayer &&
                   !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () => { return Jackal.currentTarget && PlayerControl.LocalPlayer.CanMove; };
        OnMeetingEnds = () => { jackalKillButton.Timer = jackalKillButton.MaxTimer; };
    }

    public override float Cooldown => Jackal.cooldown;

    public override void CreateButton(Transform parent)
    {
        // The old constructor took the kill button's sprite straight off the HUD.
        SetSprite(HudManager.Instance.KillButton.graphic.sprite);
        base.CreateButton(parent);
    }
}

/// <summary>
/// The Jackal's and Sidekick's sabotage lights button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>jackalAndSidekickSabotageLightsButton</c>.
/// </summary>
public sealed class JackalAndSidekickSabotageLightsButton : TorButton
{
    private static JackalAndSidekickSabotageLightsButton jackalAndSidekickSabotageLightsButton;

    public JackalAndSidekickSabotageLightsButton()
    {
        jackalAndSidekickSabotageLightsButton = this;

        SetSprite(TorAssets.LightsOutButton);
        PositionOffset = TorButtonPositions.UpperRowCenter;
        Hotkey = KeyCode.G;
        ShowButtonText = true;
        ButtonText = new ButtonText(StringNames.SabotageLabel);

        RealOnClick = () =>
        {
            ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Sabotage, (byte)SystemTypes.Electrical);
            SoundEffectsManager.play("ligherLight");
        };
        HasButton = () =>
        {
            if (Helpers.isFungle()) return false;
            return ((Jackal.jackal != null && Jackal.jackal == PlayerControl.LocalPlayer &&
                     Jackal.canSabotageLights) ||
                    (Sidekick.sidekick != null && Sidekick.sidekick == PlayerControl.LocalPlayer &&
                     Sidekick.canSabotageLights)) && !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () =>
        {
            if (Helpers.sabotageTimer() > jackalAndSidekickSabotageLightsButton.Timer || Helpers.sabotageActive())
                jackalAndSidekickSabotageLightsButton.Timer =
                    Helpers.sabotageTimer() + 5f; // this will give imps time to do another sabotage.
            return Helpers.canUseSabotage();
        };
        OnMeetingEnds = () => { jackalAndSidekickSabotageLightsButton.Timer = Helpers.sabotageTimer() + 5f; };
    }

    public override float Cooldown => 0f;
}

public static class JackalRpcs
{
    [MethodRpc((uint)TorRpc.JackalCreatesSidekick, LocalHandling = RpcLocalHandling.After)]
    public static void RpcJackalCreatesSidekick(this PlayerControl player, byte targetId)
    {
        RPCProcedure.jackalCreatesSidekick(targetId);
    }
}
