// Converted from CustomOptionHolder.cs by tools/Generate-Options.ps1; this file is now the source of truth.

using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using UnityEngine;

namespace TheOtherRoles.Options;

public class PropHuntInvisibilityOptions : TorPropHuntOptionGroup
{
    public override uint GroupPriority => 40;
    public override string GroupName => TorOptions.Title("Opt-PropHunt,18");
    public override Color GroupColor => TorOptionColors.Group(Palette.CrewmateBlue);

    public ModdedToggleOption InvisEnabled { get; } = new ModdedToggleOption("Opt-PropHunt,18", true);

    public ModdedNumberOption InvisCooldown { get; } =
        new ModdedNumberOption("Opt-PropHunt,19", 120f, 10f, 1800f, 2.5f, MiraNumberSuffixes.None, "0.##")
        {
            Visible = () => OptionGroupSingleton<PropHuntInvisibilityOptions>.Instance.InvisEnabled.Value
        };

    public ModdedNumberOption InvisDuration { get; } =
        new ModdedNumberOption("Opt-PropHunt,20", 5f, 1f, 30f, 1f, MiraNumberSuffixes.None, "0.##")
        {
            Visible = () => OptionGroupSingleton<PropHuntInvisibilityOptions>.Instance.InvisEnabled.Value
        };
}
