// Converted from CustomOptionHolder.cs by tools/Generate-Options.ps1; this file is now the source of truth.

using MiraAPI.GameOptions.OptionTypes;
using UnityEngine;

namespace TheOtherRoles.Options;

public class ModifierSettingsOptions : TorModifierOptionGroup
{
    public override uint GroupPriority => 0;
    public override string GroupName => TorOptions.Title("Opt-Heading,3");
    public override Color GroupColor => TorOptionColors.Group(Color.yellow);

    public ModdedToggleOption ModifiersAreHidden { get; } = new ModdedToggleOption("Opt-General,62", true);
}
