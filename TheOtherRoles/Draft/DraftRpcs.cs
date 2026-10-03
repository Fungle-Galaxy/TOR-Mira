using System.Linq;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace TheOtherRoles.Draft;

public static class DraftRpcs
{
    public static void BroadcastStart()
    {
        var ids = DraftManager.SlotStates.Select(x => x.PlayerId).ToArray();
        var slots = DraftManager.SlotStates.Select(x => (byte)x.SlotNumber).ToArray();
        PlayerControl.LocalPlayer.RpcDraftStart(ids, slots, DraftManager.TurnDuration);
    }

    public static void BroadcastTurn(int slot, byte pickerId)
    {
        PlayerControl.LocalPlayer.RpcDraftTurn((byte)slot, pickerId, DraftManager.CurrentOffers.ToArray(),
            DraftManager.TurnDuration);
    }

    public static void BroadcastPickConfirmed(byte slot, byte roleId) =>
        PlayerControl.LocalPlayer.RpcDraftPickConfirmed(slot, roleId);

    public static void BroadcastEnd() => PlayerControl.LocalPlayer.RpcDraftEnd();

    public static void BroadcastCancel() => PlayerControl.LocalPlayer.RpcDraftCancel();

    public static void SendPick(byte index)
    {
        if (AmongUsClient.Instance.AmHost) DraftEngine.SubmitPick(PlayerControl.LocalPlayer.PlayerId, index);
        else PlayerControl.LocalPlayer.RpcDraftSubmitPick(index);
    }

    [MethodRpc((uint)TorRpc.DraftStart, LocalHandling = RpcLocalHandling.After)]
    public static void RpcDraftStart(this PlayerControl player, byte[] playerIds, byte[] slotNumbers, float duration)
    {
        DraftManager.Reset();
        DraftManager.IsDraftActive = true;
        DraftManager.TurnDuration = duration;
        for (var i = 0; i < playerIds.Length && i < slotNumbers.Length; i++)
            DraftManager.SlotStates.Add(new DraftSlotState
            {
                PlayerId = playerIds[i],
                SlotNumber = slotNumbers[i]
            });

        DraftEngineBehaviour.Ensure();
        DraftStatusOverlay.Show();
        DraftChat.OnDraftStart();
        SoundEffectsManager.play("draft", 1f, true, true);
    }

    [MethodRpc((uint)TorRpc.DraftTurn, LocalHandling = RpcLocalHandling.After)]
    public static void RpcDraftTurn(this PlayerControl player, byte slot, byte pickerId, byte[] roleIds,
        float duration)
    {
        DraftManager.CurrentSlot = slot;
        DraftManager.CurrentOffers.Clear();
        DraftManager.CurrentOffers.AddRange(roleIds);
        DraftManager.TurnDuration = duration;
        DraftManager.TurnEndsAt = Time.time + duration;

        var local = PlayerControl.LocalPlayer;
        if (local && local.PlayerId == pickerId) DraftSelectionScreen.Show(slot, roleIds);
        else DraftStatusOverlay.SetWaiting();
        DraftStatusOverlay.Refresh();
    }

    [MethodRpc((uint)TorRpc.DraftSubmitPick, LocalHandling = RpcLocalHandling.After)]
    public static void RpcDraftSubmitPick(this PlayerControl player, byte index)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        DraftEngine.SubmitPick(player.PlayerId, index);
    }

    [MethodRpc((uint)TorRpc.DraftPickConfirmed, LocalHandling = RpcLocalHandling.After)]
    public static void RpcDraftPickConfirmed(this PlayerControl player, byte slot, byte roleId)
    {
        var state = DraftManager.GetStateForSlot(slot);
        if (state != null)
        {
            state.HasPicked = true;
            state.ChosenRoleId = roleId;
            state.PendingPick = -1;
        }

        DraftManager.AddFeed(slot, roleId);
        SoundEffectsManager.play("select");
        DraftSelectionScreen.OnPickConfirmed(state);
        DraftStatusOverlay.Refresh();
    }

    [MethodRpc((uint)TorRpc.DraftCancel, LocalHandling = RpcLocalHandling.After)]
    public static void RpcDraftCancel(this PlayerControl player)
    {
        DraftSelectionScreen.Hide();
        DraftStatusOverlay.Hide();
        DraftManager.Reset();
        DraftChat.OnDraftEnd();
        SoundEffectsManager.stop("draft");
    }

    [MethodRpc((uint)TorRpc.DraftEnd, LocalHandling = RpcLocalHandling.After)]
    public static void RpcDraftEnd(this PlayerControl player)
    {
        DraftManager.IsDraftActive = false;
        DraftManager.HasResults = true;
        DraftSelectionScreen.Hide();
        DraftStatusOverlay.Hide();
        DraftChat.OnDraftEnd();
        SoundEffectsManager.stop("draft");
    }
}
