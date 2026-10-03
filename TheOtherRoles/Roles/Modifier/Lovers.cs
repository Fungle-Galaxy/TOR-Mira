using UnityEngine;

namespace TheOtherRoles.Roles.Modifier;

public class Lovers : TorGameModifier
{
    public static Color color = new Color32(232, 57, 185, byte.MaxValue);
    public static RoleInfo Info = new(color, RoleId.Lover, isModifier: true);

    public static PlayerControl lover1;
    public static PlayerControl lover2;

    public static bool bothDie = true;
    public static bool enableChat = true;
    public static bool notAckedExiledIsLover;

    public override RoleInfo TorInfo => Info;

    // A couple only ever exists as two halves rolled together, with an evil one half the time -
    // Mira API cannot express that, so TOR picks the pair and adds the modifier itself.
    public override int GetAssignmentChance() => 0;

    public override int GetAmountPerGame() => 0;

    public override void OnActivate()
    {
        if (lover1 == null) lover1 = Player;
        else if (lover2 == null) lover2 = Player;
    }

    public override void OnDeactivate()
    {
        if (lover1 == Player) lover1 = null;
        else if (lover2 == Player) lover2 = null;
    }

    public static bool existing()
    {
        return lover1 != null && lover2 != null && !lover1.Data.Disconnected && !lover2.Data.Disconnected;
    }

    public static bool existingAndAlive()
    {
        return existing() && !lover1.Data.IsDead && !lover2.Data.IsDead && !notAckedExiledIsLover;
    }

    public static PlayerControl otherLover(PlayerControl oneLover)
    {
        if (!existingAndAlive()) return null;
        if (oneLover == lover1) return lover2;
        if (oneLover == lover2) return lover1;
        return null;
    }

    public static bool existingWithKiller()
    {
        return existing() && (lover1 == Jackal.jackal || lover2 == Jackal.jackal
                                                      || lover1 == Sidekick.sidekick || lover2 == Sidekick.sidekick
                                                      || lover1.Data.Role.IsImpostor || lover2.Data.Role.IsImpostor);
    }

    public static void clearAndReload()
    {
        lover1 = null;
        lover2 = null;
        notAckedExiledIsLover = false;
        bothDie = OptionGroupSingleton<LoversOptions>.Instance.LoverBothDie.Value;
        enableChat = OptionGroupSingleton<LoversOptions>.Instance.LoverEnableChat.Value;
    }

    public static PlayerControl getPartner(PlayerControl player)
    {
        if (player == null) return null;
        if (lover1 == player) return lover2;
        if (lover2 == player) return lover1;
        return null;
    }

    public override void ClearAndReload() => clearAndReload();
}

public static class LoversExtensions
{
    public static bool isLover(this PlayerControl player)
    {
        return player != null && (player == Lovers.lover1 || player == Lovers.lover2);
    }

    public static bool hasAliveKillingLover(this PlayerControl player)
    {
        if (!Lovers.existingAndAlive() || !Lovers.existingWithKiller())
            return false;
        return player != null && (player == Lovers.lover1 || player == Lovers.lover2);
    }

    public static PlayerControl getPartner(this PlayerControl player)
    {
        if (player == null)
            return null;
        if (Lovers.lover1 == player)
            return Lovers.lover2;
        if (Lovers.lover2 == player)
            return Lovers.lover1;
        return null;
    }

    public static PlayerControl getPartner(PlayerControl player, object _dummy = null)
    {
        return player.getPartner();
    }
}