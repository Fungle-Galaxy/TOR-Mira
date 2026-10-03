using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using InnerNet;
using MiraAPI.Hud;
using TheOtherRoles.CustomGameModes;
using TheOtherRoles.Utilities;
using UnityEngine;

namespace TheOtherRoles.Roles;

public class CustomRoleManager
{
    private static CustomRoleManager _instance;

    // Special RoleInfos (no corresponding role class)
    public static RoleInfo impostor = new(Palette.ImpostorRed, RoleId.Impostor);

    public static RoleInfo crewmate = new(Color.white, RoleId.Crewmate);

    public static RoleInfo hunter = new(Palette.ImpostorRed, RoleId.Impostor);

    public static RoleInfo hunted = new(Color.white, RoleId.Crewmate);

    public static RoleInfo prop = new(Color.white, RoleId.Crewmate);

    private static string ReadmePage = "";

    private readonly List<ITorRoleLifecycle> _roles = new();

    // MiraAPI registers TOR's roles (they are RoleBehaviour components) while scanning the plugin,
    // which happens outside TOR's own startup - so they are pulled in lazily.
    private int linkedMiraRoleCount = -1;

    public static CustomRoleManager Instance => _instance ??= new CustomRoleManager();

    private void LinkMiraRoles()
    {
        var miraRoles = MiraAPI.Roles.CustomRoleManager.CustomMiraRoles;
        if (miraRoles.Count == linkedMiraRoleCount) return;

        linkedMiraRoleCount = miraRoles.Count;
        foreach (var role in miraRoles)
        {
            if (role is ITorRoleLifecycle tor && _roles.All(r => r.GetType() != role.GetType()))
                _roles.Add(tor);
        }
    }

    public List<RoleInfo> allRoleInfos
    {
        get
        {
            LinkMiraRoles();

            var infos = new List<RoleInfo>();
            infos.Add(impostor);
            infos.Add(crewmate);
            foreach (var role in _roles)
            {
                var info = role.GetRoleInfo();
                if (info != null) infos.Add(info);
            }

            return infos;
        }
    }

    public void ClearAndReloadAll()
    {
        LinkMiraRoles();
        foreach (var role in _roles)
            role.ClearAndReload();
    }

    // ── Lifecycle Dispatch ────────────────────────────────────────
    public void PlayerFixedUpdate(PlayerControl player)
    {
        LinkMiraRoles();
        foreach (var role in _roles)
            role.PlayerFixedUpdate(player);
    }

    public void PlayerUpdate(PlayerControl player)
    {
        LinkMiraRoles();
        foreach (var role in _roles)
            role.PlayerUpdate(player);
    }

    public void OnMeetingStart()
    {
        LinkMiraRoles();
        foreach (var role in _roles)
            role.OnMeetingStart();
    }

    public void OnMeetingEnd()
    {
        LinkMiraRoles();
        foreach (var role in _roles)
            role.OnMeetingEnd();
    }

    public void OnPlayerExiled(PlayerControl player)
    {
        LinkMiraRoles();
        foreach (var role in _roles)
            role.OnPlayerExiled(player);
    }

    public void OnPlayerDeath(PlayerControl player)
    {
        LinkMiraRoles();
        foreach (var role in _roles)
            role.OnPlayerDeath(player);
    }

    public void OnMurderPlayer(PlayerControl killer, PlayerControl victim)
    {
        LinkMiraRoles();
        foreach (var role in _roles)
            role.OnMurderPlayer(killer, victim);
    }

    public bool CanUseVent(PlayerControl player, Vent vent)
    {
        LinkMiraRoles();
        foreach (var role in _roles)
            if (role.CanUseVent(player, vent))
                return true;
        return false;
    }

    public bool CanKill(PlayerControl killer, PlayerControl target)
    {
        LinkMiraRoles();
        foreach (var role in _roles)
            if (role.CanKill(killer, target))
                return true;
        return false;
    }

    public void SetTarget(PlayerControl target)
    {
        LinkMiraRoles();
        foreach (var role in _roles)
            role.SetTarget(target);
    }

    public void OnClickButton()
    {
        LinkMiraRoles();
        foreach (var role in _roles)
            role.OnClickButton();
    }

    public static void Reset()
    {
        _instance = null;
    }

    public static List<RoleInfo> getRoleInfoForPlayer(PlayerControl p, bool showModifier = true)
    {
        var infos = new List<RoleInfo>();
        if (p == null) return infos;

        // Modifier
        if (showModifier)
        {
            if (!OptionGroupSingleton<ModifierSettingsOptions>.Instance.ModifiersAreHidden.Value || PlayerControl.LocalPlayer.Data.IsDead ||
                AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Ended)
            {
                if (Bait.bait.Any(x => x.PlayerId == p.PlayerId)) infos.Add(Bait.Info);
                if (Bloody.bloody.Any(x => x.PlayerId == p.PlayerId)) infos.Add(Bloody.Info);
                if (Vip.vip.Any(x => x.PlayerId == p.PlayerId)) infos.Add(Vip.Info);
            }

            if (p == Lovers.lover1 || p == Lovers.lover2) infos.Add(Lovers.Info);
            if (p == Tiebreaker.tiebreaker) infos.Add(Tiebreaker.Info);
            if (AntiTeleport.antiTeleport.Any(x => x.PlayerId == p.PlayerId)) infos.Add(AntiTeleport.Info);
            if (Sunglasses.sunglasses.Any(x => x.PlayerId == p.PlayerId)) infos.Add(Sunglasses.Info);
            if (p == Mini.mini) infos.Add(Mini.Info);
            if (Invert.invert.Any(x => x.PlayerId == p.PlayerId)) infos.Add(Invert.Info);
            if (Chameleon.chameleon.Any(x => x.PlayerId == p.PlayerId)) infos.Add(Chameleon.Info);
            if (p == Armored.armored) infos.Add(Armored.Info);
            if (p == Shifter.shifter) infos.Add(Shifter.Info);
        }

        var count = infos.Count;

        // Special roles
        if (p == Jester.jester) infos.Add(Jester.Info);
        if (p == Mayor.mayor) infos.Add(Mayor.Info);
        if (p == Portalmaker.portalmaker) infos.Add(Portalmaker.Info);
        if (p == Engineer.engineer) infos.Add(Engineer.Info);
        if (p == Sheriff.sheriff || p == Sheriff.formerSheriff) infos.Add(Sheriff.Info);
        if (p == Deputy.deputy) infos.Add(Deputy.Info);
        if (p == Lighter.lighter) infos.Add(Lighter.Info);
        if (p == Godfather.godfather) infos.Add(Godfather.Info);
        if (p == Mafioso.mafioso) infos.Add(Mafioso.Info);
        if (p == Janitor.janitor) infos.Add(Janitor.Info);
        if (p == Morphling.morphling) infos.Add(Morphling.Info);
        if (p == Camouflager.camouflager) infos.Add(Camouflager.Info);
        if (p == Vampire.vampire) infos.Add(Vampire.Info);
        if (p == Eraser.eraser) infos.Add(Eraser.Info);
        if (p == Trickster.trickster) infos.Add(Trickster.Info);
        if (p == Cleaner.cleaner) infos.Add(Cleaner.Info);
        if (p == Warlock.warlock) infos.Add(Warlock.Info);
        if (p == Witch.witch) infos.Add(Witch.Info);
        if (p == Ninja.ninja) infos.Add(Ninja.Info);
        if (p == Bomber.bomber) infos.Add(Bomber.Info);
        if (p == Yoyo.yoyo) infos.Add(Yoyo.Info);
        if (p == Detective.detective) infos.Add(Detective.Info);
        if (p == TimeMaster.timeMaster) infos.Add(TimeMaster.Info);
        if (p == Medic.medic) infos.Add(Medic.Info);
        if (p == Swapper.swapper) infos.Add(Swapper.Info);
        if (p == Seer.seer) infos.Add(Seer.Info);
        if (p == Hacker.hacker) infos.Add(Hacker.Info);
        if (p == Tracker.tracker) infos.Add(Tracker.Info);
        if (p == Snitch.snitch) infos.Add(Snitch.Info);
        if (p == Jackal.jackal ||
            (Jackal.formerJackals != null && Jackal.formerJackals.Any(x => x.PlayerId == p.PlayerId)))
            infos.Add(Jackal.Info);
        if (p == Sidekick.sidekick) infos.Add(Sidekick.Info);
        if (p == Spy.spy) infos.Add(Spy.Info);
        if (p == SecurityGuard.securityGuard) infos.Add(SecurityGuard.Info);
        if (p == Arsonist.arsonist) infos.Add(Arsonist.Info);
        if (p == Guesser.niceGuesser) infos.Add(Guesser.NiceInfo);
        if (p == Guesser.evilGuesser) infos.Add(Guesser.EvilInfo);
        if (p == BountyHunter.bountyHunter) infos.Add(BountyHunter.Info);
        if (p == Vulture.vulture) infos.Add(Vulture.Info);
        if (p == Medium.medium) infos.Add(Medium.Info);
        if (p == Lawyer.lawyer && !Lawyer.isProsecutor) infos.Add(Lawyer.Info);
        if (p == Lawyer.lawyer && Lawyer.isProsecutor) infos.Add(Lawyer.ProsecutorInfo);
        if (p == Trapper.trapper) infos.Add(Trapper.Info);
        if (p == Pursuer.pursuer) infos.Add(Pursuer.Info);
        if (p == Thief.thief) infos.Add(Thief.Info);
        if (p == SchrodingerCat.cat)
        {
            // Hide role: show as crewmate if cat has no team, is alive, and hideRole is enabled
            if (SchrodingerCat.hideRole && !SchrodingerCat.hasTeam() && !PlayerControl.LocalPlayer.Data.IsDead)
                infos.Add(crewmate);
            else
                infos.Add(SchrodingerCat.Info);
        }

        // Default roles
        if (infos.Count == count)
        {
            if (p.Data.Role.IsImpostor)
                infos.Add(HideNSeek.isHideNSeekGM || PropHunt.isPropHuntGM ? hunter : impostor);
            else
                infos.Add(HideNSeek.isHideNSeekGM ? hunted : PropHunt.isPropHuntGM ? prop : crewmate);
        }

        return infos;
    }

    public static string GetRolesString(PlayerControl p, bool useColors, bool showModifier = true,
        bool suppressGhostInfo = false)
    {
        string roleName;
        roleName = string.Join(" ",
            getRoleInfoForPlayer(p, showModifier).Select(x => useColors ? Helpers.cs(x.color, x.name) : x.name)
                .ToArray());
        if (Lawyer.target != null && p.PlayerId == Lawyer.target.PlayerId && PlayerControl.LocalPlayer != Lawyer.target)
            roleName += useColors ? Helpers.cs(Pursuer.color, " §") : " §";
        if (HandleGuesser.isGuesserGm && HandleGuesser.isGuesser(p.PlayerId))
        {
            var remainingShots = HandleGuesser.remainingShots(p.PlayerId);
            var (playerCompleted, playerTotal) = TasksHandler.taskInfo(p.Data);
            var guesserTag = ModTranslation.GetString("RoleInfo-Text", 7);
            if ((!Helpers.isEvil(p) && playerCompleted < HandleGuesser.tasksToUnlock) || remainingShots == 0)
                roleName += Helpers.cs(Color.gray, guesserTag);
            else
                roleName += Helpers.cs(Color.white, guesserTag);
        }

        if (!suppressGhostInfo && p != null)
        {
            if (p == Shifter.shifter &&
                (PlayerControl.LocalPlayer == Shifter.shifter || Helpers.shouldShowGhostInfo()) &&
                Shifter.futureShift != null)
                roleName += Helpers.cs(Color.yellow,
                    string.Format(ModTranslation.GetString("RoleInfo-Text", 8), Shifter.futureShift.Data.PlayerName));
            if (p == Vulture.vulture && (PlayerControl.LocalPlayer == Vulture.vulture || Helpers.shouldShowGhostInfo()))
                roleName = roleName + Helpers.cs(Vulture.color,
                    string.Format(ModTranslation.GetString("RoleInfo-Text", 9),
                        Vulture.vultureNumberToWin - Vulture.eatenBodies));
            if (Helpers.shouldShowGhostInfo())
            {
                if (Eraser.futureErased.Contains(p))
                    roleName = Helpers.cs(Color.gray, ModTranslation.GetString("RoleInfo-Text", 10)) + roleName;
                if (Vampire.vampire != null && !Vampire.vampire.Data.IsDead && Vampire.bitten == p && !p.Data.IsDead)
                    roleName = Helpers.cs(Vampire.color,
                        string.Format(ModTranslation.GetString("RoleInfo-Text", 11),
                            CustomButtonSingleton<VampireKillButton>.Instance.Timer + 1)) + roleName;
                if (Deputy.handcuffedPlayers.Contains(p.PlayerId))
                    roleName = Helpers.cs(Color.gray, ModTranslation.GetString("RoleInfo-Text", 12)) + roleName;
                if (Deputy.handcuffedKnows.ContainsKey(p.PlayerId))
                    roleName = Helpers.cs(Deputy.color, ModTranslation.GetString("RoleInfo-Text", 12)) + roleName;
                if (p == Warlock.curseVictim)
                    roleName = Helpers.cs(Warlock.color, ModTranslation.GetString("RoleInfo-Text", 13)) + roleName;
                if (p == Ninja.ninjaMarked)
                    roleName = Helpers.cs(Ninja.color, ModTranslation.GetString("RoleInfo-Text", 14)) + roleName;
                if (Pursuer.blankedList.Contains(p) && !p.Data.IsDead)
                    roleName = Helpers.cs(Pursuer.color, ModTranslation.GetString("RoleInfo-Text", 15)) + roleName;
                if (Witch.futureSpelled.Contains(p) && !MeetingHud.Instance)
                    roleName = Helpers.cs(Witch.color, "☆ ") + roleName;
                if (BountyHunter.bounty == p)
                    roleName = Helpers.cs(BountyHunter.color, ModTranslation.GetString("RoleInfo-Text", 16)) + roleName;
                if (Arsonist.dousedPlayers.Contains(p))
                    roleName = Helpers.cs(Arsonist.color, "♨ ") + roleName;
                if (p == Arsonist.arsonist)
                    roleName = roleName + Helpers.cs(Arsonist.color,
                        string.Format(ModTranslation.GetString("RoleInfo-Text", 9),
                            PlayerControl.AllPlayerControls.ToArray().Count(x => { return x != Arsonist.arsonist && !x.Data.IsDead && !x.Data.Disconnected && !Arsonist.dousedPlayers.Any(y => y.PlayerId == x.PlayerId); })));
                if (p == Jackal.fakeSidekick)
                    roleName = Helpers.cs(Sidekick.color, ModTranslation.GetString("RoleInfo-Text", 17)) + roleName;

                // Death Reason on Ghosts
                if (p.Data.IsDead)
                {
                    var deathReasonString = "";
                    var deadPlayer = GameHistory.deadPlayers.FirstOrDefault(x => x.player.PlayerId == p.PlayerId);

                    Color killerColor = new();
                    if (deadPlayer != null && deadPlayer.killerIfExisting != null)
                        killerColor = getRoleInfoForPlayer(deadPlayer.killerIfExisting, false).FirstOrDefault().color;

                    if (deadPlayer != null)
                    {
                        switch (deadPlayer.deathReason)
                        {
                            case DeadPlayer.CustomDeathReason.Disconnect:
                                deathReasonString = ModTranslation.GetString("RoleInfo-Text", 18);
                                break;
                            case DeadPlayer.CustomDeathReason.Exile:
                                deathReasonString = ModTranslation.GetString("RoleInfo-Text", 19);
                                break;
                            case DeadPlayer.CustomDeathReason.Kill:
                                deathReasonString = string.Format(ModTranslation.GetString("RoleInfo-Text", 20),
                                    Helpers.cs(killerColor, deadPlayer.killerIfExisting.Data.PlayerName));
                                break;
                            case DeadPlayer.CustomDeathReason.Guess:
                                if (deadPlayer.killerIfExisting.Data.PlayerName == p.Data.PlayerName)
                                    deathReasonString = ModTranslation.GetString("RoleInfo-Text", 21);
                                else
                                    deathReasonString = string.Format(ModTranslation.GetString("RoleInfo-Text", 22),
                                        Helpers.cs(killerColor, deadPlayer.killerIfExisting.Data.PlayerName));
                                break;
                            case DeadPlayer.CustomDeathReason.Shift:
                                deathReasonString = string.Format(ModTranslation.GetString("RoleInfo-Text", 24),
                                    Helpers.cs(Color.yellow, ModTranslation.GetString("RoleInfo-Text", 23)),
                                    Helpers.cs(killerColor, deadPlayer.killerIfExisting.Data.PlayerName));
                                break;
                            case DeadPlayer.CustomDeathReason.WitchExile:
                                deathReasonString = string.Format(ModTranslation.GetString("RoleInfo-Text", 26),
                                    Helpers.cs(Witch.color, ModTranslation.GetString("RoleInfo-Text", 25)),
                                    Helpers.cs(killerColor, deadPlayer.killerIfExisting.Data.PlayerName));
                                break;
                            case DeadPlayer.CustomDeathReason.LoverSuicide:
                                deathReasonString = Helpers.cs(Lovers.color, ModTranslation.GetString("RoleInfo-Text", 27));
                                break;
                            case DeadPlayer.CustomDeathReason.LawyerSuicide:
                                deathReasonString = Helpers.cs(Lawyer.color, ModTranslation.GetString("RoleInfo-Text", 28));
                                break;
                            case DeadPlayer.CustomDeathReason.Bomb:
                                deathReasonString = string.Format(ModTranslation.GetString("RoleInfo-Text", 29),
                                    Helpers.cs(killerColor, deadPlayer.killerIfExisting.Data.PlayerName));
                                break;
                            case DeadPlayer.CustomDeathReason.Arson:
                                deathReasonString = string.Format(ModTranslation.GetString("RoleInfo-Text", 30),
                                    Helpers.cs(killerColor, deadPlayer.killerIfExisting.Data.PlayerName));
                                break;
                        }

                        roleName = roleName + deathReasonString;
                    }
                }
            }
        }

        return roleName;
    }
}