using MiraAPI.GameModes;
using TheOtherRoles.CustomGameModes;
using UnityEngine;

namespace TheOtherRoles.Utilities;

public static class HandleGuesser
{
    private static Sprite targetSprite;
    /// <summary>
    /// Live view of "is the Guesser mode the selected one", read straight from Mira API's
    /// <see cref="CustomGameModeManager.ActiveMode"/> instead of being re-seeded in
    /// <c>clearAndReload()</c> from TOR's own game mode flag.
    /// </summary>
    public static bool isGuesserGm => CustomGameModeManager.ActiveMode is TorGuesserMode;
    public static bool hasMultipleShotsPerMeeting;
    public static bool killsThroughShield = true;
    public static bool evilGuesserCanGuessSpy = true;
    public static bool guesserCantGuessSnitch;

    public static int tasksToUnlock =
        Mathf.RoundToInt(OptionGroupSingleton<GuesserSettingsOptions>.Instance.CrewGuesserNumberOfTasks.Value);

    public static Sprite getTargetSprite()
    {
        if (targetSprite) return targetSprite;
        targetSprite = TorAssets.TargetIcon.LoadAsset();
        return targetSprite;
    }

    public static bool isGuesser(byte playerId)
    {
        if (isGuesserGm) return GuesserGM.isGuesser(playerId);
        return Guesser.isGuesser(playerId);
    }

    public static void clear(byte playerId)
    {
        if (isGuesserGm) GuesserGM.clear(playerId);
        else Guesser.clear(playerId);
    }

    public static int remainingShots(byte playerId, bool shoot = false)
    {
        if (isGuesserGm) return GuesserGM.remainingShots(playerId, shoot);
        return Guesser.remainingShots(playerId, shoot);
    }

    public static void ClearAndReload()
    {
        Guesser.clearAndReload();
        GuesserGM.clearAndReload();
        if (isGuesserGm)
        {
            guesserCantGuessSnitch = OptionGroupSingleton<GuesserSettingsOptions>.Instance.CantGuessSnitchIfTaksDone.Value;
            hasMultipleShotsPerMeeting = OptionGroupSingleton<GuesserSettingsOptions>.Instance.HasMultipleShotsPerMeeting.Value;
            killsThroughShield = OptionGroupSingleton<GuesserSettingsOptions>.Instance.KillsThroughShield.Value;
            evilGuesserCanGuessSpy = OptionGroupSingleton<GuesserSettingsOptions>.Instance.EvilCanKillSpy.Value;
            tasksToUnlock = Mathf.RoundToInt(OptionGroupSingleton<GuesserSettingsOptions>.Instance.CrewGuesserNumberOfTasks.Value);
        }
        else
        {
            guesserCantGuessSnitch = OptionGroupSingleton<GuesserOptions>.Instance.CantGuessSnitchIfTaksDone.Value;
            hasMultipleShotsPerMeeting = OptionGroupSingleton<GuesserOptions>.Instance.HasMultipleShotsPerMeeting.Value;
            killsThroughShield = OptionGroupSingleton<GuesserOptions>.Instance.KillsThroughShield.Value;
            evilGuesserCanGuessSpy = OptionGroupSingleton<GuesserOptions>.Instance.EvilCanKillSpy.Value;
        }
    }
}