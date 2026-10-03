using System.Collections.Generic;
using UnityEngine;

namespace TheOtherRoles.Roles.Modifier;

public class Vip : TorGameModifier
{
    public static Color color = Color.yellow;

    public static RoleInfo Info = new(color, RoleId.Vip, isModifier: true);

    public static List<PlayerControl> vip = new();
    public static bool showColor = true;

    public override RoleInfo TorInfo => Info;

    public override int GetAssignmentChance() =>
        ChanceOf(OptionGroupSingleton<VipOptions>.Instance.Vip);

    public override int GetAmountPerGame() =>
        OptionGroupSingleton<VipOptions>.Instance.VipQuantity.Quantity();

    public override void OnActivate() => vip.Add(Player);

    public override void OnDeactivate() => vip.Remove(Player);

    public override void ClearAndReload() => clearAndReload();

    public static void clearAndReload()
    {
        vip = new List<PlayerControl>();
        showColor = OptionGroupSingleton<VipOptions>.Instance.VipShowColor.Value;
    }
}
