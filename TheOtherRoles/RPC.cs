using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Assets.CoreScripts;
using InnerNet;
using TheOtherRoles.CustomGameModes;
using TheOtherRoles.Objects;
using TheOtherRoles.Patches;
using TheOtherRoles.Utilities;
using TMPro;
using UnityEngine;
using static TheOtherRoles.GameHistory;
using static TheOtherRoles.TORMapOptions;
using Object = UnityEngine.Object;

namespace TheOtherRoles;

public enum RoleId
{
    // Crewmates
    Crewmate,
    Mayor,
    Portalmaker,
    Engineer,
    Sheriff,
    Deputy,
    Lighter,
    Detective,
    TimeMaster,
    Medic,
    Swapper,
    Seer,
    Hacker,
    Tracker,
    Snitch,
    Spy,
    SecurityGuard,
    Medium,
    Trapper,
    // Impostors
    Impostor,
    Godfather,
    Mafioso,
    Janitor,
    Morphling,
    Camouflager,
    Vampire,
    Eraser,
    Trickster,
    Cleaner,
    Warlock,
    BountyHunter,
    Witch,
    Ninja,
    Bomber,
    Yoyo,
    // Neutrals
    Jester,
    Jackal,
    Sidekick,
    Arsonist,
    EvilGuesser,
    NiceGuesser,
    Vulture,
    Lawyer,
    Prosecutor,
    Pursuer,
    Thief,
    SchrodingerCat,

    // Modifier ---
    Lover,
    Bait,
    Bloody,
    AntiTeleport,
    Tiebreaker,
    Sunglasses,
    Mini,
    Vip,
    Invert,
    Chameleon,
    Armored,
    Shifter
}

public static class RPCProcedure
{

    // Main Controls

    public static void resetVariables()
    {
        Garlic.clearGarlics();
        JackInTheBox.clearJackInTheBoxes();
        NinjaTrace.clearTraces();
        Silhouette.clearSilhouettes();
        Portal.clearPortals();
        Bloodytrail.resetSprites();
        Trap.clearTraps();
        clearAndReloadMapOptions();
        CustomRoleManager.Instance.ClearAndReloadAll();
        TorGameModifier.ClearAndReloadAll();
        HandleGuesser.ClearAndReload();
        HideNSeek.ClearAndReload();
        PropHunt.ClearAndReload();
        clearGameHistory();
        // The old setCustomButtonCooldowns() only existed to push values into mutable MaxTimer
        // fields; Cooldown is a live per-role override now, so only the accumulated offsets reset.
        Buttons.TorButtons.ResetCooldownOffsets();
        Buttons.TorButtons.ReloadHotkeys();
        reloadPluginOptions();
        Helpers.toggleZoom(true);
        GameStartManagerPatch.GameStartManagerUpdatePatch.startingTimer = 0;
        SurveillanceMinigamePatch.nightVisionOverlays = null;
        EventUtility.clearAndReload();
        MapBehaviourPatch.clearAndReload();
    }

    public static void forceEnd()
    {
        if (AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started) return;
        foreach (var player in PlayerControl.AllPlayerControls)
            if (!player.Data.Role.IsImpostor)
            {
                GameData.Instance
                    .GetPlayerById(player
                        .PlayerId); // player.RemoveInfected(); (was removed in 2022.12.08, no idea if we ever need that part again, replaced by these 2 lines.) 
                player.CoSetRole(RoleTypes.Crewmate, true);

                player.MurderPlayer(player);
                player.Data.IsDead = true;
            }
    }

    private static readonly Dictionary<RoleId, Action<PlayerControl>> RoleSetters = new()
    {
        { RoleId.Jester, p => Jester.jester = p },
        { RoleId.Mayor, p => Mayor.mayor = p },
        { RoleId.Portalmaker, p => Portalmaker.portalmaker = p },
        { RoleId.Engineer, p => Engineer.engineer = p },
        { RoleId.Sheriff, p => Sheriff.sheriff = p },
        { RoleId.Deputy, p => Deputy.deputy = p },
        { RoleId.Lighter, p => Lighter.lighter = p },
        { RoleId.Godfather, p => Godfather.godfather = p },
        { RoleId.Mafioso, p => Mafioso.mafioso = p },
        { RoleId.Janitor, p => Janitor.janitor = p },
        { RoleId.Detective, p => Detective.detective = p },
        { RoleId.TimeMaster, p => TimeMaster.timeMaster = p },
        { RoleId.Medic, p => Medic.medic = p },
        { RoleId.Shifter, p => Shifter.shifter = p },
        { RoleId.Swapper, p => Swapper.swapper = p },
        { RoleId.Seer, p => Seer.seer = p },
        { RoleId.Morphling, p => Morphling.morphling = p },
        { RoleId.Camouflager, p => Camouflager.camouflager = p },
        { RoleId.Hacker, p => Hacker.hacker = p },
        { RoleId.Tracker, p => Tracker.tracker = p },
        { RoleId.Vampire, p => Vampire.vampire = p },
        { RoleId.Snitch, p => Snitch.snitch = p },
        { RoleId.Jackal, p => Jackal.jackal = p },
        { RoleId.Sidekick, p => Sidekick.sidekick = p },
        { RoleId.Eraser, p => Eraser.eraser = p },
        { RoleId.Spy, p => Spy.spy = p },
        { RoleId.Trickster, p => Trickster.trickster = p },
        { RoleId.Cleaner, p => Cleaner.cleaner = p },
        { RoleId.Warlock, p => Warlock.warlock = p },
        { RoleId.SecurityGuard, p => SecurityGuard.securityGuard = p },
        { RoleId.Arsonist, p => Arsonist.arsonist = p },
        { RoleId.EvilGuesser, p => Guesser.evilGuesser = p },
        { RoleId.NiceGuesser, p => Guesser.niceGuesser = p },
        { RoleId.BountyHunter, p => BountyHunter.bountyHunter = p },
        { RoleId.Vulture, p => Vulture.vulture = p },
        { RoleId.Medium, p => Medium.medium = p },
        { RoleId.Trapper, p => Trapper.trapper = p },
        { RoleId.Lawyer, p => Lawyer.lawyer = p },
        { RoleId.Prosecutor, p => { Lawyer.lawyer = p; Lawyer.isProsecutor = true; } },
        { RoleId.Pursuer, p => Pursuer.pursuer = p },
        { RoleId.Witch, p => Witch.witch = p },
        { RoleId.Ninja, p => Ninja.ninja = p },
        { RoleId.Thief, p => Thief.thief = p },
        { RoleId.SchrodingerCat, p => SchrodingerCat.cat = p },
        { RoleId.Bomber, p => Bomber.bomber = p },
        { RoleId.Yoyo, p => Yoyo.yoyo = p },
    };

    public static void setRole(byte roleId, byte playerId)
    {
        var player = Helpers.playerById(playerId);
        if (player == null) return;

        clearAssignedRole(player);
        if (RoleSetters.TryGetValue((RoleId)roleId, out var setter))
            setter(player);

        // Keep Mira API's own RoleTypes in step, otherwise the game would still show whatever role
        // it handed this player first while TOR shows the new one.
        var behaviour = MiraAPI.Roles.CustomRoleManager.AllRoles
            .FirstOrDefault(x => x is Roles.ITorRole tor && tor.TorRoleId == (RoleId)roleId);
        if (behaviour != null && player.Data != null && player.Data.RoleType != behaviour.Role)
            RoleManager.Instance.SetRole(player, behaviour.Role);
    }

    /// <summary>Strips the TOR role this player is currently holding, modifiers stay untouched.</summary>
    public static void clearAssignedRole(PlayerControl player)
    {
        if (player == null) return;

        foreach (var (match, clear) in RoleClearers)
            if (match(player)) clear();
        if (Guesser.isGuesser(player.PlayerId)) Guesser.clear(player.PlayerId);
    }

    public static void setTorRole(RoleTypes roleType, PlayerControl player)
    {
        if (player == null) return;
        if (!MiraAPI.Roles.CustomRoleManager.GetCustomRoleBehaviour(roleType, out var behaviour)) return;
        if (behaviour is not Roles.ITorRole tor) return;

        if (RoleSetters.TryGetValue(tor.TorRoleId, out var setter))
            setter(player);
    }

    // Role functionality

    public static void cleanBody(byte playerId, byte cleaningPlayerId)
    {
        if (Medium.futureDeadBodies != null)
        {
            var deadBody = Medium.futureDeadBodies.Find(x => x.Item1.player.PlayerId == playerId).Item1;
            if (deadBody != null) deadBody.wasCleaned = true;
        }

        DeadBody[] array = Object.FindObjectsOfType<DeadBody>();
        for (var i = 0; i < array.Length; i++)
            if (GameData.Instance.GetPlayerById(array[i].ParentId).PlayerId == playerId)
                Object.Destroy(array[i].gameObject);

        if (Vulture.vulture != null && cleaningPlayerId == Vulture.vulture.PlayerId)
        {
            Vulture.eatenBodies++;
            if (Vulture.eatenBodies == Vulture.vultureNumberToWin) Vulture.triggerVultureWin = true;
        }
    }

    // Shows the broken time shield animation of the Time Master to every player

    public static void shifterShift(byte targetId)
    {
        var oldShifter = Shifter.shifter;
        var player = Helpers.playerById(targetId);
        if (player == null || oldShifter == null) return;

        Shifter.futureShift = null;
        Shifter.clearAndReload();

        // Suicide (exile) when impostor or impostor variants
        if ((player.Data.Role.IsImpostor || Helpers.isNeutral(player)) && !oldShifter.Data.IsDead)
        {
            oldShifter.Exiled();
            overrideDeathReasonAndKiller(oldShifter, DeadPlayer.CustomDeathReason.Shift, player);
            if (oldShifter == Lawyer.target && AmongUsClient.Instance.AmHost && Lawyer.lawyer != null)
            {
                PlayerControl.LocalPlayer.RpcLawyerPromotesToPursuer();
            }

            return;
        }

        Shifter.shiftRole(oldShifter, player);

        // Set cooldowns to max for both players
        if (PlayerControl.LocalPlayer == oldShifter || PlayerControl.LocalPlayer == player)
            Buttons.TorButtons.ResetAllCooldowns();
    }

    public static void deputyPromotes()
    {
        if (Deputy.deputy != null)
        {
            // Deputy should never be null here, but there appeared to be a race condition during testing, which was removed.
            Sheriff.replaceCurrentSheriff(Deputy.deputy);
            Sheriff.formerDeputy = Deputy.deputy;
            Deputy.deputy = null;
            // No clear and reload, as we need to keep the number of handcuffs left etc
        }
    }

    public static void jackalCreatesSidekick(byte targetId)
    {
        var player = Helpers.playerById(targetId);
        if (player == null) return;
        if (Lawyer.target == player && Lawyer.isProsecutor && Lawyer.lawyer != null && !Lawyer.lawyer.Data.IsDead)
            Lawyer.isProsecutor = false;

        if (!Jackal.canCreateSidekickFromImpostor && player.Data.Role.IsImpostor)
        {
            Jackal.fakeSidekick = player;
        }
        else
        {
            var wasSpy = Spy.spy != null && player == Spy.spy;
            var wasImpostor = player.Data.Role.IsImpostor; // This can only be reached if impostors can be sidekicked.
            FastDestroyableSingleton<RoleManager>.Instance.SetRole(player, RoleTypes.Crewmate);
            if (player == Lawyer.lawyer && Lawyer.target != null)
            {
                var playerInfoTransform = Lawyer.target.cosmetics.nameText.transform.parent.FindChild("Info");
                var playerInfo = playerInfoTransform != null ? playerInfoTransform.GetComponent<TextMeshPro>() : null;
                if (playerInfo != null) playerInfo.text = "";
            }

            erasePlayerRoles(player.PlayerId);
            Sidekick.sidekick = player;
            if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId) PlayerControl.LocalPlayer.moveable = true;
            if (wasSpy || wasImpostor) Sidekick.wasTeamRed = true;
            Sidekick.wasSpy = wasSpy;
            Sidekick.wasImpostor = wasImpostor;
            if (player == PlayerControl.LocalPlayer) SoundEffectsManager.play("jackalSidekick");
            if (HandleGuesser.isGuesserGm && OptionGroupSingleton<GuesserForcedOptions>.Instance.GamemodeSidekickIsAlwaysGuesser.Value &&
                !HandleGuesser.isGuesser(targetId))
                setGuesserGm(targetId);
        }

        Jackal.canCreateSidekick = false;
    }

    public static void sidekickPromotes()
    {
        Jackal.removeCurrentJackal();
        Jackal.jackal = Sidekick.sidekick;
        Jackal.canCreateSidekick = Jackal.jackalPromotedFromSidekickCanCreateSidekick;
        Jackal.wasTeamRed = Sidekick.wasTeamRed;
        Jackal.wasSpy = Sidekick.wasSpy;
        Jackal.wasImpostor = Sidekick.wasImpostor;
        Sidekick.clearAndReload();
    }

    private static readonly (Func<PlayerControl, bool> match, Action clear)[] RoleClearers =
    {
        (p => p == Mayor.mayor, Mayor.clearAndReload),
        (p => p == Portalmaker.portalmaker, Portalmaker.clearAndReload),
        (p => p == Engineer.engineer, Engineer.clearAndReload),
        (p => p == Sheriff.sheriff, Sheriff.clearAndReload),
        (p => p == Deputy.deputy, Deputy.clearAndReload),
        (p => p == Lighter.lighter, Lighter.clearAndReload),
        (p => p == Detective.detective, Detective.clearAndReload),
        (p => p == TimeMaster.timeMaster, TimeMaster.clearAndReload),
        (p => p == Medic.medic, Medic.clearAndReload),
        (p => p == Shifter.shifter, Shifter.clearAndReload),
        (p => p == Seer.seer, Seer.clearAndReload),
        (p => p == Hacker.hacker, Hacker.clearAndReload),
        (p => p == Tracker.tracker, Tracker.clearAndReload),
        (p => p == Snitch.snitch, Snitch.clearAndReload),
        (p => p == Swapper.swapper, Swapper.clearAndReload),
        (p => p == Spy.spy, Spy.clearAndReload),
        (p => p == SecurityGuard.securityGuard, SecurityGuard.clearAndReload),
        (p => p == Medium.medium, Medium.clearAndReload),
        (p => p == Trapper.trapper, Trapper.clearAndReload),
        (p => p == Morphling.morphling, Morphling.clearAndReload),
        (p => p == Camouflager.camouflager, Camouflager.clearAndReload),
        (p => p == Godfather.godfather, Godfather.clearAndReload),
        (p => p == Mafioso.mafioso, Mafioso.clearAndReload),
        (p => p == Janitor.janitor, Janitor.clearAndReload),
        (p => p == Vampire.vampire, Vampire.clearAndReload),
        (p => p == Eraser.eraser, Eraser.clearAndReload),
        (p => p == Trickster.trickster, Trickster.clearAndReload),
        (p => p == Cleaner.cleaner, Cleaner.clearAndReload),
        (p => p == Warlock.warlock, Warlock.clearAndReload),
        (p => p == Witch.witch, Witch.clearAndReload),
        (p => p == Ninja.ninja, Ninja.clearAndReload),
        (p => p == Bomber.bomber, Bomber.clearAndReload),
        (p => p == Yoyo.yoyo, Yoyo.clearAndReload),
        (p => p == Jester.jester, Jester.clearAndReload),
        (p => p == Arsonist.arsonist, Arsonist.clearAndReload),
        (p => p == Sidekick.sidekick, Sidekick.clearAndReload),
        (p => p == BountyHunter.bountyHunter, BountyHunter.clearAndReload),
        (p => p == Vulture.vulture, Vulture.clearAndReload),
        (p => p == Lawyer.lawyer, (Action)(() => Lawyer.clearAndReload(false))),
        (p => p == Pursuer.pursuer, Pursuer.clearAndReload),
        (p => p == Thief.thief, Thief.clearAndReload),
        (p => p == SchrodingerCat.cat, SchrodingerCat.clearAndReload),
    };

    private static readonly (Func<PlayerControl, bool> match, Action<byte> remove)[] ModifierRemovers =
    {
        (p => Bait.bait.Any(x => x.PlayerId == p.PlayerId), id => Bait.bait.RemoveAll(x => x.PlayerId == id)),
        (p => Bloody.bloody.Any(x => x.PlayerId == p.PlayerId), id => Bloody.bloody.RemoveAll(x => x.PlayerId == id)),
        (p => AntiTeleport.antiTeleport.Any(x => x.PlayerId == p.PlayerId), id => AntiTeleport.antiTeleport.RemoveAll(x => x.PlayerId == id)),
        (p => Sunglasses.sunglasses.Any(x => x.PlayerId == p.PlayerId), id => Sunglasses.sunglasses.RemoveAll(x => x.PlayerId == id)),
        (p => Vip.vip.Any(x => x.PlayerId == p.PlayerId), id => Vip.vip.RemoveAll(x => x.PlayerId == id)),
        (p => Invert.invert.Any(x => x.PlayerId == p.PlayerId), id => Invert.invert.RemoveAll(x => x.PlayerId == id)),
        (p => Chameleon.chameleon.Any(x => x.PlayerId == p.PlayerId), id => Chameleon.chameleon.RemoveAll(x => x.PlayerId == id)),
    };

    public static void erasePlayerRoles(byte playerId, bool ignoreModifier = true)
    {
        var player = Helpers.playerById(playerId);
        if (player == null || !player.canBeErased()) return;

        foreach (var (match, clear) in RoleClearers)
            if (match(player)) clear();

        if (Guesser.isGuesser(player.PlayerId)) Guesser.clear(player.PlayerId);
        if (player == Jackal.jackal)
        {
            if (Sidekick.promotesToJackal && Sidekick.sidekick != null && !Sidekick.sidekick.Data.IsDead)
                sidekickPromotes();
            else
                Jackal.clearAndReload();
        }

        if (!ignoreModifier)
        {
            if (player == Lovers.lover1 || player == Lovers.lover2)
                Lovers.clearAndReload();
            foreach (var (match, remove) in ModifierRemovers)
                if (match(player)) remove(player.PlayerId);
            if (player == Tiebreaker.tiebreaker) Tiebreaker.clearAndReload();
            if (player == Mini.mini) Mini.clearAndReload();
            if (player == Armored.armored) Armored.clearAndReload();
        }
    }

    public static void usePortal(byte playerId, byte exit)
    {
        Portal.startTeleport(playerId, exit);
    }

    public static void lawyerPromotesToPursuer()
    {
        var player = Lawyer.lawyer;
        var client = Lawyer.target;
        Lawyer.clearAndReload(false);

        Pursuer.pursuer = player;

        if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId && client != null)
        {
            var playerInfoTransform = client.cosmetics.nameText.transform.parent.FindChild("Info");
            var playerInfo = playerInfoTransform != null ? playerInfoTransform.GetComponent<TextMeshPro>() : null;
            if (playerInfo != null) playerInfo.text = "";
        }
    }

    public static void guesserShoot(byte killerId, byte dyingTargetId, byte guessedTargetId, byte guessedRoleId)
    {
        var dyingTarget = Helpers.playerById(dyingTargetId);
        if (dyingTarget == null) return;
        if (Lawyer.target != null && dyingTarget == Lawyer.target)
            Lawyer.targetWasGuessed = true; // Lawyer shouldn't be exiled with the client for guesses
        var dyingLoverPartner = Lovers.bothDie ? dyingTarget.getPartner() : null; // Lover check
        if (Lawyer.target != null && dyingLoverPartner == Lawyer.target)
            Lawyer.targetWasGuessed = true; // Lawyer shouldn't be exiled with the client for guesses

        var guesser = Helpers.playerById(killerId);
        if (Thief.thief != null && Thief.thief.PlayerId == killerId && Thief.canStealWithGuess)
        {
            var roleInfo = CustomRoleManager.Instance.allRoleInfos.FirstOrDefault(x => (byte)x.roleId == guessedRoleId);
            if (!Thief.thief.Data.IsDead && !Thief.isFailedThiefKill(dyingTarget, guesser, roleInfo))
                thiefStealsRole(dyingTarget.PlayerId);
        }

        var lawyerDiedAdditionally = false;
        if (Lawyer.lawyer != null && !Lawyer.isProsecutor && Lawyer.lawyer.PlayerId == killerId &&
            Lawyer.target != null && Lawyer.target.PlayerId == dyingTargetId)
        {
            // Lawyer guessed client.
            if (PlayerControl.LocalPlayer == Lawyer.lawyer)
            {
                FastDestroyableSingleton<HudManager>.Instance.KillOverlay.ShowKillAnimation(Lawyer.lawyer.Data,
                    Lawyer.lawyer.Data);
                if (MeetingHudPatch.guesserUI != null) MeetingHudPatch.guesserUIExitButton.OnClick.Invoke();
            }

            Lawyer.lawyer.Exiled();
            lawyerDiedAdditionally = true;
            overrideDeathReasonAndKiller(Lawyer.lawyer, DeadPlayer.CustomDeathReason.LawyerSuicide, guesser);
        }

        SchrodingerCat.skipRevival = true;
        dyingTarget.Exiled();
        overrideDeathReasonAndKiller(dyingTarget, DeadPlayer.CustomDeathReason.Guess, guesser);
        var partnerId = dyingLoverPartner != null ? dyingLoverPartner.PlayerId : dyingTargetId;

        HandleGuesser.remainingShots(killerId, true);
        if (Constants.ShouldPlaySfx()) SoundManager.Instance.PlaySound(dyingTarget.KillSfx, false, 0.8f);
        if (MeetingHud.Instance)
        {
            foreach (var pva in MeetingHud.Instance.playerStates)
            {
                if (pva.PlayerId == dyingTargetId || pva.PlayerId == partnerId ||
                    (lawyerDiedAdditionally && Lawyer.lawyer.PlayerId == pva.PlayerId))
                {
                    pva.SetDead(true);
                    pva.Overlay.gameObject.SetActive(true);
                    MeetingHudPatch.swapperCheckAndReturnSwap(MeetingHud.Instance, pva.PlayerId);
                }

                //Give players back their vote if target is shot dead
                if (pva.VotedForId != dyingTargetId && pva.VotedForId != partnerId &&
                    (!lawyerDiedAdditionally || Lawyer.lawyer.PlayerId != pva.VotedForId)) continue;
                pva.UnsetVote();
                var voteAreaPlayer = Helpers.playerById(pva.PlayerId);
                if (!voteAreaPlayer.AmOwner) continue;
                MeetingHud.Instance.ClearVote(pva.PlayerId, true);
            }

            if (AmongUsClient.Instance.AmHost)
                MeetingHud.Instance.CheckForEndVoting();
        }

        if (FastDestroyableSingleton<HudManager>.Instance != null && guesser != null)
            if (PlayerControl.LocalPlayer == dyingTarget)
            {
                FastDestroyableSingleton<HudManager>.Instance.KillOverlay.ShowKillAnimation(guesser.Data,
                    dyingTarget.Data);
                if (MeetingHudPatch.guesserUI != null) MeetingHudPatch.guesserUIExitButton.OnClick.Invoke();
            }
            else if (dyingLoverPartner != null && PlayerControl.LocalPlayer == dyingLoverPartner)
            {
                FastDestroyableSingleton<HudManager>.Instance.KillOverlay.ShowKillAnimation(dyingLoverPartner.Data,
                    dyingLoverPartner.Data);
                if (MeetingHudPatch.guesserUI != null) MeetingHudPatch.guesserUIExitButton.OnClick.Invoke();
            }

        // remove shoot button from targets for all guessers and close their guesserUI
        if (GuesserGM.isGuesser(PlayerControl.LocalPlayer.PlayerId) && PlayerControl.LocalPlayer != guesser &&
            !PlayerControl.LocalPlayer.Data.IsDead &&
            GuesserGM.remainingShots(PlayerControl.LocalPlayer.PlayerId) > 0 && MeetingHud.Instance)
        {
            MeetingHud.Instance.playerStates.ToList().ForEach(x =>
            {
                if (x.PlayerId == dyingTarget.PlayerId && x.transform.FindChild("ShootButton") != null)
                    Object.Destroy(x.transform.FindChild("ShootButton").gameObject);
            });
            if (dyingLoverPartner != null)
                MeetingHud.Instance.playerStates.ToList().ForEach(x =>
                {
                    if (x.PlayerId == dyingLoverPartner.PlayerId && x.transform.FindChild("ShootButton") != null)
                        Object.Destroy(x.transform.FindChild("ShootButton").gameObject);
                });

            if (MeetingHudPatch.guesserUI != null && MeetingHudPatch.guesserUIExitButton != null)
            {
                if (MeetingHudPatch.guesserCurrentTarget == dyingTarget.PlayerId)
                    MeetingHudPatch.guesserUIExitButton.OnClick.Invoke();
                else if (dyingLoverPartner != null &&
                         MeetingHudPatch.guesserCurrentTarget == dyingLoverPartner.PlayerId)
                    MeetingHudPatch.guesserUIExitButton.OnClick.Invoke();
            }
        }

        var guessedTarget = Helpers.playerById(guessedTargetId);
        if (PlayerControl.LocalPlayer.Data.IsDead && guessedTarget != null && guesser != null)
        {
            var roleInfo = CustomRoleManager.Instance.allRoleInfos.FirstOrDefault(x => (byte)x.roleId == guessedRoleId);
            var msg =
                string.Format(ModTranslation.GetString("Game-Normal", 5), guesser.Data.PlayerName, roleInfo?.name ?? "", guessedTarget.Data.PlayerName);
            if (AmongUsClient.Instance.AmClient && FastDestroyableSingleton<HudManager>.Instance)
                FastDestroyableSingleton<HudManager>.Instance.Chat.AddChat(guesser, msg);
            if (msg.IndexOf("who", StringComparison.OrdinalIgnoreCase) >= 0)
                FastDestroyableSingleton<UnityTelemetry>.Instance.SendWho();
        }
    }

    private static readonly (Func<PlayerControl, bool> match, Action<PlayerControl> assign)[] ThiefRoleAssigners =
    {
        (t => t == Sheriff.sheriff, (thief) => Sheriff.sheriff = thief),
        (t => t == Guesser.evilGuesser, (thief) => Guesser.evilGuesser = thief),
        (t => t == Godfather.godfather, (thief) => Godfather.godfather = thief),
        (t => t == Mafioso.mafioso, (thief) => Mafioso.mafioso = thief),
        (t => t == Janitor.janitor, (thief) => Janitor.janitor = thief),
        (t => t == Morphling.morphling, (thief) => Morphling.morphling = thief),
        (t => t == Camouflager.camouflager, (thief) => Camouflager.camouflager = thief),
        (t => t == Vampire.vampire, (thief) => Vampire.vampire = thief),
        (t => t == Eraser.eraser, (thief) => Eraser.eraser = thief),
        (t => t == Trickster.trickster, (thief) => Trickster.trickster = thief),
        (t => t == Cleaner.cleaner, (thief) => Cleaner.cleaner = thief),
        (t => t == Warlock.warlock, (thief) => Warlock.warlock = thief),
        (t => t == BountyHunter.bountyHunter, (thief) => BountyHunter.bountyHunter = thief),
        (t => t == Ninja.ninja, (thief) => Ninja.ninja = thief),
        (t => t == Bomber.bomber, (thief) => Bomber.bomber = thief),
    };

    public static void thiefStealsRole(byte playerId)
    {
        var target = Helpers.playerById(playerId);
        var thief = Thief.thief;
        if (target == null) return;

        // Simple role reassignment via lookup
        foreach (var (match, assign) in ThiefRoleAssigners)
            if (match(target)) assign(thief);

        // Special cases with extra logic
        if (target == Jackal.jackal)
        {
            Jackal.jackal = thief;
            Jackal.formerJackals.Add(target);
        }
        else if (target == Sidekick.sidekick)
        {
            Sidekick.sidekick = thief;
            Jackal.formerJackals.Add(target);
            if (HandleGuesser.isGuesserGm && OptionGroupSingleton<GuesserForcedOptions>.Instance.GamemodeSidekickIsAlwaysGuesser.Value &&
                !HandleGuesser.isGuesser(thief.PlayerId))
                setGuesserGm(thief.PlayerId);
        }

        if (target == Witch.witch)
        {
            Witch.witch = thief;
            if (MeetingHud.Instance)
            {
                if (Witch.witchVoteSavesTargets)
                    Witch.futureSpelled = new List<PlayerControl>();
                else
                    Witch.futureSpelled.RemoveAll(x => x.PlayerId == thief.PlayerId);
            }
        }

        if (target == Yoyo.yoyo)
        {
            Yoyo.yoyo = thief;
            Yoyo.markedLocation = null;
        }

        if (target.Data.Role.IsImpostor)
        {
            FastDestroyableSingleton<RoleManager>.Instance.SetRole(Thief.thief, RoleTypes.Impostor);
            FastDestroyableSingleton<HudManager>.Instance.KillButton.SetCoolDown(Thief.thief.killTimer,
                GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown);
        }

        if (Lawyer.lawyer != null && target == Lawyer.target)
            Lawyer.target = thief;
        if (Thief.thief == PlayerControl.LocalPlayer) Buttons.TorButtons.ResetAllCooldowns();
        Thief.clearAndReload();
        Thief.formerThief = thief;
    }

    public static void setGuesserGm(byte playerId)
    {
        var target = Helpers.playerById(playerId);
        if (target == null) return;
        new GuesserGM(target);
    }

    public static void yoyoBlink(bool isFirstJump, byte[] buff)
    {
        if (Yoyo.yoyo == null || Yoyo.markedLocation == null) return;
        var markedPos = (Vector3)Yoyo.markedLocation;
        Yoyo.yoyo.NetTransform.SnapTo(markedPos);

        var markedSilhouette = Silhouette.silhouettes.FirstOrDefault(s =>
            s.gameObject.transform.position.x == markedPos.x && s.gameObject.transform.position.y == markedPos.y);
        if (markedSilhouette != null)
            markedSilhouette.permanent = false;

        var position = Vector3.zero;
        position.x = BitConverter.ToSingle(buff, 0 * sizeof(float));
        position.y = BitConverter.ToSingle(buff, 1 * sizeof(float));
        // Create Silhoutte At Start Position:
        if (isFirstJump)
        {
            Yoyo.markLocation(position);
            new Silhouette(position, Yoyo.blinkDuration);
        }
        else
        {
            new Silhouette(position, 5);
            Yoyo.markedLocation = null;
        }

        if (Chameleon.chameleon.Any(x => x.PlayerId == Yoyo.yoyo.PlayerId)) // Make the Yoyo visible if chameleon!
            Chameleon.lastMoved[Yoyo.yoyo.PlayerId] = Time.time;
    }

}

