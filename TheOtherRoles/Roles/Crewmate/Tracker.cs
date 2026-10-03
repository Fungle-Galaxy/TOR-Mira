using System;
using Object = UnityEngine.Object;
using MiraAPI.GameOptions.OptionTypes;
using System.Collections.Generic;
using System.Linq;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using Reactor.Utilities.Extensions;
using TheOtherRoles.Buttons;
using TheOtherRoles.Objects;
using TheOtherRoles.Patches;
using TheOtherRoles.Utilities;
using UnityEngine;
using MiraAPI.Utilities;

namespace TheOtherRoles.Roles.Crewmate;

public class Tracker(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = new Color32(100, 58, 220, byte.MaxValue);

    public static RoleInfo Info = new(color, RoleId.Tracker);

    public static PlayerControl tracker;
    public static List<Arrow> localArrows = new();

    public static float updateIntervall = 5f;
    public static bool resetTargetAfterMeeting;
    public static bool canTrackCorpses;
    public static float corpsesTrackingCooldown = 30f;
    public static float corpsesTrackingDuration = 5f;
    public static float corpsesTrackingTimer;
    public static int trackingMode;
    public static List<Vector3> deadBodyPositions = new();

    public static PlayerControl currentTarget;
    public static PlayerControl tracked;
    public static bool usedTracker;
    public static float timeUntilUpdate;
    public static Arrow arrow = new(Color.blue);

    public static GameObject DangerMeterParent;
    public static DangerMeter Meter;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Tracker;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<TrackerOptions>.Instance.SpawnRate;

    public static void resetTracked()
    {
        currentTarget = tracked = null;
        usedTracker = false;
        if (arrow?.arrow != null) Object.Destroy(arrow.arrow);
        arrow = new Arrow(Color.blue);
        if (arrow.arrow != null) arrow.arrow.SetActive(false);
    }

    public static void clearAndReload()
    {
        tracker = null;
        resetTracked();
        timeUntilUpdate = 0f;
        updateIntervall = OptionGroupSingleton<TrackerOptions>.Instance.UpdateIntervall.Value;
        resetTargetAfterMeeting = OptionGroupSingleton<TrackerOptions>.Instance.ResetTargetAfterMeeting.Value;
        if (localArrows != null)
            foreach (var arrow in localArrows)
                if (arrow?.arrow != null)
                    Object.Destroy(arrow.arrow);
        deadBodyPositions = new List<Vector3>();
        corpsesTrackingTimer = 0f;
        corpsesTrackingCooldown = OptionGroupSingleton<TrackerOptions>.Instance.CorpsesTrackingCooldown.Value;
        corpsesTrackingDuration = OptionGroupSingleton<TrackerOptions>.Instance.CorpsesTrackingDuration.Value;
        canTrackCorpses = OptionGroupSingleton<TrackerOptions>.Instance.CanTrackCorpses.Value;
        trackingMode = OptionGroupSingleton<TrackerOptions>.Instance.TrackingMethod.Selection();
        if (DangerMeterParent)
        {
            Meter.gameObject.Destroy();
            DangerMeterParent.Destroy();
        }
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        Tracker.corpsesTrackingTimer -= Time.deltaTime;
        // trackerSetTarget
        if (Tracker.tracker == null || Tracker.tracker != player) return;
        Tracker.currentTarget = PlayerControlFixedUpdatePatch.setTarget();
        if (!Tracker.usedTracker) PlayerControlFixedUpdatePatch.setPlayerOutline(Tracker.currentTarget, Tracker.color);

        // trackerUpdate - player tracking
        if (Tracker.arrow?.arrow != null)
        {
            if (Tracker.tracker == null || player != Tracker.tracker)
            {
                Tracker.arrow.arrow.SetActive(false);
                if (Tracker.DangerMeterParent) Tracker.DangerMeterParent.SetActive(false);
            }
            else if (Tracker.tracked != null && !Tracker.tracker.Data.IsDead)
            {
                Tracker.timeUntilUpdate -= Time.fixedDeltaTime;
                if (Tracker.timeUntilUpdate <= 0f)
                {
                    var trackedOnMap = !Tracker.tracked.Data.IsDead;
                    var position = Tracker.tracked.transform.position;
                    if (!trackedOnMap)
                    {
                        var body = Object.FindObjectsOfType<DeadBody>()
                            .FirstOrDefault(b => b.ParentId == Tracker.tracked.PlayerId);
                        if (body != null) { trackedOnMap = true; position = body.transform.position; }
                    }
                    if (Tracker.trackingMode == 1 || Tracker.trackingMode == 2) Arrow.UpdateProximity(position);
                    if (Tracker.trackingMode == 0 || Tracker.trackingMode == 2)
                    {
                        Tracker.arrow.Update(position);
                        Tracker.arrow.arrow.SetActive(trackedOnMap);
                    }
                    Tracker.timeUntilUpdate = Tracker.updateIntervall;
                }
                else
                {
                    if (Tracker.trackingMode == 0 || Tracker.trackingMode == 2) Tracker.arrow.Update();
                }
            }
            else if (Tracker.tracker.Data.IsDead)
            {
                Tracker.DangerMeterParent?.SetActive(false);
                Tracker.Meter?.gameObject.SetActive(false);
            }
        }

        // trackerUpdate - corpses tracking
        if (Tracker.tracker != null && Tracker.tracker == player &&
            Tracker.corpsesTrackingTimer >= 0f && !Tracker.tracker.Data.IsDead)
        {
            var arrowsCountChanged = Tracker.localArrows.Count != Tracker.deadBodyPositions.Count();
            var index = 0;
            if (arrowsCountChanged)
            {
                foreach (var arrow in Tracker.localArrows) Object.Destroy(arrow.arrow);
                Tracker.localArrows = new List<Arrow>();
            }
            foreach (var position in Tracker.deadBodyPositions)
            {
                if (arrowsCountChanged)
                {
                    Tracker.localArrows.Add(new Arrow(Tracker.color));
                    Tracker.localArrows[index].arrow.SetActive(true);
                }
                if (Tracker.localArrows[index] != null) Tracker.localArrows[index].Update(position);
                index++;
            }
        }
        else if (Tracker.localArrows.Count > 0)
        {
            foreach (var arrow in Tracker.localArrows) Object.Destroy(arrow.arrow);
            Tracker.localArrows = new List<Arrow>();
        }
    }
}

/// <summary>
/// The settings of TrackerOptions. They live next to the role on purpose: the group is bound
/// to Tracker, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class TrackerOptions : TorRoleOptionGroup<Tracker>
{
    public override uint GroupPriority => 500;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Tracker));
    public override Color GroupColor => TorOptionColors.Group(Tracker.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.Tracker), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedNumberOption UpdateIntervall { get; } =
        new ModdedNumberOption("Opt-Tracker,1", 5f, 1f, 30f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedToggleOption ResetTargetAfterMeeting { get; } =
        new ModdedToggleOption("Opt-Tracker,2", false);

    public ModdedToggleOption CanTrackCorpses { get; } =
        new ModdedToggleOption("Opt-Tracker,3", true);

    public ModdedNumberOption CorpsesTrackingCooldown { get; } =
        new ModdedNumberOption("Opt-Tracker,4", 30f, 5f, 120f, 5f, MiraNumberSuffixes.None, "0.##")
        {
            Visible = () => OptionGroupSingleton<TrackerOptions>.Instance.CanTrackCorpses.Value
        };

    public ModdedNumberOption CorpsesTrackingDuration { get; } =
        new ModdedNumberOption("Opt-Tracker,5", 5f, 2.5f, 30f, 2.5f, MiraNumberSuffixes.None, "0.##")
        {
            Visible = () => OptionGroupSingleton<TrackerOptions>.Instance.CanTrackCorpses.Value
        };

    public ModdedStringOption TrackingMethod { get; } =
        new ModdedStringOption("Opt-Tracker,6", "Opt-Tracker,100", ["Opt-Tracker,100", "Opt-Tracker,101", "Opt-Tracker,102"]);
}

/// <summary>
/// The Tracker's "mark a player" button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>trackerTrackPlayerButton</c>.
/// </summary>
public sealed class TrackerTrackPlayerButton : TorButton
{
    private static TrackerTrackPlayerButton trackerTrackPlayerButton;

    public TrackerTrackPlayerButton()
    {
        trackerTrackPlayerButton = this;

        SetSprite(TorAssets.TrackerButton);
        PositionOffset = TorButtonPositions.LowerRowRight;
        Hotkey = KeyCode.F;
        ShowButtonText = true;

        RealOnClick = () =>
        {
            PlayerControl.LocalPlayer.RpcTrackerUsedTracker(Tracker.currentTarget.PlayerId);
            SoundEffectsManager.play("trackerTrackPlayer");
        };
        HasButton = () =>
        {
            return Tracker.tracker != null && Tracker.tracker == PlayerControl.LocalPlayer &&
                   !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () =>
        {
            return PlayerControl.LocalPlayer.CanMove && Tracker.currentTarget != null && !Tracker.usedTracker;
        };
        OnMeetingEnds = () =>
        {
            if (Tracker.resetTargetAfterMeeting) Tracker.resetTracked();
            else if (Tracker.currentTarget != null && Tracker.currentTarget.Data.IsDead)
                Tracker.currentTarget = null;
        };
    }

    public override float Cooldown => 0f;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        ButtonText = new ButtonText(11,
            FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.UseButton]
                .FontMaterial);
    }
}

/// <summary>
/// The Tracker's "mark corpses" button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>trackerTrackCorpsesButton</c>.
/// </summary>
public sealed class TrackerTrackCorpsesButton : TorButton
{
    private static TrackerTrackCorpsesButton trackerTrackCorpsesButton;

    public TrackerTrackCorpsesButton()
    {
        trackerTrackCorpsesButton = this;

        SetSprite(TorAssets.PathfindButton);
        PositionOffset = TorButtonPositions.LowerRowCenter;
        Hotkey = KeyCode.G;
        EffectEnabled = true;
        ShowButtonText = true;

        RealOnClick = () =>
        {
            Tracker.corpsesTrackingTimer = Tracker.corpsesTrackingDuration;
            SoundEffectsManager.play("trackerTrackCorpses");
        };
        HasButton = () =>
        {
            return Tracker.tracker != null && Tracker.tracker == PlayerControl.LocalPlayer &&
                   !PlayerControl.LocalPlayer.Data.IsDead && Tracker.canTrackCorpses;
        };
        CouldUse = () => { return PlayerControl.LocalPlayer.CanMove; };
        OnMeetingEnds = () =>
        {
            trackerTrackCorpsesButton.Timer = trackerTrackCorpsesButton.MaxTimer;
            trackerTrackCorpsesButton.isEffectActive = false;
            trackerTrackCorpsesButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
        };
    }

    public override float Cooldown => Tracker.corpsesTrackingCooldown;

    public override float EffectDuration => Tracker.corpsesTrackingDuration;

    public override void OnEffectEnd()
    {
        trackerTrackCorpsesButton.Timer = trackerTrackCorpsesButton.MaxTimer;
    }

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        ButtonText = new ButtonText(12,
            FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.UseButton]
                .FontMaterial);
    }
}

public static class TrackerRpcs
{
    [MethodRpc((uint)TorRpc.TrackerUsedTracker, LocalHandling = RpcLocalHandling.After)]
    public static void RpcTrackerUsedTracker(this PlayerControl player, byte targetId)
    {
        Tracker.usedTracker = true;
        foreach (var p in PlayerControl.AllPlayerControls)
            if (p.PlayerId == targetId)
                Tracker.tracked = p;
    }
}
