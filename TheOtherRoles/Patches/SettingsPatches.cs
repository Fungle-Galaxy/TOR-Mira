using AmongUs.GameOptions;
using HarmonyLib;

namespace TheOtherRoles.Modules;

[HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.CreateSettings))]
internal class GameOptionsMenuCreateSettingsPatch
{
    public static void Postfix(GameOptionsMenu __instance)
    {
        if (__instance.gameObject.name == "GAME SETTINGS TAB")
            adaptTaskCount(__instance);
    }

    private static NumberOption findNumberOption(OptionBehaviour[] children, Int32OptionNames name)
    {
        foreach (var child in children)
        {
            if (child == null) continue;
            var numberOption = child.TryCast<NumberOption>();
            if (numberOption != null && numberOption.intOptionName == name) return numberOption;
        }

        return null;
    }

    private static void adaptTaskCount(GameOptionsMenu __instance)
    {
        // Adapt task count for main options. On a Mira API mod page there are no vanilla number options at all.
        var children = __instance.Children?.ToArray() ?? new OptionBehaviour[0];

        var commonTasksOption = findNumberOption(children, Int32OptionNames.NumCommonTasks);
        if (commonTasksOption != null) commonTasksOption.ValidRange = new FloatRange(0f, 4f);

        var shortTasksOption = findNumberOption(children, Int32OptionNames.NumShortTasks);
        if (shortTasksOption != null) shortTasksOption.ValidRange = new FloatRange(0f, 23f);

        var longTasksOption = findNumberOption(children, Int32OptionNames.NumLongTasks);
        if (longTasksOption != null) longTasksOption.ValidRange = new FloatRange(0f, 15f);
    }
}

[HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Start))]
internal class GameOptionsMenuStartPatch
{
    public static void Postfix(GameSettingMenu __instance)
    {
        if (GameOptionsManager.Instance.currentGameOptions.GameMode == GameModes.HideNSeek) return;

        // Open TOR's own tab of the settings menu (Mira API owns the presets tab now).
        __instance.ChangeTab(1, false);
    }
}

[HarmonyPatch]
public class AddToKillDistanceSetting
{
    [HarmonyPatch(typeof(LegacyGameOptions), nameof(LegacyGameOptions.AreInvalid))]
    [HarmonyPrefix]
    public static bool Prefix(LegacyGameOptions __instance, ref int maxExpectedPlayers)
    {
        //making the killdistances bound check higher since extra short is added
        return __instance.MaxPlayers > maxExpectedPlayers || __instance.NumImpostors < 1
                                                          || __instance.NumImpostors > 3 || __instance.KillDistance < 0
                                                          || __instance.KillDistance >=
                                                          LegacyGameOptions.KillDistances.Count
                                                          || __instance.PlayerSpeedMod <= 0f ||
                                                          __instance.PlayerSpeedMod > 3f;
    }

    [HarmonyPatch(typeof(NormalGameOptionsV11), nameof(NormalGameOptionsV11.AreInvalid))]
    [HarmonyPrefix]
    public static bool Prefix(NormalGameOptionsV11 __instance, ref int maxExpectedPlayers)
    {
        return __instance.MaxPlayers > maxExpectedPlayers || __instance.NumImpostors < 1
                                                          || __instance.NumImpostors > 3 || __instance.KillDistance < 0
                                                          || __instance.KillDistance >=
                                                          LegacyGameOptions.KillDistances.Count
                                                          || __instance.PlayerSpeedMod <= 0f ||
                                                          __instance.PlayerSpeedMod > 3f;
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.Initialize))]
    [HarmonyPrefix]
    public static void Prefix(StringOption __instance)
    {
        //prevents indexoutofrange exception breaking the setting if long happens to be selected
        //when host opens the laptop
        if (__instance.Title == StringNames.GameKillDistance && __instance.Value == 3)
        {
            __instance.Value = 1;
            GameOptionsManager.Instance.currentNormalGameOptions.KillDistance = 1;
            GameManager.Instance.LogicOptions.SyncOptions();
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.Initialize))]
    [HarmonyPostfix]
    public static void Postfix(StringOption __instance)
    {
        if (__instance.Title == StringNames.GameKillDistance && __instance.Values.Count == 3)
            __instance.Values = new Il2CppStructArray<StringNames>(
                new[]
                {
                    (StringNames)49999, StringNames.SettingShort, StringNames.SettingMedium, StringNames.SettingLong
                });
    }

    [HarmonyPatch(typeof(IGameOptionsExtensions), nameof(IGameOptionsExtensions.AppendItem),
        typeof(Il2CppSystem.Text.StringBuilder), typeof(StringNames), typeof(string))]
    [HarmonyPrefix]
    public static void Prefix(ref StringNames stringName, ref string value)
    {
        if (stringName == StringNames.GameKillDistance)
        {
            int index;
            if (GameOptionsManager.Instance.currentGameMode == GameModes.Normal)
                index = GameOptionsManager.Instance.currentNormalGameOptions.KillDistance;
            else
                index = GameOptionsManager.Instance.currentHideNSeekGameOptions.KillDistance;
            value = LegacyGameOptions.KillDistanceStrings[index];
        }
    }

    [HarmonyPatch(typeof(TranslationController), nameof(TranslationController.GetString), typeof(StringNames),
        typeof(Il2CppReferenceArray<Il2CppSystem.Object>))]
    [HarmonyPriority(Priority.Last)]
    public static bool Prefix(ref string __result, ref StringNames id)
    {
        if ((int)id == 49999)
        {
            __result = ModTranslation.GetString("CustomOption-Text", 27);
            return false;
        }

        return true;
    }

    public static void addKillDistance()
    {
        LegacyGameOptions.KillDistances = new Il2CppStructArray<float>(new[] { 0.5f, 1f, 1.8f, 2.5f });
        LegacyGameOptions.KillDistanceStrings =
            new Il2CppStringArray(new[] { ModTranslation.GetString("CustomOption-Text", 27), ModTranslation.GetString("CustomOption-Text", 28), ModTranslation.GetString("CustomOption-Text", 29), ModTranslation.GetString("CustomOption-Text", 30) });
    }

    [HarmonyPatch(typeof(StringGameSetting), nameof(StringGameSetting.GetValueString))]
    [HarmonyPrefix]
    public static bool AjdustStringForViewPanel(StringGameSetting __instance, float value, ref string __result)
    {
        if (__instance.OptionName != Int32OptionNames.KillDistance) return true;
        __result = LegacyGameOptions.KillDistanceStrings[(int)value];
        return false;
    }
}
