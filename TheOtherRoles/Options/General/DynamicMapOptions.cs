// Converted from CustomOptionHolder.cs by tools/Generate-Options.ps1; this file is now the source of truth.

using MiraAPI.GameOptions.OptionTypes;

namespace TheOtherRoles.Options;

public class DynamicMapOptions : TorGameOptionGroup
{
    public override uint GroupPriority => 50;
    public override string GroupName => TorOptions.Title("Opt-Heading,16");

    public ModdedToggleOption DynamicMap { get; } = new ModdedToggleOption("Opt-General,37", false);

    public ModdedStringOption EnableSkeld { get; } =
        new ModdedStringOption("Opt-General,100", TorOptions.Rates[0], TorOptions.Rates)
        {
            Visible = () => OptionGroupSingleton<DynamicMapOptions>.Instance.DynamicMap.Value
        };

    public ModdedStringOption EnableMira { get; } =
        new ModdedStringOption("Opt-General,101", TorOptions.Rates[0], TorOptions.Rates)
        {
            Visible = () => OptionGroupSingleton<DynamicMapOptions>.Instance.DynamicMap.Value
        };

    public ModdedStringOption EnablePolus { get; } =
        new ModdedStringOption("Opt-General,102", TorOptions.Rates[0], TorOptions.Rates)
        {
            Visible = () => OptionGroupSingleton<DynamicMapOptions>.Instance.DynamicMap.Value
        };

    public ModdedStringOption EnableAirShip { get; } =
        new ModdedStringOption("Opt-General,103", TorOptions.Rates[0], TorOptions.Rates)
        {
            Visible = () => OptionGroupSingleton<DynamicMapOptions>.Instance.DynamicMap.Value
        };

    public ModdedStringOption EnableFungle { get; } =
        new ModdedStringOption("Opt-General,104", TorOptions.Rates[0], TorOptions.Rates)
        {
            Visible = () => OptionGroupSingleton<DynamicMapOptions>.Instance.DynamicMap.Value
        };

    public ModdedStringOption EnableSubmerged { get; } =
        new ModdedStringOption("Opt-General,105", TorOptions.Rates[0], TorOptions.Rates)
        {
            Visible = () => OptionGroupSingleton<DynamicMapOptions>.Instance.DynamicMap.Value
        };
}
