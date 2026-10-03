// Converted from CustomOptionHolder.cs by tools/Generate-Options.ps1; this file is now the source of truth.

using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using UnityEngine;

namespace TheOtherRoles.Options;

public class BloodyOptions : TorModifierOptionGroup
{
    public override uint GroupPriority => 10;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Bloody));
    public override Color GroupColor => TorOptionColors.Group(Color.yellow);

    public ModdedStringOption Bloody { get; } = new ModdedStringOption(TorOptions.RoleKey(RoleId.Bloody), TorOptions.Rates[0], TorOptions.Rates);

    public ModdedStringOption BloodyQuantity { get; } =
        new ModdedStringOption("Opt-Bloody,1", TorOptions.Counts[0], TorOptions.Counts)
        {
            Visible = () => OptionGroupSingleton<BloodyOptions>.Instance.Bloody.Enabled()
        };

    public ModdedNumberOption BloodyDuration { get; } =
        new ModdedNumberOption("Opt-Bloody,2", 10f, 3f, 60f, 1f, MiraNumberSuffixes.None, "0.##")
        {
            Visible = () => OptionGroupSingleton<BloodyOptions>.Instance.Bloody.Enabled()
        };
}
