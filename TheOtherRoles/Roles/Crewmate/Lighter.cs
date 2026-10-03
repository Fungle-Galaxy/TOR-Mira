using System;
using MiraAPI.GameOptions.OptionTypes;
using UnityEngine;
using MiraAPI.Utilities;

namespace TheOtherRoles.Roles.Crewmate;

public class Lighter(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = new Color32(238, 229, 190, byte.MaxValue);

    public static RoleInfo Info = new(color, RoleId.Lighter);

    public static PlayerControl lighter;

    public static float lighterModeLightsOnVision = 2f;
    public static float lighterModeLightsOffVision = 0.75f;
    public static float flashlightWidth = 0.75f;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Lighter;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<LighterOptions>.Instance.SpawnRate;

    public static void clearAndReload()
    {
        lighter = null;
        flashlightWidth = OptionGroupSingleton<LighterOptions>.Instance.FlashlightWidth.Value;
        lighterModeLightsOnVision = OptionGroupSingleton<LighterOptions>.Instance.ModeLightsOnVision.Value;
        lighterModeLightsOffVision = OptionGroupSingleton<LighterOptions>.Instance.ModeLightsOffVision.Value;
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;
}

/// <summary>
/// The settings of LighterOptions. They live next to the role on purpose: the group is bound
/// to Lighter, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class LighterOptions : TorRoleOptionGroup<Lighter>
{
    public override uint GroupPriority => 430;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Lighter));
    public override Color GroupColor => TorOptionColors.Group(Lighter.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.Lighter), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedNumberOption ModeLightsOnVision { get; } =
        new ModdedNumberOption("Opt-Lighter,1", 1.5f, 0.25f, 5f, 0.25f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption ModeLightsOffVision { get; } =
        new ModdedNumberOption("Opt-Lighter,2", 0.5f, 0.25f, 5f, 0.25f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption FlashlightWidth { get; } =
        new ModdedNumberOption("Opt-Lighter,3", 0.3f, 0.1f, 1f, 0.1f, MiraNumberSuffixes.None, "0.##");
}
