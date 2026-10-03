// Converted from CustomOptionHolder.cs by tools/Generate-Options.ps1; this file is now the source of truth.

using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using UnityEngine;

namespace TheOtherRoles.Options;

public class PropHuntHunterOptions : TorPropHuntOptionGroup
{
    public override uint GroupPriority => 20;
    public override string GroupName => TorOptions.Title("Opt-Heading,10");
    public override Color GroupColor => TorOptionColors.Group(Color.red);

    public ModdedNumberOption NumberOfHunters { get; } = new ModdedNumberOption("Opt-HideNSeek-Main,2", 1f, 1f, 5f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption HunterInitialBlackoutTime { get; } = new ModdedNumberOption("Opt-PropHunt,8", 10f, 5f, 20f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption HunterMissCooldown { get; } = new ModdedNumberOption("Opt-PropHunt,9", 10f, 2.5f, 60f, 2.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption HunterHitCooldown { get; } = new ModdedNumberOption("Opt-PropHunt,10", 10f, 2.5f, 60f, 2.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption RevealCooldown { get; } = new ModdedNumberOption("Opt-PropHunt,11", 30f, 10f, 90f, 2.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption RevealDuration { get; } = new ModdedNumberOption("Opt-PropHunt,12", 5f, 1f, 60f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption RevealPunish { get; } = new ModdedNumberOption("Opt-PropHunt,13", 10f, 0f, 1800f, 5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption AdminCooldown { get; } = new ModdedNumberOption("Opt-HideNSeek-Roles,5", 30f, 2.5f, 1800f, 2.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption FindCooldown { get; } = new ModdedNumberOption("Opt-PropHunt,15", 60f, 2.5f, 1800f, 2.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption FindDuration { get; } = new ModdedNumberOption("Opt-PropHunt,16", 5f, 1f, 15f, 1f, MiraNumberSuffixes.None, "0.##");
}
