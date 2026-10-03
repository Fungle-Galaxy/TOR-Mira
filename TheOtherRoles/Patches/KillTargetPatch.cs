using HarmonyLib;
using InnerNet;

namespace TheOtherRoles.Patches;

// Vanilla only resolves a kill target for ImpostorRole, so the target is refreshed here too,
// after everything else in the frame has had a chance to touch the button.
public static class KillButtonTargetUpdatePatch
{
    public static void Postfix()
    {
        if (AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started) return;
        PlayerControlFixedUpdatePatch.impostorSetTarget();
    }
}
