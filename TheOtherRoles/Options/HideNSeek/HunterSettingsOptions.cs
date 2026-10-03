// Converted from CustomOptionHolder.cs by tools/Generate-Options.ps1; this file is now the source of truth.

using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using UnityEngine;

namespace TheOtherRoles.Options;

public class HunterSettingsOptions : TorHideNSeekOptionGroup
{
    public override uint GroupPriority => 10;
    public override string GroupName => TorOptions.Title("Opt-Heading,7");
    public override Color GroupColor => TorOptionColors.Group(Color.red);

    public ModdedNumberOption LightCooldown { get; } = new ModdedNumberOption("Opt-HideNSeek-Roles,1", 30f, 5f, 60f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption LightDuration { get; } = new ModdedNumberOption("Opt-HideNSeek-Roles,2", 5f, 1f, 60f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption LightVision { get; } = new ModdedNumberOption("Opt-HideNSeek-Roles,3", 3f, 1f, 5f, 0.25f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption LightPunish { get; } = new ModdedNumberOption("Opt-HideNSeek-Roles,4", 5f, 0f, 30f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption AdminCooldown { get; } = new ModdedNumberOption("Opt-HideNSeek-Roles,5", 30f, 5f, 60f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption AdminDuration { get; } = new ModdedNumberOption("Opt-HideNSeek-Roles,6", 5f, 1f, 60f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption AdminPunish { get; } = new ModdedNumberOption("Opt-HideNSeek-Roles,7", 5f, 0f, 30f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption ArrowCooldown { get; } = new ModdedNumberOption("Opt-HideNSeek-Roles,8", 30f, 5f, 60f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption ArrowDuration { get; } = new ModdedNumberOption("Opt-HideNSeek-Roles,9", 5f, 0f, 60f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption ArrowPunish { get; } = new ModdedNumberOption("Opt-HideNSeek-Roles,10", 5f, 0f, 30f, 1f, MiraNumberSuffixes.None, "0.##");
}
