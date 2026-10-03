// Converted from CustomOptionHolder.cs by tools/Generate-Options.ps1; this file is now the source of truth.

using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using UnityEngine;

namespace TheOtherRoles.Options;

public class HuntedSettingsOptions : TorHideNSeekOptionGroup
{
    public override uint GroupPriority => 20;
    public override string GroupName => TorOptions.Title("Opt-Heading,8");
    public override Color GroupColor => TorOptionColors.Group(Color.gray);

    public ModdedNumberOption ShieldCooldown { get; } = new ModdedNumberOption("Opt-HideNSeek-Roles,11", 30f, 5f, 60f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption ShieldDuration { get; } = new ModdedNumberOption("Opt-HideNSeek-Roles,12", 5f, 1f, 60f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption ShieldRewindTime { get; } = new ModdedNumberOption("Opt-HideNSeek-Roles,13", 3f, 1f, 10f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption ShieldNumber { get; } = new ModdedNumberOption("Opt-HideNSeek-Roles,14", 3f, 1f, 15f, 1f, MiraNumberSuffixes.None, "0.##");
}
