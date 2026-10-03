using System;
using MiraAPI.GameOptions.OptionTypes;
using UnityEngine;

namespace TheOtherRoles.Roles.Impostor;

public class Spy(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = Palette.ImpostorRed;

    public static RoleInfo Info = new(color, RoleId.Spy);

    public static PlayerControl spy;
    public static bool impostorsCanKillAnyone = true;
    public static bool canEnterVents;
    public static bool hasImpostorVision;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Spy;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<SpyOptions>.Instance.SpawnRate;

    // A Spy has nothing to blend in with in a single impostor game.
    public override bool CanSpawnOnCurrentMode() => TorSpawn.ImpostorSeats > 1;

    public static void clearAndReload()
    {
        spy = null;
        impostorsCanKillAnyone = OptionGroupSingleton<SpyOptions>.Instance.ImpostorsCanKillAnyone.Value;
        canEnterVents = OptionGroupSingleton<SpyOptions>.Instance.CanEnterVents.Value;
        hasImpostorVision = OptionGroupSingleton<SpyOptions>.Instance.HasImpostorVision.Value;
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;
}

/// <summary>
/// The settings of SpyOptions. They live next to the role on purpose: the group is bound
/// to Spy, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class SpyOptions : TorRoleOptionGroup<Spy>
{
    public override uint GroupPriority => 520;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Spy));
    public override Color GroupColor => TorOptionColors.Group(Spy.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.Spy), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedToggleOption CanDieToSheriff { get; } =
        new ModdedToggleOption("Opt-Spy,1", false);

    public ModdedToggleOption ImpostorsCanKillAnyone { get; } =
        new ModdedToggleOption("Opt-Spy,2", true);

    public ModdedToggleOption CanEnterVents { get; } =
        new ModdedToggleOption("Opt-Spy,3", false);

    public ModdedToggleOption HasImpostorVision { get; } =
        new ModdedToggleOption("Opt-Spy,4", false);
}
