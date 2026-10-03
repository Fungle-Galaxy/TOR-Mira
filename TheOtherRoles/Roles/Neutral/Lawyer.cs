using System;
using MiraAPI.GameOptions.OptionTypes;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using UnityEngine;
using MiraAPI.Utilities;

namespace TheOtherRoles.Roles.Neutral;

public class Lawyer(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = new Color32(134, 153, 25, byte.MaxValue);
    public static RoleInfo Info = new(color, RoleId.Lawyer, isNeutral: true);

    public static RoleInfo ProsecutorInfo = new(color, RoleId.Prosecutor, isNeutral: true);

    public static PlayerControl lawyer;
    public static PlayerControl target;
    public static bool triggerProsecutorWin;
    public static bool isProsecutor;
    public static bool canCallEmergency = true;

    public static float vision = 1f;
    public static bool lawyerKnowsRole;
    public static bool targetCanBeJester;
    public static bool targetWasGuessed;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Lawyer;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<LawyerOptions>.Instance.SpawnRate;

    public static void clearAndReload(bool clearTarget = true)
    {
        lawyer = null;
        if (clearTarget)
        {
            target = null;
            targetWasGuessed = false;
        }

        isProsecutor = false;
        triggerProsecutorWin = false;
        vision = OptionGroupSingleton<LawyerOptions>.Instance.Vision.Value;
        lawyerKnowsRole = OptionGroupSingleton<LawyerOptions>.Instance.KnowsRole.Value;
        targetCanBeJester = OptionGroupSingleton<LawyerOptions>.Instance.TargetCanBeJester.Value;
        canCallEmergency = OptionGroupSingleton<LawyerOptions>.Instance.CanCallEmergency.Value;
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        if (Lawyer.lawyer == null || Lawyer.lawyer != player) return;
        if (Lawyer.target != null && Lawyer.target.Data.Disconnected && !Lawyer.lawyer.Data.IsDead)
        {
            PlayerControl.LocalPlayer.RpcLawyerPromotesToPursuer();
        }
    }
}

/// <summary>
/// The Lawyer's alternate pick. It shares every option with <see cref="Lawyer"/>, so its own page
/// only carries the chance - and even that is the "Is Prosecutor Chance" switch living on the
/// Lawyer's page, which is why this row shows it read-only.
/// </summary>
public class Prosecutor(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Prosecutor;

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Lawyer.ProsecutorInfo;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<LawyerOptions>.Instance.IsProsecutorChance;

    [HideFromIl2Cpp]
    protected override bool ChanceControlledByAnotherRole => true;

    // Prosecutor and Lawyer are the same seat: TOR rolls the switch on the Lawyer page and swaps
    // the Lawyer over, so Mira API must never put both on the table.
    public override bool CanSpawnOnCurrentMode() => false;

    public override bool? ForceShowRoleOnWiki => true;
}

/// <summary>
/// The settings of LawyerOptions. They live next to the role on purpose: the group is bound
/// to Lawyer, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class LawyerOptions : TorRoleOptionGroup<Lawyer>
{
    public override uint GroupPriority => 350;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Lawyer));
    public override Color GroupColor => TorOptionColors.Group(Lawyer.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.Lawyer), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedStringOption IsProsecutorChance { get; } =
        new ModdedStringOption("Opt-Lawyer,1", TorOptions.Rates[0], TorOptions.Rates);

    public ModdedNumberOption Vision { get; } =
        new ModdedNumberOption("Opt-Lawyer,2", 1f, 0.25f, 3f, 0.25f, MiraNumberSuffixes.None, "0.##");

    public ModdedToggleOption KnowsRole { get; } =
        new ModdedToggleOption("Opt-Lawyer,3", false);

    public ModdedToggleOption CanCallEmergency { get; } =
        new ModdedToggleOption("Opt-Lawyer,4", true);

    public ModdedToggleOption TargetCanBeJester { get; } =
        new ModdedToggleOption("Opt-Lawyer,5", false);

    public ModdedNumberOption PursuerCooldown { get; } =
        new ModdedNumberOption("Opt-Lawyer,6", 30f, 5f, 60f, 2.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption PursuerBlanksNumber { get; } =
        new ModdedNumberOption("Opt-Lawyer,7", 5f, 1f, 20f, 1f, MiraNumberSuffixes.None, "0.##");
}

public static class LawyerRpcs
{
    [MethodRpc((uint)TorRpc.LawyerPromotesToPursuer, LocalHandling = RpcLocalHandling.After)]
    public static void RpcLawyerPromotesToPursuer(this PlayerControl player)
    {
        RPCProcedure.lawyerPromotesToPursuer();
    }

    [MethodRpc((uint)TorRpc.LawyerSetTarget, LocalHandling = RpcLocalHandling.After)]
    public static void RpcLawyerSetTarget(this PlayerControl player, byte playerId)
    {
        Lawyer.target = Helpers.playerById(playerId);
    }
}
