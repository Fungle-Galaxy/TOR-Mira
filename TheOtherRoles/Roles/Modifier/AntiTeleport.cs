using System.Collections.Generic;
using UnityEngine;

namespace TheOtherRoles.Roles.Modifier;

public class AntiTeleport : TorGameModifier
{
    public static Color color = Color.yellow;

    public static RoleInfo Info = new(color, RoleId.AntiTeleport, isModifier: true);

    public static List<PlayerControl> antiTeleport = new();
    public static Vector3 position;

    public override RoleInfo TorInfo => Info;

    public override int GetAssignmentChance() =>
        ChanceOf(OptionGroupSingleton<AntiTeleportOptions>.Instance.AntiTeleport);

    public override int GetAmountPerGame() =>
        OptionGroupSingleton<AntiTeleportOptions>.Instance.AntiTeleportQuantity.Quantity();

    public override void OnActivate() => antiTeleport.Add(Player);

    public override void OnDeactivate() => antiTeleport.Remove(Player);

    public override void ClearAndReload() => clearAndReload();

    public static void clearAndReload()
    {
        antiTeleport = new List<PlayerControl>();
        position = Vector3.zero;
    }

    public static void setPosition()
    {
        if (position == Vector3.zero) return;
        if (antiTeleport.FindAll(x => x.PlayerId == PlayerControl.LocalPlayer.PlayerId).Count > 0)
        {
            PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(position);
            if (SubmergedCompatibility.IsSubmerged) SubmergedCompatibility.ChangeFloor(position.y > -7);
        }
    }
}
