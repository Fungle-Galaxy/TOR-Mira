using System;
using MiraAPI.GameOptions.OptionTypes;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheOtherRoles.Buttons;
using TheOtherRoles.Utilities;
using UnityEngine;
using MiraAPI.Utilities;

namespace TheOtherRoles.Roles.Crewmate;

public class Engineer(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = new Color32(0, 40, 245, byte.MaxValue);

    public static RoleInfo Info = new(color, RoleId.Engineer);

    public static PlayerControl engineer;

    public static int remainingFixes = 1;
    public static bool highlightForImpostors = true;
    public static bool highlightForTeamJackal = true;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Engineer;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<EngineerOptions>.Instance.SpawnRate;

    public static void clearAndReload()
    {
        engineer = null;
        remainingFixes = Mathf.RoundToInt(OptionGroupSingleton<EngineerOptions>.Instance.NumberOfFixes.Value);
        highlightForImpostors = OptionGroupSingleton<EngineerOptions>.Instance.HighlightForImpostors.Value;
        highlightForTeamJackal = OptionGroupSingleton<EngineerOptions>.Instance.HighlightForTeamJackal.Value;
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        var jackalHighlight = Engineer.highlightForTeamJackal && (player == Jackal.jackal || player == Sidekick.sidekick);
        var impostorHighlight = Engineer.highlightForImpostors && player.Data.Role.IsImpostor;
        if ((jackalHighlight || impostorHighlight) && MapUtilities.CachedShipStatus?.AllVents != null)
            foreach (var vent in MapUtilities.CachedShipStatus.AllVents)
                try
                {
                    if (vent?.myRend?.material != null)
                    {
                        if (Engineer.engineer != null && Engineer.engineer.inVent)
                        {
                            vent.myRend.material.SetFloat("_Outline", 1f);
                            vent.myRend.material.SetColor("_OutlineColor", Engineer.color);
                        }
                        else if (vent.myRend.material.GetColor("_AddColor") != Color.red)
                        {
                            vent.myRend.material.SetFloat("_Outline", 0);
                        }
                    }
                }
                catch { }
    }
}

/// <summary>
/// The settings of EngineerOptions. They live next to the role on purpose: the group is bound
/// to Engineer, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class EngineerOptions : TorRoleOptionGroup<Engineer>
{
    public override uint GroupPriority => 410;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Engineer));
    public override Color GroupColor => TorOptionColors.Group(Engineer.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.Engineer), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedNumberOption NumberOfFixes { get; } =
        new ModdedNumberOption("Opt-Engineer,1", 1f, 1f, 3f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedToggleOption HighlightForImpostors { get; } =
        new ModdedToggleOption("Opt-Engineer,2", true);

    public ModdedToggleOption HighlightForTeamJackal { get; } =
        new ModdedToggleOption("Opt-Engineer,3", true);
}

/// <summary>
/// The Engineer's "fix sabotage" button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>engineerRepairButton</c>.
/// </summary>
public sealed class EngineerRepairButton : TorButton
{
    private static EngineerRepairButton engineerRepairButton;

    public EngineerRepairButton()
    {
        engineerRepairButton = this;

        SetSprite(TorAssets.RepairButton);
        PositionOffset = TorButtonPositions.UpperRowRight;
        Hotkey = KeyCode.F;
        ShowButtonText = true;

        RealOnClick = () =>
        {
            engineerRepairButton.Timer = 0f;
            PlayerControl.LocalPlayer.RpcEngineerUsedRepair();
            SoundEffectsManager.play("engineerRepair");
            foreach (var task in PlayerControl.LocalPlayer.myTasks.GetFastEnumerator())
                if (task.TaskType == TaskTypes.FixLights)
                {
                    PlayerControl.LocalPlayer.RpcEngineerFixLights();
                }
                else if (task.TaskType == TaskTypes.RestoreOxy)
                {
                    MapUtilities.CachedShipStatus.RpcRepairSystem(SystemTypes.LifeSupp, 0 | 64);
                    MapUtilities.CachedShipStatus.RpcRepairSystem(SystemTypes.LifeSupp, 1 | 64);
                }
                else if (task.TaskType == TaskTypes.ResetReactor)
                {
                    MapUtilities.CachedShipStatus.RpcRepairSystem(SystemTypes.Reactor, 16);
                }
                else if (task.TaskType == TaskTypes.ResetSeismic)
                {
                    MapUtilities.CachedShipStatus.RpcRepairSystem(SystemTypes.Laboratory, 16);
                }
                else if (task.TaskType == TaskTypes.FixComms)
                {
                    MapUtilities.CachedShipStatus.RpcRepairSystem(SystemTypes.Comms, 16 | 0);
                    MapUtilities.CachedShipStatus.RpcRepairSystem(SystemTypes.Comms, 16 | 1);
                }
                else if (task.TaskType == TaskTypes.StopCharles)
                {
                    MapUtilities.CachedShipStatus.RpcRepairSystem(SystemTypes.Reactor, 0 | 16);
                    MapUtilities.CachedShipStatus.RpcRepairSystem(SystemTypes.Reactor, 1 | 16);
                }
                else if (SubmergedCompatibility.IsSubmerged &&
                         task.TaskType == SubmergedCompatibility.RetrieveOxygenMask)
                {
                    PlayerControl.LocalPlayer.RpcEngineerFixSubmergedOxygen();
                }
        };
        HasButton = () =>
        {
            return Engineer.engineer != null && Engineer.engineer == PlayerControl.LocalPlayer &&
                   Engineer.remainingFixes > 0 && !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () =>
        {
            SetUses(Engineer.remainingFixes);
            var sabotageActive = false;
            foreach (var task in PlayerControl.LocalPlayer.myTasks.GetFastEnumerator())
                if (task.TaskType == TaskTypes.FixLights || task.TaskType == TaskTypes.RestoreOxy ||
                    task.TaskType == TaskTypes.ResetReactor || task.TaskType == TaskTypes.ResetSeismic ||
                    task.TaskType == TaskTypes.FixComms || task.TaskType == TaskTypes.StopCharles
                    || (SubmergedCompatibility.IsSubmerged &&
                        task.TaskType == SubmergedCompatibility.RetrieveOxygenMask))
                    sabotageActive = true;
            return sabotageActive && Engineer.remainingFixes > 0 && PlayerControl.LocalPlayer.CanMove;
        };
        OnMeetingEnds = () => { };
    }

    public override float Cooldown => 0f;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        ButtonText = new ButtonText(1,
            FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.UseButton]
                .FontMaterial);
    }
}

public static class EngineerRpcs
{
    [MethodRpc((uint)TorRpc.EngineerFixLights, LocalHandling = RpcLocalHandling.After)]
    public static void RpcEngineerFixLights(this PlayerControl player)
    {
        var switchSystem = MapUtilities.Systems[SystemTypes.Electrical].CastFast<SwitchSystem>();
        switchSystem.ActualSwitches = switchSystem.ExpectedSwitches;
    }

    [MethodRpc((uint)TorRpc.EngineerFixSubmergedOxygen, LocalHandling = RpcLocalHandling.After)]
    public static void RpcEngineerFixSubmergedOxygen(this PlayerControl player)
    {
        SubmergedCompatibility.RepairOxygen();
    }

    [MethodRpc((uint)TorRpc.EngineerUsedRepair, LocalHandling = RpcLocalHandling.After)]
    public static void RpcEngineerUsedRepair(this PlayerControl player)
    {
        Engineer.remainingFixes--;
        if (Helpers.shouldShowGhostInfo()) Helpers.showFlash(Engineer.color, 0.5f, ModTranslation.GetString("Game-Normal", 2));
    }
}
