using System;
using MiraAPI.GameOptions.OptionTypes;
using System.Collections.Generic;
using UnityEngine;
using MiraAPI.Utilities;

namespace TheOtherRoles.Roles.Crewmate;

public class Seer(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = new Color32(97, 178, 108, byte.MaxValue);

    public static RoleInfo Info = new(color, RoleId.Seer);

    public static PlayerControl seer;
    public static List<Vector3> deadBodyPositions = new();

    public static float soulDuration = 15f;
    public static bool limitSoulDuration;
    public static int mode;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Seer;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<SeerOptions>.Instance.SpawnRate;

    public static void clearAndReload()
    {
        seer = null;
        deadBodyPositions = new List<Vector3>();
        limitSoulDuration = OptionGroupSingleton<SeerOptions>.Instance.LimitSoulDuration.Value;
        soulDuration = OptionGroupSingleton<SeerOptions>.Instance.SoulDuration.Value;
        mode = OptionGroupSingleton<SeerOptions>.Instance.Mode.Selection();
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;
}

/// <summary>
/// The settings of SeerOptions. They live next to the role on purpose: the group is bound
/// to Seer, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class SeerOptions : TorRoleOptionGroup<Seer>
{
    public override uint GroupPriority => 480;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Seer));
    public override Color GroupColor => TorOptionColors.Group(Seer.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.Seer), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedStringOption Mode { get; } =
        new ModdedStringOption("Opt-Seer,1", "Opt-Seer,100", ["Opt-Seer,100", "Opt-Seer,101", "Opt-Seer,102"]);

    public ModdedToggleOption LimitSoulDuration { get; } =
        new ModdedToggleOption("Opt-Seer,2", false);

    public ModdedNumberOption SoulDuration { get; } =
        new ModdedNumberOption("Opt-Seer,3", 15f, 0f, 120f, 5f, MiraNumberSuffixes.None, "0.##")
        {
            Visible = () => OptionGroupSingleton<SeerOptions>.Instance.LimitSoulDuration.Value
        };
}
