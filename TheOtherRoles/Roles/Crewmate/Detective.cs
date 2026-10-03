using System;
using MiraAPI.GameOptions.OptionTypes;
using TheOtherRoles.Objects;
using UnityEngine;
using MiraAPI.Utilities;

namespace TheOtherRoles.Roles.Crewmate;

public class Detective(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = new Color32(45, 106, 165, byte.MaxValue);

    public static RoleInfo Info = new(color, RoleId.Detective);

    public static PlayerControl detective;

    public static float footprintIntervall = 1f;
    public static float footprintDuration = 1f;
    public static bool anonymousFootprints;
    public static float reportNameDuration;
    public static float reportColorDuration = 20f;
    public static float timer = 6.2f;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Detective;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<DetectiveOptions>.Instance.SpawnRate;

    public static void clearAndReload()
    {
        detective = null;
        anonymousFootprints = OptionGroupSingleton<DetectiveOptions>.Instance.AnonymousFootprints.Value;
        footprintIntervall = OptionGroupSingleton<DetectiveOptions>.Instance.FootprintIntervall.Value;
        footprintDuration = OptionGroupSingleton<DetectiveOptions>.Instance.FootprintDuration.Value;
        reportNameDuration = OptionGroupSingleton<DetectiveOptions>.Instance.ReportNameDuration.Value;
        reportColorDuration = OptionGroupSingleton<DetectiveOptions>.Instance.ReportColorDuration.Value;
        timer = 6.2f;
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        if (Detective.detective == null || Detective.detective != player) return;
        Detective.timer -= Time.fixedDeltaTime;
        if (Detective.timer <= 0f)
        {
            Detective.timer = Detective.footprintIntervall;
            foreach (var p in PlayerControl.AllPlayerControls)
                if (p != null && p != player && !p.Data.IsDead && !p.inVent)
                    FootprintHolder.Instance.MakeFootprint(p);
        }
    }
}

/// <summary>
/// The settings of DetectiveOptions. They live next to the role on purpose: the group is bound
/// to Detective, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class DetectiveOptions : TorRoleOptionGroup<Detective>
{
    public override uint GroupPriority => 440;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Detective));
    public override Color GroupColor => TorOptionColors.Group(Detective.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.Detective), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedToggleOption AnonymousFootprints { get; } =
        new ModdedToggleOption("Opt-Detective,1", false);

    public ModdedNumberOption FootprintIntervall { get; } =
        new ModdedNumberOption("Opt-Detective,2", 0.5f, 0.25f, 10f, 0.25f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption FootprintDuration { get; } =
        new ModdedNumberOption("Opt-Detective,3", 5f, 0.25f, 10f, 0.25f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption ReportNameDuration { get; } =
        new ModdedNumberOption("Opt-Detective,4", 0f, 0f, 60f, 2.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption ReportColorDuration { get; } =
        new ModdedNumberOption("Opt-Detective,5", 20f, 0f, 120f, 2.5f, MiraNumberSuffixes.None, "0.##");
}
