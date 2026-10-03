using System.Collections.Generic;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace TheOtherRoles.CustomGameModes;

internal class GuesserGM
{
    // Guesser Gamemode
    public static List<GuesserGM> guessers = new();
    public static Color color = new Color32(255, 255, 0, byte.MaxValue);

    public PlayerControl guesser;
    public int shots = Mathf.RoundToInt(OptionGroupSingleton<GuesserSettingsOptions>.Instance.NumberOfShots.Value);
    public int tasksToUnlock = Mathf.RoundToInt(OptionGroupSingleton<GuesserSettingsOptions>.Instance.CrewGuesserNumberOfTasks.Value);

    public GuesserGM(PlayerControl player)
    {
        guesser = player;
        guessers.Add(this);
    }

    public static int remainingShots(byte playerId, bool shoot = false)
    {
        var g = guessers.FindLast(x => x.guesser.PlayerId == playerId);
        if (g == null) return 0;
        if (shoot) g.shots--;
        return g.shots;
    }

    public static void clear(byte playerId)
    {
        var g = guessers.FindLast(x => x.guesser.PlayerId == playerId);
        if (g == null) return;
        g.guesser = null;
        g.shots = Mathf.RoundToInt(OptionGroupSingleton<GuesserSettingsOptions>.Instance.NumberOfShots.Value);
        g.tasksToUnlock = Mathf.RoundToInt(OptionGroupSingleton<GuesserSettingsOptions>.Instance.CrewGuesserNumberOfTasks.Value);

        guessers.Remove(g);
    }

    public static void clearAndReload()
    {
        guessers = new List<GuesserGM>();
    }

    public static bool isGuesser(byte playerId)
    {
        return guessers.FindAll(x => x.guesser.PlayerId == playerId).Count > 0;
    }
}

public static class GuesserGmRpcs
{
    [MethodRpc((uint)TorRpc.SetGuesserGm, LocalHandling = RpcLocalHandling.After)]
    public static void RpcSetGuesserGm(this PlayerControl player, byte playerId)
    {
        RPCProcedure.setGuesserGm(playerId);
    }
}