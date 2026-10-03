using System;
using MiraAPI.GameOptions.OptionTypes;
using UnityEngine;

namespace TheOtherRoles.Roles.Impostor;

public class Godfather(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = Palette.ImpostorRed;
    public static RoleInfo Info = new(color, RoleId.Godfather);

    public static PlayerControl godfather;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Godfather;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<MafiaOptions>.Instance.SpawnRate;

    // The family is three seats wide, so a game with fewer impostors can never fill it.
    public override bool CanSpawnOnCurrentMode() => TorSpawn.ImpostorSeats >= 3;

    public override bool? ForceShowRoleOnWiki => true;

    public static void clearAndReload()
    {
        godfather = null;
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;
}

/// <summary>
/// The settings of MafiaOptions. They live next to the role on purpose: the group is bound
/// to Godfather, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class MafiaOptions : TorRoleOptionGroup<Godfather>
{
    public override uint GroupPriority => 100;
    public override string GroupName => TorOptions.Title("Opt-Mafia,1");
    public override Color GroupColor => TorOptionColors.Group(Janitor.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption("Opt-Mafia,1", TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };
}
