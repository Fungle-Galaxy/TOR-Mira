using BepInEx.Configuration;
using MiraAPI.LocalSettings;
using MiraAPI.LocalSettings.Attributes;
using MiraAPI.Utilities.Assets;

namespace TheOtherRoles;

/// <summary>
/// TOR's client side preferences, exposed as a tab of the vanilla Options menu through Mira API's
/// local settings interface - the same shape Town of Us uses (one tab, one <c>[LocalToggleSetting]</c>
/// property per entry) instead of the old hand drawn "Mod Options..." popup.
///
/// The tab owns the config binds so Mira API can construct it as soon as the plugin is chained in;
/// <see cref="TheOtherRolesPlugin.Load"/> therefore binds with <c>??=</c> and keeps whichever
/// instance was created first. Section, key and default are unchanged from the old popup, so
/// existing cfg files keep working.
///
/// All nine entries live in the same <c>Custom</c> section, so the category label would only be
/// noise - <see cref="ShouldCreateLabels"/> turns it off.
/// </summary>
public class TorLocalSettings(ConfigFile config) : LocalSettingsTab(config)
{
    /// <inheritdoc />
    public override string TabName => "ClientOptions-Button-Text,1";

    /// <inheritdoc />
    protected override bool ShouldCreateLabels => false;

    /// <inheritdoc />
    public override LocalSettingTabAppearance TabAppearance => new()
    {
        TabIcon = MiraAssets.SettingsIcon,
        HideIconOnHover = false,
    };

    /// <inheritdoc />
    public override void OnOptionChanged(ConfigEntryBase configEntry)
    {
        base.OnOptionChanged(configEntry);

        TORMapOptions.reloadPluginOptions();

        // Turning the sound effects off mid game has to cut anything that is still looping.
        if (!TORMapOptions.enableSoundEffects)
        {
            SoundEffectsManager.stopAll();
        }
    }

    /// <summary>Whether ghosts see the remaining tasks and the other round information.</summary>
    [LocalToggleSetting(name: "ClientOptions-Text,1")]
    public ConfigEntry<bool> GhostsSeeInformation { get; private set; } =
        TheOtherRolesPlugin.GhostsSeeInformation ??=
            config.Bind("Custom", "Ghosts See Remaining Tasks", true);

    /// <summary>Whether ghosts can see the vote recaps.</summary>
    [LocalToggleSetting(name: "ClientOptions-Text,2")]
    public ConfigEntry<bool> GhostsSeeVotes { get; private set; } =
        TheOtherRolesPlugin.GhostsSeeVotes ??= config.Bind("Custom", "Ghosts See Votes", true);

    /// <summary>Whether ghosts can see the roles of the other players.</summary>
    [LocalToggleSetting(name: "ClientOptions-Text,3")]
    public ConfigEntry<bool> GhostsSeeRoles { get; private set; } =
        TheOtherRolesPlugin.GhostsSeeRoles ??= config.Bind("Custom", "Ghosts See Roles", true);

    /// <summary>Whether ghosts can additionally see modifiers.</summary>
    [LocalToggleSetting(name: "ClientOptions-Text,4")]
    public ConfigEntry<bool> GhostsSeeModifier { get; private set; } =
        TheOtherRolesPlugin.GhostsSeeModifier ??= config.Bind("Custom", "Ghosts See Modifier", true);

    /// <summary>Whether the role summary is shown on the meeting / intro screen.</summary>
    [LocalToggleSetting(name: "ClientOptions-Text,5")]
    public ConfigEntry<bool> ShowRoleSummary { get; private set; } =
        TheOtherRolesPlugin.ShowRoleSummary ??= config.Bind("Custom", "Show Role Summary", true);

    /// <summary>Whether the lighter / darker colour hint is shown.</summary>
    [LocalToggleSetting(name: "ClientOptions-Text,6")]
    public ConfigEntry<bool> ShowLighterDarker { get; private set; } =
        TheOtherRolesPlugin.ShowLighterDarker ??= config.Bind("Custom", "Show Lighter / Darker", true);

    /// <summary>Whether TOR's own sound effects are played at all.</summary>
    [LocalToggleSetting(name: "ClientOptions-Text,7")]
    public ConfigEntry<bool> EnableSoundEffects { get; private set; } =
        TheOtherRolesPlugin.EnableSoundEffects ??= config.Bind("Custom", "Enable Sound Effects", true);

    /// <summary>Whether vents are drawn on the minimap.</summary>
    [LocalToggleSetting(name: "ClientOptions-Text,8")]
    public ConfigEntry<bool> ShowVentsOnMap { get; private set; } =
        TheOtherRolesPlugin.ShowVentsOnMap ??= config.Bind("Custom", "Show vent positions on minimap", false);

    /// <summary>Whether chat notifications (command feedback, ...) are printed.</summary>
    [LocalToggleSetting(name: "ClientOptions-Text,9")]
    public ConfigEntry<bool> ShowChatNotifications { get; private set; } =
        TheOtherRolesPlugin.ShowChatNotifications ??=
            config.Bind("Custom", "Show Chat Notifications", true);
}
