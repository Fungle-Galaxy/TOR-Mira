// Converted from CustomOptionHolder.cs by tools/Generate-Options.ps1; this file is now the source of truth.

using MiraAPI.GameOptions.OptionTypes;

namespace TheOtherRoles.Options;

public class CamerasOptions : TorGameOptionGroup
{
    public override uint GroupPriority => 40;
    public override string GroupName => TorOptions.Title("Opt-Heading,13");

    public ModdedToggleOption CamsNightVision { get; } = new ModdedToggleOption("Opt-General,35", false);

    public ModdedToggleOption CamsNoNightVisionIfImpVision { get; } =
        new ModdedToggleOption("Opt-General,36", false)
        {
            Visible = () => OptionGroupSingleton<CamerasOptions>.Instance.CamsNightVision.Value
        };
}
