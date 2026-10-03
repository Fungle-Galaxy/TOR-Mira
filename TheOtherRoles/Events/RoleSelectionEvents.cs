using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.GameModes;
using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using TheOtherRoles.CustomGameModes;
using TheOtherRoles.Draft;
using TheOtherRoles.Networking;
using UnityEngine;

namespace TheOtherRoles.Events;

public static class RoleSelectionEvents
{
    [RegisterEvent]
    public static void AfterRolesAssigned(IntroBeginEvent @event)
    {
        // Every client lays the draft over Mira API's roll itself: the host's role messages land
        // after the intro has already picked which team to show, so waiting for them would show
        // the rolled faction instead of the drafted one.
        if (!AmongUsClient.Instance.AmHost)
        {
            if (DraftManager.HasResults) DraftApplier.Apply();
            DraftManager.HasResults = false;
            return;
        }

        if (HideNSeek.isHideNSeekGM || PropHunt.isPropHuntGM ||
            GameOptionsManager.Instance.currentGameOptions.GameMode == GameModes.HideNSeek)
            return; // Don't assign roles in Hide N Seek

        // The draft already forced the dependent conditions (Mafia family, Deputy behind a
        // Sheriff, Lawyer against Prosecutor), so only the steps it does not cover still run.
        if (DraftManager.HasResults)
        {
            DraftApplier.Apply();
            DraftManager.HasResults = false;
        }
        else
        {
            assignDependentRoles(); // Mafia family, Deputy next to a Sheriff, Lawyer vs Prosecutor
        }

        assignRoleTargets(); // Assign targets for Lawyer & Prosecutor
        if (CustomGameModeManager.ActiveMode is TorGuesserMode) assignGuesserGamemode();
        assignModifiers(); // Assign modifier
    }

    public static void assignDependentRoles()
    {
        var players = PlayerControl.AllPlayerControls.ToArray()
            .Where(x => x != null && x.Data != null && !x.Data.IsDead && !x.Data.Disconnected).ToList();
        var impostors = players.FindAll(x => x.Data.Role.IsImpostor);
        var crewmates = players.FindAll(x => !x.Data.Role.IsImpostor);

        // The Mafia only arrives as the full family, and only in a game that seats three impostors.
        if (Godfather.godfather != null && Mafioso.mafioso == null && Janitor.janitor == null &&
            TorSpawn.ImpostorSeats >= 3 && impostors.Count >= 3)
        {
            var seats = replaceableSeats(impostors);
            if (seats.Count >= 2)
            {
                setRoleToRandomPlayer(RoleId.Mafioso, seats);
                setRoleToRandomPlayer(RoleId.Janitor, seats);
            }
            else
            {
                TheOtherRolesPlugin.Logger.LogWarning("Mafia could not be filled: no free impostor seat.");
            }
        }

        // The Deputy is handed out by the Sheriff, never on its own.
        // The Sheriff is handed the seat; the Deputy's own chance decides whether it is filled.
        if (Sheriff.sheriff != null && Deputy.deputy == null &&
            OptionGroupSingleton<SheriffOptions>.Instance.DeputyEnabled.Value &&
            Helpers.rnd.Next(1, 101) <= OptionGroupSingleton<DeputyOptions>.Instance.SpawnRate.Selection() * TorOptions.RateStep)
        {
            var seats = replaceableSeats(crewmates);
            if (seats.Count > 0) setRoleToRandomPlayer(RoleId.Deputy, seats);
        }

        // Prosecutor and Lawyer share one seat; the switch on the Lawyer page picks which one sits in it.
        if (Lawyer.lawyer != null && !Lawyer.isProsecutor &&
            Helpers.rnd.Next(1, 101) <= OptionGroupSingleton<LawyerOptions>.Instance.IsProsecutorChance.Selection() * TorOptions.RateStep)
        {
            setRoleToRandomPlayer(RoleId.Prosecutor, new List<PlayerControl> { Lawyer.lawyer });
        }
    }

    private static readonly RoleId[] ReplaceableRoles =
    {
        RoleId.Mayor, RoleId.Portalmaker, RoleId.Engineer, RoleId.Lighter, RoleId.Detective,
        RoleId.TimeMaster, RoleId.Medic, RoleId.Seer, RoleId.Hacker, RoleId.Tracker, RoleId.Snitch,
        RoleId.Medium, RoleId.Trapper, RoleId.SecurityGuard, RoleId.Swapper, RoleId.Spy,
        RoleId.Morphling, RoleId.Camouflager, RoleId.Vampire, RoleId.Eraser, RoleId.Trickster,
        RoleId.Cleaner, RoleId.Warlock, RoleId.Witch, RoleId.Ninja, RoleId.Bomber, RoleId.Yoyo,
        RoleId.BountyHunter, RoleId.NiceGuesser, RoleId.EvilGuesser
    };

    private static List<PlayerControl> replaceableSeats(List<PlayerControl> pool)
    {
        return pool.Where(p => CustomRoleManager.getRoleInfoForPlayer(p, false)
            .All(x => x.roleId is RoleId.Crewmate or RoleId.Impostor || ReplaceableRoles.Contains(x.roleId)))
            .OrderBy(x => Helpers.rnd.Next()).ToList();
    }

    private static void setRoleToRandomPlayer(RoleId roleId, List<PlayerControl> playerList)
    {
        if (playerList.Count == 0) return;
        var index = Helpers.rnd.Next(0, playerList.Count);
        var playerId = playerList[index].PlayerId;
        playerList.RemoveAt(index);

        PlayerControl.LocalPlayer.RpcSetRole((byte)roleId, playerId);
    }

    public static void assignGuesserGamemode()
    {
        var impPlayer = PlayerControl.AllPlayerControls.ToArray().ToList().OrderBy(x => Guid.NewGuid()).ToList();
        var neutralPlayer = PlayerControl.AllPlayerControls.ToArray().ToList().OrderBy(x => Guid.NewGuid()).ToList();
        var crewPlayer = PlayerControl.AllPlayerControls.ToArray().ToList().OrderBy(x => Guid.NewGuid()).ToList();
        impPlayer.RemoveAll(x => !x.Data.Role.IsImpostor);
        neutralPlayer.RemoveAll(x => !Helpers.isNeutral(x));
        crewPlayer.RemoveAll(x => x.Data.Role.IsImpostor || Helpers.isNeutral(x));
        assignGuesserGamemodeToPlayers(crewPlayer,
            Mathf.RoundToInt(OptionGroupSingleton<GuesserTeamCountOptions>.Instance.CrewNumber.Value));
        assignGuesserGamemodeToPlayers(neutralPlayer,
            Mathf.RoundToInt(OptionGroupSingleton<GuesserTeamCountOptions>.Instance.NeutralNumber.Value),
            OptionGroupSingleton<GuesserForcedOptions>.Instance.ForceJackalGuesser.Value,
            OptionGroupSingleton<GuesserForcedOptions>.Instance.ForceThiefGuesser.Value);
        assignGuesserGamemodeToPlayers(impPlayer,
            Mathf.RoundToInt(OptionGroupSingleton<GuesserTeamCountOptions>.Instance.ImpNumber.Value));
    }

    private static void assignGuesserGamemodeToPlayers(List<PlayerControl> playerList, int count,
        bool forceJackal = false, bool forceThief = false)
    {
        for (var i = 0; i < count && playerList.Count > 0; i++)
        {
            var index = Helpers.rnd.Next(0, playerList.Count);
            if (forceThief && !forceJackal)
            {
                if (Thief.thief != null) index = playerList.FindIndex(x => x == Thief.thief);
                forceThief = false;
            }

            if (forceJackal)
            {
                if (Jackal.jackal != null) index = playerList.FindIndex(x => x == Jackal.jackal);
                forceJackal = false;
            }

            if (index < 0 || index >= playerList.Count) continue;
            var playerId = playerList[index].PlayerId;
            playerList.RemoveAt(index);

            PlayerControl.LocalPlayer.RpcSetGuesserGm(playerId);
        }
    }

    public static void assignRoleTargets()
    {
        // Set Lawyer or Prosecutor Target
        if (Lawyer.lawyer != null)
        {
            var possibleTargets = new List<PlayerControl>();
            if (!Lawyer.isProsecutor)
            {
                // Lawyer
                foreach (var p in PlayerControl.AllPlayerControls)
                    if (!p.Data.IsDead && !p.Data.Disconnected && p != Lovers.lover1 && p != Lovers.lover2 &&
                        (p.Data.Role.IsImpostor || p == Jackal.jackal ||
                         (Lawyer.targetCanBeJester && p == Jester.jester)))
                        possibleTargets.Add(p);
            }
            else
            {
                // Prosecutor
                foreach (var p in PlayerControl.AllPlayerControls)
                    if (!p.Data.IsDead && !p.Data.Disconnected && p != Lovers.lover1 && p != Lovers.lover2 &&
                        p != Mini.mini && !p.Data.Role.IsImpostor && !Helpers.isNeutral(p) && p != Swapper.swapper)
                        possibleTargets.Add(p);
            }

            if (possibleTargets.Count == 0)
            {
                PlayerControl.LocalPlayer.RpcLawyerPromotesToPursuer();
            }
            else
            {
                var target = possibleTargets[Helpers.rnd.Next(0, possibleTargets.Count)];
                PlayerControl.LocalPlayer.RpcLawyerSetTarget(target.PlayerId);
            }
        }
    }

    public static void assignModifiers()
    {
        var players = PlayerControl.AllPlayerControls.ToArray().ToList();
        if (CustomGameModeManager.ActiveMode is TorGuesserMode &&
            !OptionGroupSingleton<GuesserSettingsOptions>.Instance.HaveModifier.Value)
            players.RemoveAll(x => GuesserGM.isGuesser(x.PlayerId));

        if (Helpers.rnd.Next(1, 101) <= OptionGroupSingleton<LoversOptions>.Instance.Lover.Selection() * TorOptions.RateStep)
        {
            // Two halves of one pair, with an evil one only half the time - Mira API rolls every
            // modifier on its own, so the couple still comes off TOR's own pair roll.
            var isEvilLover = Helpers.rnd.Next(1, 101) <= OptionGroupSingleton<LoversOptions>.Instance.LoverImpLoverRate.Selection() * TorOptions.RateStep;
            var impPlayer = new List<PlayerControl>(players);
            var crewPlayer = new List<PlayerControl>(players);
            impPlayer.RemoveAll(x => !x.Data.Role.IsImpostor);
            crewPlayer.RemoveAll(x => x.Data.Role.IsImpostor || x == Lawyer.lawyer);

            var firstLover = takeLover(isEvilLover ? impPlayer : crewPlayer);
            var secondLover = firstLover == null ? null : takeLover(crewPlayer);
            if (firstLover != null && secondLover != null)
            {
                firstLover.RpcAddModifier<Lovers>();
                secondLover.RpcAddModifier<Lovers>();
                players.Remove(firstLover);
                players.Remove(secondLover);
            }
        }

        assignGameModifiers(players);
    }

    private static void assignGameModifiers(List<PlayerControl> playerList)
    {
        var modifiers = ModifierManager.Modifiers.OfType<GameModifier>()
            .Where(x => x.GetAmountPerGame() > 0 && x.GetAssignmentChance() > 0)
            .OrderByDescending(x => x.Priority())
            .ThenByDescending(x => x is Shifter ? 2 : x is Sunglasses ? 1 : 0) // TOR: crew-only seats are picked first
            .ThenBy(x => Helpers.rnd.Next())
            .ToArray();

        foreach (var modifier in modifiers)
        {
            var chance = Math.Clamp(modifier.GetAssignmentChance(), 0, 100);
            var assignments = Math.Min(modifier.GetAmountPerGame(), playerList.Count);

            for (var i = 0; i < assignments; i++)
            {
                if (Helpers.rnd.Next(100) >= chance) continue;
                var candidates = playerList.Where(x => isValidOn(x, modifier)).ToList();
                if (candidates.Count == 0) break;

                var plr = candidates[Helpers.rnd.Next(0, candidates.Count)];
                plr.RpcAddModifier(modifier.TypeId);
                playerList.Remove(plr);
            }
        }
    }

    private static bool isValidOn(PlayerControl player, GameModifier modifier)
    {
        return (player.Data.Role is not MiraAPI.Roles.ICustomRole role || role.IsModifierApplicable(modifier)) &&
               !player.HasModifier(modifier.TypeId) && modifier.IsModifierValidOn(player.Data.Role) &&
               modifier.IsModifierValidOnPostCheck(player.Data.Role) && modifier.CanSpawnOnCurrentMode();
    }

    private static PlayerControl takeLover(List<PlayerControl> pool)
    {
        if (pool.Count == 0) return null;
        var lover = pool[Helpers.rnd.Next(0, pool.Count)];
        pool.Remove(lover);
        return lover;
    }
}
