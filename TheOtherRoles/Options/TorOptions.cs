using System;
using MiraAPI.GameOptions.OptionTypes;
using UnityEngine;

namespace TheOtherRoles.Options;

/// <summary>
/// Shared vocabulary for the TOR option groups: raw translation keys, spawn rate tables and the
/// helpers that make every group file read like the TownOfUs-Mira ones - a strongly typed property
/// per option, no magic ids anywhere.
/// </summary>
public static class TorOptions
{
    /// <summary>Raw ("Category,Id") translation key.</summary>
    public static string Key(string category, int id) => $"{category},{id}";

    /// <summary>Raw translation key of a role name.</summary>
    public static string RoleKey(RoleId roleId) => $"Role-Name,{(int)roleId}";

    /// <summary>
    /// Resolves a raw translation key ("Category,Id") into the current language. Plain strings
    /// such as "0%" or "-10%" are returned unchanged.
    /// </summary>
    public static string Title(string key)
    {
        var i = key.LastIndexOf(',');
        if (i > 0 && int.TryParse(key[(i + 1)..], out var id))
            return ModTranslation.GetString(key[..i], id);
        return key;
    }

    /// <summary>Role name in the currently selected language. Only meant for group headers.</summary>
    public static string RoleName(RoleId roleId) => ModTranslation.GetString("Role-Name", (int)roleId);

    /// <summary>
    /// Spawn rates. Five percent steps, so Mira API's chance spinner (10% per press, 5% while
    /// holding shift) always lands on a value this mod can actually store.
    /// </summary>
    public static string[] Rates { get; } =
    [
        "0%", "5%", "10%", "15%", "20%", "25%", "30%", "35%", "40%", "45%", "50%",
        "55%", "60%", "65%", "70%", "75%", "80%", "85%", "90%", "95%", "100%"
    ];

    /// <summary>Percent a <see cref="Rates"/> option stands for, per index step.</summary>
    public const int RateStep = 5;

    /// <summary>Index of the "100%" entry, i.e. the full span of a Rates option.</summary>
    public const int MaxRate = 100 / RateStep;

    /// <summary>Quantities (1 - 15), used for the "amount of x" modifier options.</summary>
    public static string[] Counts { get; } =
    [
        "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15"
    ];
}

/// <summary>
/// Replacements for the old <c>CustomOption.getSelection() / getQuantity() / getBool()</c>
/// helpers, expressed in terms of the native Mira API options.
/// </summary>
public static class TorOptionExtensions
{
    public static int Selection(this ModdedStringOption option)
    {
        return Array.IndexOf(option.Values, option.Value);
    }

    public static int Quantity(this ModdedStringOption option)
    {
        return Array.IndexOf(option.Values, option.Value) + 1;
    }

    public static bool Enabled(this ModdedStringOption option)
    {
        return Array.IndexOf(option.Values, option.Value) > 0;
    }

    public static int Selection(this ModdedNumberOption option)
    {
        return Mathf.RoundToInt((option.Value - option.Min) / option.Increment);
    }

    public static int Quantity(this ModdedNumberOption option)
    {
        return Mathf.RoundToInt((option.Value - option.Min) / option.Increment) + 1;
    }

    public static int Selection(this ModdedToggleOption option)
    {
        return option.Value ? 1 : 0;
    }
}
