using System;
using MiraAPI.GameOptions.OptionTypes;
using UnityEngine;

namespace TheOtherRoles.Roles.Impostor;

public class Mafioso(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = Palette.ImpostorRed;

    public static RoleInfo Info = new(color, RoleId.Mafioso);

    public static PlayerControl mafioso;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Mafioso;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<MafiaOptions>.Instance.SpawnRate;

    [HideFromIl2Cpp]
    protected override bool ChanceControlledByAnotherRole => true;

    // Mira API must not roll the family piecemeal: TOR only ever hands out the Mafioso when a
    // Godfather is already sitting at the table.
    public override bool CanSpawnOnCurrentMode() => false;

    public override bool? ForceShowRoleOnWiki => true;

    public static void clearAndReload()
    {
        mafioso = null;
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;
}