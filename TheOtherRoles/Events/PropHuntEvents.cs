using AmongUs.GameOptions;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Player;
using MiraAPI.Events.Vanilla.Usables;
using MiraAPI.Hud;
using TheOtherRoles.CustomGameModes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheOtherRoles.Events;

/// <summary>
/// Prop Hunt behaviour that used to be a Harmony patch on <c>MapConsole.Use</c> and
/// <c>PlayerControl.Die</c> and is now raised by Mira API as <see cref="PlayerUseEvent"/> and
/// <see cref="PlayerDeathEvent"/>. The <c>MapConsole.CanUse</c> half was not a Harmony patch
/// candidate at all: Mira API already routes that console through the
/// <c>AbstractGameMode.CanUseMapConsole</c> hook, so the cooldown lives in
/// <see cref="TorPropHuntMode.CanUseMapConsole"/>.
/// </summary>
public static class PropHuntEvents
{
    /// <summary>
    /// Was <c>AdminUsePostfix</c> (a prefix, despite the name): the admin console routes straight
    /// to the Prop Hunt admin button instead of opening the vanilla console.
    /// </summary>
    [RegisterEvent]
    public static void PropHuntMapConsoleUse(PlayerUseEvent @event)
    {
        if (!PropHunt.isPropHuntGM || @event.Usable.TryCast<MapConsole>() == null) return;

        var localPlayer = PlayerControl.LocalPlayer;
        if (localPlayer == null || !localPlayer.Data.Role.IsImpostor) return;

        CustomButtonSingleton<PropHuntAdminButton>.Instance.onClickEvent();
        @event.Cancel();
    }

    /// <summary>
    /// Was <c>MakePropImpostorPatch</c>: a found prop shrinks, and either turns into the hunter or
    /// leaves behind a body wearing its own sprite.
    /// </summary>
    [RegisterEvent]
    public static void PropHuntOnDeath(PlayerDeathEvent @event)
    {
        if (!PropHunt.isPropHuntGM) return;

        var __instance = @event.Player;
        __instance.transform.localScale = new Vector3(0.7f, 0.7f, 1);
        if (!__instance.Data.Role.IsImpostor && PropHunt.propBecomesHunterWhenFound)
        {
            __instance.Revive();
            DestroyableSingleton<RoleManager>.Instance.SetRole(__instance, RoleTypes.Impostor);
            if (__instance == PlayerControl.LocalPlayer)
            {
                CustomButtonSingleton<PropHuntRevealButton>.Instance.Timer = PropHunt.revealCooldown;
                CustomButtonSingleton<PropHuntFindButton>.Instance.Timer = PropHunt.findCooldown;
                CustomButtonSingleton<PropHuntAdminButton>.Instance.Timer = PropHunt.adminCooldown;
            }

            __instance.MyPhysics.SetBodyType(PlayerBodyTypes.Seeker);
        }
        else
        {
            // Find correct dead body, set sprite to dead console object...
            var currentPlayerSprite = __instance.GetComponent<SpriteRenderer>().sprite;
            foreach (var db in Object.FindObjectsOfType<DeadBody>())
                if (db.ParentId == __instance.PlayerId && currentPlayerSprite != null)
                {
                    db.bodyRenderers[0].sprite = currentPlayerSprite;
                    db.bodyRenderers[0].color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                }
        }

        __instance.GetComponent<SpriteRenderer>().sprite = null;
    }
}
