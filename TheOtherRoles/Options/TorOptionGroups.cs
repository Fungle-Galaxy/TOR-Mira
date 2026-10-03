using System;
using MiraAPI.GameModes;
using MiraAPI.PluginLoading;
using MiraAPI.Roles;
using TheOtherRoles.CustomGameModes;

namespace TheOtherRoles.Options;

/// <summary>
/// Gamemode driven visibility rules shared by every TOR option group. They mirror the tab list the
/// old lobby settings page built: classic / guesser gamemodes show the game and modifier groups,
/// Hide N Seek and Prop Hunt have their own option groups which are bound to the mode itself and
/// therefore show up under the "Gamemode" dropdown on the vanilla Game page.
/// </summary>
public static class TorOptionVisibility
{
    public static bool ClassicOrGuesser() => CustomGameModeManager.IsClassic();

    public static bool Guesser() => CustomGameModeManager.IsActiveGameMode<TorGuesserMode>();

    public static bool HideNSeek() => CustomGameModeManager.IsActiveGameMode<TorHideNSeekMode>();

    public static bool PropHunt() => CustomGameModeManager.IsActiveGameMode<TorPropHuntMode>();
}

/// <summary>Groups that live on the vanilla "Game" tab (general, impostor, neutral, crew and guesser settings).</summary>
[MiraIgnore] // Mira API scans for AbstractOptionGroup subclasses and would try to instantiate these bases.
public abstract class TorGameOptionGroup : AbstractOptionGroup
{
    public override MenuCategory ParentMenu => MenuCategory.Game;
    public override Func<bool> GroupVisible => TorOptionVisibility.ClassicOrGuesser;
}

/// <summary>
/// The home of a role's own settings. Deriving from Mira API's <see cref="AbstractRoleOptionGroup{T}"/>
/// binds the group to its role type, which does two things at once:
/// it sets <see cref="AbstractOptionGroup.ParentMenu"/> to <see cref="MenuCategory.Roles"/> so the group
/// disappears from the mod's own Game tab, and it makes Mira API render it as the advanced panel behind
/// the cog on that role's row on the Role Settings page - exactly where TownOfUs-Mira puts its role
/// options. The role's Spawn Rate is part of this group too, so the whole role is configured in one place.
/// </summary>
[MiraIgnore] // abstract: Mira API's plugin scan (the one for regular mods) does not skip abstract types.
public abstract class TorRoleOptionGroup<T> : AbstractRoleOptionGroup<T> where T : ICustomRole
{
    public override Func<bool> GroupVisible => TorOptionVisibility.ClassicOrGuesser;
}

/// <summary>Groups that live on the "Modifiers" tab.</summary>
[MiraIgnore]
public abstract class TorModifierOptionGroup : AbstractOptionGroup
{
    public override MenuCategory ParentMenu => MenuCategory.Modifiers;
    public override Func<bool> GroupVisible => TorOptionVisibility.ClassicOrGuesser;
}

/// <summary>
/// Hide N Seek groups. Binding <see cref="AbstractOptionGroup.OptionableType"/> to a mode makes
/// Mira API render them under the "Gamemode" dropdown on the vanilla Game page whenever
/// <see cref="TorHideNSeekMode"/> is the active mode; mode bound groups are excluded from the
/// mod's own Game tab (GameOptionsMenuPatch only takes groups with a null OptionableType there),
/// so they are never listed twice.
/// </summary>
[MiraIgnore]
public abstract class TorHideNSeekOptionGroup : AbstractOptionGroup
{
    public override Type OptionableType => typeof(TorHideNSeekMode);
    public override MenuCategory ParentMenu => MenuCategory.Game;
    public override Func<bool> GroupVisible => TorOptionVisibility.HideNSeek;
}

/// <summary>Prop Hunt groups, bound to <see cref="TorPropHuntMode"/> the same way.</summary>
[MiraIgnore]
public abstract class TorPropHuntOptionGroup : AbstractOptionGroup
{
    public override Type OptionableType => typeof(TorPropHuntMode);
    public override MenuCategory ParentMenu => MenuCategory.Game;
    public override Func<bool> GroupVisible => TorOptionVisibility.PropHunt;
}

/// <summary>
/// Guesser gamemode groups. Binding them to <see cref="TorGuesserMode"/> is what makes them appear
/// under the "Gamemode" dropdown on the vanilla Game page while the Guesser mode is selected -
/// before this they had a null <see cref="AbstractOptionGroup.OptionableType"/>, which only ever
/// put them on the mod's own Game tab, so switching to Guesser never showed any guesser options.
/// </summary>
[MiraIgnore]
public abstract class TorGuesserOptionGroup : AbstractOptionGroup
{
    public override Type OptionableType => typeof(TorGuesserMode);
    public override MenuCategory ParentMenu => MenuCategory.Game;
    public override Func<bool> GroupVisible => TorOptionVisibility.Guesser;
}
