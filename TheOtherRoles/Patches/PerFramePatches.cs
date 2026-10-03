using HarmonyLib;
using TheOtherRoles.CustomGameModes;
using TheOtherRoles.Modules;

namespace TheOtherRoles.Patches;

// Mira API has no per frame event, so the individual HudManager.Update / PlayerControl.FixedUpdate
// postfixes are driven from here instead of every file declaring its own patch.
[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
internal static class TorHudManagerUpdatePatch
{
    private static void Postfix(HudManager __instance)
    {
        HudManagerUpdatePatch.Postfix(__instance);
        LobbyRoleInfo.HudManagerRoleInfoUpdate.Postfix(__instance);
        ChatCommands.EnableChat.Postfix(__instance);
        PropHunt.MapSetPostfix();
        // Last: the kill target must be resolved after everything else touched the button.
        KillButtonTargetUpdatePatch.Postfix();
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
internal static class TorPlayerControlFixedUpdatePatch
{
    private static void Postfix(PlayerControl __instance)
    {
        PlayerControlFixedUpdatePatch.Postfix(__instance);
        VentButtonVisibilityPatch.Postfix(__instance);
        PropHunt.PlayerControlFixedUpdatePatch(__instance);
    }
}
