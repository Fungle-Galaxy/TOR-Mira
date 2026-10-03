// Converted from CustomOptionHolder.cs by tools/Generate-Options.ps1; this file is now the source of truth.

using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using UnityEngine;

namespace TheOtherRoles.Options;

public class ChameleonOptions : TorModifierOptionGroup
{
    public override uint GroupPriority => 100;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Chameleon));
    public override Color GroupColor => TorOptionColors.Group(Color.yellow);

    public ModdedStringOption Chameleon { get; } = new ModdedStringOption(TorOptions.RoleKey(RoleId.Chameleon), TorOptions.Rates[0], TorOptions.Rates);

    public ModdedStringOption ChameleonQuantity { get; } =
        new ModdedStringOption("Opt-Chameleon,1", TorOptions.Counts[0], TorOptions.Counts)
        {
            Visible = () => OptionGroupSingleton<ChameleonOptions>.Instance.Chameleon.Enabled()
        };

    public ModdedNumberOption ChameleonHoldDuration { get; } =
        new ModdedNumberOption("Opt-Chameleon,2", 3f, 1f, 10f, 0.5f, MiraNumberSuffixes.None, "0.##")
        {
            Visible = () => OptionGroupSingleton<ChameleonOptions>.Instance.Chameleon.Enabled()
        };

    public ModdedNumberOption ChameleonFadeDuration { get; } =
        new ModdedNumberOption("Opt-Chameleon,3", 1f, 0.25f, 10f, 0.25f, MiraNumberSuffixes.None, "0.##")
        {
            Visible = () => OptionGroupSingleton<ChameleonOptions>.Instance.Chameleon.Enabled()
        };

    public ModdedStringOption ChameleonMinVisibility { get; } =
        new ModdedStringOption("Opt-Chameleon,4", "0%", ["0%", "10%", "20%", "30%", "40%", "50%"])
        {
            Visible = () => OptionGroupSingleton<ChameleonOptions>.Instance.Chameleon.Enabled()
        };
}
