// Converted from CustomOptionHolder.cs by tools/Generate-Options.ps1; this file is now the source of truth.

using MiraAPI.GameOptions.OptionTypes;
using UnityEngine;

namespace TheOtherRoles.Options;

public class LoversOptions : TorModifierOptionGroup
{
    public override uint GroupPriority => 50;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Lover));
    public override Color GroupColor => TorOptionColors.Group(Lovers.color);

    public ModdedStringOption Lover { get; } = new ModdedStringOption(TorOptions.RoleKey(RoleId.Lover), TorOptions.Rates[0], TorOptions.Rates);

    public ModdedStringOption LoverImpLoverRate { get; } =
        new ModdedStringOption("Opt-Lovers,1", TorOptions.Rates[0], TorOptions.Rates)
        {
            Visible = () => OptionGroupSingleton<LoversOptions>.Instance.Lover.Enabled()
        };

    public ModdedToggleOption LoverBothDie { get; } =
        new ModdedToggleOption("Opt-Lovers,2", true)
        {
            Visible = () => OptionGroupSingleton<LoversOptions>.Instance.Lover.Enabled()
        };

    public ModdedToggleOption LoverEnableChat { get; } =
        new ModdedToggleOption("Opt-Lovers,3", true)
        {
            Visible = () => OptionGroupSingleton<LoversOptions>.Instance.Lover.Enabled()
        };
}
