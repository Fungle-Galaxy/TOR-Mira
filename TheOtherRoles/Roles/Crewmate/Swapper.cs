using System;
using MiraAPI.GameOptions.OptionTypes;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using UnityEngine;
using MiraAPI.Utilities;

namespace TheOtherRoles.Roles.Crewmate;

public class Swapper(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = new Color32(134, 55, 86, byte.MaxValue);

    public static RoleInfo Info = new(color, RoleId.Swapper);

    public static PlayerControl swapper;
    public static bool canCallEmergency;
    public static bool canOnlySwapOthers;
    public static int charges;
    public static float rechargeTasksNumber;
    public static float rechargedTasks;

    public static byte playerId1 = byte.MaxValue;
    public static byte playerId2 = byte.MaxValue;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Swapper;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<SwapperOptions>.Instance.SpawnRate;

    public static void clearAndReload()
    {
        swapper = null;
        playerId1 = byte.MaxValue;
        playerId2 = byte.MaxValue;
        canCallEmergency = OptionGroupSingleton<SwapperOptions>.Instance.CanCallEmergency.Value;
        canOnlySwapOthers = OptionGroupSingleton<SwapperOptions>.Instance.CanOnlySwapOthers.Value;
        charges = Mathf.RoundToInt(OptionGroupSingleton<SwapperOptions>.Instance.SwapsNumber.Value);
        rechargeTasksNumber = Mathf.RoundToInt(OptionGroupSingleton<SwapperOptions>.Instance.RechargeTasksNumber.Value);
        rechargedTasks = Mathf.RoundToInt(OptionGroupSingleton<SwapperOptions>.Instance.RechargeTasksNumber.Value);
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        if (Swapper.swapper == null || player != Swapper.swapper || player.Data.IsDead) return;
        var taskInfo = TasksHandler.taskInfo(player.Data);
        int playerCompleted = taskInfo.Item1;
        if (playerCompleted == Swapper.rechargedTasks)
        {
            Swapper.rechargedTasks += Swapper.rechargeTasksNumber;
            Swapper.charges++;
        }
    }
}

/// <summary>
/// The settings of SwapperOptions. They live next to the role on purpose: the group is bound
/// to Swapper, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class SwapperOptions : TorRoleOptionGroup<Swapper>
{
    public override uint GroupPriority => 470;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Swapper));
    public override Color GroupColor => TorOptionColors.Group(Swapper.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.Swapper), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedToggleOption CanCallEmergency { get; } =
        new ModdedToggleOption("Opt-Swapper,1", false);

    public ModdedToggleOption CanOnlySwapOthers { get; } =
        new ModdedToggleOption("Opt-Swapper,2", false);

    public ModdedNumberOption SwapsNumber { get; } =
        new ModdedNumberOption("Opt-Swapper,3", 1f, 0f, 5f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption RechargeTasksNumber { get; } =
        new ModdedNumberOption("Opt-Swapper,4", 2f, 1f, 10f, 1f, MiraNumberSuffixes.None, "0.##");
}

public static class SwapperRpcs
{
    [MethodRpc((uint)TorRpc.SwapperSwap, LocalHandling = RpcLocalHandling.After)]
    public static void RpcSwapperSwap(this PlayerControl player, byte playerId1, byte playerId2)
    {
        if (MeetingHud.Instance)
        {
            Swapper.playerId1 = playerId1;
            Swapper.playerId2 = playerId2;
        }
    }
}
