using System.Collections.Generic;
using System.Linq;
using MiraAPI.Roles;
using TheOtherRoles.CustomGameModes;

namespace TheOtherRoles.Draft;

public class DraftSeat
{
    public RoleId RoleId;
    public int Group = -1;
    public bool Taken;
    public int Chance = 100;
    public ModdedRoleTeams Team;

    public bool Offerable = true;

    public bool Plain;
}

public static class DraftPool
{
    public static readonly List<DraftSeat> Seats = new();
    public static int ImpCap;

    private static int crewMin;
    private static int crewMax = 15;
    private static int impMin = 1;
    private static int impMax = 3;
    private static int neutralMin;
    private static int neutralMax = 15;

    // Never drafted directly: vanilla seats and roles that only exist next to another one (the
    // Mafia family is one group seat, Deputy waits for a Sheriff, Lawyer rolls against Prosecutor).
    private static readonly RoleId[] NeverDrafted =
    {
        RoleId.Crewmate, RoleId.Impostor, RoleId.Sidekick, RoleId.Pursuer,
        RoleId.Mafioso, RoleId.Janitor, RoleId.Godfather, RoleId.Lawyer, RoleId.Prosecutor
    };

    public static int TakenImpostorSeats => Seats.Count(x => x.Taken && x.Team == ModdedRoleTeams.Impostor);

    public static bool Build()
    {
        Seats.Clear();

        // The guesser game mode hands the two guesser teams out after the draft has ended.
        var guesserMode = MiraAPI.GameModes.CustomGameModeManager.ActiveMode is TorGuesserMode;

        foreach (var role in MiraAPI.Roles.CustomRoleManager.AllRoles.OfType<ITorRole>())
        {
            var id = role.TorRoleId;
            if (NeverDrafted.Contains(id) || role.GetRoleInfo().isModifier) continue;
            if (guesserMode && (id == RoleId.NiceGuesser || id == RoleId.EvilGuesser)) continue;

            var chance = role.GetChance();
            if (chance is not > 0) continue;
            if (id == RoleId.Deputy)
            {
                if (!OptionGroupSingleton<SheriffOptions>.Instance.DeputyEnabled.Value) continue;
            }
            else if (!role.CanSpawnOnCurrentMode()) continue;

            Seats.Add(new DraftSeat { RoleId = id, Team = role.Team, Chance = chance.Value });
        }

        AddMafiaFamily();
        AddLawyerSeat();
        AddPlainImpostors();

        readFactionLimits();
        ImpCap = System.Math.Min(Seats.Count(x => x.Team == ModdedRoleTeams.Impostor), TorSpawn.ImpostorSeats);
        return Seats.Count > 0;
    }

    // One card per impostor seat the options opened up. It is how the draft fills the impostor
    // count without taking a crew pick away from the player who made it.
    private static void AddPlainImpostors()
    {
        for (var i = 0; i < TorSpawn.ImpostorSeats; i++)
            Seats.Add(new DraftSeat { RoleId = RoleId.Impostor, Team = ModdedRoleTeams.Impostor, Plain = true });
    }

    private static void readFactionLimits()
    {
        var options = OptionGroupSingleton<DraftOptions>.Instance;
        if (options == null) return;

        crewMax = (int)options.CrewRolesMax.Value;
        neutralMax = (int)options.NeutralRolesMax.Value;
        impMax = System.Math.Min((int)options.ImpRolesMax.Value, TorSpawn.ImpostorSeats);
        crewMin = System.Math.Min((int)options.CrewRolesMin.Value, crewMax);
        neutralMin = System.Math.Min((int)options.NeutralRolesMin.Value, neutralMax);
        impMin = System.Math.Min((int)options.ImpRolesMin.Value, impMax);
        impMin = System.Math.Min(impMin, TorSpawn.ImpostorSeats);
    }

    private static void AddMafiaFamily()
    {
        var godfather = MiraAPI.Roles.CustomRoleManager.AllRoles.OfType<ITorRole>()
            .FirstOrDefault(x => x.TorRoleId == RoleId.Godfather);
        if (godfather == null || godfather.GetChance() is not > 0 || TorSpawn.ImpostorSeats < 3) return;

        var players = PlayerControl.AllPlayerControls.ToArray()
            .Count(x => x != null && x.Data != null && !x.Data.Disconnected);
        if (players < 3) return;

        var group = Seats.Count;
        var chance = godfather.GetChance() ?? 100;
        Seats.Add(new DraftSeat { RoleId = RoleId.Godfather, Group = group, Team = ModdedRoleTeams.Impostor, Chance = chance });
        Seats.Add(new DraftSeat { RoleId = RoleId.Mafioso, Group = group, Team = ModdedRoleTeams.Impostor, Chance = chance, Offerable = false });
        Seats.Add(new DraftSeat { RoleId = RoleId.Janitor, Group = group, Team = ModdedRoleTeams.Impostor, Chance = chance, Offerable = false });
    }

    private static void AddLawyerSeat()
    {
        var lawyer = MiraAPI.Roles.CustomRoleManager.AllRoles.OfType<ITorRole>()
            .FirstOrDefault(x => x.TorRoleId == RoleId.Lawyer);
        if (lawyer == null || lawyer.GetChance() is not > 0 || !lawyer.CanSpawnOnCurrentMode()) return;

        var prosecutor = Helpers.rnd.Next(1, 101) <=
                         OptionGroupSingleton<LawyerOptions>.Instance.IsProsecutorChance.Selection() *
                         TorOptions.RateStep;
        Seats.Add(new DraftSeat
        {
            RoleId = prosecutor ? RoleId.Prosecutor : RoleId.Lawyer,
            Team = ModdedRoleTeams.Custom,
            Chance = lawyer.GetChance() ?? 100
        });
    }

    public static List<DraftSeat> BuildOffers(DraftSlotState slot, int count)
    {
        var result = new List<DraftSeat>();
        var pool = EligibleFor(slot).ToList();
        while (result.Count < count && pool.Count > 0)
        {
            var seat = TakeWeighted(pool);
            pool.RemoveAll(x => x.RoleId == seat.RoleId);
            result.Add(seat);
        }

        return result;
    }

    public const int CrewSeat = 0;
    public const int ImpostorSeat = 1;
    public const int NeutralSeat = 2;

    public static int FactionOf(RoleId roleId)
    {
        if (!RoleInfo.roleInfoById.TryGetValue(roleId, out var info)) return CrewSeat;
        if (info.isNeutral) return NeutralSeat;
        return info.isImpostor ? ImpostorSeat : CrewSeat;
    }

    private static int pickedFaction(int faction) =>
        DraftManager.SlotStates.Count(x => x.HasPicked && x.ChosenRoleId != 0 &&
                                            FactionOf((RoleId)x.ChosenRoleId) == faction);

    // How many of the impostor picks went to a real role rather than the plain seat.
    private static int pickedCustomImpostors() =>
        pickedFaction(ImpostorSeat) - DraftManager.SlotStates.Count(x => x.ChosenRoleId == (byte)RoleId.Impostor);

    private static int minOf(int faction) =>
        faction == ImpostorSeat ? impMin : faction == NeutralSeat ? neutralMin : crewMin;

    private static int maxOf(int faction) =>
        faction == ImpostorSeat ? impMax : faction == NeutralSeat ? neutralMax : crewMax;

    private static HashSet<int> forcedFactions()
    {
        var remaining = DraftManager.SlotStates.Count(x => !x.HasPicked);
        if (remaining <= 0) return null;

        var impostorOwed = TorSpawn.ImpostorSeats - pickedFaction(ImpostorSeat);
        if (impostorOwed > 0 && impostorOwed >= remaining)
            return new HashSet<int> { ImpostorSeat };

        var owed = 0;
        var factions = new HashSet<int>();
        for (var faction = 0; faction < 3; faction++)
        {
            var missing = minOf(faction) - pickedFaction(faction);
            if (missing <= 0) continue;
            owed += missing;
            factions.Add(faction);
        }

        return owed > 0 && remaining <= owed ? factions : null;
    }

    public static DraftSeat PickWeighted(DraftSlotState slot)
    {
        var pool = EligibleFor(slot).ToList();
        return pool.Count == 0 ? null : TakeWeighted(pool);
    }

    private static IEnumerable<DraftSeat> EligibleFor(DraftSlotState slot)
    {
        var picked = new HashSet<byte>(DraftManager.SlotStates.Where(x => x.HasPicked).Select(x => x.ChosenRoleId));
        var forced = forcedFactions();
        foreach (var seat in Seats)
        {
            if (seat.Taken || !seat.Offerable) continue;
            var faction = FactionOf(seat.RoleId);
            if (seat.Plain)
            {
                // The plain seat fills what the custom cards left over, so the maximum never
                // touches it - it just stays out of the way until the minimum is drafted.
                if (pickedCustomImpostors() < impMin) continue;
            }
            else if (pickedFaction(faction) + seatSize(seat) > maxOf(faction)) continue;

            if (forced != null && !forced.Contains(faction)) continue;
            if (seat.Team == ModdedRoleTeams.Impostor && TakenImpostorSeats + seatSize(seat) > ImpCap) continue;
            if (seat.Group >= 0 && FreeSlots(slot) < 2) continue;
            if (seat.RoleId == RoleId.Deputy && !picked.Contains((byte)RoleId.Sheriff)) continue;
            if (blocked(seat.RoleId, picked)) continue;
            yield return seat;
        }
    }

    // A family card locks three seats at once, so it may only be offered while the cap can take all.
    private static int seatSize(DraftSeat seat) => seat.Group >= 0 ? Seats.Count(x => x.Group == seat.Group) : 1;

    private static int FreeSlots(DraftSlotState current) =>
        DraftManager.SlotStates.Count(x => !x.HasPicked && x.PlayerId != current.PlayerId);

    private static bool blocked(RoleId roleId, HashSet<byte> picked)
    {
        return CustomOptionHolder.blockedRolePairings.TryGetValue((byte)roleId, out var blocked) &&
               blocked.Any(picked.Contains);
    }

    private static DraftSeat TakeWeighted(List<DraftSeat> pool)
    {
        var total = pool.Sum(x => x.Chance);
        if (total <= 0) return pool[Helpers.rnd.Next(pool.Count)];

        var roll = Helpers.rnd.Next(total);
        foreach (var seat in pool)
        {
            roll -= seat.Chance;
            if (roll < 0) return seat;
        }

        return pool[pool.Count - 1];
    }
}
