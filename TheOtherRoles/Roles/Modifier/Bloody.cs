using System.Collections.Generic;
using System.Linq;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheOtherRoles.Objects;
using UnityEngine;

namespace TheOtherRoles.Roles.Modifier;

public class Bloody : TorGameModifier
{
    public static Color color = Color.yellow;

    public static RoleInfo Info = new(color, RoleId.Bloody, isModifier: true);

    public static List<PlayerControl> bloody = new();
    public static Dictionary<byte, float> active = new();
    public static Dictionary<byte, byte> bloodyKillerMap = new();

    public static float duration = 5f;

    public override RoleInfo TorInfo => Info;

    public override int GetAssignmentChance() =>
        ChanceOf(OptionGroupSingleton<BloodyOptions>.Instance.Bloody);

    public override int GetAmountPerGame() =>
        OptionGroupSingleton<BloodyOptions>.Instance.BloodyQuantity.Quantity();

    public override void OnActivate() => bloody.Add(Player);

    public override void OnDeactivate() => bloody.Remove(Player);

    public override void ClearAndReload() => clearAndReload();

    public static void clearAndReload()
    {
        bloody = new List<PlayerControl>();
        active = new Dictionary<byte, float>();
        bloodyKillerMap = new Dictionary<byte, byte>();
        duration = OptionGroupSingleton<BloodyOptions>.Instance.BloodyDuration.Value;
    }

    public static void tick()
    {
        if (!active.Any()) return;
        foreach (var entry in new Dictionary<byte, float>(active))
        {
            var p = Helpers.playerById(entry.Key);
            var bloodyPlayer = Helpers.playerById(bloodyKillerMap[p.PlayerId]);
            active[entry.Key] = entry.Value - Time.fixedDeltaTime;
            if (entry.Value <= 0 || p.Data.IsDead)
            {
                active.Remove(entry.Key);
                continue;
            }
            new Bloodytrail(p, bloodyPlayer);
        }
    }
}

public static class BloodyRpcs
{
    [MethodRpc((uint)TorRpc.Bloody, LocalHandling = RpcLocalHandling.After)]
    public static void RpcBloody(this PlayerControl player, byte killerPlayerId, byte bloodyPlayerId)
    {
        if (Bloody.active.ContainsKey(killerPlayerId)) return;
        Bloody.active.Add(killerPlayerId, Bloody.duration);
        Bloody.bloodyKillerMap.Add(killerPlayerId, bloodyPlayerId);
    }
}
