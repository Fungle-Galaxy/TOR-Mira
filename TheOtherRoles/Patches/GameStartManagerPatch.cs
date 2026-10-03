using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Reactor.Utilities.Extensions;
using TheOtherRoles.CustomGameModes;
using TheOtherRoles.Draft;
using TheOtherRoles.Networking;
using TheOtherRoles.Utilities;
using TMPro;
using UnityEngine;

namespace TheOtherRoles.Patches;

public class GameStartManagerPatch
{
    [HarmonyPatch(typeof(PlayerPhysics._CoSpawnPlayer_d__42), "MoveNext")]
    public class PlayerPhysicsCoSpawnPlayerPatch
    {
        public static void Postfix(PlayerPhysics._CoSpawnPlayer_d__42 __instance)
        {
            if (PlayerControl.LocalPlayer != null && AmongUsClient.Instance.AmHost)
            {
                GameManager.Instance.LogicOptions.SyncOptions();
            }
        }
    }

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
    public class GameStartManagerUpdatePatch
    {
        public static float startingTimer;
        private static bool update;
        private static string currentText = "";
        private static GameObject copiedStartButton;

        public static void Prefix(GameStartManager __instance)
        {
            if (!GameData.Instance) return; // No instance
            __instance.MinPlayers = 1;
            update = GameData.Instance.PlayerCount != __instance.LastPlayerCount;
        }

        public static void Postfix(GameStartManager __instance)
        {
            // Display message to the host
            if (AmongUsClient.Instance.AmHost)
            {
                __instance.GameStartText.transform.localPosition = Vector3.zero;
                __instance.GameStartText.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
                if (!__instance.GameStartText.text.Contains(FastDestroyableSingleton<TranslationController>.Instance
                        .GetString(StringNames.GameStarting).Replace("{0}", "")))
                {
                    __instance.GameStartText.text = string.Empty;
                    __instance.GameStartTextParent.SetActive(false);
                }

                if (__instance.startState != GameStartManager.StartingStates.Countdown)
                    copiedStartButton?.Destroy();

                // Make starting info available to clients:
                if (startingTimer <= 0 && __instance.startState == GameStartManager.StartingStates.Countdown)
                {
                    PlayerControl.LocalPlayer.RpcSetGameStarting();

                    // Activate Stop-Button
                    copiedStartButton = GameObject.Instantiate(__instance.StartButton.gameObject,
                        __instance.StartButton.gameObject.transform.parent);
                    copiedStartButton.transform.localPosition = __instance.StartButton.transform.localPosition;
                    copiedStartButton.SetActive(true);
                    var startButtonText = copiedStartButton.GetComponentInChildren<TextMeshPro>();
                    startButtonText.text = "";
                    startButtonText.fontSize *= 0.8f;
                    startButtonText.fontSizeMax = startButtonText.fontSize;
                    startButtonText.gameObject.transform.localPosition = Vector3.zero;
                    var startButtonPassiveButton = copiedStartButton.GetComponent<PassiveButton>();

                    void StopStartFunc()
                    {
                        __instance.ResetStartState();
                        PlayerControl.LocalPlayer.RpcStopStart(PlayerControl.LocalPlayer.PlayerId);
                        copiedStartButton.Destroy();
                        startingTimer = 0;
                        SoundManager.Instance.StopSound(GameStartManager.Instance.gameStartSound);
                    }

                    startButtonPassiveButton.OnClick.AddListener((Action)(() => StopStartFunc()));
                    __instance.StartCoroutine(Effects.Lerp(.1f,
                        new Action<float>(p => { startButtonText.text = ""; })));
                }
            }
            // Client update
            else
            {
                __instance.GameStartText.transform.localPosition = Vector3.zero;
                __instance.GameStartText.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
                if (!__instance.GameStartText.text.Contains(FastDestroyableSingleton<TranslationController>.Instance
                        .GetString(StringNames.GameStarting).Replace("{0}", "")))
                {
                    __instance.GameStartText.text = string.Empty;
                    __instance.GameStartTextParent.SetActive(false);
                }

                if (!__instance.GameStartText.text.Contains(FastDestroyableSingleton<TranslationController>.Instance
                        .GetString(StringNames.GameStarting).Replace("{0}", "")) ||
                    !OptionGroupSingleton<GameSettingsOptions>.Instance.AnyPlayerCanStopStart.Value)
                    copiedStartButton?.Destroy();

                if (OptionGroupSingleton<GameSettingsOptions>.Instance.AnyPlayerCanStopStart.Value && copiedStartButton == null &&
                    __instance.GameStartText.text.Contains(FastDestroyableSingleton<TranslationController>.Instance
                        .GetString(StringNames.GameStarting).Replace("{0}", "")))
                {
                    // Activate Stop-Button
                    copiedStartButton = GameObject.Instantiate(__instance.StartButton.gameObject,
                        __instance.StartButton.gameObject.transform.parent);
                    copiedStartButton.transform.localPosition = __instance.StartButton.transform.localPosition;
                    copiedStartButton.SetActive(true);
                    var startButtonText = copiedStartButton.GetComponentInChildren<TextMeshPro>();
                    startButtonText.text = "";
                    startButtonText.fontSize *= 0.8f;
                    startButtonText.fontSizeMax = startButtonText.fontSize;
                    startButtonText.gameObject.transform.localPosition = Vector3.zero;
                    var startButtonPassiveButton = copiedStartButton.GetComponent<PassiveButton>();

                    void StopStartFunc()
                    {
                        PlayerControl.LocalPlayer.RpcStopStart(PlayerControl.LocalPlayer.PlayerId);
                        copiedStartButton.Destroy();
                        __instance.GameStartText.text = string.Empty;
                        startingTimer = 0;
                        SoundManager.Instance.StopSound(GameStartManager.Instance.gameStartSound);
                    }

                    startButtonPassiveButton.OnClick.AddListener((Action)(() => StopStartFunc()));
                    __instance.StartCoroutine(Effects.Lerp(.1f,
                        new Action<float>(p => { startButtonText.text = ""; })));
                }
            }

            // Start Timer
            if (startingTimer > 0) startingTimer -= Time.deltaTime;

            // Lobby timer
            if (!GameData.Instance || !__instance.PlayerCounter) return; // No instance

            if (update) currentText = __instance.PlayerCounter.text;

            if (!AmongUsClient.Instance) return;
        }
    }

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.BeginGame))]
    public class GameStartManagerBeginGame
    {
        public static bool Prefix(GameStartManager __instance)
        {
            // The draft swallows this first call, so the map roll belongs to the real start.
            if (DraftManager.IsDraftActive) return true;

            if (AmongUsClient.Instance.AmHost)
            {
                if ((HideNSeek.isHideNSeekGM || PropHunt.isPropHuntGM) &&
                    GameOptionsManager.Instance.CurrentGameOptions.MapId != 6)
                {
                    byte mapId = 0;
                    if (HideNSeek.isHideNSeekGM)
                        mapId = (byte)OptionGroupSingleton<HideNSeekOptions>.Instance.Map.Selection();
                    else if (PropHunt.isPropHuntGM)
                        mapId = (byte)OptionGroupSingleton<PropHuntMapOptions>.Instance.Map.Selection();
                    if (mapId >= 3) mapId++;
                    PlayerControl.LocalPlayer.RpcDynamicMapOption(mapId);
                }
                else if (OptionGroupSingleton<DynamicMapOptions>.Instance.DynamicMap.Value)
                {
                    // 0 = Skeld
                    // 1 = Mira HQ
                    // 2 = Polus
                    // 3 = Dleks - deactivated
                    // 4 = Airship
                    // 5 = Fungle
                    // 6 = Submerged
                    byte chosenMapId = 0;
                    List<float> probabilities =
                    [
                        OptionGroupSingleton<DynamicMapOptions>.Instance.EnableSkeld.Selection() / (float)TorOptions.MaxRate,
                        OptionGroupSingleton<DynamicMapOptions>.Instance.EnableMira.Selection() / (float)TorOptions.MaxRate,
                        OptionGroupSingleton<DynamicMapOptions>.Instance.EnablePolus.Selection() / (float)TorOptions.MaxRate,
                        OptionGroupSingleton<DynamicMapOptions>.Instance.EnableAirShip.Selection() / (float)TorOptions.MaxRate,
                        OptionGroupSingleton<DynamicMapOptions>.Instance.EnableFungle.Selection() / (float)TorOptions.MaxRate,
                        OptionGroupSingleton<DynamicMapOptions>.Instance.EnableSubmerged.Selection() / (float)TorOptions.MaxRate
                    ];

                    // if any map is at 100%, remove all maps that are not!
                    if (probabilities.Contains(1.0f))
                        for (var i = 0; i < probabilities.Count; i++)
                            if (probabilities[i] != 1.0)
                                probabilities[i] = 0;

                    var sum = probabilities.Sum();
                    if (sum == 0) return true; // All maps set to 0, why are you doing this???
                    for (var i = 0; i < probabilities.Count; i++) // Normalize to [0,1]
                        probabilities[i] /= sum;
                    var selection = (float)Helpers.rnd.NextDouble();
                    float cumsum = 0;
                    for (byte i = 0; i < probabilities.Count; i++)
                    {
                        cumsum += probabilities[i];
                        if (cumsum > selection)
                        {
                            chosenMapId = i;
                            break;
                        }
                    }

                    if (chosenMapId >= 3) chosenMapId++; // Skip dlekS

                    PlayerControl.LocalPlayer.RpcDynamicMapOption(chosenMapId);
                }
            }

            return true;
        }
    }
}