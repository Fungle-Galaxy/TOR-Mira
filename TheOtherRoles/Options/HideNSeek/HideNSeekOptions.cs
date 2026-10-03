// Converted from CustomOptionHolder.cs by tools/Generate-Options.ps1; this file is now the source of truth.

using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using UnityEngine;

namespace TheOtherRoles.Options;

public class HideNSeekOptions : TorHideNSeekOptionGroup
{
    public override uint GroupPriority => 0;
    public override string GroupName => TorOptions.Title("Opt-Snitch,101");
    public override Color GroupColor => TorOptionColors.Group(Color.yellow);

    public ModdedStringOption Map { get; } =
        new ModdedStringOption("Opt-Snitch,101", "Opt-General,100", ["Opt-General,100", "Opt-General,101", "Opt-General,102", "Opt-General,103", "Opt-General,104", "Opt-General,105", "Opt-General,106"])
        {
            ChangedEvent = _ =>
            {
                var map = OptionGroupSingleton<HideNSeekOptions>.Instance.Map.Selection();
                if (map >= 3) map++;
                GameOptionsManager.Instance.currentNormalGameOptions.MapId = (byte)map;
            }
        };

    public ModdedNumberOption HunterCount { get; } = new ModdedNumberOption("Opt-HideNSeek-Main,2", 1f, 1f, 3f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption KillCooldown { get; } = new ModdedNumberOption("Opt-HideNSeek-Main,3", 10f, 2.5f, 60f, 2.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption HunterVision { get; } = new ModdedNumberOption("Opt-HideNSeek-Main,4", 0.5f, 0.25f, 2f, 0.25f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption HuntedVision { get; } = new ModdedNumberOption("Opt-HideNSeek-Main,5", 2f, 0.25f, 5f, 0.25f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption CommonTasks { get; } = new ModdedNumberOption("Opt-HideNSeek-Main,6", 1f, 0f, 4f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption ShortTasks { get; } = new ModdedNumberOption("Opt-HideNSeek-Main,7", 3f, 1f, 23f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption LongTasks { get; } = new ModdedNumberOption("Opt-HideNSeek-Main,8", 3f, 0f, 15f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption Timer { get; } = new ModdedNumberOption("Opt-HideNSeek-Main,9", 5f, 1f, 30f, 0.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedToggleOption TaskWin { get; } = new ModdedToggleOption("Opt-HideNSeek-Main,10", false);

    public ModdedNumberOption TaskPunish { get; } = new ModdedNumberOption("Opt-HideNSeek-Main,11", 10f, 0f, 30f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedToggleOption CanSabotage { get; } = new ModdedToggleOption("Opt-HideNSeek-Main,12", false);

    public ModdedNumberOption HunterWaiting { get; } = new ModdedNumberOption("Opt-HideNSeek-Main,13", 15f, 2.5f, 60f, 2.5f, MiraNumberSuffixes.None, "0.##");
}
