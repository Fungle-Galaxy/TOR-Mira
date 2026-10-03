// Converted from CustomOptionHolder.cs by tools/Generate-Options.ps1; this file is now the source of truth.

using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using UnityEngine;

namespace TheOtherRoles.Options;

public class PropHuntSpeedboostOptions : TorPropHuntOptionGroup
{
    public override uint GroupPriority => 50;
    public override string GroupName => TorOptions.Title("Opt-PropHunt,21");
    public override Color GroupColor => TorOptionColors.Group(Palette.CrewmateBlue);

    public ModdedToggleOption SpeedboostEnabled { get; } = new ModdedToggleOption("Opt-PropHunt,21", true);

    public ModdedNumberOption SpeedboostCooldown { get; } =
        new ModdedNumberOption("Opt-PropHunt,22", 60f, 2.5f, 1800f, 2.5f, MiraNumberSuffixes.None, "0.##")
        {
            Visible = () => OptionGroupSingleton<PropHuntSpeedboostOptions>.Instance.SpeedboostEnabled.Value
        };

    public ModdedNumberOption SpeedboostDuration { get; } =
        new ModdedNumberOption("Opt-PropHunt,23", 5f, 1f, 15f, 1f, MiraNumberSuffixes.None, "0.##")
        {
            Visible = () => OptionGroupSingleton<PropHuntSpeedboostOptions>.Instance.SpeedboostEnabled.Value
        };

    public ModdedNumberOption SpeedboostSpeed { get; } =
        new ModdedNumberOption("Opt-PropHunt,24", 2f, 1.25f, 5f, 0.25f, MiraNumberSuffixes.None, "0.##")
        {
            Visible = () => OptionGroupSingleton<PropHuntSpeedboostOptions>.Instance.SpeedboostEnabled.Value
        };
}
