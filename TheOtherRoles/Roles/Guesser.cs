using System;
using MiraAPI.GameOptions.OptionTypes;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using UnityEngine;
using MiraAPI.Utilities;

namespace TheOtherRoles.Roles;

public static class Guesser
{
    public static PlayerControl niceGuesser;
    public static PlayerControl evilGuesser;
    public static Color color = new Color32(255, 255, 0, byte.MaxValue);

    public static RoleInfo NiceInfo = new(color, RoleId.NiceGuesser);

    public static RoleInfo EvilInfo = new(Palette.ImpostorRed, RoleId.EvilGuesser);

    public static int remainingShotsEvilGuesser = 2;
    public static int remainingShotsNiceGuesser = 2;

    public static bool isGuesser(byte playerId)
    {
        if ((niceGuesser != null && niceGuesser.PlayerId == playerId) ||
            (evilGuesser != null && evilGuesser.PlayerId == playerId)) return true;
        return false;
    }

    public static void clear(byte playerId)
    {
        if (niceGuesser != null && niceGuesser.PlayerId == playerId) niceGuesser = null;
        else if (evilGuesser != null && evilGuesser.PlayerId == playerId) evilGuesser = null;
    }

    public static int remainingShots(byte playerId, bool shoot = false)
    {
        var remainingShots = remainingShotsEvilGuesser;
        if (niceGuesser != null && niceGuesser.PlayerId == playerId)
        {
            remainingShots = remainingShotsNiceGuesser;
            if (shoot) remainingShotsNiceGuesser = Mathf.Max(0, remainingShotsNiceGuesser - 1);
        }
        else if (shoot)
        {
            remainingShotsEvilGuesser = Mathf.Max(0, remainingShotsEvilGuesser - 1);
        }

        return remainingShots;
    }

    public static void clearAndReload()
    {
        niceGuesser = null;
        evilGuesser = null;
        remainingShotsEvilGuesser = Mathf.RoundToInt(OptionGroupSingleton<GuesserOptions>.Instance.NumberOfShots.Value);
        remainingShotsNiceGuesser = Mathf.RoundToInt(OptionGroupSingleton<GuesserOptions>.Instance.NumberOfShots.Value);
    }
}

/// <summary>
/// The crew side of the Guesser. The legacy <see cref="Guesser"/> statics still hold the players
/// and the shots; this class only exists so MiraAPI registers a real role for it - that is what
/// puts it on the Role Settings page, in the role guide and in the guess menu.
/// </summary>
public class NiceGuesser(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.NiceGuesser;

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Guesser.NiceInfo;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<GuesserOptions>.Instance.SpawnRate;
}

/// <summary>
/// The impostor side of the Guesser. Its chance is the "Is Imp Guesser Rate" switch on the
/// Guesser page, so this row shows that chance read-only instead of offering a second spinner.
/// </summary>
public class EvilGuesser(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.EvilGuesser;

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Guesser.EvilInfo;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<GuesserOptions>.Instance.IsImpGuesserRate;

    [HideFromIl2Cpp]
    protected override bool ChanceControlledByAnotherRole => true;
}

/// <summary>
/// The settings of GuesserOptions. They live next to the role on purpose: the group is bound
/// to NiceGuesser, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class GuesserOptions : TorRoleOptionGroup<NiceGuesser>
{
    public override uint GroupPriority => 300;
    public override string GroupName => TorOptions.Title("Opt-Guesser,0");
    public override Color GroupColor => TorOptionColors.Group(Guesser.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption("Opt-Guesser,0", TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedStringOption IsImpGuesserRate { get; } =
        new ModdedStringOption("Opt-Guesser,1", TorOptions.Rates[0], TorOptions.Rates);

    public ModdedNumberOption NumberOfShots { get; } =
        new ModdedNumberOption("Opt-Guesser,2", 2f, 1f, 15f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedToggleOption HasMultipleShotsPerMeeting { get; } =
        new ModdedToggleOption("Opt-Guesser,3", false);

    public ModdedToggleOption KillsThroughShield { get; } =
        new ModdedToggleOption("Opt-Guesser,4", true);

    public ModdedToggleOption EvilCanKillSpy { get; } =
        new ModdedToggleOption("Opt-Guesser,5", true);

    public ModdedToggleOption CantGuessSnitchIfTaksDone { get; } =
        new ModdedToggleOption("Opt-Guesser,7", true);
}

public static class GuesserRpcs
{
    [MethodRpc((uint)TorRpc.GuesserShoot, LocalHandling = RpcLocalHandling.After)]
    public static void RpcGuesserShoot(this PlayerControl player, byte killerId, byte dyingTargetId,
        byte guessedTargetId, byte guessedRoleId)
    {
        RPCProcedure.guesserShoot(killerId, dyingTargetId, guessedTargetId, guessedRoleId);
    }
}
