// Converted from CustomOptionHolder.cs by tools/Generate-Options.ps1; this file is now the source of truth.

using MiraAPI.GameOptions.OptionTypes;
using UnityEngine;

namespace TheOtherRoles.Options;

public class PropHuntPropOptions : TorPropHuntOptionGroup
{
    public override uint GroupPriority => 30;
    public override string GroupName => TorOptions.Title("Opt-Heading,11");
    public override Color GroupColor => TorOptionColors.Group(Palette.CrewmateBlue);

    public ModdedToggleOption BecomesHunterWhenFound { get; } = new ModdedToggleOption("Opt-PropHunt,17", false);
}
