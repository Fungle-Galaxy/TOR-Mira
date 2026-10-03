// Converted from CustomOptionHolder.cs by tools/Generate-Options.ps1; this file is now the source of truth.

using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;

namespace TheOtherRoles.Options;

public class GameSettingsOptions : TorGameOptionGroup
{
    public override uint GroupPriority => 30;
    public override string GroupName => TorOptions.Title("Opt-Heading,12");

    public ModdedNumberOption MaxNumberOfMeetings { get; } = new ModdedNumberOption("Opt-General,26", 10f, 0f, 15f, 1f, MiraNumberSuffixes.None, "0.##");
    
    public ModdedToggleOption AnyPlayerCanStopStart { get; } = new ModdedToggleOption("Opt-General,27", false);

    public ModdedToggleOption BlockSkippingInEmergencyMeetings { get; } = new ModdedToggleOption("Opt-General,28", false);

    public ModdedToggleOption NoVoteIsSelfVote { get; } =
        new ModdedToggleOption("Opt-General,29", false)
        {
            Visible = () => OptionGroupSingleton<GameSettingsOptions>.Instance.BlockSkippingInEmergencyMeetings.Value
        };

    public ModdedToggleOption HidePlayerNames { get; } = new ModdedToggleOption("Opt-General,30", false);

    public ModdedToggleOption AllowParallelMedBayScans { get; } = new ModdedToggleOption("Opt-General,31", false);

    public ModdedToggleOption ShieldFirstKill { get; } = new ModdedToggleOption("Opt-General,32", false);

    public ModdedToggleOption FinishTasksBeforeHauntingOrZoomingOut { get; } = new ModdedToggleOption("Opt-General,33", true);

    public ModdedToggleOption DeadImpsBlockSabotage { get; } = new ModdedToggleOption("Opt-General,34", false);
}
