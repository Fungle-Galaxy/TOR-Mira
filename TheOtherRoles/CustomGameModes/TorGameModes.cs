using MiraAPI.GameModes;
using MiraAPI.Hud;
using MiraAPI.Utilities.Assets;
using UnityEngine;

namespace TheOtherRoles.CustomGameModes;

// TOR's three custom game modes. They are picked up automatically by Mira API's plugin loader,
// which scans this assembly for concrete AbstractGameMode subclasses and registers them in the
// vanilla "Gamemode" dropdown of the settings menu (and syncs the selection to every client).
//
// Classic is intentionally NOT declared here: Mira API registers its own ClassicMode as id 0 and
// the option list cannot be replaced, so a second classic entry would show up twice in the dropdown.
// Mira API's ClassicMode has no hooks of its own, which is exactly what TOR wants - all classic
// behaviour lives in TOR's own patches.
//
// These classes are also where mode specific rules live now: Mira API invokes the AbstractGameMode
// hooks from its own patches, so anything that used to be a TOR prefix / postfix reading
// "which mode is active" and then deciding something belongs in here as an override instead.

/// <summary>Classic mode with the guesser team force-enabled.</summary>
public class TorGuesserMode : ClassicMode
{
    public override string Name => "GameOptions-Text,1";
    public override string Description => "GameOptions-Text,2";
    public override Color Color { get; } = new Color32(255, 210, 71, 255);

    // Mira API only lays the description out next to an icon (and left aligns + wraps it) when the
    // mode provides one, so without this the Guesser description would be centred and clipped.
    public override LoadableAsset<Sprite> Icon => TorAssets.TargetIcon;
}

/// <summary>
/// Hide N Seek. Deliberately derives from <see cref="AbstractGameMode"/> instead of Mira API's own
/// <c>HideAndSeekMode</c>: that class is a full re-implementation of vanilla HnS (role assignment,
/// intro cutscene, game end, hidden game settings) which would fight TOR's own Hunter/Hunted logic.
/// </summary>
public class TorHideNSeekMode : AbstractGameMode
{
    public override string Name => "GameOptions-Text,3";
    public override string Description => "GameOptions-Text,4";
    public override Color Color { get; } = new Color32(255, 88, 90, 255);
    public override LoadableAsset<Sprite> Icon => TorAssets.HideNSeekArrowButton;

    /// <summary>
    /// Was <c>ShowSabotageMapPatch</c>: hunters may only pull up the sabotage overlay while the
    /// mode option allows sabotages. Mira API calls this from a prefix on
    /// <see cref="MapBehaviour.ShowSabotageMap"/> and swaps to the normal map when it returns false,
    /// which is what the old TOR prefix expressed with its own return value.
    /// </summary>
    public override bool ShouldShowSabotageMap(MapBehaviour map) => HideNSeek.canSabotage;
}

/// <summary>Prop Hunt.</summary>
public class TorPropHuntMode : AbstractGameMode
{
    public override string Name => "GameOptions-Text,5";
    public override string Description => "GameOptions-Text,6";
    public override Color Color { get; } = new Color32(116, 221, 136, 255);
    public override LoadableAsset<Sprite> Icon => TorAssets.FindButton;

    /// <summary>Was <c>ShowSabotageMapPatch</c>: props never get the sabotage overlay.</summary>
    public override bool ShouldShowSabotageMap(MapBehaviour map) => false;

    /// <summary>
    /// Was <c>PropHuntEvents.PropHuntMapConsoleCanUse</c> (a postfix on <c>MapConsole.CanUse</c>):
    /// only the hunter has to wait for the admin button to come off cooldown, and props keep the
    /// vanilla console. Mira API routes both <c>MapConsole.CanUse</c> and <c>MapConsole.Use</c>
    /// through this hook and turns <c>false</c> into <c>canUse = couldUse = false</c>, which is
    /// exactly what the old postfix produced.
    /// </summary>
    public override bool CanUseMapConsole(MapConsole console)
    {
        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || !localPlayer.Data.Role.IsImpostor) return true;
        return CustomButtonSingleton<PropHuntAdminButton>.Instance.Timer <= 0f;
    }
}
