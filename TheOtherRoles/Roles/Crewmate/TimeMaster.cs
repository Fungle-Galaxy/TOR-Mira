using MiraAPI.GameOptions.OptionTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheOtherRoles.Buttons;
using TheOtherRoles.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;
using MiraAPI.Utilities;

namespace TheOtherRoles.Roles.Crewmate;

public class TimeMaster(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = new Color32(112, 142, 239, byte.MaxValue);

    public static RoleInfo Info = new(color, RoleId.TimeMaster);

    public static PlayerControl timeMaster;

    public static bool reviveDuringRewind = false;
    public static float rewindTime = 3f;
    public static float shieldDuration = 3f;
    public static float cooldown = 30f;

    // Active rewind (RewindButton) settings
    public static bool canRewind = false;
    public static float rewindCooldown = 30f;

    // State of the rewind which is currently running
    // rewindDuration is the amount of game time (in seconds) which is being rewound
    public static float rewindDuration = 3f;
    public static float rewindEndTime;

    public static bool shieldActive;
    public static bool isRewinding;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.TimeMaster;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<TimeMasterOptions>.Instance.SpawnRate;
    
    public static void clearAndReload()
    {
        timeMaster = null;
        isRewinding = false;
        shieldActive = false;
        rewindTime = OptionGroupSingleton<TimeMasterOptions>.Instance.RewindTime.Value;
        shieldDuration = OptionGroupSingleton<TimeMasterOptions>.Instance.ShieldDuration.Value;
        cooldown = OptionGroupSingleton<TimeMasterOptions>.Instance.Cooldown.Value;
        canRewind = OptionGroupSingleton<TimeMasterOptions>.Instance.CanRewind.Value;
        rewindCooldown = OptionGroupSingleton<TimeMasterOptions>.Instance.RewindCooldown.Value;
        reviveDuringRewind = OptionGroupSingleton<TimeMasterOptions>.Instance.ReviveDuringRewind.Value;
        rewindDuration = rewindTime;
        rewindEndTime = 0f;
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        if (TimeMaster.isRewinding)
        {
            if (GameHistory.localPlayerPositions.Count > 0 &&
                (TimeMaster.rewindEndTime <= 0f || Time.time <= TimeMaster.rewindEndTime))
            {
                var next = GameHistory.localPlayerPositions[0];
                if (next.Item2)
                {
                    if (player.inVent)
                        foreach (var vent in MapUtilities.CachedShipStatus.AllVents)
                        {
                            bool canUse;
                            bool couldUse;
                            vent.CanUse(player.Data, out canUse, out couldUse);
                            if (canUse)
                            {
                                player.MyPhysics.RpcExitVent(vent.Id);
                                vent.SetButtons(false);
                            }
                        }
                    player.transform.position = next.Item1;
                }
                else if (GameHistory.localPlayerPositions.Any(x => x.Item2))
                {
                    player.transform.position = next.Item1;
                }
                if (SubmergedCompatibility.IsSubmerged) SubmergedCompatibility.ChangeFloor(next.Item1.y > -7);
                GameHistory.localPlayerPositions.RemoveAt(0);
                if (GameHistory.localPlayerPositions.Count > 1)
                    GameHistory.localPlayerPositions.RemoveAt(0);
            }
            else
            {
                TimeMaster.isRewinding = false;
                player.moveable = true;
            }
        }
        else
        {
            // Keep enough positions in the history to be able to rewind the longest possible rewind
            var historyDuration = Mathf.Max(TimeMaster.rewindTime, TimeMaster.shieldDuration);
            while (GameHistory.localPlayerPositions.Count >= Mathf.Round(historyDuration / Time.fixedDeltaTime))
                GameHistory.localPlayerPositions.RemoveAt(GameHistory.localPlayerPositions.Count - 1);
            GameHistory.localPlayerPositions.Insert(0,
                new Tuple<Vector3, bool>(player.transform.position, player.CanMove));
        }
    }

    // Revives everyone who died within the time span which is currently being rewound.
    // Has to be called on every client, as it is executed within the rewind RPC.
    public static void revivePlayersDiedDuringRewind()
    {
        if (!reviveDuringRewind || TimeMaster.timeMaster == null) return;

        var now = DateTime.UtcNow;
        List<DeadPlayer> diedDuringRewind = GameHistory.deadPlayers
            .Where(x => x.player != null && !x.player.Data.Disconnected &&
                        (now - x.timeOfDeath).TotalSeconds <= TimeMaster.rewindDuration)
            .ToList();

        foreach (var deadPlayer in diedDuringRewind) revivePlayer(deadPlayer);
    }

    private static void revivePlayer(DeadPlayer deadPlayer)
    {
        var player = deadPlayer.player;
        if (player == null) return;

        // Remove the corpse
        foreach (var body in Object.FindObjectsOfType<DeadBody>())
            if (body.ParentId == player.PlayerId)
            {
                Object.Destroy(body.gameObject);
                break;
            }

        // The Medium keeps track of dead players as well
        Medium.deadBodies?.RemoveAll(x => x.Item1?.player?.PlayerId == player.PlayerId);
        Medium.futureDeadBodies?.RemoveAll(x => x.Item1?.player?.PlayerId == player.PlayerId);

        if (player.Data.IsDead)
        {
            player.Revive();
            // Dying replaced the alive role with a ghost role, so it has to be restored.
            // RoleWhenAlive is an Il2CppSystem.Nullable, hence the null check and the .Value access.
            RoleTypes aliveRole;
            var roleWhenAlive = player.Data.RoleWhenAlive;
            if (roleWhenAlive != null)
                aliveRole = roleWhenAlive.Value;
            else
                aliveRole = player.Data.Role.IsImpostor ? RoleTypes.Impostor : RoleTypes.Crewmate;
            FastDestroyableSingleton<RoleManager>.Instance.SetRole(player, aliveRole);

            // Restore venting for roles which only vent as a non impostor (see RPCProcedure.setRole)
            if (AmongUsClient.Instance.AmHost && player.roleCanUseVents() && !player.Data.Role.IsImpostor)
            {
                player.RpcSetRole(RoleTypes.Engineer);
                player.CoSetRole(RoleTypes.Engineer, true);
            }
        }

        GameHistory.deadPlayers.Remove(deadPlayer);
    }
}

/// <summary>
/// The settings of TimeMasterOptions. They live next to the role on purpose: the group is bound
/// to TimeMaster, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class TimeMasterOptions : TorRoleOptionGroup<TimeMaster>
{
    public override uint GroupPriority => 450;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.TimeMaster));
    public override Color GroupColor => TorOptionColors.Group(TimeMaster.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.TimeMaster), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedNumberOption Cooldown { get; } =
        new ModdedNumberOption("Opt-TimeMaster,1", 30f, 10f, 120f, 2.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption RewindTime { get; } =
        new ModdedNumberOption("Opt-TimeMaster,2", 3f, 1f, 10f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption ShieldDuration { get; } =
        new ModdedNumberOption("Opt-TimeMaster,3", 3f, 1f, 20f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedToggleOption CanRewind { get; } =
        new ModdedToggleOption("Opt-TimeMaster,4", false);

    public ModdedNumberOption RewindCooldown { get; } =
        new ModdedNumberOption("Opt-TimeMaster,5", 30f, 10f, 120f, 2.5f, MiraNumberSuffixes.None, "0.##")
        {
            Visible = () => OptionGroupSingleton<TimeMasterOptions>.Instance.CanRewind.Value
        };

    public ModdedToggleOption ReviveDuringRewind { get; } =
        new ModdedToggleOption("Opt-TimeMaster,6", false);
}

/// <summary>
/// The Time Master's shield button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>timeMasterShieldButton</c>.
/// </summary>
public sealed class TimeMasterShieldButton : TorButton
{
    private static TimeMasterShieldButton timeMasterShieldButton;

    public TimeMasterShieldButton()
    {
        timeMasterShieldButton = this;

        SetSprite(TorAssets.TimeShieldButton);
        PositionOffset = TorButtonPositions.LowerRowRight;
        Hotkey = KeyCode.F;
        EffectEnabled = true;
        ShowButtonText = true;

        RealOnClick = () =>
        {
            PlayerControl.LocalPlayer.RpcTimeMasterShield();
            SoundEffectsManager.play("timemasterShield");
        };
        HasButton = () =>
        {
            return TimeMaster.timeMaster != null && TimeMaster.timeMaster == PlayerControl.LocalPlayer &&
                   !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () => { return PlayerControl.LocalPlayer.CanMove; };
        OnMeetingEnds = () =>
        {
            timeMasterShieldButton.Timer = timeMasterShieldButton.MaxTimer;
            timeMasterShieldButton.isEffectActive = false;
            timeMasterShieldButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
        };
    }

    public override float Cooldown => TimeMaster.cooldown;

    public override float EffectDuration => TimeMaster.shieldDuration;

    public override void OnEffectEnd()
    {
        timeMasterShieldButton.Timer = timeMasterShieldButton.MaxTimer;
        SoundEffectsManager.stop("timemasterShield");
    }

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        ButtonText = new ButtonText(4,
            FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.UseButton]
                .FontMaterial);
    }
}

/// <summary>
/// The Time Master's active rewind button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>timeMasterRewindButton</c>.
/// </summary>
public sealed class TimeMasterRewindButton : TorButton
{
    private static TimeMasterRewindButton timeMasterRewindButton;

    public TimeMasterRewindButton()
    {
        timeMasterRewindButton = this;

        SetSprite(TorAssets.RewindButton);
        PositionOffset = TorButtonPositions.LowerRowCenter;
        Hotkey = KeyCode.G;
        ShowButtonText = true;

        RealOnClick = () =>
        {
            PlayerControl.LocalPlayer.RpcTimeMasterRewindTime(TimeMaster.shieldDuration, false);

            timeMasterRewindButton.Timer = timeMasterRewindButton.MaxTimer;
        };
        HasButton = () =>
        {
            return TimeMaster.canRewind && TimeMaster.timeMaster != null &&
                   TimeMaster.timeMaster == PlayerControl.LocalPlayer &&
                   !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () =>
        {
            return PlayerControl.LocalPlayer.CanMove && !TimeMaster.isRewinding;
        };
        OnMeetingEnds = () => { };
    }

    public override float Cooldown => TimeMaster.rewindCooldown;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        ButtonText = new ButtonText(48,
            FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.UseButton]
                .FontMaterial);
    }
}

public static class TimeMasterRpcs
{
    [MethodRpc((uint)TorRpc.TimeMasterShield, LocalHandling = RpcLocalHandling.After)]
    public static void RpcTimeMasterShield(this PlayerControl player)
    {
        TimeMaster.shieldActive = true;
        FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(TimeMaster.shieldDuration,
            new Action<float>(p =>
            {
                if (p == 1f) TimeMaster.shieldActive = false;
            })));
    }

    [MethodRpc((uint)TorRpc.TimeMasterRewindTime, LocalHandling = RpcLocalHandling.After)]
    public static void RpcTimeMasterRewindTime(this PlayerControl player, float duration, bool shieldTriggered)
    {
        TimeMaster.rewindDuration = duration;
        // The positions are rewound at double speed, hence the rewind lasts half of the rewound time span
        TimeMaster.rewindEndTime = Time.time + duration / 2f;

        if (shieldTriggered)
        {
            TimeMaster.shieldActive = false; // Shield is no longer active when rewinding
            SoundEffectsManager.stop("timemasterShield"); // Shield sound stopped when rewinding
            if (TimeMaster.timeMaster != null && TimeMaster.timeMaster == PlayerControl.LocalPlayer)
                HudManagerStartPatch.resetTimeMasterButton();
        }

        // Everyone who died within the rewound time span comes back to life
        TimeMaster.revivePlayersDiedDuringRewind();

        FastDestroyableSingleton<HudManager>.Instance.FullScreen.color = new Color(0f, 0.5f, 0.8f, 0.3f);
        FastDestroyableSingleton<HudManager>.Instance.FullScreen.enabled = true;
        FastDestroyableSingleton<HudManager>.Instance.FullScreen.gameObject.SetActive(true);
        FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(duration / 2,
            new Action<float>(p =>
            {
                if (p == 1f) FastDestroyableSingleton<HudManager>.Instance.FullScreen.enabled = false;
            })));

        if (TimeMaster.timeMaster == null || PlayerControl.LocalPlayer == TimeMaster.timeMaster)
            return; // Time Master himself does not rewind

        TimeMaster.isRewinding = true;

        if (MapBehaviour.Instance)
            MapBehaviour.Instance.Close();
        if (Minigame.Instance)
            Minigame.Instance.ForceClose();
        PlayerControl.LocalPlayer.moveable = false;
    }

    // Shows the broken time shield animation of the Time Master to every player
    [MethodRpc((uint)TorRpc.TimeMasterShieldBreak, LocalHandling = RpcLocalHandling.After)]
    public static void RpcTimeMasterShieldBreak(this PlayerControl player)
    {
        if (TimeMaster.timeMaster == null || TimeMaster.timeMaster.Data == null) return;
        TimeMaster.timeMaster.ShowFailedMurder();
    }
}
