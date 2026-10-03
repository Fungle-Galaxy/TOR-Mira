using System.Collections.Generic;
using System.Linq;
using TheOtherRoles.Networking;
using UnityEngine;

namespace TheOtherRoles.Roles.Modifier;

public class Bait : TorGameModifier
{
    public static Color color = new Color32(0, 247, 255, byte.MaxValue);

    public static RoleInfo Info = new(color, RoleId.Bait, isModifier: true);

    public static List<PlayerControl> bait = new();
    public static Dictionary<DeadPlayer, float> active = new();

    public static float reportDelayMin;
    public static float reportDelayMax;
    public static bool showKillFlash = true;

    public override RoleInfo TorInfo => Info;

    public override int GetAssignmentChance() =>
        ChanceOf(OptionGroupSingleton<BaitOptions>.Instance.Bait);

    public override int GetAmountPerGame() =>
        OptionGroupSingleton<BaitOptions>.Instance.BaitQuantity.Quantity();

    public override void OnActivate() => bait.Add(Player);

    public override void OnDeactivate() => bait.Remove(Player);

    public override void ClearAndReload() => clearAndReload();

    public static void clearAndReload()
    {
        bait = new List<PlayerControl>();
        active = new Dictionary<DeadPlayer, float>();
        reportDelayMin = OptionGroupSingleton<BaitOptions>.Instance.BaitReportDelayMin.Value;
        reportDelayMax = OptionGroupSingleton<BaitOptions>.Instance.BaitReportDelayMax.Value;
        if (reportDelayMin > reportDelayMax) reportDelayMin = reportDelayMax;
        showKillFlash = OptionGroupSingleton<BaitOptions>.Instance.BaitShowKillFlash.Value;
    }

    public static void tick()
    {
        if (!active.Any()) return;
        foreach (var entry in new Dictionary<DeadPlayer, float>(active))
        {
            active[entry.Key] = entry.Value - Time.fixedDeltaTime;
            if (entry.Value <= 0)
            {
                active.Remove(entry.Key);
                if (entry.Key.killerIfExisting != null &&
                    entry.Key.killerIfExisting.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                {
                    Helpers.handleVampireBiteOnBodyReport();
                    PlayerControl.LocalPlayer.RpcUncheckedCmdReportDeadBody(entry.Key.killerIfExisting.PlayerId,
                        entry.Key.player.PlayerId);
                }
            }
        }
    }
}
