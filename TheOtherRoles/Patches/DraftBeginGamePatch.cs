using AmongUs.GameOptions;
using HarmonyLib;
using TheOtherRoles.CustomGameModes;
using TheOtherRoles.Draft;

namespace TheOtherRoles.Patches;

[HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.BeginGame))]
internal class DraftBeginGamePatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static bool Prefix()
    {
        if (DraftEngine.SkipNextStartIntercept)
        {
            DraftEngine.SkipNextStartIntercept = false;
            return true;
        }

        if (!AmongUsClient.Instance.AmHost) return true;
        if (DraftManager.IsDraftActive) return false;

        DraftManager.Reset();
        if (!shouldDraft()) return true;
        if (DraftEngine.TryStartDraft(out var reason)) return false;

        TheOtherRolesPlugin.Logger.LogInfo($"[Draft] enabled but not started: {reason}");
        return true;
    }

    private static bool shouldDraft()
    {
        var options = OptionGroupSingleton<DraftOptions>.Instance;
        if (options == null || !options.EnableDraft.Value) return false;
        if (HideNSeek.isHideNSeekGM || PropHunt.isPropHuntGM) return false;
        return GameOptionsManager.Instance.currentGameOptions.GameMode != GameModes.HideNSeek;
    }
}
