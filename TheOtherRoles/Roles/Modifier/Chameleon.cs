using System.Collections.Generic;
using AmongUs.Data;
using UnityEngine;

namespace TheOtherRoles.Roles.Modifier;

public class Chameleon : TorGameModifier
{
    public static Color color = Color.yellow;

    public static RoleInfo Info = new(color, RoleId.Chameleon, isModifier: true);

    public static List<PlayerControl> chameleon = new();
    public static float minVisibility = 0.2f;
    public static float holdDuration = 1f;
    public static float fadeDuration = 0.5f;
    public static Dictionary<byte, float> lastMoved;

    public override RoleInfo TorInfo => Info;

    public override int GetAssignmentChance() =>
        ChanceOf(OptionGroupSingleton<ChameleonOptions>.Instance.Chameleon);

    public override int GetAmountPerGame() =>
        OptionGroupSingleton<ChameleonOptions>.Instance.ChameleonQuantity.Quantity();

    public override void OnActivate() => chameleon.Add(Player);

    public override void OnDeactivate() => chameleon.Remove(Player);

    public override void ClearAndReload() => clearAndReload();

    public static void clearAndReload()
    {
        chameleon = new List<PlayerControl>();
        lastMoved = new Dictionary<byte, float>();
        holdDuration = OptionGroupSingleton<ChameleonOptions>.Instance.ChameleonHoldDuration.Value;
        fadeDuration = OptionGroupSingleton<ChameleonOptions>.Instance.ChameleonFadeDuration.Value;
        minVisibility = OptionGroupSingleton<ChameleonOptions>.Instance.ChameleonMinVisibility.Selection() / 10f;
    }

    public static float visibility(byte playerId)
    {
        var visibility = 1f;
        if (lastMoved != null && lastMoved.ContainsKey(playerId))
        {
            var tStill = Time.time - lastMoved[playerId];
            if (tStill > holdDuration)
            {
                if (tStill - holdDuration > fadeDuration) visibility = minVisibility;
                else visibility = (1 - (tStill - holdDuration) / fadeDuration) * (1 - minVisibility) + minVisibility;
            }
        }

        if (PlayerControl.LocalPlayer.Data.IsDead && visibility < 0.1f) visibility = 0.1f;
        return visibility;
    }

    public static void update()
    {
        foreach (var chameleonPlayer in chameleon)
        {
            if (chameleonPlayer == Ninja.ninja && Ninja.isInvisble) continue;
            var playerPhysics = chameleonPlayer.MyPhysics;
            var currentPhysicsAnim = playerPhysics.Animations.Animator.GetCurrentAnimation();
            if (currentPhysicsAnim != playerPhysics.Animations.group.IdleAnim)
                lastMoved[chameleonPlayer.PlayerId] = Time.time;
            var visibility = Chameleon.visibility(chameleonPlayer.PlayerId);
            var petVisibility = visibility;
            if (chameleonPlayer.Data.IsDead)
            {
                visibility = 0.5f;
                petVisibility = 1f;
            }

            try
            {
                chameleonPlayer.cosmetics.currentBodySprite.BodySprite.color =
                    chameleonPlayer.cosmetics.currentBodySprite.BodySprite.color.SetAlpha(visibility);
                if (DataManager.Settings.Accessibility.ColorBlindMode)
                    chameleonPlayer.cosmetics.colorBlindText.color =
                        chameleonPlayer.cosmetics.colorBlindText.color.SetAlpha(visibility);
                chameleonPlayer.SetHatAndVisorAlpha(visibility);
                chameleonPlayer.cosmetics.skin.layer.color =
                    chameleonPlayer.cosmetics.skin.layer.color.SetAlpha(visibility);
                chameleonPlayer.cosmetics.nameText.color =
                    chameleonPlayer.cosmetics.nameText.color.SetAlpha(visibility);
                foreach (var rend in chameleonPlayer.cosmetics.currentPet.renderers)
                    rend.color = rend.color.SetAlpha(petVisibility);
                foreach (var shadowRend in chameleonPlayer.cosmetics.currentPet.shadows)
                    shadowRend.color = shadowRend.color.SetAlpha(petVisibility);
            }
            catch
            {
            }
        }
    }

}