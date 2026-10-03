using System;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Hud;
using System.Collections.Generic;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheOtherRoles.Buttons;
using TheOtherRoles.CustomGameModes;
using TheOtherRoles.Networking;
using TheOtherRoles.Objects;
using TheOtherRoles.Patches;
using UnityEngine;
using MiraAPI.Utilities;
using TheOtherRoles.Utilities;

namespace TheOtherRoles.Roles.Impostor;

public class Vampire(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = Palette.ImpostorRed;

    public static RoleInfo Info = new(color, RoleId.Vampire);

    public static PlayerControl vampire;
    public static float delay = 10f;
    public static float cooldown = 30f;
    public static bool canKillNearGarlics = true;
    public static bool localPlacedGarlic;
    public static bool garlicsActive = true;

    public static PlayerControl currentTarget;
    public static PlayerControl bitten;
    public static bool targetNearGarlic;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Vampire;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<VampireOptions>.Instance.SpawnRate;


    public static void clearAndReload()
    {
        vampire = null;
        bitten = null;
        targetNearGarlic = false;
        localPlacedGarlic = false;
        currentTarget = null;
        garlicsActive = OptionGroupSingleton<VampireOptions>.Instance.SpawnRate.Selection() > 0;
        delay = OptionGroupSingleton<VampireOptions>.Instance.KillDelay.Value;
        cooldown = OptionGroupSingleton<VampireOptions>.Instance.Cooldown.Value;
        canKillNearGarlics = OptionGroupSingleton<VampireOptions>.Instance.CanKillNearGarlics.Value;
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        if (Vampire.vampire == null || Vampire.vampire != player) return;
        PlayerControl target = null;
        if (Spy.spy != null || Sidekick.wasSpy || Jackal.wasSpy)
        {
            if (Spy.impostorsCanKillAnyone)
                target = PlayerControlFixedUpdatePatch.setTarget(false, true);
            else
                target = PlayerControlFixedUpdatePatch.setTarget(true, true,
                    new List<PlayerControl>
                    {
                        Spy.spy, Sidekick.wasTeamRed ? Sidekick.sidekick : null,
                        Jackal.wasTeamRed ? Jackal.jackal : null
                    });
        }
        else
        {
            target = PlayerControlFixedUpdatePatch.setTarget(true, true,
                new List<PlayerControl>
                    { Sidekick.wasImpostor ? Sidekick.sidekick : null, Jackal.wasImpostor ? Jackal.jackal : null });
        }
        var targetNearGarlic = false;
        if (target != null)
            foreach (var garlic in Garlic.garlics)
                if (Vector2.Distance(garlic.garlic.transform.position, target.transform.position) <= 1.91f)
                    targetNearGarlic = true;
        Vampire.targetNearGarlic = targetNearGarlic;
        Vampire.currentTarget = target;
        PlayerControlFixedUpdatePatch.setPlayerOutline(Vampire.currentTarget, Vampire.color);
    }
}

/// <summary>
/// The settings of VampireOptions. They live next to the role on purpose: the group is bound
/// to Vampire, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class VampireOptions : TorRoleOptionGroup<Vampire>
{
    public override uint GroupPriority => 130;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Vampire));
    public override Color GroupColor => TorOptionColors.Group(Vampire.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.Vampire), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedNumberOption KillDelay { get; } =
        new ModdedNumberOption("Opt-Vampire,1", 10f, 1f, 20f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption Cooldown { get; } =
        new ModdedNumberOption("Opt-Vampire,2", 30f, 10f, 60f, 2.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedToggleOption CanKillNearGarlics { get; } =
        new ModdedToggleOption("Opt-Vampire,3", true);
}

/// <summary>
/// The Vampire's bite/kill button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>vampireKillButton</c>.
/// </summary>
public sealed class VampireKillButton : TorButton
{
    private static VampireKillButton vampireKillButton;

    public VampireKillButton()
    {
        vampireKillButton = this;

        SetSprite(TorAssets.VampireButton);
        PositionOffset = TorButtonPositions.UpperRowLeft;
        Hotkey = KeyCode.Q;
        ShowButtonText = true;
        ButtonText = new ButtonText(13);

        RealOnClick = () =>
        {
            var murder = Helpers.checkMuderAttempt(Vampire.vampire, Vampire.currentTarget);
            if (murder == MurderAttemptResult.PerformKill)
            {
                if (Vampire.targetNearGarlic)
                {
                    PlayerControl.LocalPlayer.RpcUncheckedMurderPlayer(Vampire.vampire.PlayerId,
                        Vampire.currentTarget.PlayerId, byte.MaxValue);

                    vampireKillButton.EffectEnabled = false; // Block effect on this click
                    vampireKillButton.Timer = vampireKillButton.MaxTimer;
                }
                else
                {
                    Vampire.bitten = Vampire.currentTarget;
                    // Notify players about bitten
                    PlayerControl.LocalPlayer.RpcVampireSetBitten(Vampire.bitten.PlayerId, 0);

                    var lastTimer = (byte)Vampire.delay;
                    FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(Vampire.delay,
                        new Action<float>(p =>
                        {
                            // Delayed action
                            if (p <= 1f)
                            {
                                var timer = (byte)vampireKillButton.Timer;
                                if (timer != lastTimer)
                                {
                                    lastTimer = timer;
                                    PlayerControl.LocalPlayer.RpcGhostVampireTimer(
                                        PlayerControl.LocalPlayer.PlayerId, timer);
                                }
                            }

                            if (p == 1f)
                            {
                                // Perform kill if possible and reset bitten (regardless whether the kill was successful or not)
                                var res = Helpers.checkMurderAttemptAndKill(Vampire.vampire, Vampire.bitten,
                                    showAnimation: false);
                                if (res == MurderAttemptResult.PerformKill)
                                {
                                    PlayerControl.LocalPlayer.RpcVampireSetBitten(byte.MaxValue, byte.MaxValue);
                                }
                            }
                        })));
                    SoundEffectsManager.play("vampireBite");

                    vampireKillButton.EffectEnabled = true; // Trigger effect on this click
                }
            }
            else if (murder == MurderAttemptResult.BlankKill)
            {
                vampireKillButton.Timer = vampireKillButton.MaxTimer;
                vampireKillButton.EffectEnabled = false;
            }
            else
            {
                vampireKillButton.EffectEnabled = false;
            }
        };
        HasButton = () =>
        {
            return Vampire.vampire != null && Vampire.vampire == PlayerControl.LocalPlayer &&
                   !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () =>
        {
            if (Vampire.targetNearGarlic && Vampire.canKillNearGarlics)
            {
                vampireKillButton.actionButton.graphic.sprite = HudManager.Instance.KillButton.graphic.sprite;
            }
            else
            {
                vampireKillButton.actionButton.graphic.sprite = TorAssets.VampireButton.LoadAsset();
            }

            return Vampire.currentTarget != null && PlayerControl.LocalPlayer.CanMove &&
                   (!Vampire.targetNearGarlic || Vampire.canKillNearGarlics);
        };
        OnMeetingEnds = () =>
        {
            vampireKillButton.Timer = vampireKillButton.MaxTimer;
            vampireKillButton.isEffectActive = false;
            vampireKillButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
        };
    }

    public override float Cooldown => Vampire.cooldown;

    public override float EffectDuration => Vampire.delay;

    public override void OnEffectEnd()
    {
        vampireKillButton.Timer = vampireKillButton.MaxTimer;
    }
}

/// <summary>
/// The Vampire's garlic placement button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>garlicButton</c>.
/// </summary>
public sealed class GarlicButton : TorButton
{
    private static GarlicButton garlicButton;

    public GarlicButton()
    {
        garlicButton = this;

        SetSprite(TorAssets.GarlicButton);
        PositionOffset = new Vector3(Application.platform == RuntimePlatform.Android ? -1f : 0, -0.06f, 0);
        Mirror = true;
        ShowButtonText = true;

        RealOnClick = () =>
        {
            Vampire.localPlacedGarlic = true;
            var pos = PlayerControl.LocalPlayer.transform.position;
            var buff = new byte[sizeof(float) * 2];
            Buffer.BlockCopy(BitConverter.GetBytes(pos.x), 0, buff, 0 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(pos.y), 0, buff, 1 * sizeof(float), sizeof(float));

            PlayerControl.LocalPlayer.RpcPlaceGarlic(buff);
            SoundEffectsManager.play("garlic");
        };
        HasButton = () =>
        {
            return !Vampire.localPlacedGarlic && !PlayerControl.LocalPlayer.Data.IsDead && Vampire.garlicsActive &&
                   !HideNSeek.isHideNSeekGM && !PropHunt.isPropHuntGM;
        };
        CouldUse = () => { return PlayerControl.LocalPlayer.CanMove && !Vampire.localPlacedGarlic; };
        OnMeetingEnds = () => { };
    }

    public override float Cooldown => 0f;

    // Bottom left like the old garlic button, Mira API's container keeps clear of the mobile joystick.
    public override ButtonLocation Location => ButtonLocation.BottomLeft;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        ButtonText = new ButtonText(14,
            FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.VitalsButton]
                .FontMaterial);
    }
}

public static class VampireRpcs
{
    [MethodRpc((uint)TorRpc.VampireSetBitten, LocalHandling = RpcLocalHandling.After)]
    public static void RpcVampireSetBitten(this PlayerControl player, byte targetId, byte performReset)
    {
        if (performReset != 0)
        {
            Vampire.bitten = null;
            return;
        }

        if (Vampire.vampire == null) return;
        foreach (var player2 in PlayerControl.AllPlayerControls)
            if (player2.PlayerId == targetId && !player2.Data.IsDead)
                Vampire.bitten = player2;
    }

    [MethodRpc((uint)TorRpc.PlaceGarlic, LocalHandling = RpcLocalHandling.After)]
    public static void RpcPlaceGarlic(this PlayerControl player, byte[] buff)
    {
        var position = Vector3.zero;
        position.x = BitConverter.ToSingle(buff, 0 * sizeof(float));
        position.y = BitConverter.ToSingle(buff, 1 * sizeof(float));
        new Garlic(position);
    }
}
