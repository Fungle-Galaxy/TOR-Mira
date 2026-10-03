using System;
using MiraAPI.GameOptions.OptionTypes;
using System.Collections.Generic;
using System.Linq;
using TheOtherRoles.Patches;
using TheOtherRoles.Networking;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using UnityEngine;
using MiraAPI.Utilities;
using TheOtherRoles.Buttons;

namespace TheOtherRoles.Roles.Neutral;

public class Arsonist(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = new Color32(238, 112, 46, byte.MaxValue);
    public static RoleInfo Info = new(color, RoleId.Arsonist, isNeutral: true);

    public static PlayerControl arsonist;

    public static float cooldown = 30f;
    public static float duration = 3f;
    public static bool triggerArsonistWin;

    public static PlayerControl currentTarget;
    public static PlayerControl douseTarget;
    public static List<PlayerControl> dousedPlayers = new();

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Arsonist;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<ArsonistOptions>.Instance.SpawnRate;

    public static bool dousedEveryoneAlive()
    {
        return PlayerControl.AllPlayerControls.ToArray().All(x =>
        {
            return x == arsonist || x.Data.IsDead || x.Data.Disconnected ||
                   dousedPlayers.Any(y => y.PlayerId == x.PlayerId);
        });
    }

    public static void clearAndReload()
    {
        arsonist = null;
        currentTarget = null;
        douseTarget = null;
        triggerArsonistWin = false;
        dousedPlayers = new List<PlayerControl>();
        foreach (var p in TORMapOptions.playerIcons.Values)
            if (p != null && p.gameObject != null)
                p.gameObject.SetActive(false);
        cooldown = OptionGroupSingleton<ArsonistOptions>.Instance.Cooldown.Value;
        duration = OptionGroupSingleton<ArsonistOptions>.Instance.Duration.Value;
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        if (Arsonist.arsonist == null || Arsonist.arsonist != player) return;
        List<PlayerControl> untargetables;
        if (Arsonist.douseTarget != null)
        {
            untargetables = new List<PlayerControl>();
            foreach (var p in PlayerControl.AllPlayerControls)
                if (p.PlayerId != Arsonist.douseTarget.PlayerId)
                    untargetables.Add(p);
        }
        else
        {
            untargetables = Arsonist.dousedPlayers;
        }
        Arsonist.currentTarget = PlayerControlFixedUpdatePatch.setTarget(untargetablePlayers: untargetables);
        if (Arsonist.currentTarget != null) PlayerControlFixedUpdatePatch.setPlayerOutline(Arsonist.currentTarget, Arsonist.color);
    }
}

/// <summary>
/// The settings of ArsonistOptions. They live next to the role on purpose: the group is bound
/// to Arsonist, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class ArsonistOptions : TorRoleOptionGroup<Arsonist>
{
    public override uint GroupPriority => 320;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Arsonist));
    public override Color GroupColor => TorOptionColors.Group(Arsonist.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.Arsonist), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedNumberOption Cooldown { get; } =
        new ModdedNumberOption("Opt-Arsonist,1", 12.5f, 2.5f, 60f, 2.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption Duration { get; } =
        new ModdedNumberOption("Opt-Arsonist,2", 3f, 1f, 10f, 1f, MiraNumberSuffixes.None, "0.##");
}

/// <summary>
/// The Arsonist's douse/ignite button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>arsonistButton</c>.
/// </summary>
public sealed class ArsonistButton : TorButton
{
    private static ArsonistButton arsonistButton;

    public ArsonistButton()
    {
        arsonistButton = this;

        SetSprite(TorAssets.DouseButton);
        PositionOffset = TorButtonPositions.LowerRowRight;
        Hotkey = KeyCode.F;
        EffectEnabled = true;
        ShowButtonText = true;
        ButtonText = new ButtonText(22);

        RealOnClick = () =>
        {
            var dousedEveryoneAlive = Arsonist.dousedEveryoneAlive();
            if (dousedEveryoneAlive)
            {
                PlayerControl.LocalPlayer.RpcArsonistWin();
                arsonistButton.EffectEnabled = false;
            }
            else if (Arsonist.currentTarget != null)
            {
                Arsonist.douseTarget = Arsonist.currentTarget;
                arsonistButton.EffectEnabled = true;
                SoundEffectsManager.play("arsonistDouse");
            }
        };
        HasButton = () =>
        {
            return Arsonist.arsonist != null && Arsonist.arsonist == PlayerControl.LocalPlayer &&
                   !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () =>
        {
            var dousedEveryoneAlive = Arsonist.dousedEveryoneAlive();
            if (dousedEveryoneAlive)
            {
                arsonistButton.actionButton.graphic.sprite = TorAssets.IgniteButton.LoadAsset();
                arsonistButton.ButtonText = new ButtonText(23);
            }

            if (arsonistButton.isEffectActive && Arsonist.douseTarget != Arsonist.currentTarget)
            {
                Arsonist.douseTarget = null;
                arsonistButton.Timer = 0f;
                arsonistButton.isEffectActive = false;
            }

            return PlayerControl.LocalPlayer.CanMove && (dousedEveryoneAlive || Arsonist.currentTarget != null);
        };
        OnMeetingEnds = () =>
        {
            arsonistButton.Timer = arsonistButton.MaxTimer;
            arsonistButton.isEffectActive = false;
            Arsonist.douseTarget = null;
        };
    }

    public override float Cooldown => Arsonist.cooldown;

    public override float EffectDuration => Arsonist.duration;

    public override void OnEffectEnd()
    {
        if (Arsonist.douseTarget != null) Arsonist.dousedPlayers.Add(Arsonist.douseTarget);

        arsonistButton.Timer = Arsonist.dousedEveryoneAlive() ? 0 : arsonistButton.MaxTimer;

        foreach (var p in Arsonist.dousedPlayers)
            if (TORMapOptions.playerIcons.ContainsKey(p.PlayerId))
                TORMapOptions.playerIcons[p.PlayerId].setSemiTransparent(false);

        // Ghost Info
        PlayerControl.LocalPlayer.RpcGhostArsonistDouse(PlayerControl.LocalPlayer.PlayerId,
            Arsonist.douseTarget.PlayerId);

        Arsonist.douseTarget = null;
    }
}

public static class ArsonistRpcs
{
    [MethodRpc((uint)TorRpc.ArsonistWin, LocalHandling = RpcLocalHandling.After)]
    public static void RpcArsonistWin(this PlayerControl player)
    {
        Arsonist.triggerArsonistWin = true;
        foreach (var p in PlayerControl.AllPlayerControls)
            if (p != Arsonist.arsonist && !p.Data.IsDead)
            {
                p.Exiled();
                GameHistory.overrideDeathReasonAndKiller(p, DeadPlayer.CustomDeathReason.Arson, Arsonist.arsonist);
            }
    }
}
