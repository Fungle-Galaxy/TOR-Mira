using HarmonyLib;

namespace TheOtherRoles.Patches;

[HarmonyPatch(typeof(RoleBehaviour), nameof(RoleBehaviour.AppendTaskHint), typeof(Il2CppSystem.Text.StringBuilder))]
internal static class AppendTaskHintPatch
{
    private static bool Prefix(RoleBehaviour __instance) => __instance is not TorRoleBehaviour;
}
