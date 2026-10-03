using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TheOtherRoles.CustomGameModes;
using TheOtherRoles.Utilities;
using UnityEngine;

namespace TheOtherRoles.Patches;

internal enum CustomGameOverReason
{
    LoversWin = 10,
    TeamJackalWin = 11,
    MiniLose = 12,
    JesterWin = 13,
    ArsonistWin = 14,
    VultureWin = 15,
    ProsecutorWin = 16
}

internal enum WinCondition
{
    Default,
    LoversTeamWin,
    LoversSoloWin,
    JesterWin,
    JackalWin,
    MiniLose,
    ArsonistWin,
    VultureWin,
    AdditionalLawyerBonusWin,
    AdditionalAlivePursuerWin,
    ProsecutorWin
}

internal static class AdditionalTempData
{
    // Should be implemented using a proper GameOverReason in the future
    public static WinCondition winCondition = WinCondition.Default;
    public static List<WinCondition> additionalWinConditions = new();
    public static List<PlayerRoleInfo> playerRoles = new();
    public static float timer;

    public static void clear()
    {
        playerRoles.Clear();
        additionalWinConditions.Clear();
        winCondition = WinCondition.Default;
        timer = 0;
    }

    internal class PlayerRoleInfo
    {
        public string PlayerName { get; set; }
        public List<RoleInfo> Roles { get; set; }
        public string RoleNames { get; set; }
        public int TasksCompleted { get; set; }
        public int TasksTotal { get; set; }
        public bool IsGuesser { get; set; }
        public int? Kills { get; set; }
        public bool IsAlive { get; set; }
    }
}

// TOR verdict on who won. Mira API builds EndGameResult.CachedWinners from Role.DidWin, which
// TorRoleBehaviour answers from here; players who died carry a vanilla ghost role instead of their
// TOR role, so the verdict still has to be applied - from EndGameManager.SetEverythingUp, which the
// game calls after every patch on AmongUsClient.OnGameEnd.
internal static class TorEndGameWinners
{
    private struct Winner
    {
        public PlayerControl Player;
        public bool IsImpostor;
    }

    private static readonly List<Winner> winners = new();

    private static HashSet<string> winnerNames = new();
    private static Il2CppSystem.Collections.Generic.List<CachedPlayerData> customSnapshot;
    private static bool snapshotTaken;

    public static WinCondition Outcome { get; private set; } = WinCondition.Default;

    public static List<WinCondition> Additional { get; } = new();

    public static bool DidWin(RoleBehaviour role, GameOverReason reason)
    {
        var player = PlayerOf(role);
        if (player == null) return false;

        Build(reason);
        var id = player.PlayerId;
        return winners.Exists(x => x.Player.PlayerId == id);
    }

    public static void TakeSnapshot(GameOverReason reason)
    {
        Build(reason);

        winnerNames = new HashSet<string>();
        foreach (var winner in winners) winnerNames.Add(winner.Player.Data.PlayerName);

        customSnapshot = null;
        if (Outcome != WinCondition.Default)
        {
            customSnapshot = new Il2CppSystem.Collections.Generic.List<CachedPlayerData>();
            foreach (var winner in winners)
            {
                var data = new CachedPlayerData(winner.Player.Data) { IsImpostor = winner.IsImpostor };
                // The Mini lost - show the entry, but nobody gets a victory banner out of it.
                if (reason == (GameOverReason)CustomGameOverReason.MiniLose && winner.Player == Mini.mini)
                    data.IsYou = false;
                customSnapshot.Add(data);
            }
        }

        snapshotTaken = true;
    }

    public static void Apply()
    {
        if (!snapshotTaken || EndGameResult.CachedWinners == null) return;
        snapshotTaken = false;

        if (Outcome != WinCondition.Default)
        {
            EndGameResult.CachedWinners = customSnapshot;
            return;
        }

        // TorRoleBehaviour.DidWin already put TOR winners here. Players who died carry a vanilla
        // ghost role and claim wins TOR does not give them, so drop exactly those.
        if (winnerNames.Count == 0) return;
        var drop = new List<CachedPlayerData>();
        foreach (var winner in EndGameResult.CachedWinners.ToArray())
            if (!winnerNames.Contains(winner.PlayerName))
                drop.Add(winner);
        foreach (var winner in drop) EndGameResult.CachedWinners.Remove(winner);
    }

    private static PlayerControl PlayerOf(RoleBehaviour role)
    {
        foreach (var player in PlayerControl.AllPlayerControls)
            if (player != null && player.Data != null && player.Data.Role == role)
                return player;
        return null;
    }

    private static void Add(PlayerControl player, bool? isImpostor = null)
    {
        if (player == null || player.Data == null) return;
        var id = player.PlayerId;
        if (winners.Exists(x => x.Player.PlayerId == id)) return;
        winners.Add(new Winner
        {
            Player = player,
            IsImpostor = isImpostor ?? (player.Data.Role != null && player.Data.Role.IsImpostor)
        });
    }

    private static void Build(GameOverReason reason)
    {
        winners.Clear();
        Additional.Clear();
        Outcome = WinCondition.Default;

        var jesterWin = Jester.jester != null && reason == (GameOverReason)CustomGameOverReason.JesterWin;
        var arsonistWin = Arsonist.arsonist != null && reason == (GameOverReason)CustomGameOverReason.ArsonistWin;
        var miniLose = Mini.mini != null && reason == (GameOverReason)CustomGameOverReason.MiniLose;
        var loversWin = Lovers.existingAndAlive() &&
                        (reason == (GameOverReason)CustomGameOverReason.LoversWin ||
                         (GameManager.Instance.DidHumansWin(reason) && !Lovers.existingWithKiller()));
        var teamJackalWin = reason == (GameOverReason)CustomGameOverReason.TeamJackalWin &&
                            ((Jackal.jackal != null && !Jackal.jackal.Data.IsDead) ||
                             (Sidekick.sidekick != null && !Sidekick.sidekick.Data.IsDead));
        var vultureWin = Vulture.vulture != null && reason == (GameOverReason)CustomGameOverReason.VultureWin;
        var prosecutorWin = Lawyer.lawyer != null && reason == (GameOverReason)CustomGameOverReason.ProsecutorWin;
        var isPursurerLose = jesterWin || arsonistWin || miniLose || vultureWin || teamJackalWin;

        if (miniLose)
        {
            Add(Mini.mini);
            Outcome = WinCondition.MiniLose;
        }
        else if (jesterWin)
        {
            Add(Jester.jester);
            Outcome = WinCondition.JesterWin;
        }
        else if (arsonistWin)
        {
            Add(Arsonist.arsonist);
            Outcome = WinCondition.ArsonistWin;
        }
        else if (vultureWin)
        {
            Add(Vulture.vulture);
            Outcome = WinCondition.VultureWin;
        }
        else if (prosecutorWin)
        {
            Add(Lawyer.lawyer);
            Outcome = WinCondition.ProsecutorWin;
        }
        else if (loversWin)
        {
            if (Lovers.existingWithKiller())
            {
                Outcome = WinCondition.LoversSoloWin;
                Add(Lovers.lover1);
                Add(Lovers.lover2);
            }
            else
            {
                Outcome = WinCondition.LoversTeamWin;
                foreach (var player in PlayerControl.AllPlayerControls)
                {
                    if (player == null) continue;
                    if (player == Lovers.lover1 || player == Lovers.lover2)
                        Add(player);
                    else if (player == Pursuer.pursuer && !Pursuer.pursuer.Data.IsDead)
                        Add(player);
                    else if (player != Jester.jester && player != Jackal.jackal && player != Sidekick.sidekick &&
                             player != Arsonist.arsonist && player != Vulture.vulture &&
                             player != SchrodingerCat.cat && !Jackal.formerJackals.Contains(player) &&
                             !player.Data.Role.IsImpostor)
                        Add(player);
                }
            }
        }
        else if (teamJackalWin)
        {
            Outcome = WinCondition.JackalWin;
            Add(Jackal.jackal, false);
            if (Sidekick.sidekick != null) Add(Sidekick.sidekick, false);
            foreach (var player in Jackal.formerJackals) Add(player, false);
            if (SchrodingerCat.cat != null && SchrodingerCat.team == SchrodingerCat.CatTeam.Jackal)
                Add(SchrodingerCat.cat, false);
        }
        else
        {
            var crewmatesWin = reason is GameOverReason.CrewmatesByTask or GameOverReason.CrewmatesByVote
                or GameOverReason.ImpostorDisconnect or GameOverReason.HideAndSeek_CrewmatesByTimer;
            var impostorsWin = reason is GameOverReason.ImpostorsByKill or GameOverReason.ImpostorsBySabotage
                or GameOverReason.ImpostorsByVote or GameOverReason.CrewmateDisconnect
                or GameOverReason.HideAndSeek_ImpostorsByKills;
            if (crewmatesWin || impostorsWin)
            {
                foreach (var player in PlayerControl.AllPlayerControls)
                {
                    if (player == null || player.Data == null || player.Data.Disconnected) continue;
                    var isWinner = crewmatesWin
                        ? !player.Data.Role.IsImpostor && !Helpers.isNeutral(player)
                        : player.Data.Role.IsImpostor;
                    if (isWinner) Add(player);
                }
            }
        }

        // Schrodingers Cat wins with its team
        if (SchrodingerCat.cat != null && !SchrodingerCat.cat.Data.IsDead && SchrodingerCat.hasTeam())
        {
            if (SchrodingerCat.team == SchrodingerCat.CatTeam.Impostor && winners.Exists(x => x.IsImpostor))
                Add(SchrodingerCat.cat);
            else if (SchrodingerCat.team == SchrodingerCat.CatTeam.Crewmate &&
                     !winners.Exists(x => x.IsImpostor) && !teamJackalWin)
                Add(SchrodingerCat.cat);
        }

        // Lawyer wins together with the client
        if (Lawyer.lawyer != null && Lawyer.target != null &&
            (!Lawyer.target.Data.IsDead || Lawyer.target == Jester.jester) && !Pursuer.notAckedExiled &&
            !Lawyer.isProsecutor && winners.Exists(x => x.Player.PlayerId == Lawyer.target.PlayerId))
        {
            Add(Lawyer.lawyer);
            Additional.Add(WinCondition.AdditionalLawyerBonusWin);
        }

        // Pursuer wins alive with the crew
        if (Pursuer.pursuer != null && !Pursuer.pursuer.Data.IsDead && !Pursuer.notAckedExiled &&
            !isPursurerLose && !winners.Exists(x => x.IsImpostor))
        {
            Add(Pursuer.pursuer);
            Additional.Add(WinCondition.AdditionalAlivePursuerWin);
        }
    }
}

// Mira API raises no event before EndGameManager.SetEverythingUp, so this stays a prefix. It runs
// after every OnGameEnd patch, which is what makes EndGamePatch Priority.Last unnecessary.
[HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.SetEverythingUp))]
internal static class EndGameManagerSetupPatch
{
    public static void Prefix()
    {
        TorEndGameWinners.Apply();
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
public static class OnGameEndPatch
{
    public static GameOverReason gameOverReason = GameOverReason.CrewmatesByTask;

    public static void Prefix(AmongUsClient __instance, [HarmonyArgument(0)] ref EndGameResult endGameResult)
    {
        gameOverReason = endGameResult.GameOverReason;
        if ((int)endGameResult.GameOverReason >= 10) endGameResult.GameOverReason = GameOverReason.ImpostorsByKill;

        // Reset zoomed out ghosts
        Helpers.toggleZoom(true);
    }

    // Mira API builds EndGameResult.CachedWinners from Role.DidWin, so TOR answers there
    // (TorRoleBehaviour.DidWin) and applies the verdict from EndGameManager.SetEverythingUp,
    // which the game calls after every patch on this method - no patch priority needed.
    public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ref EndGameResult endGameResult)
    {
        AdditionalTempData.clear();

        foreach (var playerControl in PlayerControl.AllPlayerControls)
        {
            var roles = CustomRoleManager.getRoleInfoForPlayer(playerControl);
            var (tasksCompleted, tasksTotal) = TasksHandler.taskInfo(playerControl.Data);
            var isGuesser = HandleGuesser.isGuesserGm && HandleGuesser.isGuesser(playerControl.PlayerId);
            int? killCount = GameHistory.deadPlayers.FindAll(x =>
                x.killerIfExisting != null && x.killerIfExisting.PlayerId == playerControl.PlayerId).Count;
            if (killCount == 0 &&
                !(new List<RoleInfo> { Sheriff.Info, Jackal.Info, Sidekick.Info, Thief.Info }.Contains(CustomRoleManager
                      .getRoleInfoForPlayer(playerControl, false).FirstOrDefault()) ||
                  playerControl.Data.Role.IsImpostor)) killCount = null;
            var roleString = CustomRoleManager.GetRolesString(playerControl, true);
            // Override cat's role display with team-specific text
            if (SchrodingerCat.cat != null && playerControl == SchrodingerCat.cat && SchrodingerCat.hasTeam())
            {
                var catTeamName = SchrodingerCat.team switch
                {
                    SchrodingerCat.CatTeam.Impostor => Helpers.cs(Palette.ImpostorRed, SchrodingerCat.Info.name + " (Impostor)"),
                    SchrodingerCat.CatTeam.Crewmate => Helpers.cs(Color.white, SchrodingerCat.Info.name + " (Crewmate)"),
                    SchrodingerCat.CatTeam.Jackal => Helpers.cs(Jackal.color, SchrodingerCat.Info.name + " (Jackal)"),
                    _ => roleString
                };
                roleString = catTeamName;
            }
            AdditionalTempData.playerRoles.Add(new AdditionalTempData.PlayerRoleInfo
            {
                PlayerName = playerControl.Data.PlayerName, Roles = roles, RoleNames = roleString,
                TasksTotal = tasksTotal, TasksCompleted = tasksCompleted, IsGuesser = isGuesser, Kills = killCount,
                IsAlive = !playerControl.Data.IsDead
            });
        }

        // While TOR role statics are still alive: the verdict TorRoleBehaviour.DidWin reports to
        // Mira API, and the one EndGameManagerSetupPatch installs once every OnGameEnd patch ran.
        TorEndGameWinners.TakeSnapshot(gameOverReason);
        AdditionalTempData.winCondition = TorEndGameWinners.Outcome;
        AdditionalTempData.additionalWinConditions.AddRange(TorEndGameWinners.Additional);

        AdditionalTempData.timer =
            (float)(DateTime.UtcNow - (HideNSeek.isHideNSeekGM ? HideNSeek.startTime : PropHunt.startTime))
            .TotalMilliseconds / 1000;

        // Reset Settings
        if (HideNSeek.isHideNSeekGM) ShipStatusPatch.resetVanillaSettings();
        RPCProcedure.resetVariables();
        EventUtility.gameEndsUpdate();
    }
}

[HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.CheckEndCriteria))]
internal class CheckEndCriteriaPatch
{
    public static bool Prefix(ShipStatus __instance)
    {
        if (!GameData.Instance) return false;
        if (DestroyableSingleton<TutorialManager>
            .InstanceExists) // InstanceExists | Don't check Custom Criteria when in Tutorial
            return true;
        var statistics = new PlayerStatistics(__instance);
        if (CheckAndEndGameForMiniLose(__instance)) return false;
        if (CheckAndEndGameForJesterWin(__instance)) return false;
        if (CheckAndEndGameForArsonistWin(__instance)) return false;
        if (CheckAndEndGameForVultureWin(__instance)) return false;
        if (CheckAndEndGameForSabotageWin(__instance)) return false;
        if (CheckAndEndGameForTaskWin(__instance)) return false;
        if (CheckAndEndGameForProsecutorWin(__instance)) return false;
        if (CheckAndEndGameForLoverWin(__instance, statistics)) return false;
        if (CheckAndEndGameForJackalWin(__instance, statistics)) return false;
        if (CheckAndEndGameForImpostorWin(__instance, statistics)) return false;
        if (CheckAndEndGameForCrewmateWin(__instance, statistics)) return false;
        return false;
    }

    private static bool CheckAndEndGameForMiniLose(ShipStatus __instance)
    {
        if (Mini.triggerMiniLose)
        {
            //__instance.enabled = false;
            GameManager.Instance.RpcEndGame((GameOverReason)CustomGameOverReason.MiniLose, false);
            return true;
        }

        return false;
    }

    private static bool CheckAndEndGameForJesterWin(ShipStatus __instance)
    {
        if (Jester.triggerJesterWin)
        {
            //__instance.enabled = false;
            GameManager.Instance.RpcEndGame((GameOverReason)CustomGameOverReason.JesterWin, false);
            return true;
        }

        return false;
    }

    private static bool CheckAndEndGameForArsonistWin(ShipStatus __instance)
    {
        if (Arsonist.triggerArsonistWin)
        {
            //__instance.enabled = false;
            GameManager.Instance.RpcEndGame((GameOverReason)CustomGameOverReason.ArsonistWin, false);
            return true;
        }

        return false;
    }

    private static bool CheckAndEndGameForVultureWin(ShipStatus __instance)
    {
        if (Vulture.triggerVultureWin)
        {
            //__instance.enabled = false;
            GameManager.Instance.RpcEndGame((GameOverReason)CustomGameOverReason.VultureWin, false);
            return true;
        }

        return false;
    }

    private static bool CheckAndEndGameForSabotageWin(ShipStatus __instance)
    {
        if (MapUtilities.Systems == null) return false;
        var systemType = MapUtilities.Systems.ContainsKey(SystemTypes.LifeSupp)
            ? MapUtilities.Systems[SystemTypes.LifeSupp]
            : null;
        if (systemType != null)
        {
            var lifeSuppSystemType = systemType.TryCast<LifeSuppSystemType>();
            if (lifeSuppSystemType != null && lifeSuppSystemType.Countdown < 0f)
            {
                EndGameForSabotage(__instance);
                lifeSuppSystemType.Countdown = 10000f;
                return true;
            }
        }

        var systemType2 = MapUtilities.Systems.ContainsKey(SystemTypes.Reactor)
            ? MapUtilities.Systems[SystemTypes.Reactor]
            : null;
        if (systemType2 == null)
            systemType2 = MapUtilities.Systems.ContainsKey(SystemTypes.Laboratory)
                ? MapUtilities.Systems[SystemTypes.Laboratory]
                : null;
        if (systemType2 != null)
        {
            var criticalSystem = systemType2.TryCast<ICriticalSabotage>();
            if (criticalSystem != null && criticalSystem.Countdown < 0f)
            {
                EndGameForSabotage(__instance);
                criticalSystem.ClearSabotage();
                return true;
            }
        }

        return false;
    }

    private static bool CheckAndEndGameForTaskWin(ShipStatus __instance)
    {
        if ((HideNSeek.isHideNSeekGM && !HideNSeek.taskWinPossible) || PropHunt.isPropHuntGM) return false;
        if (GameData.Instance.TotalTasks > 0 && GameData.Instance.TotalTasks <= GameData.Instance.CompletedTasks)
        {
            //__instance.enabled = false;
            GameManager.Instance.RpcEndGame(GameOverReason.CrewmatesByTask, false);
            return true;
        }

        return false;
    }

    private static bool CheckAndEndGameForProsecutorWin(ShipStatus __instance)
    {
        if (Lawyer.triggerProsecutorWin)
        {
            //__instance.enabled = false;
            GameManager.Instance.RpcEndGame((GameOverReason)CustomGameOverReason.ProsecutorWin, false);
            return true;
        }

        return false;
    }

    private static bool CheckAndEndGameForLoverWin(ShipStatus __instance, PlayerStatistics statistics)
    {
        if (statistics.TeamLoversAlive == 2 && statistics.TotalAlive <= 3)
        {
            //__instance.enabled = false;
            GameManager.Instance.RpcEndGame((GameOverReason)CustomGameOverReason.LoversWin, false);
            return true;
        }

        return false;
    }

    private static bool CheckAndEndGameForJackalWin(ShipStatus __instance, PlayerStatistics statistics)
    {
        if (statistics.TeamJackalAlive >= statistics.TotalAlive - statistics.TeamJackalAlive &&
            statistics.TeamImpostorsAlive == 0 &&
            !(statistics.TeamJackalHasAliveLover && statistics.TeamLoversAlive == 2))
        {
            //__instance.enabled = false;
            GameManager.Instance.RpcEndGame((GameOverReason)CustomGameOverReason.TeamJackalWin, false);
            return true;
        }

        return false;
    }

    private static bool CheckAndEndGameForImpostorWin(ShipStatus __instance, PlayerStatistics statistics)
    {
        if (HideNSeek.isHideNSeekGM || PropHunt.isPropHuntGM)
            if (0 != statistics.TotalAlive - statistics.TeamImpostorsAlive)
                return false;

        if (statistics.TeamImpostorsAlive >= statistics.TotalAlive - statistics.TeamImpostorsAlive &&
            statistics.TeamJackalAlive == 0 &&
            !(statistics.TeamImpostorHasAliveLover && statistics.TeamLoversAlive == 2))
        {
            //__instance.enabled = false;
            GameOverReason endReason;
            switch (GameData.LastDeathReason)
            {
                case DeathReason.Exile:
                    endReason = GameOverReason.ImpostorsByVote;
                    break;
                case DeathReason.Kill:
                    endReason = GameOverReason.ImpostorsByKill;
                    break;
                default:
                    endReason = GameOverReason.ImpostorsByVote;
                    break;
            }

            GameManager.Instance.RpcEndGame(endReason, false);
            return true;
        }

        return false;
    }

    private static bool CheckAndEndGameForCrewmateWin(ShipStatus __instance, PlayerStatistics statistics)
    {
        if (HideNSeek.isHideNSeekGM && HideNSeek.timer <= 0 && !HideNSeek.isWaitingTimer)
        {
            //__instance.enabled = false;
            GameManager.Instance.RpcEndGame(GameOverReason.CrewmatesByVote, false);
            return true;
        }

        if (PropHunt.isPropHuntGM && PropHunt.timer <= 0 && PropHunt.timerRunning)
        {
            GameManager.Instance.RpcEndGame(GameOverReason.CrewmatesByVote, false);
            return true;
        }

        if (statistics.TeamImpostorsAlive == 0 && statistics.TeamJackalAlive == 0)
        {
            //__instance.enabled = false;
            GameManager.Instance.RpcEndGame(GameOverReason.CrewmatesByVote, false);
            return true;
        }

        return false;
    }

    private static void EndGameForSabotage(ShipStatus __instance)
    {
        //__instance.enabled = false;
        GameManager.Instance.RpcEndGame(GameOverReason.ImpostorsBySabotage, false);
    }
}

internal class PlayerStatistics
{
    public PlayerStatistics(ShipStatus __instance)
    {
        GetPlayerCounts();
    }

    public int TeamImpostorsAlive { get; set; }
    public int TeamJackalAlive { get; set; }
    public int TeamLoversAlive { get; set; }
    public int TotalAlive { get; set; }
    public bool TeamImpostorHasAliveLover { get; set; }
    public bool TeamJackalHasAliveLover { get; set; }

    private bool isLover(NetworkedPlayerInfo p)
    {
        return (Lovers.lover1 != null && Lovers.lover1.PlayerId == p.PlayerId) ||
               (Lovers.lover2 != null && Lovers.lover2.PlayerId == p.PlayerId);
    }

    private void GetPlayerCounts()
    {
        var numJackalAlive = 0;
        var numImpostorsAlive = 0;
        var numLoversAlive = 0;
        var numTotalAlive = 0;
        var impLover = false;
        var jackalLover = false;

        foreach (var playerInfo in GameData.Instance.AllPlayers.GetFastEnumerator())
            if (!playerInfo.Disconnected)
                if (!playerInfo.IsDead)
                {
                    numTotalAlive++;

                    var lover = isLover(playerInfo);
                    if (lover) numLoversAlive++;

                    if (playerInfo.Role.IsImpostor)
                    {
                        numImpostorsAlive++;
                        if (lover) impLover = true;
                    }

                    if (SchrodingerCat.cat != null && SchrodingerCat.cat.PlayerId == playerInfo.PlayerId && SchrodingerCat.team == SchrodingerCat.CatTeam.Impostor && !playerInfo.Role.IsImpostor)
                    {
                        numImpostorsAlive++;
                    }

                    if (SchrodingerCat.cat != null && SchrodingerCat.cat.PlayerId == playerInfo.PlayerId && SchrodingerCat.team == SchrodingerCat.CatTeam.Jackal)
                    {
                        numJackalAlive++;
                    }

                    if (Jackal.jackal != null && Jackal.jackal.PlayerId == playerInfo.PlayerId)
                    {
                        numJackalAlive++;
                        if (lover) jackalLover = true;
                    }

                    if (Sidekick.sidekick != null && Sidekick.sidekick.PlayerId == playerInfo.PlayerId)
                    {
                        numJackalAlive++;
                        if (lover) jackalLover = true;
                    }
                }

        TeamJackalAlive = numJackalAlive;
        TeamImpostorsAlive = numImpostorsAlive;
        TeamLoversAlive = numLoversAlive;
        TotalAlive = numTotalAlive;
        TeamImpostorHasAliveLover = impLover;
        TeamJackalHasAliveLover = jackalLover;
    }
}
