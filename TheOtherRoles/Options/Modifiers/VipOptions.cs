// Converted from CustomOptionHolder.cs by tools/Generate-Options.ps1; this file is now the source of truth.

using MiraAPI.GameOptions.OptionTypes;
using UnityEngine;

namespace TheOtherRoles.Options;

public class VipOptions : TorModifierOptionGroup
{
    public override uint GroupPriority => 80;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Vip));
    public override Color GroupColor => TorOptionColors.Group(Color.yellow);

    public ModdedStringOption Vip { get; } = new ModdedStringOption(TorOptions.RoleKey(RoleId.Vip), TorOptions.Rates[0], TorOptions.Rates);

    public ModdedStringOption VipQuantity { get; } =
        new ModdedStringOption("Opt-Vip,1", TorOptions.Counts[0], TorOptions.Counts)
        {
            Visible = () => OptionGroupSingleton<VipOptions>.Instance.Vip.Enabled()
        };

    public ModdedToggleOption VipShowColor { get; } =
        new ModdedToggleOption("Opt-Vip,2", true)
        {
            Visible = () => OptionGroupSingleton<VipOptions>.Instance.Vip.Enabled()
        };
}
