using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace TheOtherRoles.Roles.Modifier;

public class Tiebreaker : TorGameModifier
{
    public static Color color = Color.yellow;

    public static RoleInfo Info = new(color, RoleId.Tiebreaker, isModifier: true);

    public static PlayerControl tiebreaker;

    public static bool isTiebreak;

    public override RoleInfo TorInfo => Info;

    public override int GetAssignmentChance() =>
        ChanceOf(OptionGroupSingleton<TiebreakerOptions>.Instance.TieBreaker);

    public override int GetAmountPerGame() => 1;

    public override void OnActivate() => tiebreaker = Player;

    public override void OnDeactivate()
    {
        if (tiebreaker == Player) tiebreaker = null;
    }

    public override void ClearAndReload() => clearAndReload();

    public static void clearAndReload()
    {
        tiebreaker = null;
        isTiebreak = false;
    }
}

public static class TiebreakerRpcs
{
    [MethodRpc((uint)TorRpc.SetTiebreak, LocalHandling = RpcLocalHandling.After)]
    public static void RpcSetTiebreak(this PlayerControl player)
    {
        Tiebreaker.isTiebreak = true;
    }
}
