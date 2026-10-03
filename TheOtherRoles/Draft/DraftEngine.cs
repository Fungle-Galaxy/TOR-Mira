using System.Collections.Generic;
using System.Linq;
using TheOtherRoles.Options;
using UnityEngine;

namespace TheOtherRoles.Draft;

/// <summary>
/// The host side of the draft. Seats are shuffled once, then every turn offers a handful of roles
/// out of the pool; the pool only shrinks, so a pick is final. Runs off the engine behaviour's
/// Update rather than a coroutine, which keeps it independent of any lobby HUD object.
/// </summary>
public static class DraftEngine
{
    public static bool SkipNextStartIntercept;
    public static bool Running;

    private static readonly List<int> order = new();
    private static readonly List<int> deferred = new();
    private static int turnIndex;
    private static float deadline;
    private static float nextTurnAt;
    private static bool awaiting;
    private static bool finished;

    private static int OfferCount =>
        (int)OptionGroupSingleton<DraftOptions>.Instance.OffersCount.Value;

    public static bool TryStartDraft(out string reason)
    {
        reason = null;
        var players = PlayerControl.AllPlayerControls.ToArray()
            .Where(x => x != null && x.Data != null && !x.Data.Disconnected && !x.isDummy)
            .OrderBy(_ => Helpers.rnd.Next())
            .ToList();
        if (players.Count == 0)
        {
            reason = "no players";
            return false;
        }

        if (!DraftPool.Build())
        {
            reason = "empty role pool";
            return false;
        }

        DraftManager.Reset();
        for (var i = 0; i < players.Count; i++)
            DraftManager.SlotStates.Add(new DraftSlotState
            {
                PlayerId = players[i].PlayerId,
                SlotNumber = i + 1
            });

        DraftManager.IsDraftActive = true;
        DraftManager.TurnDuration = OptionGroupSingleton<DraftOptions>.Instance.TurnSeconds.Value;
        DraftManager.CurrentSlot = -1;

        order.Clear();
        order.AddRange(DraftManager.SlotStates.Select(x => x.SlotNumber));
        deferred.Clear();
        turnIndex = 0;
        deadline = 0f;
        nextTurnAt = Time.time + 3.5f;
        awaiting = false;
        finished = false;
        Running = true;

        DraftRpcs.BroadcastStart();
        DraftEngineBehaviour.Ensure();
        return true;
    }

    public static void Tick()
    {
        if (!Running || finished) return;
        if (GameStartManager.Instance == null)
        {
            Cancel();
            return;
        }

        var now = Time.time;

        if (awaiting)
        {
            resolveTurn(now);
            return;
        }

        if (now < nextTurnAt) return;
        if (!BeginNextTurn()) FinishDraft();
    }

    public static void SubmitPick(byte playerId, byte index)
    {
        if (!awaiting) return;
        var state = DraftManager.GetStateForPlayer(playerId);
        if (state == null || state.HasPicked || state.SlotNumber != DraftManager.CurrentSlot) return;
        state.PendingPick = index;
    }

    private static void resolveTurn(float now)
    {
        var state = DraftManager.GetStateForSlot(DraftManager.CurrentSlot);
        if (state == null)
        {
            awaiting = false;
            nextTurnAt = now;
            return;
        }

        var player = Helpers.playerById(state.PlayerId);
        var gone = player == null || player.Data == null || player.Data.Disconnected;
        var pick = state.PendingPick;
        if (pick < 0 && !gone && now < deadline) return;

        var offers = DraftManager.CurrentOffers;
        DraftSeat seat = null;
        if (pick >= 0 && pick < offers.Count)
            seat = DraftPool.Seats.FirstOrDefault(x => !x.Taken && x.RoleId == (RoleId)offers[pick]);
        if (seat == null)
            seat = DraftPool.PickWeighted(state);

        ApplyPick(state, seat);
        awaiting = false;
        nextTurnAt = now + 0.35f;
    }

    private static bool BeginNextTurn()
    {
        while (true)
        {
            while (turnIndex < order.Count)
            {
                var state = DraftManager.GetStateForSlot(order[turnIndex++]);
                if (state == null || state.HasPicked) continue;

                var offers = DraftPool.BuildOffers(state, OfferCount);
                if (offers.Count == 0)
                {
                    // Nothing this player can take yet - a later pass may unlock it (Deputy behind
                    // a Sheriff pick, the Mafia family behind two free slots).
                    deferred.Add(state.SlotNumber);
                    continue;
                }

                DraftManager.CurrentSlot = state.SlotNumber;
                DraftManager.CurrentOffers.Clear();
                DraftManager.CurrentOffers.AddRange(offers.Select(x => (byte)x.RoleId));
                deadline = Time.time + DraftManager.TurnDuration;
                DraftManager.TurnEndsAt = deadline;
                awaiting = true;
                DraftRpcs.BroadcastTurn(state.SlotNumber, state.PlayerId);
                return true;
            }

            if (deferred.Count == 0 || deferred.Count >= order.Count) return false;
            order.Clear();
            order.AddRange(deferred);
            deferred.Clear();
            turnIndex = 0;
        }
    }

    private static void ApplyPick(DraftSlotState state, DraftSeat seat)
    {
        if (seat != null)
        {
            seat.Taken = true;
            state.ChosenRoleId = (byte)seat.RoleId;
        }
        else
        {
            state.ChosenRoleId = 0;
        }

        state.HasPicked = true;
        state.PendingPick = -1;
        DraftRpcs.BroadcastPickConfirmed((byte)state.SlotNumber, state.ChosenRoleId);

        if (seat == null || seat.Group < 0) return;
        foreach (var rest in DraftPool.Seats.Where(x => x.Group == seat.Group && !x.Taken))
        {
            rest.Taken = true;
            var target = DraftManager.SlotStates.Where(x => !x.HasPicked)
                .OrderBy(_ => Helpers.rnd.Next()).FirstOrDefault();
            if (target == null) continue;

            target.ChosenRoleId = (byte)rest.RoleId;
            target.HasPicked = true;
            target.Forced = true;
            DraftRpcs.BroadcastPickConfirmed((byte)target.SlotNumber, target.ChosenRoleId);
        }
    }

    private static void FinishDraft()
    {
        finished = true;
        awaiting = false;
        Running = false;

        foreach (var state in DraftManager.SlotStates.Where(x => !x.HasPicked))
        {
            state.HasPicked = true;
            state.ChosenRoleId = 0;
            DraftRpcs.BroadcastPickConfirmed((byte)state.SlotNumber, 0);
        }

        DraftManager.IsDraftActive = false;
        DraftManager.HasResults = true;
        DraftRpcs.BroadcastEnd();

        var startManager = GameStartManager.Instance;
        if (startManager == null)
        {
            SkipNextStartIntercept = false;
            return;
        }

        // Vanilla BeginGame() stops at the minimum player popup when PlayerCount == MinPlayers
        // (or shakes the counter below it) instead of starting, so drop the limit for that one
        // call and put it straight back.
        SkipNextStartIntercept = true;
        startManager.gameObject.SetActive(true);
        var minPlayers = startManager.MinPlayers;
        try
        {
            startManager.MinPlayers = 0;
            startManager.BeginGame();
        }
        finally
        {
            startManager.MinPlayers = minPlayers;
        }

        startManager.countDownTimer = 0f;
    }

    public static void Cancel()
    {
        Running = false;
        awaiting = false;
        finished = true;
        DraftManager.IsDraftActive = false;
        DraftSelectionScreen.Hide();
        DraftStatusOverlay.Hide();
        DraftManager.Reset();
        DraftChat.OnDraftEnd();
        SoundEffectsManager.stop("draft");
        DraftRpcs.BroadcastCancel();
    }
}
