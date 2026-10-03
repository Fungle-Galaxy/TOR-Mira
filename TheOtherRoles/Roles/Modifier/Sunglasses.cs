using System.Collections.Generic;
using UnityEngine;

namespace TheOtherRoles.Roles.Modifier;

public class Sunglasses : TorGameModifier
{
    public static Color color = Color.yellow;

    public static RoleInfo Info = new(color, RoleId.Sunglasses, isModifier: true);

    public static List<PlayerControl> sunglasses = new();
    public static int vision = 1;

    public override RoleInfo TorInfo => Info;

    public override int GetAssignmentChance() =>
        ChanceOf(OptionGroupSingleton<SunglassesOptions>.Instance.Sunglasses);

    public override int GetAmountPerGame() =>
        OptionGroupSingleton<SunglassesOptions>.Instance.SunglassesQuantity.Quantity();

    public override bool IsModifierValidOn(RoleBehaviour role) => IsCrewSeat(role);

    public override void OnActivate() => sunglasses.Add(Player);

    public override void OnDeactivate() => sunglasses.Remove(Player);

    public override void ClearAndReload() => clearAndReload();

    public static void clearAndReload()
    {
        sunglasses = new List<PlayerControl>();
        vision = OptionGroupSingleton<SunglassesOptions>.Instance.SunglassesVision.Selection() + 1;
    }
}
