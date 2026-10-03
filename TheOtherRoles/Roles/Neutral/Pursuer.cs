using System;
using System.Collections.Generic;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheOtherRoles.Buttons;
using TheOtherRoles.Patches;
using TheOtherRoles.Utilities;
using UnityEngine;

namespace TheOtherRoles.Roles.Neutral;

public class Pursuer(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = Lawyer.color;
    public static RoleInfo Info = new(color, RoleId.Pursuer, isNeutral: true);

    public static PlayerControl pursuer;
    public static PlayerControl target;
    public static List<PlayerControl> blankedList = new();
    public static int blanks;
    public static bool notAckedExiled;

    public static float cooldown = 30f;
    public static int blanksNumber = 5;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Pursuer;

    // Promoted from the Lawyer, never rolled on its own, so it has no spawn rate option.
    [HideFromIl2Cpp]
    protected override bool HideRoleSettings => true;

    public static void clearAndReload()
    {
        pursuer = null;
        target = null;
        blankedList = new List<PlayerControl>();
        blanks = 0;
        notAckedExiled = false;

        cooldown = OptionGroupSingleton<LawyerOptions>.Instance.PursuerCooldown.Value;
        blanksNumber = Mathf.RoundToInt(OptionGroupSingleton<LawyerOptions>.Instance.PursuerBlanksNumber.Value);
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        if (Pursuer.pursuer == null || Pursuer.pursuer != player) return;
        Pursuer.target = PlayerControlFixedUpdatePatch.setTarget();
        PlayerControlFixedUpdatePatch.setPlayerOutline(Pursuer.target, Pursuer.color);
    }
}

/// <summary>
/// The Pursuer's "blank" button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>pursuerButton</c>.
/// </summary>
public sealed class PursuerButton : TorButton
{
    private static PursuerButton pursuerButton;

    public PursuerButton()
    {
        pursuerButton = this;

        SetSprite(TorAssets.PursuerButton);
        PositionOffset = TorButtonPositions.LowerRowRight;
        Hotkey = KeyCode.F;
        ShowButtonText = true;

        RealOnClick = () =>
        {
            if (Pursuer.target != null)
            {
                PlayerControl.LocalPlayer.RpcSetBlanked(Pursuer.target.PlayerId, byte.MaxValue);

                Pursuer.target = null;

                Pursuer.blanks++;
                pursuerButton.Timer = pursuerButton.MaxTimer;
                SoundEffectsManager.play("pursuerBlank");
            }
        };
        HasButton = () =>
        {
            return Pursuer.pursuer != null && Pursuer.pursuer == PlayerControl.LocalPlayer &&
                   !PlayerControl.LocalPlayer.Data.IsDead && Pursuer.blanks < Pursuer.blanksNumber;
        };
        CouldUse = () =>
        {
            SetUses(Pursuer.blanksNumber - Pursuer.blanks);

            return Pursuer.blanksNumber > Pursuer.blanks && PlayerControl.LocalPlayer.CanMove &&
                   Pursuer.target != null;
        };
        OnMeetingEnds = () => { pursuerButton.Timer = pursuerButton.MaxTimer; };
    }

    public override float Cooldown => Pursuer.cooldown;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        ButtonText = new ButtonText(26,
            FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.UseButton]
                .FontMaterial);
    }
}

public static class PursuerRpcs
{
    [MethodRpc((uint)TorRpc.SetBlanked, LocalHandling = RpcLocalHandling.After)]
    public static void RpcSetBlanked(this PlayerControl player, byte playerId, byte value)
    {
        var target = Helpers.playerById(playerId);
        if (target == null) return;
        Pursuer.blankedList.RemoveAll(x => x.PlayerId == playerId);
        if (value > 0) Pursuer.blankedList.Add(target);
    }
}