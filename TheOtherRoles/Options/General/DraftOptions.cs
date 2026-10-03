using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;

namespace TheOtherRoles.Options;

public class DraftOptions : TorGameOptionGroup
{
    public override uint GroupPriority => 60;
    public override string GroupName => TorOptions.Title("Opt-Heading,17");

    public ModdedToggleOption EnableDraft { get; } = new("Opt-General,38", false);

    public ModdedNumberOption OffersCount { get; } =
        new("Opt-General,39", 3f, 2f, 6f, 1f, MiraNumberSuffixes.None, "0")
        {
            Visible = draftOn
        };

    public ModdedNumberOption TurnSeconds { get; } =
        new("Opt-General,40", 5f, 3f, 20f, 1f, MiraNumberSuffixes.None, "0")
        {
            Visible = draftOn
        };

    public ModdedToggleOption ShowRandomCard { get; } = new("Opt-General,41", true)
    {
        Visible = draftOn
    };

    public ModdedToggleOption CanChat { get; } = new("Opt-General,42", false)
    {
        Visible = draftOn
    };

    public ModdedNumberOption CrewRolesMin { get; } =
        new("Opt-General,43", 0f, 0f, 15f, 1f, MiraNumberSuffixes.None, "0")
        {
            Visible = draftOn
        };

    public ModdedNumberOption CrewRolesMax { get; } =
        new("Opt-General,44", 15f, 0f, 15f, 1f, MiraNumberSuffixes.None, "0")
        {
            Visible = draftOn
        };

    public ModdedNumberOption ImpRolesMin { get; } =
        new("Opt-General,45", 1f, 0f, 3f, 1f, MiraNumberSuffixes.None, "0")
        {
            Visible = draftOn
        };

    public ModdedNumberOption ImpRolesMax { get; } =
        new("Opt-General,46", 3f, 0f, 3f, 1f, MiraNumberSuffixes.None, "0")
        {
            Visible = draftOn
        };

    public ModdedNumberOption NeutralRolesMin { get; } =
        new("Opt-General,47", 0f, 0f, 15f, 1f, MiraNumberSuffixes.None, "0")
        {
            Visible = draftOn
        };

    public ModdedNumberOption NeutralRolesMax { get; } =
        new("Opt-General,48", 15f, 0f, 15f, 1f, MiraNumberSuffixes.None, "0")
        {
            Visible = draftOn
        };

    private static bool draftOn()
    {
        var options = OptionGroupSingleton<DraftOptions>.Instance;
        return options != null && options.EnableDraft.Value;
    }
}
