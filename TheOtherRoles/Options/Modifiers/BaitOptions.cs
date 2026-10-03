// Converted from CustomOptionHolder.cs by tools/Generate-Options.ps1; this file is now the source of truth.

using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using UnityEngine;

namespace TheOtherRoles.Options;

public class BaitOptions : TorModifierOptionGroup
{
    public override uint GroupPriority => 40;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Bait));
    public override Color GroupColor => TorOptionColors.Group(Roles.Modifier.Bait.color);

    public ModdedStringOption Bait { get; } = new ModdedStringOption(TorOptions.RoleKey(RoleId.Bait), TorOptions.Rates[0], TorOptions.Rates);

    public ModdedStringOption BaitQuantity { get; } =
        new ModdedStringOption("Opt-Bait,1", TorOptions.Counts[0], TorOptions.Counts)
        {
            Visible = () => OptionGroupSingleton<BaitOptions>.Instance.Bait.Enabled()
        };

    public ModdedNumberOption BaitReportDelayMin { get; } =
        new ModdedNumberOption("Opt-Bait,2", 0f, 0f, 10f, 1f, MiraNumberSuffixes.None, "0.##")
        {
            Visible = () => OptionGroupSingleton<BaitOptions>.Instance.Bait.Enabled()
        };

    public ModdedNumberOption BaitReportDelayMax { get; } =
        new ModdedNumberOption("Opt-Bait,3", 0f, 0f, 10f, 1f, MiraNumberSuffixes.None, "0.##")
        {
            Visible = () => OptionGroupSingleton<BaitOptions>.Instance.Bait.Enabled()
        };

    public ModdedToggleOption BaitShowKillFlash { get; } =
        new ModdedToggleOption("Opt-Bait,4", true)
        {
            Visible = () => OptionGroupSingleton<BaitOptions>.Instance.Bait.Enabled()
        };
}
