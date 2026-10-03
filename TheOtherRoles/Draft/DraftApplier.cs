using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using TheOtherRoles.Networking;
using TheOtherRoles.Roles;

namespace TheOtherRoles.Draft;

/// <summary>
/// Hands the drafted seats out once Mira API has run its own roll - the roll is only there to put
/// vanilla crewmate/impostor bases under everyone, every seat gets overwritten here.
/// </summary>
public static class DraftApplier
{
    public static void Apply()
    {
        var states = DraftManager.SlotStates.Where(connected).ToList();
        if (states.Count == 0) return;

        var host = AmongUsClient.Instance.AmHost;
        var plainCard = (byte)RoleId.Impostor;
        var customImpostors = states.Count(x => x.ChosenRoleId != 0 && x.ChosenRoleId != plainCard &&
                                                 isImpostorRole(x.ChosenRoleId));
        var plainPicked = states.Count(x => x.ChosenRoleId == plainCard);
        var plainNeeded = System.Math.Max(0, TorSpawn.ImpostorSeats - customImpostors - plainPicked);

        // Seats that never picked can become plain impostors without anyone losing their draft. A
        // drafted card is only given up when the game would otherwise start without a single
        // killer - a lobby that already has one starts with fewer impostors than it asked for
        // instead of silently swapping a crew card for the impostor one.
        var freeSeats = states.Count(x => x.ChosenRoleId == 0);
        var takes = customImpostors + plainPicked > 0
            ? System.Math.Min(plainNeeded, freeSeats)
            : plainNeeded;

        var plainImpostors = new List<byte>();
        foreach (var state in states
                     .Where(x => x.ChosenRoleId == 0 || !isImpostorRole(x.ChosenRoleId))
                     .OrderBy(x => seatRank(x.ChosenRoleId))
                     .ThenBy(_ => Helpers.rnd.Next()))
        {
            if (takes <= 0) break;
            plainImpostors.Add(state.PlayerId);
            takes--;
        }

        foreach (var state in states)
        {
            var player = Helpers.playerById(state.PlayerId);
            if (player == null) continue;

            // A plain impostor seat - either the card that was drafted or one the lobby's own
            // impostor count still owed - outranks whatever else was drafted: without it a game
            // where nobody took an impostor card would start with no killer at all.
            if (state.ChosenRoleId == plainCard || plainImpostors.Contains(state.PlayerId))
            {
                RPCProcedure.clearAssignedRole(player);
                setPlainRole(player, RoleTypes.Impostor, host);
                continue;
            }

            if (state.ChosenRoleId != 0)
            {
                // The host broadcasts it, a client just lays the same result over its own roll -
                // waiting for the host's message would leave the intro to show the rolled faction.
                if (host) PlayerControl.LocalPlayer.RpcSetRole(state.ChosenRoleId, state.PlayerId);
                else RPCProcedure.setRole(state.ChosenRoleId, state.PlayerId);
                continue;
            }

            RPCProcedure.clearAssignedRole(player);
            if (player.Data.RoleType != RoleTypes.Crewmate)
                setPlainRole(player, RoleTypes.Crewmate, host);
        }
    }

    // The vanilla RPC only reaches the other clients, the host's own RoleType follows here - the
    // same pattern the vent restoring conversion uses. A client only sets it locally, the host's
    // broadcast confirms the very same role a moment later.
    private static void setPlainRole(PlayerControl player, RoleTypes role, bool sendRpc)
    {
        if (sendRpc) player.RpcSetRole(role);
        if (player.Data.RoleType == role) return;
        RoleManager.Instance.SetRole(player, role);
    }

    private static bool connected(DraftSlotState state)
    {
        var player = Helpers.playerById(state.PlayerId);
        return player != null && player.Data != null && !player.Data.Disconnected;
    }

    private static bool isImpostorRole(byte roleId)
    {
        if (!RoleInfo.roleInfoById.TryGetValue((RoleId)roleId, out var info)) return false;
        return info.isImpostor && !info.isNeutral;
    }

    // Seats without a role take an impostor first, then the plain crew roles, and only then the
    // neutrals - everyone who drafted a win condition of their own keeps it as long as possible.
    private static int seatRank(byte roleId)
    {
        if (roleId == 0) return 0;
        if (!RoleInfo.roleInfoById.TryGetValue((RoleId)roleId, out var info)) return 1;
        return info.isNeutral ? 2 : 1;
    }
}
