using System;
using MiraAPI.GameOptions.OptionTypes;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheOtherRoles.Buttons;
using TheOtherRoles.Networking;
using UnityEngine;
using MiraAPI.Utilities;
using TheOtherRoles.Utilities;

namespace TheOtherRoles.Roles.Crewmate;

public class Mayor(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = new Color32(32, 77, 66, byte.MaxValue);
    public static RoleInfo Info = new(color, RoleId.Mayor);

    public static PlayerControl mayor;
    public static Minigame emergency;
    public static int remoteMeetingsLeft = 1;

    public static bool canSeeVoteColors;
    public static int tasksNeededToSeeVoteColors;
    public static bool meetingButton = true;
    public static int mayorChooseSingleVote;

    public static bool voteTwice = true;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Mayor;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<MayorOptions>.Instance.SpawnRate;
    
    public static void clearAndReload()
    {
        mayor = null;
        emergency = null;
        remoteMeetingsLeft = Mathf.RoundToInt(OptionGroupSingleton<MayorOptions>.Instance.MaxRemoteMeetings.Value);
        canSeeVoteColors = OptionGroupSingleton<MayorOptions>.Instance.CanSeeVoteColors.Value;
        tasksNeededToSeeVoteColors = (int)OptionGroupSingleton<MayorOptions>.Instance.TasksNeededToSeeVoteColors.Value;
        meetingButton = OptionGroupSingleton<MayorOptions>.Instance.MeetingButton.Value;
        mayorChooseSingleVote = OptionGroupSingleton<MayorOptions>.Instance.ChooseSingleVote.Selection();
        voteTwice = true;
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;
}

/// <summary>
/// The settings of MayorOptions. They live next to the role on purpose: the group is bound
/// to Mayor, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class MayorOptions : TorRoleOptionGroup<Mayor>
{
    public override uint GroupPriority => 400;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Mayor));
    public override Color GroupColor => TorOptionColors.Group(Mayor.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.Mayor), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedToggleOption CanSeeVoteColors { get; } =
        new ModdedToggleOption("Opt-Mayor,1", false);

    public ModdedNumberOption TasksNeededToSeeVoteColors { get; } =
        new ModdedNumberOption("Opt-Mayor,2", 5f, 0f, 20f, 1f, MiraNumberSuffixes.None, "0.##")
        {
            Visible = () => OptionGroupSingleton<MayorOptions>.Instance.CanSeeVoteColors.Value
        };

    public ModdedToggleOption MeetingButton { get; } =
        new ModdedToggleOption("Opt-Mayor,3", true);

    public ModdedNumberOption MaxRemoteMeetings { get; } =
        new ModdedNumberOption("Opt-Mayor,4", 1f, 1f, 5f, 1f, MiraNumberSuffixes.None, "0.##")
        {
            Visible = () => OptionGroupSingleton<MayorOptions>.Instance.MeetingButton.Value
        };

    public ModdedStringOption ChooseSingleVote { get; } =
        new ModdedStringOption("Opt-Mayor,5", "Opt-General,69", ["Opt-General,69", "Opt-Mayor,101", "Opt-Mayor,102"]);
}

/// <summary>
/// The Mayor's remote meeting button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>mayorMeetingButton</c>.
/// </summary>
public sealed class MayorMeetingButton : TorButton
{
    private static MayorMeetingButton mayorMeetingButton;

    public MayorMeetingButton()
    {
        mayorMeetingButton = this;

        SetSprite(TorAssets.EmergencyButton);
        PositionOffset = TorButtonPositions.LowerRowRight;
        Hotkey = KeyCode.F;
        EffectEnabled = true;
        ShowButtonText = true;

        RealOnClick = () =>
        {
            PlayerControl.LocalPlayer.NetTransform.Halt(); // Stop current movement 
            Mayor.remoteMeetingsLeft--;
            Helpers
                .handleVampireBiteOnBodyReport(); // Manually call Vampire handling, since the CmdReportDeadBody Prefix won't be called
            PlayerControl.LocalPlayer.RpcUncheckedCmdReportDeadBody(PlayerControl.LocalPlayer.PlayerId,
                byte.MaxValue);
            mayorMeetingButton.Timer = 1f;
        };
        HasButton = () =>
        {
            return Mayor.mayor != null && Mayor.mayor == PlayerControl.LocalPlayer &&
                   !PlayerControl.LocalPlayer.Data.IsDead && Mayor.meetingButton;
        };
        CouldUse = () =>
        {
            mayorMeetingButton.actionButton.OverrideText(new ButtonText(30).GetText().Replace("%REMAINTIME%", Mayor.remoteMeetingsLeft.ToString()));
            var sabotageActive = false;
            foreach (var task in PlayerControl.LocalPlayer.myTasks.GetFastEnumerator())
                if (task.TaskType == TaskTypes.FixLights || task.TaskType == TaskTypes.RestoreOxy ||
                    task.TaskType == TaskTypes.ResetReactor || task.TaskType == TaskTypes.ResetSeismic ||
                    task.TaskType == TaskTypes.FixComms || task.TaskType == TaskTypes.StopCharles
                    || (SubmergedCompatibility.IsSubmerged &&
                        task.TaskType == SubmergedCompatibility.RetrieveOxygenMask))
                    sabotageActive = true;
            return !sabotageActive && PlayerControl.LocalPlayer.CanMove && Mayor.remoteMeetingsLeft > 0;
        };
        OnMeetingEnds = () => { mayorMeetingButton.Timer = mayorMeetingButton.MaxTimer; };
    }

    public override float Cooldown => GameManager.Instance != null
        ? GameManager.Instance.LogicOptions.GetEmergencyCooldown()
        : 0f;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        ButtonText = new ButtonText(31,
            FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.UseButton]
                .FontMaterial);
    }
}

public static class MayorRpcs
{
    [MethodRpc((uint)TorRpc.MayorSetVoteTwice, LocalHandling = RpcLocalHandling.None)]
    public static void RpcMayorSetVoteTwice(this PlayerControl player, bool voteTwice)
    {
        Mayor.voteTwice = voteTwice;
    }
}
