using HarmonyLib;
using TheOtherRoles.Networking;

namespace TheOtherRoles.Patches;

/// <summary>
/// Role selection itself belongs to Mira API. This only wipes what the previous round left behind,
/// and it has to run first - running it after Mira API has handed the roles out would wipe them.
/// </summary>
[HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
internal class RoleSelectionResetPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static void Prefix()
    {
        PlayerControl.LocalPlayer.RpcResetVaribles();
    }
}
