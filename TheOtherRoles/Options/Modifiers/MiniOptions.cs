// Converted from CustomOptionHolder.cs by tools/Generate-Options.ps1; this file is now the source of truth.

using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using TheOtherRoles.Utilities;
using UnityEngine;

namespace TheOtherRoles.Options;

public class MiniOptions : TorModifierOptionGroup
{
    public override uint GroupPriority => 70;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Mini));
    public override Color GroupColor => TorOptionColors.Group(Color.yellow);

    public ModdedStringOption Mini { get; } = new ModdedStringOption(TorOptions.RoleKey(RoleId.Mini), TorOptions.Rates[0], TorOptions.Rates);

    public ModdedNumberOption MiniGrowingUpDuration { get; } =
        new ModdedNumberOption("Opt-Mini,1", 400f, 100f, 1500f, 100f, MiraNumberSuffixes.None, "0.##")
        {
            Visible = () => OptionGroupSingleton<MiniOptions>.Instance.Mini.Enabled()
        };

    public ModdedToggleOption MiniGrowingUpInMeeting { get; } =
        new ModdedToggleOption("Opt-Mini,2", true)
        {
            Visible = () => OptionGroupSingleton<MiniOptions>.Instance.Mini.Enabled()
        };

    public ModdedNumberOption EventKicksPerRound { get; } =
        new ModdedNumberOption("Opt-Mini,3", 4f, 0f, 14f, 1f, MiraNumberSuffixes.None, "0.##")
        {
            Visible = () => EventUtility.canBeEnabled || EventUtility.isEnabled
        };

    public ModdedNumberOption EventHeavyAge { get; } =
        new ModdedNumberOption("Opt-Mini,4", 12f, 6f, 18f, 0.5f, MiraNumberSuffixes.None, "0.##")
        {
            Visible = () => EventUtility.canBeEnabled || EventUtility.isEnabled
        };

    public ModdedToggleOption EventReallyNoMini { get; } =
        new ModdedToggleOption("Opt-Mini,5", false)
        {
            Visible = () => (EventUtility.canBeEnabled || EventUtility.isEnabled) && !OptionGroupSingleton<MiniOptions>.Instance.Mini.Enabled()
        };
}
