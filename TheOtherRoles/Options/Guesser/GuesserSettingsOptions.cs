// Converted from CustomOptionHolder.cs by tools/Generate-Options.ps1; this file is now the source of truth.

using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;

namespace TheOtherRoles.Options;

public class GuesserSettingsOptions : TorGuesserOptionGroup
{
    public override uint GroupPriority => 620;
    public override string GroupName => TorOptions.Title("Opt-Heading,6");

    public ModdedToggleOption HaveModifier { get; } = new ModdedToggleOption("Opt-Guessers-General,7", true);

    public ModdedNumberOption NumberOfShots { get; } = new ModdedNumberOption("Opt-Guesser,2", 3f, 1f, 15f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedToggleOption HasMultipleShotsPerMeeting { get; } = new ModdedToggleOption("Opt-Guesser,3", false);

    public ModdedNumberOption CrewGuesserNumberOfTasks { get; } = new ModdedNumberOption("Opt-Guessers-General,10", 0f, 0f, 15f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedToggleOption KillsThroughShield { get; } = new ModdedToggleOption("Opt-Guesser,4", true);

    public ModdedToggleOption EvilCanKillSpy { get; } = new ModdedToggleOption("Opt-Guesser,5", true);

    public ModdedToggleOption CantGuessSnitchIfTaksDone { get; } = new ModdedToggleOption("Opt-Guesser,7", true);
}
