using System;
using MiraAPI.GameOptions.OptionTypes;
using System.Collections.Generic;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheOtherRoles.Buttons;
using TheOtherRoles.Networking;
using TheOtherRoles.Patches;
using TheOtherRoles.Utilities;
using UnityEngine;

namespace TheOtherRoles.Roles.Crewmate;

public class Deputy(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = Sheriff.color;

    public static RoleInfo Info = new(color, RoleId.Deputy);

    public static PlayerControl deputy;

    public static PlayerControl currentTarget;
    public static List<byte> handcuffedPlayers = new();
    public static int promotesToSheriff;
    public static bool keepsHandcuffsOnPromotion;
    public static float handcuffDuration;
    public static float remainingHandcuffs;
    public static float handcuffCooldown;
    public static bool knowsSheriff;
    public static Dictionary<byte, float> handcuffedKnows = new();

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Deputy;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<DeputyOptions>.Instance.SpawnRate;

    // The Deputy only exists in a game that already has a Sheriff, so Mira API must not roll it -
    // TOR hands it out itself once the Sheriff is known.
    public override bool CanSpawnOnCurrentMode() => false;

    public override bool? ForceShowRoleOnWiki => true;

    public static void setHandcuffedKnows(bool active = true, byte playerId = byte.MaxValue)
    {
        if (playerId == byte.MaxValue)
            playerId = PlayerControl.LocalPlayer.PlayerId;

        if (active && playerId == PlayerControl.LocalPlayer.PlayerId)
        {
            PlayerControl.LocalPlayer.RpcGhostHandcuffNoticed(PlayerControl.LocalPlayer.PlayerId);
        }

        if (active)
        {
            handcuffedKnows.Add(playerId, handcuffDuration);
            handcuffedPlayers.RemoveAll(x => x == playerId);
        }

        if (playerId == PlayerControl.LocalPlayer.PlayerId)
        {
            HudManagerStartPatch.setAllButtonsHandcuffedStatus(active);
            SoundEffectsManager.play("deputyHandcuff");
        }
    }

    public static void clearAndReload()
    {
        deputy = null;
        currentTarget = null;
        handcuffedPlayers = new List<byte>();
        handcuffedKnows = new Dictionary<byte, float>();
        HudManagerStartPatch.setAllButtonsHandcuffedStatus(false, true);
        promotesToSheriff = OptionGroupSingleton<DeputyOptions>.Instance.GetsPromoted.Selection();
        remainingHandcuffs = OptionGroupSingleton<DeputyOptions>.Instance.NumberOfHandcuffs.Value;
        handcuffCooldown = OptionGroupSingleton<DeputyOptions>.Instance.HandcuffCooldown.Value;
        keepsHandcuffsOnPromotion = OptionGroupSingleton<DeputyOptions>.Instance.KeepsHandcuffs.Value;
        handcuffDuration = OptionGroupSingleton<DeputyOptions>.Instance.HandcuffDuration.Value;
        knowsSheriff = OptionGroupSingleton<DeputyOptions>.Instance.KnowsSheriff.Value;
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        var keys = new List<byte>(Deputy.handcuffedKnows.Keys);
        foreach (var key in keys)
            Deputy.handcuffedKnows[key] -= Time.deltaTime;
        // deputySetTarget
        if (Deputy.deputy == null || Deputy.deputy != player) return;
        Deputy.currentTarget = PlayerControlFixedUpdatePatch.setTarget();
        PlayerControlFixedUpdatePatch.setPlayerOutline(Deputy.currentTarget, Deputy.color);

        // deputyUpdate
        if (PlayerControl.LocalPlayer == null ||
            !Deputy.handcuffedKnows.ContainsKey(PlayerControl.LocalPlayer.PlayerId)) return;
        if (Deputy.handcuffedKnows[PlayerControl.LocalPlayer.PlayerId] <= 0)
        {
            Deputy.handcuffedKnows.Remove(PlayerControl.LocalPlayer.PlayerId);
            Deputy.setHandcuffedKnows(false);
            PlayerControl.LocalPlayer.RpcGhostHandcuffOver(PlayerControl.LocalPlayer.PlayerId);
        }

        // deputyCheckPromotion
        if (Deputy.promotesToSheriff == 0 || Deputy.deputy.Data.IsDead ||
            (Deputy.promotesToSheriff == 2 && false)) return; // isMeeting always false in FixedUpdate
        if (Sheriff.sheriff == null || Sheriff.sheriff?.Data?.Disconnected == true || Sheriff.sheriff.Data.IsDead)
        {
            PlayerControl.LocalPlayer.RpcDeputyPromotes();
        }
    }
}
/// <summary>The Deputy's own page behind the cog on its Role Settings row.</summary>
public class DeputyOptions : TorRoleOptionGroup<Deputy>
{
    public override uint GroupPriority => 421;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Deputy));
    public override Color GroupColor => TorOptionColors.Group(Deputy.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.Deputy), TorOptions.Rates[0], TorOptions.Rates)
        {
            Visible = () => false
        };

    public ModdedNumberOption NumberOfHandcuffs { get; } =
        new ModdedNumberOption("Opt-Sheriff,4", 3f, 1f, 10f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption HandcuffCooldown { get; } =
        new ModdedNumberOption("Opt-Sheriff,5", 30f, 10f, 60f, 2.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption HandcuffDuration { get; } =
        new ModdedNumberOption("Opt-Sheriff,6", 15f, 5f, 60f, 2.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedToggleOption KnowsSheriff { get; } =
        new ModdedToggleOption("Opt-Sheriff,7", true);

    public ModdedStringOption GetsPromoted { get; } =
        new ModdedStringOption("Opt-Sheriff,8", "Opt-General,69", ["Opt-General,69", "Opt-Sheriff,101", "Opt-Sheriff,102"]);

    public ModdedToggleOption KeepsHandcuffs { get; } =
        new ModdedToggleOption("Opt-Sheriff,9", true)
        {
            Visible = () => OptionGroupSingleton<DeputyOptions>.Instance.GetsPromoted.Enabled()
        };
}

/// <summary>
/// The Deputy's "handcuff" button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>deputyHandcuffButton</c>.
/// </summary>
public sealed class DeputyHandcuffButton : TorButton
{
    private static DeputyHandcuffButton deputyHandcuffButton;

    public DeputyHandcuffButton()
    {
        deputyHandcuffButton = this;

        SetSprite(TorAssets.DeputyHandcuffButton);
        PositionOffset = TorButtonPositions.LowerRowRight;
        Hotkey = KeyCode.F;
        ShowButtonText = true;

        RealOnClick = () =>
        {
            byte targetId = 0;
            targetId = Sheriff.sheriff == PlayerControl.LocalPlayer
                ? Sheriff.currentTarget.PlayerId
                : Deputy.currentTarget
                    .PlayerId; // If the deputy is now the sheriff, sheriffs target, else deputies target

            PlayerControl.LocalPlayer.RpcDeputyUsedHandcuffs(targetId);
            Deputy.currentTarget = null;
            deputyHandcuffButton.Timer = deputyHandcuffButton.MaxTimer;

            SoundEffectsManager.play("deputyHandcuff");
        };
        HasButton = () =>
        {
            return ((Deputy.deputy != null && Deputy.deputy == PlayerControl.LocalPlayer) ||
                    (Sheriff.sheriff != null && Sheriff.sheriff == PlayerControl.LocalPlayer &&
                     Sheriff.sheriff == Sheriff.formerDeputy && Deputy.keepsHandcuffsOnPromotion)) &&
                   !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () =>
        {
            SetUses(Mathf.RoundToInt(Deputy.remainingHandcuffs));
            return ((Deputy.deputy != null && Deputy.deputy == PlayerControl.LocalPlayer && Deputy.currentTarget) ||
                    (Sheriff.sheriff != null && Sheriff.sheriff == PlayerControl.LocalPlayer &&
                     Sheriff.sheriff == Sheriff.formerDeputy && Sheriff.currentTarget)) &&
                   Deputy.remainingHandcuffs > 0 && PlayerControl.LocalPlayer.CanMove;
        };
        OnMeetingEnds = () => { deputyHandcuffButton.Timer = deputyHandcuffButton.MaxTimer; };
    }

    public override float Cooldown => Deputy.handcuffCooldown;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        ButtonText = new ButtonText(3,
            FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.UseButton]
                .FontMaterial);
    }
}

public static class DeputyRpcs
{
    [MethodRpc((uint)TorRpc.DeputyUsedHandcuffs, LocalHandling = RpcLocalHandling.After)]
    public static void RpcDeputyUsedHandcuffs(this PlayerControl player, byte targetId)
    {
        Deputy.remainingHandcuffs--;
        Deputy.handcuffedPlayers.Add(targetId);
    }

    [MethodRpc((uint)TorRpc.DeputyPromotes, LocalHandling = RpcLocalHandling.After)]
    public static void RpcDeputyPromotes(this PlayerControl player)
    {
        RPCProcedure.deputyPromotes();
    }
}