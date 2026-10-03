// Converted from CustomOptionHolder.cs by tools/Generate-Options.ps1; this file is now the source of truth.

using MiraAPI.GameOptions.OptionTypes;
using UnityEngine;

namespace TheOtherRoles.Options;

public class SunglassesOptions : TorModifierOptionGroup
{
    public override uint GroupPriority => 60;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Sunglasses));
    public override Color GroupColor => TorOptionColors.Group(Color.yellow);

    public ModdedStringOption Sunglasses { get; } = new ModdedStringOption(TorOptions.RoleKey(RoleId.Sunglasses), TorOptions.Rates[0], TorOptions.Rates);

    public ModdedStringOption SunglassesQuantity { get; } =
        new ModdedStringOption("Opt-Sunglasses,1", TorOptions.Counts[0], TorOptions.Counts)
        {
            Visible = () => OptionGroupSingleton<SunglassesOptions>.Instance.Sunglasses.Enabled()
        };

    public ModdedStringOption SunglassesVision { get; } =
        new ModdedStringOption("Opt-Sunglasses,2", "-10%", ["-10%", "-20%", "-30%", "-40%", "-50%"])
        {
            Visible = () => OptionGroupSingleton<SunglassesOptions>.Instance.Sunglasses.Enabled()
        };
}
