using HarmonyLib;
using MiraAPI.GameModes;
using MiraAPI.Translation;
using TMPro;
using UnityEngine;

namespace TheOtherRoles.Patches;

internal class CreateGameOptionsPatch
{
    [HarmonyPatch(typeof(CreateGameOptions), nameof(CreateGameOptions.Start))]
    public static class CreateGameOptionsStartPatch
    {
        private static void Postfix(CreateGameOptions __instance)
        {
            if (SubmergedCompatibility.Loaded) return;

            __instance.levelButtons[0].transform.parent.gameObject.SetActive(false);
            GameObject.Find("ModeOptions").transform.SetLocalY(-2.52f);
            GameObject.Find("ServerOption").transform.SetLocalY(-0.86f);
            __instance.serverDropdown.transform.SetLocalY(-0.6f);
        }
    }

    [HarmonyPatch(typeof(CreateGameOptions), nameof(CreateGameOptions.OpenConfirmPopup))]
    private static class CreateGameOptionsOpenConfirmPopupPatch
    {
        private static void Postfix(CreateGameOptions __instance)
        {
            if (SubmergedCompatibility.Loaded) return;

            __instance.containerConfirm.GetChild(10).gameObject.SetActive(false);
            __instance.containerConfirm.GetChild(8).localPosition = new Vector3(4f, -0.47f, -0.1f);
            __instance.containerConfirm.GetChild(5).GetChild(2).GetComponent<TextMeshPro>()
                .SetText(MiraLocaleManager.Get(CustomGameModeManager.ActiveMode.Name));
        }
    }
}
