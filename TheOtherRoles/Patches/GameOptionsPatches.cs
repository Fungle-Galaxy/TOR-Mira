using AmongUs.GameOptions;
using HarmonyLib;
using TheOtherRoles.CustomGameModes;
using UnityEngine;

namespace TheOtherRoles.Patches;

/// <summary>
/// The original TOR interception of the vanilla special roles: with mod roles active every
/// vanilla count comes back 0, so Mira API's selector can only ever fall back to Crewmate /
/// Impostor seats. TOR's own roles are left alone because their count comes from Mira API's
/// GetNum prefix, and Hide N Seek / Prop Hunt keep whatever the vanilla options say.
/// </summary>
[HarmonyPatch(typeof(RoleOptionsCollectionV11), nameof(RoleOptionsCollectionV11.GetNumPerGame))]
internal class RoleOptionsDataGetNumPerGamePatch
{
    public static void Postfix(RoleTypes role, ref int __result)
    {
        if (GameOptionsManager.Instance.CurrentGameOptions.GameMode != GameModes.Normal) return;
        // TOR's own roles answer through Mira API's GetNum prefix, leave those counts alone.
        if (MiraAPI.Roles.CustomRoleManager.GetCustomRoleBehaviour(role, out var customRole) &&
            customRole != null) return;
        __result = 0; // Deactivate Vanilla Roles if the mod roles are active
    }
}

[HarmonyPatch(typeof(IGameOptionsExtensions), nameof(IGameOptionsExtensions.GetAdjustedNumImpostors))]
internal class LegacyGameOptionsGetAdjustedNumImpostorsPatch
{
    public static void Postfix(ref int __result)
    {
        if (HideNSeek.isHideNSeekGM || PropHunt.isPropHuntGM)
        {
            __result = HideNSeek.isHideNSeekGM
                ? Mathf.RoundToInt(OptionGroupSingleton<HideNSeekOptions>.Instance.HunterCount.Value)
                : OptionGroupSingleton<PropHuntHunterOptions>.Instance.NumberOfHunters.Quantity();
        }
        else if (GameOptionsManager.Instance.CurrentGameOptions.GameMode == GameModes.Normal)
        {
            __result = Mathf.Clamp(GameOptionsManager.Instance.CurrentGameOptions.NumImpostors, 1, 3);
        }
    }
}

[HarmonyPatch(typeof(LegacyGameOptions), nameof(LegacyGameOptions.Validate))]
internal class LegacyGameOptionsValidatePatch
{
    public static void Postfix(LegacyGameOptions __instance)
    {
        if (HideNSeek.isHideNSeekGM ||
            GameOptionsManager.Instance.CurrentGameOptions.GameMode != GameModes.Normal) return;
        if (PropHunt.isPropHuntGM)
            __instance.NumImpostors = OptionGroupSingleton<PropHuntHunterOptions>.Instance.NumberOfHunters.Quantity();
        __instance.NumImpostors = GameOptionsManager.Instance.CurrentGameOptions.NumImpostors;
    }
}
