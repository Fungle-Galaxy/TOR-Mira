using System;
using Object = UnityEngine.Object;
using AmongUs.Data;
using MiraAPI.GameOptions.OptionTypes;
using System.Collections.Generic;
using System.Linq;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheOtherRoles.Objects;
using TheOtherRoles.Patches;
using TheOtherRoles.Buttons;
using TheOtherRoles.Networking;
using UnityEngine;
using MiraAPI.Utilities;

namespace TheOtherRoles.Roles.Impostor;

public class Ninja(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = Palette.ImpostorRed;

    public static RoleInfo Info = new(color, RoleId.Ninja);

    public static PlayerControl ninja;
    public static PlayerControl ninjaMarked;
    public static PlayerControl currentTarget;
    public static float cooldown = 30f;
    public static float traceTime = 1f;
    public static bool knowsTargetLocation;
    public static float invisibleDuration = 5f;

    public static float invisibleTimer;
    public static bool isInvisble;
    public static Arrow arrow = new(Color.black);

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Ninja;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<NinjaOptions>.Instance.SpawnRate;
    
    public static void clearAndReload()
    {
        ninja = null;
        currentTarget = ninjaMarked = null;
        cooldown = OptionGroupSingleton<NinjaOptions>.Instance.Cooldown.Value;
        knowsTargetLocation = OptionGroupSingleton<NinjaOptions>.Instance.KnowsTargetLocation.Value;
        traceTime = OptionGroupSingleton<NinjaOptions>.Instance.TraceTime.Value;
        invisibleDuration = OptionGroupSingleton<NinjaOptions>.Instance.InvisibleDuration.Value;
        invisibleTimer = 0f;
        isInvisble = false;
        if (arrow?.arrow != null) Object.Destroy(arrow.arrow);
        arrow = new Arrow(Color.black);
        if (arrow.arrow != null) arrow.arrow.SetActive(false);
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        Ninja.invisibleTimer -= Time.deltaTime;
        // ninjaSetTarget
        if (Ninja.ninja == null || Ninja.ninja != player) return;
        var untargetables = new List<PlayerControl>();
        if (Spy.spy != null && !Spy.impostorsCanKillAnyone) untargetables.Add(Spy.spy);
        if (Mini.mini != null && !Mini.isGrownUp()) untargetables.Add(Mini.mini);
        if (Sidekick.wasTeamRed && !Spy.impostorsCanKillAnyone) untargetables.Add(Sidekick.sidekick);
        if (Jackal.wasTeamRed && !Spy.impostorsCanKillAnyone) untargetables.Add(Jackal.jackal);
        Ninja.currentTarget =
            PlayerControlFixedUpdatePatch.setTarget(Spy.spy == null || !Spy.impostorsCanKillAnyone, untargetablePlayers: untargetables);
        PlayerControlFixedUpdatePatch.setPlayerOutline(Ninja.currentTarget, Ninja.color);

        // ninjaUpdate
        if (Ninja.isInvisble && Ninja.invisibleTimer <= 0 && Ninja.ninja == player)
        {
            PlayerControl.LocalPlayer.RpcSetInvisible(Ninja.ninja.PlayerId, byte.MaxValue);
        }
        if (Ninja.arrow?.arrow != null)
        {
            if (Ninja.ninja == null || Ninja.ninja != player || !Ninja.knowsTargetLocation)
            {
                Ninja.arrow.arrow.SetActive(false);
                return;
            }
            if (Ninja.ninjaMarked != null && !player.Data.IsDead)
            {
                var trackedOnMap = !Ninja.ninjaMarked.Data.IsDead;
                var position = Ninja.ninjaMarked.transform.position;
                if (!trackedOnMap)
                {
                    var body = Object.FindObjectsOfType<DeadBody>()
                        .FirstOrDefault(b => b.ParentId == Ninja.ninjaMarked.PlayerId);
                    if (body != null) { trackedOnMap = true; position = body.transform.position; }
                }
                Ninja.arrow.Update(position);
                Ninja.arrow.arrow.SetActive(trackedOnMap);
            }
            else
            {
                Ninja.arrow.arrow.SetActive(false);
            }
        }
    }
}

/// <summary>
/// The settings of NinjaOptions. They live next to the role on purpose: the group is bound
/// to Ninja, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class NinjaOptions : TorRoleOptionGroup<Ninja>
{
    public override uint GroupPriority => 200;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Ninja));
    public override Color GroupColor => TorOptionColors.Group(Ninja.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.Ninja), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedNumberOption Cooldown { get; } =
        new ModdedNumberOption("Opt-Ninja,1", 30f, 10f, 120f, 5f, MiraNumberSuffixes.None, "0.##");

    public ModdedToggleOption KnowsTargetLocation { get; } =
        new ModdedToggleOption("Opt-Ninja,2", true);

    public ModdedNumberOption TraceTime { get; } =
        new ModdedNumberOption("Opt-Ninja,3", 5f, 1f, 20f, 0.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption TraceColorTime { get; } =
        new ModdedNumberOption("Opt-Ninja,4", 2f, 0f, 20f, 0.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption InvisibleDuration { get; } =
        new ModdedNumberOption("Opt-Ninja,5", 3f, 0f, 20f, 1f, MiraNumberSuffixes.None, "0.##");
}

/// <summary>
/// The Ninja's mark/assassinate button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>ninjaButton</c>.
/// </summary>
public sealed class NinjaButton : TorButton
{
    private static NinjaButton ninjaButton;

    public NinjaButton()
    {
        ninjaButton = this;

        SetSprite(TorAssets.NinjaMarkButton);
        PositionOffset = TorButtonPositions.UpperRowLeft;
        Hotkey = KeyCode.F;
        ShowButtonText = true;
        ButtonText = new ButtonText(28);

        RealOnClick = () =>
        {
            if (Ninja.ninjaMarked != null)
            {
                // Murder attempt with teleport
                var attempt = Helpers.checkMuderAttempt(Ninja.ninja, Ninja.ninjaMarked);
                if (attempt == MurderAttemptResult.PerformKill)
                {
                    // Create first trace before killing
                    var pos = PlayerControl.LocalPlayer.transform.position;
                    var buff = new byte[sizeof(float) * 2];
                    Buffer.BlockCopy(BitConverter.GetBytes(pos.x), 0, buff, 0 * sizeof(float), sizeof(float));
                    Buffer.BlockCopy(BitConverter.GetBytes(pos.y), 0, buff, 1 * sizeof(float), sizeof(float));

                    PlayerControl.LocalPlayer.RpcPlaceNinjaTrace(buff);

                    PlayerControl.LocalPlayer.RpcSetInvisible(Ninja.ninja.PlayerId, byte.MinValue);

                    // Perform Kill
                    if (SubmergedCompatibility.IsSubmerged)
                        SubmergedCompatibility.ChangeFloor(Ninja.ninjaMarked.transform.localPosition.y > -7);
                    PlayerControl.LocalPlayer.RpcUncheckedMurderPlayer(PlayerControl.LocalPlayer.PlayerId,
                        Ninja.ninjaMarked.PlayerId, byte.MaxValue);

                    // Create Second trace after killing
                    pos = Ninja.ninjaMarked.transform.position;
                    buff = new byte[sizeof(float) * 2];
                    Buffer.BlockCopy(BitConverter.GetBytes(pos.x), 0, buff, 0 * sizeof(float), sizeof(float));
                    Buffer.BlockCopy(BitConverter.GetBytes(pos.y), 0, buff, 1 * sizeof(float), sizeof(float));

                    PlayerControl.LocalPlayer.RpcPlaceNinjaTrace(buff);
                }

                if (attempt == MurderAttemptResult.BlankKill || attempt == MurderAttemptResult.PerformKill)
                {
                    ninjaButton.Timer = ninjaButton.MaxTimer;
                    Ninja.ninja.killTimer = GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown;
                }
                else if (attempt == MurderAttemptResult.SuppressKill)
                {
                    ninjaButton.Timer = 0f;
                }

                Ninja.ninjaMarked = null;
                return;
            }

            if (Ninja.currentTarget != null)
            {
                Ninja.ninjaMarked = Ninja.currentTarget;
                ninjaButton.Timer = 5f;
                SoundEffectsManager.play("warlockCurse");

                // Ghost Info
                PlayerControl.LocalPlayer.RpcGhostNinjaMarked(PlayerControl.LocalPlayer.PlayerId,
                    Ninja.ninjaMarked.PlayerId);
            }
        };
        HasButton = () =>
        {
            return Ninja.ninja != null && Ninja.ninja == PlayerControl.LocalPlayer &&
                   !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () =>
        {
            // CouldUse
            ninjaButton.SetSprite(Ninja.ninjaMarked != null
                ? TorAssets.NinjaAssassinateButton
                : TorAssets.NinjaMarkButton);
            ninjaButton.ButtonText = Ninja.ninjaMarked != null
                ? new ButtonText(29)
                : new ButtonText(28);
            return (Ninja.currentTarget != null || (Ninja.ninjaMarked != null &&
                                                    !TransportationToolPatches.isUsingTransportation(
                                                        Ninja.ninjaMarked))) && PlayerControl.LocalPlayer.CanMove;
        };
        OnMeetingEnds = () =>
        {
            // on meeting ends
            ninjaButton.Timer = ninjaButton.MaxTimer;
            Ninja.ninjaMarked = null;
        };
    }

    public override float Cooldown => Ninja.cooldown;
}

public static class NinjaRpcs
{
    [MethodRpc((uint)TorRpc.SetInvisible, LocalHandling = RpcLocalHandling.After)]
    public static void RpcSetInvisible(this PlayerControl player, byte playerId, byte flag)
    {
        var target = Helpers.playerById(playerId);
        if (target == null) return;
        if (flag == byte.MaxValue)
        {
            target.cosmetics.currentBodySprite.BodySprite.color = Color.white;
            target.cosmetics.colorBlindText.gameObject.SetActive(DataManager.Settings.Accessibility.ColorBlindMode);
            target.cosmetics.colorBlindText.color = target.cosmetics.colorBlindText.color.SetAlpha(1f);

            if (Camouflager.camouflageTimer <= 0 && !Helpers.MushroomSabotageActive()) target.setDefaultLook();
            Ninja.isInvisble = false;
            return;
        }

        target.setLook("", 6, "", "", "", "");
        var color = Color.clear;
        var canSee = PlayerControl.LocalPlayer.Data.Role.IsImpostor || PlayerControl.LocalPlayer.Data.IsDead;
        if (canSee) color.a = 0.1f;
        target.cosmetics.currentBodySprite.BodySprite.color = color;
        target.cosmetics.colorBlindText.gameObject.SetActive(false);
        target.cosmetics.colorBlindText.color = target.cosmetics.colorBlindText.color.SetAlpha(canSee ? 0.1f : 0f);
        Ninja.invisibleTimer = Ninja.invisibleDuration;
        Ninja.isInvisble = true;
    }

    [MethodRpc((uint)TorRpc.PlaceNinjaTrace, LocalHandling = RpcLocalHandling.After)]
    public static void RpcPlaceNinjaTrace(this PlayerControl player, byte[] buff)
    {
        var position = Vector3.zero;
        position.x = BitConverter.ToSingle(buff, 0 * sizeof(float));
        position.y = BitConverter.ToSingle(buff, 1 * sizeof(float));
        new NinjaTrace(position, Ninja.traceTime);
        if (PlayerControl.LocalPlayer != Ninja.ninja)
            Ninja.ninjaMarked = null;
    }
}
