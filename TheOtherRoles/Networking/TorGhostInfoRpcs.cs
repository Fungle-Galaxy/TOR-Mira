using MiraAPI.Hud;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheOtherRoles.Utilities;

namespace TheOtherRoles.Networking;

// The old ShareGhostInfo payload was variable length, so it is split into one typed RPC per info type.
// senderId is an explicit parameter because every sender wrote it itself (Medium writes the target id).
public static class TorGhostInfoRpcs
{
    [MethodRpc((uint)TorRpc.GhostHandcuffNoticed, LocalHandling = RpcLocalHandling.None)]
    public static void RpcGhostHandcuffNoticed(this PlayerControl player, byte senderId)
    {
        Deputy.setHandcuffedKnows(true, senderId);
    }

    [MethodRpc((uint)TorRpc.GhostHandcuffOver, LocalHandling = RpcLocalHandling.None)]
    public static void RpcGhostHandcuffOver(this PlayerControl player, byte senderId)
    {
        _ = Deputy.handcuffedKnows.Remove(senderId);
    }

    [MethodRpc((uint)TorRpc.GhostArsonistDouse, LocalHandling = RpcLocalHandling.None)]
    public static void RpcGhostArsonistDouse(this PlayerControl player, byte senderId, byte playerId)
    {
        Arsonist.dousedPlayers.Add(Helpers.playerById(playerId));
    }

    [MethodRpc((uint)TorRpc.GhostBountyTarget, LocalHandling = RpcLocalHandling.None)]
    public static void RpcGhostBountyTarget(this PlayerControl player, byte senderId, byte playerId)
    {
        BountyHunter.bounty = Helpers.playerById(playerId);
    }

    [MethodRpc((uint)TorRpc.GhostNinjaMarked, LocalHandling = RpcLocalHandling.None)]
    public static void RpcGhostNinjaMarked(this PlayerControl player, byte senderId, byte playerId)
    {
        Ninja.ninjaMarked = Helpers.playerById(playerId);
    }

    [MethodRpc((uint)TorRpc.GhostWarlockTarget, LocalHandling = RpcLocalHandling.None)]
    public static void RpcGhostWarlockTarget(this PlayerControl player, byte senderId, byte playerId)
    {
        Warlock.curseVictim = Helpers.playerById(playerId);
    }

    [MethodRpc((uint)TorRpc.GhostMediumInfo, LocalHandling = RpcLocalHandling.None)]
    public static void RpcGhostMediumInfo(this PlayerControl player, byte senderId, string info)
    {
        var sender = Helpers.playerById(senderId);
        if (Helpers.shouldShowGhostInfo())
            FastDestroyableSingleton<HudManager>.Instance.Chat.AddChat(sender, info);
    }

    [MethodRpc((uint)TorRpc.GhostDetectiveOrMedicInfo, LocalHandling = RpcLocalHandling.None)]
    public static void RpcGhostDetectiveOrMedicInfo(this PlayerControl player, byte senderId, string info)
    {
        var sender = Helpers.playerById(senderId);
        if (Helpers.shouldShowGhostInfo())
            FastDestroyableSingleton<HudManager>.Instance.Chat.AddChat(sender, info);
    }

    [MethodRpc((uint)TorRpc.GhostVampireTimer, LocalHandling = RpcLocalHandling.None)]
    public static void RpcGhostVampireTimer(this PlayerControl player, byte senderId, byte timer)
    {
        CustomButtonSingleton<VampireKillButton>.Instance.Timer = timer;
    }

    [MethodRpc((uint)TorRpc.GhostDeathReasonAndKiller, LocalHandling = RpcLocalHandling.None)]
    public static void RpcGhostDeathReasonAndKiller(this PlayerControl player, byte senderId, byte playerId,
        byte deathReason, byte killerId)
    {
        GameHistory.overrideDeathReasonAndKiller(Helpers.playerById(playerId),
            (DeadPlayer.CustomDeathReason)deathReason, Helpers.playerById(killerId));
    }
}
