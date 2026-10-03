using System;
using MiraAPI.GameOptions.OptionTypes;
using UnityEngine;

namespace TheOtherRoles.Roles.Neutral;

public class Jester(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = new Color32(236, 98, 165, byte.MaxValue);
    public static RoleInfo Info = new(color, RoleId.Jester, isNeutral: true);

    public static PlayerControl jester;

    public static bool triggerJesterWin;
    public static bool canCallEmergency = true;
    public static bool hasImpostorVision;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Jester;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<JesterOptions>.Instance.SpawnRate;

    public static void clearAndReload()
    {
        jester = null;
        triggerJesterWin = false;
        canCallEmergency = OptionGroupSingleton<JesterOptions>.Instance.CanCallEmergency.Value;
        hasImpostorVision = OptionGroupSingleton<JesterOptions>.Instance.HasImpostorVision.Value;
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;
}

/// <summary>
/// The settings of JesterOptions. They live next to the role on purpose: the group is bound
/// to Jester, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class JesterOptions : TorRoleOptionGroup<Jester>
{
    public override uint GroupPriority => 310;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Jester));
    public override Color GroupColor => TorOptionColors.Group(Jester.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.Jester), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedToggleOption CanCallEmergency { get; } =
        new ModdedToggleOption("Opt-Jester,1", true);

    public ModdedToggleOption HasImpostorVision { get; } =
        new ModdedToggleOption("Opt-Jester,2", false);
}
