// Converted from CustomOptionHolder.cs by tools/Generate-Options.ps1; this file is now the source of truth.

using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using UnityEngine;

namespace TheOtherRoles.Options;

public class GuesserTeamCountOptions : TorGuesserOptionGroup
{
    public override uint GroupPriority => 600;
    public override string GroupName => TorOptions.Title("Opt-Heading,4");
    public override Color GroupColor => TorOptionColors.Group(Guesser.color);

    public ModdedNumberOption CrewNumber { get; } = new ModdedNumberOption("Opt-Guessers-General,1", 15f, 0f, 15f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption NeutralNumber { get; } = new ModdedNumberOption("Opt-Guessers-General,2", 15f, 0f, 15f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption ImpNumber { get; } = new ModdedNumberOption("Opt-Guessers-General,3", 15f, 0f, 15f, 1f, MiraNumberSuffixes.None, "0.##");
}
