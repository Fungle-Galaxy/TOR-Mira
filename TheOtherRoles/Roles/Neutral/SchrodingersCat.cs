using System;
using Object = UnityEngine.Object;
using MiraAPI.GameOptions.OptionTypes;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheOtherRoles.Buttons;
using TheOtherRoles.Networking;
using TheOtherRoles.Patches;
using TheOtherRoles.Utilities;
using UnityEngine;
using MiraAPI.Utilities;

namespace TheOtherRoles.Roles.Neutral;

public class SchrodingerCat(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public enum CatTeam
    {
        None,
        Crewmate,
        Impostor,
        Jackal
    }

    public enum ExileType
    {
        None,
        Crewmate,
        Random
    }

    public static Color color = Color.grey;
    public static RoleInfo Info = new(color, RoleId.SchrodingerCat, isNeutral: true);

    public static PlayerControl cat;
    public static CatTeam team = CatTeam.None;
    public static PlayerControl killer;
    public static ExileType exileType = ExileType.None;

    public static bool skipRevival;
    private static int _lastDeathFrame = -10;

    // Settings
    public static float killCooldown = 20f;
    public static bool killsKiller = false;
    public static bool cantKillUntilLastOne = false;
    public static bool hideRole = false;
    public static bool canChooseTeam = false;

    // Kill button / team chooser button: instances live in SchrodingerCatKillButton and
    // SchrodingerCatSwitchButton below; reach them via CustomButtonSingleton<T>.Instance.

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.SchrodingerCat;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<SchrodingerCatOptions>.Instance.SpawnRate;

    public static bool hasTeam()
    {
        return team != CatTeam.None;
    }

    public static bool tasksComplete(PlayerControl player)
    {
        if (player == null || player.Data.IsDead) return false;
        var taskInfo = TasksHandler.taskInfo(player.Data);
        int playerCompleted = taskInfo.Item1;
        int playerTotal = taskInfo.Item2;
        return playerTotal > 0 && playerCompleted >= playerTotal;
    }

    public static void setTeamRPC(CatTeam newTeam)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        PlayerControl.LocalPlayer.RpcSchrodingerCatSetTeam((byte)newTeam);
    }

    public static void setTeam(CatTeam newTeam)
    {
        team = newTeam;
        switch (newTeam)
        {
            case CatTeam.Crewmate:
                if (cat != null) cat.clearAllTasks();
                break;
            case CatTeam.Impostor:
                if (cat != null)
                {
                    cat.clearAllTasks();
                    cat.Data.Role.TeamType = RoleTeamTypes.Impostor;
                    FastDestroyableSingleton<RoleManager>.Instance.SetRole(cat, RoleTypes.Impostor);
                }
                break;
            case CatTeam.Jackal:
                if (cat != null) cat.clearAllTasks();
                break;
        }
    }

    public static void onDeath(PlayerControl victim, PlayerControl killerPlayer)
    {
        if (victim == null || victim != cat || hasTeam()) return;

        // Don't revive if this is a bomb/guess/etc kill
        if (skipRevival)
        {
            skipRevival = false;
            return;
        }

        // Prevent simultaneous kills from reviving (e.g. Witch + Jackal same frame)
        int currentFrame = Time.frameCount;
        if (currentFrame - _lastDeathFrame < 3) return;

        if (killerPlayer == null) return;

        CatTeam newTeam;
        if (killerPlayer.Data.Role.IsImpostor)
        {
            newTeam = CatTeam.Impostor;
        }
        else if (killerPlayer == Jackal.jackal || (Sidekick.sidekick != null && killerPlayer == Sidekick.sidekick))
        {
            newTeam = CatTeam.Jackal;
        }
        else
        {
            newTeam = CatTeam.Crewmate;
        }

        // Mark death frame BEFORE revive to prevent double-processing
        _lastDeathFrame = currentFrame;

        // Revive FIRST (before setting team, since SetRole needs alive player)
        victim.Revive();

        // Remove dead body
        foreach (var db in Object.FindObjectsOfType<DeadBody>())
        {
            if (db.ParentId == victim.PlayerId)
            {
                Object.Destroy(db.gameObject);
                break;
            }
        }

        // Remove death record
        var deadPlayer = GameHistory.deadPlayers.FirstOrDefault(x => x.player?.PlayerId == victim.PlayerId);
        if (deadPlayer != null) GameHistory.deadPlayers.Remove(deadPlayer);

        // Set team (after revive, so SetRole works on alive player)
        setTeamRPC(newTeam);

        if (killsKiller && newTeam != CatTeam.Crewmate && killerPlayer != null && !killerPlayer.Data.IsDead)
        {
            killer = killerPlayer;
        }
    }

    public static void onExiled()
    {
        if (cat == null || cat.Data.Disconnected) return;
        if (hasTeam()) return;

        // If guessed (skipRevival set by guess RPC), die permanently
        if (skipRevival)
        {
            skipRevival = false;
            return;
        }

        switch (exileType)
        {
            case ExileType.None:
                break;
            case ExileType.Crewmate:
                setTeamRPC(CatTeam.Crewmate);
                break;
            case ExileType.Random:
                var availableTeams = new List<CatTeam> { CatTeam.Crewmate };
                if (PlayerControl.AllPlayerControls.ToArray().Any(p => p.Data.Role.IsImpostor))
                    availableTeams.Add(CatTeam.Impostor);
                if (Jackal.jackal != null || Sidekick.sidekick != null)
                    availableTeams.Add(CatTeam.Jackal);
                var randomTeam = availableTeams[Helpers.rnd.Next(availableTeams.Count)];
                setTeamRPC(randomTeam);
                break;
        }
    }

    public static void showTeamMenu()
    {
        var availableTeams = new List<CatTeam>();
        if (PlayerControl.AllPlayerControls.ToArray().Any(p => p != cat && !p.Data.IsDead && p.Data.Role.IsImpostor))
            availableTeams.Add(CatTeam.Impostor);
        if (Jackal.jackal != null || Sidekick.sidekick != null)
            availableTeams.Add(CatTeam.Jackal);
        availableTeams.Add(CatTeam.Crewmate);

        if (availableTeams.Count > 0)
        {
            var currentIndex = availableTeams.IndexOf(team);
            var nextIndex = (currentIndex + 1) % availableTeams.Count;
            if (currentIndex < 0) nextIndex = 0;
            setTeamRPC(availableTeams[nextIndex]);
        }
    }

    public static void handleKillsKiller()
    {
        if (killer == null || !killsKiller) return;

        if (killer.Data.IsDead || killer.Data.Disconnected)
        {
            killer = null;
            return;
        }
    }
    
    public static void clearAndReload()
    {
        cat = null;
        team = CatTeam.None;
        killer = null;
        skipRevival = false;
        _lastDeathFrame = -10;
        killCooldown = OptionGroupSingleton<SchrodingerCatOptions>.Instance.KillCooldown.Value;
        killsKiller = OptionGroupSingleton<SchrodingerCatOptions>.Instance.KillsKiller.Value;
        cantKillUntilLastOne = OptionGroupSingleton<SchrodingerCatOptions>.Instance.CantKillUntilLastOne.Value;
        hideRole = OptionGroupSingleton<SchrodingerCatOptions>.Instance.HideRole.Value;
        canChooseTeam = OptionGroupSingleton<SchrodingerCatOptions>.Instance.CanChooseTeam.Value;
        var exileSelection = OptionGroupSingleton<SchrodingerCatOptions>.Instance.ExileType.Selection();
        exileType = exileSelection switch
        {
            0 => ExileType.None,
            1 => ExileType.Crewmate,
            2 => ExileType.Random,
            _ => ExileType.None
        };
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo()
    {
        if (!hasTeam()) return Info;
        Color teamColor = team switch
        {
            CatTeam.Impostor => Palette.ImpostorRed,
            CatTeam.Jackal => Jackal.color,
            CatTeam.Crewmate => Color.white,
            _ => color
        };
        return new RoleInfo(teamColor, RoleId.SchrodingerCat, isNeutral: !hasTeam());
    }

    public override void OnMurderPlayer(PlayerControl killer, PlayerControl target)
    {
        if (target == cat && !hasTeam())
        {
            onDeath(target, killer);
        }
    }

    public override void OnPlayerExiled(PlayerControl player)
    {
        if (player == cat && !hasTeam())
        {
            onExiled();
        }
    }

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        if (cat == null || cat != player) return;
        handleKillsKiller();
    }

    public override void OnMeetingStart()
    {
        if (killer != null && killsKiller && !killer.Data.IsDead && !killer.Data.Disconnected)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                killer.MurderPlayer(killer);
                var deadPlayer = new DeadPlayer(killer, System.DateTime.UtcNow, DeadPlayer.CustomDeathReason.Kill, killer);
                GameHistory.deadPlayers.Add(deadPlayer);
            }
            killer = null;
        }
    }
}

/// <summary>
/// The settings of SchrodingerCatOptions. They live next to the role on purpose: the group is bound
/// to SchrodingerCat, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class SchrodingerCatOptions : TorRoleOptionGroup<SchrodingerCat>
{
    public override uint GroupPriority => 370;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.SchrodingerCat));
    public override Color GroupColor => TorOptionColors.Group(SchrodingerCat.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.SchrodingerCat), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedNumberOption KillCooldown { get; } =
        new ModdedNumberOption("Opt-SchrodingerCat,1", 20f, 1f, 60f, 0.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedToggleOption KillsKiller { get; } =
        new ModdedToggleOption("Opt-SchrodingerCat,3", false);

    public ModdedToggleOption CantKillUntilLastOne { get; } =
        new ModdedToggleOption("Opt-SchrodingerCat,4", false);

    public ModdedStringOption ExileType { get; } =
        new ModdedStringOption("Opt-SchrodingerCat,5", "Opt-SchrodingerCat,100", ["Opt-SchrodingerCat,100", "Opt-SchrodingerCat,101", "Opt-SchrodingerCat,102"]);

    public ModdedToggleOption HideRole { get; } =
        new ModdedToggleOption("Opt-SchrodingerCat,6", false);

    public ModdedToggleOption CanChooseTeam { get; } =
        new ModdedToggleOption("Opt-SchrodingerCat,7", false)
        {
            Visible = () => OptionGroupSingleton<SchrodingerCatOptions>.Instance.HideRole.Value
        };
}

/// <summary>
/// Schrödinger's Cat kill button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>SchrodingerCatKillButton</c>.
/// </summary>
public sealed class SchrodingerCatKillButton : TorButton
{
    private static SchrodingerCatKillButton schrodingerCatKillButton;

    public SchrodingerCatKillButton()
    {
        schrodingerCatKillButton = this;

        PositionOffset = TorButtonPositions.UpperRowRight;
        Hotkey = KeyCode.Q;
        ShowButtonText = true;
        ButtonText = new ButtonText(StringNames.KillLabel);

        RealOnClick = () =>
        {
            if (SchrodingerCat.cat != null && SchrodingerCat.cat == PlayerControl.LocalPlayer)
            {
                var target = PlayerControlFixedUpdatePatch.setTarget();
                if (target != null)
                {
                    var murderAttemptResult = Helpers.checkMuderAttempt(SchrodingerCat.cat, target);
                    if (murderAttemptResult == MurderAttemptResult.SuppressKill) return;
                    if (murderAttemptResult == MurderAttemptResult.PerformKill)
                    {
                        PlayerControl.LocalPlayer.RpcUncheckedMurderPlayer(SchrodingerCat.cat.Data.PlayerId,
                            target.Data.PlayerId, byte.MaxValue);
                    }
                }
                schrodingerCatKillButton.Timer = schrodingerCatKillButton.MaxTimer;
            }
        };
        HasButton = () =>
        {
            if (SchrodingerCat.cat == null || SchrodingerCat.cat != PlayerControl.LocalPlayer
                || !SchrodingerCat.hasTeam() || SchrodingerCat.team == SchrodingerCat.CatTeam.Crewmate
                || PlayerControl.LocalPlayer.Data.IsDead)
                return false;

            // cantKillUntilLastOne: only allow kill when cat is the last alive on its team
            if (SchrodingerCat.cantKillUntilLastOne)
            {
                int aliveOnTeam = 0;
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc.Data.IsDead || pc.Data.Disconnected) continue;
                    if (SchrodingerCat.team == SchrodingerCat.CatTeam.Impostor && pc.Data.Role.IsImpostor) aliveOnTeam++;
                    else if (SchrodingerCat.team == SchrodingerCat.CatTeam.Jackal &&
                             (pc == Jackal.jackal || pc == Sidekick.sidekick || pc == SchrodingerCat.cat)) aliveOnTeam++;
                }
                if (aliveOnTeam > 1) return false;
            }

            return true;
        };
        CouldUse = () =>
        {
            var target = PlayerControlFixedUpdatePatch.setTarget();
            return target != null && PlayerControl.LocalPlayer.CanMove;
        };
        OnMeetingEnds = () => { };
    }

    public override float Cooldown => SchrodingerCat.killCooldown;

    public override void CreateButton(Transform parent)
    {
        // The old constructor took the kill button's sprite straight off the HUD.
        SetSprite(HudManager.Instance.KillButton.graphic.sprite);
        base.CreateButton(parent);
    }
}

/// <summary>
/// Schrödinger's Cat team switch button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>SchrodingerCatSwitchButton</c>.
/// </summary>
public sealed class SchrodingerCatSwitchButton : TorButton
{
    private static SchrodingerCatSwitchButton schrodingerCatSwitchButton;

    public SchrodingerCatSwitchButton()
    {
        schrodingerCatSwitchButton = this;

        SetSprite(TorAssets.ShiftButton);
        PositionOffset = TorButtonPositions.UpperRowCenter;
        ShowButtonText = true;

        RealOnClick = () =>
        {
            SchrodingerCat.showTeamMenu();
        };
        HasButton = () =>
        {
            return SchrodingerCat.cat != null && SchrodingerCat.cat == PlayerControl.LocalPlayer
                && !SchrodingerCat.hasTeam()
                && SchrodingerCat.canChooseTeam
                && SchrodingerCat.tasksComplete(PlayerControl.LocalPlayer)
                && !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () => true;
        OnMeetingEnds = () => { };
    }

    public override float Cooldown => 0f;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        ButtonText = new ButtonText(47,
            FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.UseButton]
                .FontMaterial);
    }
}

public static class SchrodingerCatRpcs
{
    [MethodRpc((uint)TorRpc.SchrodingerCatSetTeam, LocalHandling = RpcLocalHandling.After)]
    public static void RpcSchrodingerCatSetTeam(this PlayerControl player, byte newTeam)
    {
        SchrodingerCat.setTeam((SchrodingerCat.CatTeam)newTeam);
    }
}
