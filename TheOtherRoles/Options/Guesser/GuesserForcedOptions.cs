// Converted from CustomOptionHolder.cs by tools/Generate-Options.ps1; this file is now the source of truth.

using MiraAPI.GameOptions.OptionTypes;

namespace TheOtherRoles.Options;

public class GuesserForcedOptions : TorGuesserOptionGroup
{
    public override uint GroupPriority => 610;
    public override string GroupName => TorOptions.Title("Opt-Heading,5");

    public ModdedToggleOption ForceJackalGuesser { get; } = new ModdedToggleOption("Opt-Guessers-General,4", false);

    public ModdedToggleOption GamemodeSidekickIsAlwaysGuesser { get; } = new ModdedToggleOption("Opt-Guessers-General,5", false);

    public ModdedToggleOption ForceThiefGuesser { get; } = new ModdedToggleOption("Opt-Guessers-General,6", false);
}
