// Converted from CustomOptionHolder.cs by tools/Generate-Options.ps1; this file is now the source of truth.

using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using UnityEngine;

namespace TheOtherRoles.Options;

public class InvertOptions : TorModifierOptionGroup
{
    public override uint GroupPriority => 90;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Invert));
    public override Color GroupColor => TorOptionColors.Group(Color.yellow);

    public ModdedStringOption Invert { get; } = new ModdedStringOption(TorOptions.RoleKey(RoleId.Invert), TorOptions.Rates[0], TorOptions.Rates);

    public ModdedStringOption InvertQuantity { get; } =
        new ModdedStringOption("Opt-Invert,1", TorOptions.Counts[0], TorOptions.Counts)
        {
            Visible = () => OptionGroupSingleton<InvertOptions>.Instance.Invert.Enabled()
        };

    public ModdedNumberOption InvertDuration { get; } =
        new ModdedNumberOption("Opt-Invert,2", 3f, 1f, 15f, 1f, MiraNumberSuffixes.None, "0.##")
        {
            Visible = () => OptionGroupSingleton<InvertOptions>.Instance.Invert.Enabled()
        };
}
