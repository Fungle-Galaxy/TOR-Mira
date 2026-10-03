using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TheOtherRoles.Draft;

public class DraftSlotState
{
    public byte PlayerId;
    public int SlotNumber;
    public bool HasPicked;
    public byte ChosenRoleId;
    public bool Forced;
    public int PendingPick = -1;
}

public static class DraftManager
{
    public static bool IsDraftActive;
    public static bool HasResults;
    public static float TurnDuration = 10f;
    public static float TurnEndsAt;
    public static int CurrentSlot = -1;
    public static readonly List<DraftSlotState> SlotStates = new();
    public static readonly List<byte> CurrentOffers = new();
    public static readonly List<string> Feed = new();

    public static float TimeLeft => Mathf.Max(0f, TurnEndsAt - Time.time);

    public static DraftSlotState GetStateForSlot(int slot) => SlotStates.FirstOrDefault(x => x.SlotNumber == slot);

    public static DraftSlotState GetStateForPlayer(byte playerId) =>
        SlotStates.FirstOrDefault(x => x.PlayerId == playerId);

    public static int GetSlotForPlayer(byte playerId) => GetStateForPlayer(playerId)?.SlotNumber ?? 0;

    public static bool PickedRole(RoleId roleId) =>
        SlotStates.Any(x => x.HasPicked && x.ChosenRoleId == (byte)roleId);

    public static void AddFeed(byte slot, byte roleId)
    {
        var state = GetStateForSlot(slot);
        var player = state == null ? null : Helpers.playerById(state.PlayerId);
        var name = player != null && player.Data != null ? player.Data.PlayerName : "?";

        var role = roleId == 0
            ? "—"
            : TorOptions.RoleName((RoleId)roleId);
        if (roleId != 0 && RoleInfo.roleInfoById.TryGetValue((RoleId)roleId, out var info))
            role = Helpers.cs(info.color, role);

        Feed.Add($"{slot}  {name} → <b>{role}</b>");
        if (Feed.Count > 8) Feed.RemoveAt(0);
    }

    public static void Reset()
    {
        IsDraftActive = false;
        HasResults = false;
        SlotStates.Clear();
        CurrentOffers.Clear();
        Feed.Clear();
        CurrentSlot = -1;
        TurnEndsAt = 0f;
    }
}
