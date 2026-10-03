using HarmonyLib;
using TheOtherRoles.Draft;

namespace TheOtherRoles.Patches;

[HarmonyPatch(typeof(ChatController), nameof(ChatController.Toggle))]
internal class DraftChatTogglePatch
{
    [HarmonyPrefix]
    public static bool Prefix() => DraftChat.CanOpen;
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.SetVisible))]
internal class DraftChatVisiblePatch
{
    [HarmonyPrefix]
    public static bool Prefix(bool visible) => !visible || DraftChat.CanOpen;
}
