using System;
using System.Collections.Generic;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheOtherRoles.Buttons;
using TheOtherRoles.Patches;
using UnityEngine;

namespace TheOtherRoles.Roles.Neutral;

public class Sidekick(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = new Color32(0, 180, 235, byte.MaxValue);

    public static RoleInfo Info = new(color, RoleId.Sidekick, isNeutral: true);

    public static PlayerControl sidekick;

    public static PlayerControl currentTarget;

    public static bool wasTeamRed;
    public static bool wasImpostor;
    public static bool wasSpy;

    public static float cooldown = 30f;
    public static bool canUseVents = true;
    public static bool canKill = true;
    public static bool promotesToJackal = true;
    public static bool hasImpostorVision;
    public static bool canSabotageLights;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Sidekick;

    // Created from the Jackal, never rolled on its own, so it has no spawn rate option.
    [HideFromIl2Cpp]
    protected override bool HideRoleSettings => true;

    public static void clearAndReload()
    {
        sidekick = null;
        currentTarget = null;
        cooldown = OptionGroupSingleton<JackalOptions>.Instance.KillCooldown.Value;
        canUseVents = OptionGroupSingleton<JackalOptions>.Instance.SidekickCanUseVents.Value;
        canKill = OptionGroupSingleton<JackalOptions>.Instance.SidekickCanKill.Value;
        promotesToJackal = OptionGroupSingleton<JackalOptions>.Instance.SidekickPromotesToJackal.Value;
        hasImpostorVision = OptionGroupSingleton<JackalOptions>.Instance.AndSidekickHaveImpostorVision.Value;
        wasTeamRed = wasImpostor = wasSpy = false;
        canSabotageLights = OptionGroupSingleton<JackalOptions>.Instance.SidekickCanSabotageLights.Value;
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        if (Sidekick.sidekick == null || Sidekick.sidekick != player) return;
        var untargetablePlayers = new List<PlayerControl>();
        if (Jackal.jackal != null) untargetablePlayers.Add(Jackal.jackal);
        if (Mini.mini != null && !Mini.isGrownUp())
            untargetablePlayers.Add(Mini.mini);
        Sidekick.currentTarget = PlayerControlFixedUpdatePatch.setTarget(untargetablePlayers: untargetablePlayers);
        if (Sidekick.canKill) PlayerControlFixedUpdatePatch.setPlayerOutline(Sidekick.currentTarget, Palette.ImpostorRed);

        // sidekickCheckPromotion
        if (Sidekick.sidekick.Data.IsDead || !Sidekick.promotesToJackal) return;
        if (Jackal.jackal == null || Jackal.jackal?.Data?.Disconnected == true)
        {
            PlayerControl.LocalPlayer.RpcSidekickPromotes();
        }
    }
}

/// <summary>
/// The Sidekick's kill button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>sidekickKillButton</c>.
/// </summary>
public sealed class SidekickKillButton : TorButton
{
    private static SidekickKillButton sidekickKillButton;

    public SidekickKillButton()
    {
        sidekickKillButton = this;

        PositionOffset = TorButtonPositions.UpperRowRight;
        Hotkey = KeyCode.Q;
        ShowButtonText = true;
        ButtonText = new ButtonText(StringNames.KillLabel);

        RealOnClick = () =>
        {
            if (Helpers.checkMurderAttemptAndKill(Sidekick.sidekick, Sidekick.currentTarget) ==
                MurderAttemptResult.SuppressKill) return;
            sidekickKillButton.Timer = sidekickKillButton.MaxTimer;
            Sidekick.currentTarget = null;
        };
        HasButton = () =>
        {
            return Sidekick.canKill && Sidekick.sidekick != null &&
                   Sidekick.sidekick == PlayerControl.LocalPlayer && !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () => { return Sidekick.currentTarget && PlayerControl.LocalPlayer.CanMove; };
        OnMeetingEnds = () => { sidekickKillButton.Timer = sidekickKillButton.MaxTimer; };
    }

    public override float Cooldown => Sidekick.cooldown;

    public override void CreateButton(Transform parent)
    {
        // The old constructor took the kill button's sprite straight off the HUD.
        SetSprite(HudManager.Instance.KillButton.graphic.sprite);
        base.CreateButton(parent);
    }
}

public static class SidekickRpcs
{
    [MethodRpc((uint)TorRpc.SidekickPromotes, LocalHandling = RpcLocalHandling.After)]
    public static void RpcSidekickPromotes(this PlayerControl player)
    {
        RPCProcedure.sidekickPromotes();
    }
}