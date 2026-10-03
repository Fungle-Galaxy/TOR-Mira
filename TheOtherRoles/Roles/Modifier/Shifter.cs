using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheOtherRoles.Buttons;
using TheOtherRoles.Patches;
using TheOtherRoles.Utilities;
using UnityEngine;

namespace TheOtherRoles.Roles.Modifier;

public class Shifter : TorGameModifier
{
    public static Color color = Color.yellow;

    public static RoleInfo Info = new(color, RoleId.Shifter, isModifier: true);

    public static PlayerControl shifter;

    public static PlayerControl futureShift;
    public static PlayerControl currentTarget;

    public static bool shiftsMedicShield;

    public override RoleInfo TorInfo => Info;

    public override int GetAssignmentChance() =>
        ChanceOf(OptionGroupSingleton<ShifterOptions>.Instance.Shifter);

    public override int GetAmountPerGame() => 1;

    public override bool IsModifierValidOn(RoleBehaviour role) => IsCrewSeat(role) && role is not Spy;

    public override void OnActivate() => shifter = Player;

    public override void OnDeactivate()
    {
        if (shifter == Player) shifter = null;
    }

    public static void shiftRole(PlayerControl player1, PlayerControl player2, bool repeat = true)
    {
        if (Mayor.mayor != null && Mayor.mayor == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Mayor.mayor = player1;
        }
        else if (Portalmaker.portalmaker != null && Portalmaker.portalmaker == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Portalmaker.portalmaker = player1;
        }
        else if (Engineer.engineer != null && Engineer.engineer == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Engineer.engineer = player1;
        }
        else if (Sheriff.sheriff != null && Sheriff.sheriff == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            if (Sheriff.formerDeputy != null && Sheriff.formerDeputy == Sheriff.sheriff) Sheriff.formerDeputy = player1;
            Sheriff.sheriff = player1;
        }
        else if (Deputy.deputy != null && Deputy.deputy == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Deputy.deputy = player1;
        }
        else if (Lighter.lighter != null && Lighter.lighter == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Lighter.lighter = player1;
        }
        else if (Detective.detective != null && Detective.detective == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Detective.detective = player1;
        }
        else if (TimeMaster.timeMaster != null && TimeMaster.timeMaster == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            TimeMaster.timeMaster = player1;
        }
        else if (Medic.medic != null && Medic.medic == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Medic.medic = player1;
            if (Medic.shielded != null && Medic.shielded == player1 && shiftsMedicShield)
                Medic.shielded = player2;
        }
        else if (Swapper.swapper != null && Swapper.swapper == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Swapper.swapper = player1;
        }
        else if (Seer.seer != null && Seer.seer == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Seer.seer = player1;
        }
        else if (Hacker.hacker != null && Hacker.hacker == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Hacker.hacker = player1;
        }
        else if (Tracker.tracker != null && Tracker.tracker == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Tracker.tracker = player1;
        }
        else if (Snitch.snitch != null && Snitch.snitch == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Snitch.snitch = player1;
        }
        else if (Spy.spy != null && Spy.spy == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Spy.spy = player1;
        }
        else if (SecurityGuard.securityGuard != null && SecurityGuard.securityGuard == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            SecurityGuard.securityGuard = player1;
        }
        else if (Guesser.niceGuesser != null && Guesser.niceGuesser == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Guesser.niceGuesser = player1;
        }
        else if (Medium.medium != null && Medium.medium == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Medium.medium = player1;
        }
        else if (Pursuer.pursuer != null && Pursuer.pursuer == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Pursuer.pursuer = player1;
        }
        else if (Trapper.trapper != null && Trapper.trapper == player2)
        {
            if (repeat) shiftRole(player2, player1, false);
            Trapper.trapper = player1;
        }
    }

    public override void ClearAndReload() => clearAndReload();

    public static void clearAndReload()
    {
        shifter = null;
        currentTarget = null;
        futureShift = null;
        shiftsMedicShield = OptionGroupSingleton<ShifterOptions>.Instance.ShifterShiftsMedicShield.Value;
    }

    public override void FixedUpdate()
    {
        if (Player != PlayerControl.LocalPlayer) return;
        currentTarget = PlayerControlFixedUpdatePatch.setTarget();
        if (futureShift == null) PlayerControlFixedUpdatePatch.setPlayerOutline(currentTarget, Color.yellow);
    }
}

/// <summary>
/// The Shifter's "shift" button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>shifterShiftButton</c>.
/// </summary>
public sealed class ShifterShiftButton : TorButton
{
    private static ShifterShiftButton shifterShiftButton;

    public ShifterShiftButton()
    {
        shifterShiftButton = this;

        SetSprite(TorAssets.ShiftButton);
        PositionOffset = new Vector3(0, 1f, 0);
        Mirror = true;
        ShowButtonText = true;

        RealOnClick = () =>
        {
            PlayerControl.LocalPlayer.RpcSetFutureShifted(Shifter.currentTarget.PlayerId);
            SoundEffectsManager.play("shifterShift");
        };
        HasButton = () =>
        {
            return Shifter.shifter != null && Shifter.shifter == PlayerControl.LocalPlayer &&
                   Shifter.futureShift == null && !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () => { return Shifter.currentTarget && Shifter.futureShift == null && PlayerControl.LocalPlayer.CanMove; };
        OnMeetingEnds = () => { };
    }

    public override float Cooldown => 0f;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        ButtonText = new ButtonText(6,
            FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.UseButton]
                .FontMaterial);
    }
}

public static class ShifterRpcs
{
    [MethodRpc((uint)TorRpc.SetFutureShifted, LocalHandling = RpcLocalHandling.After)]
    public static void RpcSetFutureShifted(this PlayerControl player, byte playerId)
    {
        Shifter.futureShift = Helpers.playerById(playerId);
    }

    [MethodRpc((uint)TorRpc.ShifterShift, LocalHandling = RpcLocalHandling.After)]
    public static void RpcShifterShift(this PlayerControl player, byte targetId)
    {
        RPCProcedure.shifterShift(targetId);
    }
}