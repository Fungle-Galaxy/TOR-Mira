using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace TheOtherRoles.Roles.Modifier;

public class Armored : TorGameModifier
{
    public static Color color = Color.yellow;

    public static RoleInfo Info = new(color, RoleId.Armored, isModifier: true);

    public static PlayerControl armored;

    public static bool isBrokenArmor;

    public override RoleInfo TorInfo => Info;

    public override int GetAssignmentChance() =>
        ChanceOf(OptionGroupSingleton<ArmoredOptions>.Instance.Armored);

    public override int GetAmountPerGame() => 1;

    public override void OnActivate() => armored = Player;

    public override void OnDeactivate()
    {
        if (armored == Player) armored = null;
    }

    public override void ClearAndReload() => clearAndReload();

    public static void clearAndReload()
    {
        armored = null;
        isBrokenArmor = false;
    }
}

public static class ArmoredRpcs
{
    [MethodRpc((uint)TorRpc.BreakArmor, LocalHandling = RpcLocalHandling.After)]
    public static void RpcBreakArmor(this PlayerControl player)
    {
        if (Armored.armored == null || Armored.isBrokenArmor) return;
        Armored.isBrokenArmor = true;
        if (PlayerControl.LocalPlayer.Data.IsDead) Armored.armored.ShowFailedMurder();
    }
}
