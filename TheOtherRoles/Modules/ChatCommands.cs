using System;
using System.Collections;
using System.Linq;
using HarmonyLib;
using InnerNet;
using MiraAPI.GameModes;
using TheOtherRoles.CustomGameModes;
using TheOtherRoles.Utilities;

namespace TheOtherRoles.Modules;

/// <summary>
/// Slash commands typed into the chat, plus the chat visibility / colouring rules that go with them.
/// Ported from the original The Other Roles.
/// </summary>
[HarmonyPatch]
public static class ChatCommands
{
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
    private static class SendChatPatch
    {
        private static bool Prefix(ChatController __instance)
        {
            var text = __instance.freeChatField.Text;
            var handled = false;
            if (AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started)
            {
                if (text.ToLower().StartsWith("/kick "))
                {
                    var playerName = text.Substring(6);
                    var target = PlayerControl.AllPlayerControls.ToArray()
                        .FirstOrDefault(x => x.Data.PlayerName.Equals(playerName));
                    if (target != null && AmongUsClient.Instance != null && AmongUsClient.Instance.CanBan())
                    {
                        var client = AmongUsClient.Instance.GetClient(target.OwnerId);
                        if (client != null)
                        {
                            AmongUsClient.Instance.KickPlayer(client.Id, false);
                            handled = true;
                        }
                    }
                }
                else if (text.ToLower().StartsWith("/ban "))
                {
                    var playerName = text.Substring(5);
                    var target = PlayerControl.AllPlayerControls.ToArray()
                        .FirstOrDefault(x => x.Data.PlayerName.Equals(playerName));
                    if (target != null && AmongUsClient.Instance != null && AmongUsClient.Instance.CanBan())
                    {
                        var client = AmongUsClient.Instance.GetClient(target.OwnerId);
                        if (client != null)
                        {
                            AmongUsClient.Instance.KickPlayer(client.Id, true);
                            handled = true;
                        }
                    }
                }
                else if (text.ToLower().StartsWith("/gm"))
                {
                    var gm = text.Length > 4 ? text.Substring(4).ToLower() : "";
                    Type modeType;
                    if (gm.StartsWith("prop") || gm.StartsWith("ph")) modeType = typeof(TorPropHuntMode);
                    else if (gm.StartsWith("guess")) modeType = typeof(TorGuesserMode);
                    else if (gm.StartsWith("hide") || gm.StartsWith("hn")) modeType = typeof(TorHideNSeekMode);
                    else modeType = typeof(ClassicMode); // including no argument at all

                    if (AmongUsClient.Instance.AmHost) SetGameMode(modeType);
                    else __instance.AddChat(PlayerControl.LocalPlayer, ModTranslation.GetString("ChatCommands", 1));

                    handled = true;
                }
            }

            if (AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay)
            {
                if (text.ToLower().Equals("/murder"))
                {
                    PlayerControl.LocalPlayer.Exiled();
                    FastDestroyableSingleton<HudManager>.Instance.KillOverlay.ShowKillAnimation(
                        PlayerControl.LocalPlayer.Data, PlayerControl.LocalPlayer.Data);
                    handled = true;
                }
                else if (text.ToLower().StartsWith("/color "))
                {
                    handled = true;
                    if (!int.TryParse(text.Substring(7), out var col))
                        __instance.AddChat(PlayerControl.LocalPlayer, ModTranslation.GetString("ChatCommands", 2));
                    col = Math.Clamp(col, 0, Palette.PlayerColors.Length - 1);
                    PlayerControl.LocalPlayer.SetColor(col);
                    __instance.AddChat(PlayerControl.LocalPlayer, ModTranslation.GetString("ChatCommands", 3));
                }
            }

            if (text.ToLower().StartsWith("/tp ") && PlayerControl.LocalPlayer.Data.IsDead)
            {
                var playerName = text.Substring(4).ToLower();
                var target = PlayerControl.AllPlayerControls.ToArray()
                    .FirstOrDefault(x => x.Data.PlayerName.ToLower().Equals(playerName));
                if (target != null)
                {
                    PlayerControl.LocalPlayer.transform.position = target.transform.position;
                    handled = true;
                }
            }
            
            if (handled)
            {
                __instance.freeChatField.Clear();
                __instance.quickChatMenu.Clear();
            }

            return !handled;
        }
    }

    /// <summary>
    /// Switches the host to the given mode through Mira API's own game mode option, so every other
    /// client is updated by the very same path the settings menu dropdown uses.
    /// </summary>
    /// <remarks>
    /// Mira API exposes no public way to change the mode - <c>GameModeOption.Set</c>,
    /// <c>GameModeOption.RpcSyncGamemode</c> and <c>CustomGameModeManager.IdToModeMap</c> are all
    /// internal because the dropdown is the supported entry point. The one command that still has to
    /// poke it from chat therefore reaches for them with <see cref="AccessTools"/>.
    /// </remarks>
    private static void SetGameMode(Type modeType)
    {
        var map = AccessTools.Field(typeof(CustomGameModeManager), "IdToModeMap")?.GetValue(null) as IDictionary;
        if (map == null) return;

        var id = 0u;
        var found = false;
        foreach (DictionaryEntry entry in map)
        {
            if (entry.Value.GetType() != modeType) continue;
            id = (uint)entry.Key;
            found = true;
            break;
        }

        if (!found) return;

        // Applies the value locally and broadcasts it; GameModeOption.Value doubles as the index into
        // the dropdown and as the id passed to CustomGameModeManager.GetAndSetGameMode(), so the mode
        // id can be handed over as-is.
        AccessTools.Method(typeof(GameModeOption), "RpcSyncGamemode")
            ?.Invoke(null, new object[] { PlayerControl.LocalPlayer, (int)id });
    }

    public static class EnableChat
    {
        public static void Postfix(HudManager __instance)
        {
            if (!__instance.Chat.isActiveAndEnabled && (AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay ||
                                                        (PlayerControl.LocalPlayer.isLover() && Lovers.enableChat)))
                __instance.Chat.SetVisible(true);
        }
    }

    [HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetName))]
    public static class SetBubbleName
    {
        public static void Postfix(ChatBubble __instance, [HarmonyArgument(0)] string playerName)
        {
            var sourcePlayer = PlayerControl.AllPlayerControls.ToArray().ToList()
                .FirstOrDefault(x => x.Data != null && x.Data.PlayerName.Equals(playerName));
            if (sourcePlayer != null && PlayerControl.LocalPlayer != null &&
                PlayerControl.LocalPlayer.Data?.Role?.IsImpostor == true &&
                ((Spy.spy != null && sourcePlayer.PlayerId == Spy.spy.PlayerId) ||
                 (Sidekick.sidekick != null && Sidekick.wasTeamRed &&
                  sourcePlayer.PlayerId == Sidekick.sidekick.PlayerId) ||
                 (Jackal.jackal != null && Jackal.wasTeamRed && sourcePlayer.PlayerId == Jackal.jackal.PlayerId)) &&
                __instance != null) __instance.NameText.color = Palette.ImpostorRed;
        }
    }

    [HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
    public static class AddChat
    {
        public static bool Prefix(ChatController __instance, [HarmonyArgument(0)] PlayerControl sourcePlayer)
        {
            if (__instance != FastDestroyableSingleton<HudManager>.Instance.Chat)
                return true;
            var localPlayer = PlayerControl.LocalPlayer;
            return localPlayer == null || MeetingHud.Instance != null || LobbyBehaviour.Instance != null ||
                   localPlayer.Data.IsDead || (localPlayer.isLover() && Lovers.enableChat) ||
                   sourcePlayer.PlayerId == PlayerControl.LocalPlayer.PlayerId;
        }
    }
}
