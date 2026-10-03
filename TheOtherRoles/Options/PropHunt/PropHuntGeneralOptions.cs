// Converted from CustomOptionHolder.cs by tools/Generate-Options.ps1; this file is now the source of truth.

using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using UnityEngine;

namespace TheOtherRoles.Options;

public class PropHuntGeneralOptions : TorPropHuntOptionGroup
{
    public override uint GroupPriority => 10;
    public override string GroupName => TorOptions.Title("Opt-Heading,9");
    public override Color GroupColor => TorOptionColors.Group(Color.yellow);

    public ModdedNumberOption Timer { get; } = new ModdedNumberOption("Opt-HideNSeek-Main,9", 5f, 1f, 30f, 0.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption UnstuckCooldown { get; } = new ModdedNumberOption("Opt-PropHunt,3", 30f, 2.5f, 60f, 2.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption UnstuckDuration { get; } = new ModdedNumberOption("Opt-PropHunt,4", 2f, 1f, 60f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption PropHunterVision { get; } = new ModdedNumberOption("Opt-HideNSeek-Main,4", 0.5f, 0.25f, 2f, 0.25f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption PropVision { get; } = new ModdedNumberOption("Opt-PropHunt,6", 2f, 0.25f, 5f, 0.25f, MiraNumberSuffixes.None, "0.##");
}
