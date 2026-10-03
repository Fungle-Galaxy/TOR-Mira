using System.Collections.Generic;
using UnityEngine;

namespace TheOtherRoles.Roles.Modifier;

public class Invert : TorGameModifier
{
    public static Color color = Color.yellow;

    public static RoleInfo Info = new(color, RoleId.Invert, isModifier: true);

    public static List<PlayerControl> invert = new();
    public static int meetings = 3;

    public override RoleInfo TorInfo => Info;

    public override int GetAssignmentChance() =>
        ChanceOf(OptionGroupSingleton<InvertOptions>.Instance.Invert);

    public override int GetAmountPerGame() =>
        OptionGroupSingleton<InvertOptions>.Instance.InvertQuantity.Quantity();

    public override void OnActivate() => invert.Add(Player);

    public override void OnDeactivate() => invert.Remove(Player);

    public override void ClearAndReload() => clearAndReload();

    public static void clearAndReload()
    {
        invert = new List<PlayerControl>();
        meetings = (int)OptionGroupSingleton<InvertOptions>.Instance.InvertDuration.Value;
    }
}
