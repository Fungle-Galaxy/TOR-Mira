// Converted from CustomOptionHolder.cs by tools/Generate-Options.ps1; this file is now the source of truth.

using MiraAPI.GameOptions.OptionTypes;
using UnityEngine;

namespace TheOtherRoles.Options;

public class ShifterOptions : TorModifierOptionGroup
{
    public override uint GroupPriority => 120;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Shifter));
    public override Color GroupColor => TorOptionColors.Group(Color.yellow);

    public ModdedStringOption Shifter { get; } = new ModdedStringOption(TorOptions.RoleKey(RoleId.Shifter), TorOptions.Rates[0], TorOptions.Rates);

    public ModdedToggleOption ShifterShiftsMedicShield { get; } =
        new ModdedToggleOption("Opt-Shifter,1", false)
        {
            Visible = () => OptionGroupSingleton<ShifterOptions>.Instance.Shifter.Enabled()
        };
}
