using System;
using Hazel;
using InnerNet;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheOtherRoles.Objects;
using TheOtherRoles.Patches;

namespace TheOtherRoles.Networking;

public static class TorControlRpcs
{
    [MethodRpc((uint)TorRpc.ResetVaribles, LocalHandling = RpcLocalHandling.After)]
    public static void RpcResetVaribles(this PlayerControl player)
    {
        RPCProcedure.resetVariables();
    }

    [MethodRpc((uint)TorRpc.ForceEnd, LocalHandling = RpcLocalHandling.After)]
    public static void RpcForceEnd(this PlayerControl player)
    {
        RPCProcedure.forceEnd();
    }

    [MethodRpc((uint)TorRpc.SetRole, LocalHandling = RpcLocalHandling.After)]
    public static void RpcSetRole(this PlayerControl player, byte roleId, byte playerId)
    {
        RPCProcedure.setRole(roleId, playerId);
    }

    [MethodRpc((uint)TorRpc.UseUncheckedVent, LocalHandling = RpcLocalHandling.After)]
    public static void RpcUseUncheckedVent(this PlayerControl player, int ventId, byte playerId, byte isEnter)
    {
        var target = Helpers.playerById(playerId);
        if (target == null) return;
        // Fill dummy MessageReader and call MyPhysics.HandleRpc as the corountines cannot be accessed
        var reader = new MessageReader();
        var bytes = BitConverter.GetBytes(ventId);
        if (!BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        reader.Buffer = bytes;
        reader.Length = bytes.Length;

        JackInTheBox.startAnimation(ventId);
        target.MyPhysics.HandleRpc(isEnter != 0 ? (byte)19 : (byte)20, reader);
    }

    [MethodRpc((uint)TorRpc.UncheckedMurderPlayer, LocalHandling = RpcLocalHandling.After)]
    public static void RpcUncheckedMurderPlayer(this PlayerControl player, byte sourceId, byte targetId,
        byte showAnimation)
    {
        if (AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started) return;
        var source = Helpers.playerById(sourceId);
        var target = Helpers.playerById(targetId);
        if (source != null && target != null)
        {
            if (showAnimation == 0) KillAnimationCoPerformKillPatch.hideNextAnimation = true;
            source.MurderPlayer(target);
        }
    }

    [MethodRpc((uint)TorRpc.UncheckedCmdReportDeadBody, LocalHandling = RpcLocalHandling.Before)]
    public static void RpcUncheckedCmdReportDeadBody(this PlayerControl player, byte sourceId, byte targetId)
    {
        var source = Helpers.playerById(sourceId);
        var t = targetId == byte.MaxValue ? null : Helpers.playerById(targetId).Data;
        if (source != null) source.ReportDeadBody(t);
    }

    [MethodRpc((uint)TorRpc.UncheckedExilePlayer, LocalHandling = RpcLocalHandling.After)]
    public static void RpcUncheckedExilePlayer(this PlayerControl player, byte targetId)
    {
        var target = Helpers.playerById(targetId);
        if (target != null) target.Exiled();
    }

    [MethodRpc((uint)TorRpc.DynamicMapOption, LocalHandling = RpcLocalHandling.After)]
    public static void RpcDynamicMapOption(this PlayerControl player, byte mapId)
    {
        GameOptionsManager.Instance.currentNormalGameOptions.MapId = mapId;
    }

    [MethodRpc((uint)TorRpc.SetGameStarting, LocalHandling = RpcLocalHandling.After)]
    public static void RpcSetGameStarting(this PlayerControl player)
    {
        GameStartManagerPatch.GameStartManagerUpdatePatch.startingTimer = 5f;
    }

    [MethodRpc((uint)TorRpc.StopStart, LocalHandling = RpcLocalHandling.None)]
    public static void RpcStopStart(this PlayerControl player, byte playerId)
    {
        if (!OptionGroupSingleton<GameSettingsOptions>.Instance.AnyPlayerCanStopStart.Value)
            return;
        SoundManager.Instance.StopSound(GameStartManager.Instance.gameStartSound);
        if (AmongUsClient.Instance.AmHost)
        {
            GameStartManager.Instance.ResetStartState();
            HudManager.Instance.Notifier.AddDisconnectMessage(string.Format(ModTranslation.GetString("Game-Normal", 1),
                Helpers.playerById(playerId).Data.PlayerName));
        }
    }

    [MethodRpc((uint)TorRpc.SetFirstKill, LocalHandling = RpcLocalHandling.After)]
    public static void RpcSetFirstKill(this PlayerControl player, byte playerId)
    {
        var target = Helpers.playerById(playerId);
        if (target == null) return;
        TORMapOptions.firstKillPlayer = target;
    }
}
