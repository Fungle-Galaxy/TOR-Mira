using System;
using MiraAPI.GameOptions.OptionTypes;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheOtherRoles.Buttons;
using TheOtherRoles.Patches;
using UnityEngine;
using MiraAPI.Utilities;

namespace TheOtherRoles.Roles.Impostor;

public class Morphling(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = Palette.ImpostorRed;

    public static RoleInfo Info = new(color, RoleId.Morphling);

    public static PlayerControl morphling;

    public static float cooldown = 30f;
    public static float duration = 10f;

    public static PlayerControl currentTarget;
    public static PlayerControl sampledTarget;
    public static PlayerControl morphTarget;
    public static float morphTimer;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Morphling;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<MorphlingOptions>.Instance.SpawnRate;

    public static void resetMorph()
    {
        morphTarget = null;
        morphTimer = 0f;
        if (morphling == null) return;
        morphling.setDefaultLook();
    }

    public static void clearAndReload()
    {
        resetMorph();
        morphling = null;
        currentTarget = null;
        sampledTarget = null;
        morphTarget = null;
        morphTimer = 0f;
        cooldown = OptionGroupSingleton<MorphlingOptions>.Instance.Cooldown.Value;
        duration = OptionGroupSingleton<MorphlingOptions>.Instance.Duration.Value;
    }
    
    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        if (Morphling.morphling == null || Morphling.morphling != player) return;
        Morphling.currentTarget = PlayerControlFixedUpdatePatch.setTarget();
        PlayerControlFixedUpdatePatch.setPlayerOutline(Morphling.currentTarget, Morphling.color);
    }
}

/// <summary>
/// The settings of MorphlingOptions. They live next to the role on purpose: the group is bound
/// to Morphling, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class MorphlingOptions : TorRoleOptionGroup<Morphling>
{
    public override uint GroupPriority => 110;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Morphling));
    public override Color GroupColor => TorOptionColors.Group(Morphling.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.Morphling), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedNumberOption Cooldown { get; } =
        new ModdedNumberOption("Opt-Morphling,1", 30f, 10f, 60f, 2.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption Duration { get; } =
        new ModdedNumberOption("Opt-Morphling,2", 10f, 1f, 20f, 0.5f, MiraNumberSuffixes.None, "0.##");
}

/// <summary>
/// The Morphling's sample/morph button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>morphlingButton</c>.
/// </summary>
public sealed class MorphlingButton : TorButton
{
    private static MorphlingButton morphlingButton;

    public MorphlingButton()
    {
        morphlingButton = this;

        SetSprite(TorAssets.SampleButton);
        PositionOffset = TorButtonPositions.UpperRowLeft;
        Hotkey = KeyCode.F;
        EffectEnabled = true;
        // The sample phase is 1s, the morph phase is Morphling.duration - both are pushed into the
        // field-backed EffectLength below, so this class must NOT override EffectDuration.
        EffectLength = Morphling.duration;
        ShowButtonText = true;
        ButtonText = new ButtonText(7);

        RealOnClick = () =>
        {
            if (Morphling.sampledTarget != null)
            {
                PlayerControl.LocalPlayer.RpcMorphlingMorph(Morphling.sampledTarget.PlayerId);
                Morphling.sampledTarget = null;
                morphlingButton.EffectLength = Morphling.duration;
                SoundEffectsManager.play("morphlingMorph");
            }
            else if (Morphling.currentTarget != null)
            {
                Morphling.sampledTarget = Morphling.currentTarget;
                morphlingButton.SetSprite(TorAssets.MorphButton);
                morphlingButton.ButtonText = new ButtonText(7);
                morphlingButton.EffectLength = 1f;
                SoundEffectsManager.play("morphlingSample");

                // Add poolable player to the button so that the target outfit is shown
                HudManagerStartPatch.setButtonTargetDisplay(Morphling.sampledTarget, morphlingButton);
            }
        };
        HasButton = () =>
        {
            return Morphling.morphling != null && Morphling.morphling == PlayerControl.LocalPlayer &&
                   !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () =>
        {
            return (Morphling.currentTarget || Morphling.sampledTarget) && PlayerControl.LocalPlayer.CanMove &&
                   !Helpers.MushroomSabotageActive();
        };
        OnMeetingEnds = () =>
        {
            morphlingButton.Timer = morphlingButton.MaxTimer;
            morphlingButton.SetSprite(TorAssets.SampleButton);
            morphlingButton.ButtonText = new ButtonText(8);
            morphlingButton.isEffectActive = false;
            morphlingButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
            Morphling.sampledTarget = null;
            HudManagerStartPatch.setButtonTargetDisplay(null);
        };
    }

    public override float Cooldown => Morphling.cooldown;

    public override void OnEffectEnd()
    {
        if (Morphling.sampledTarget == null)
        {
            morphlingButton.Timer = morphlingButton.MaxTimer;
            morphlingButton.SetSprite(TorAssets.SampleButton);
            SoundEffectsManager.play("morphlingMorph");

            // Reset the poolable player
            HudManagerStartPatch.setButtonTargetDisplay(null);
        }
    }
}

public static class MorphlingRpcs
{
    [MethodRpc((uint)TorRpc.MorphlingMorph, LocalHandling = RpcLocalHandling.After)]
    public static void RpcMorphlingMorph(this PlayerControl player, byte playerId)
    {
        var target = Helpers.playerById(playerId);
        if (Morphling.morphling == null || target == null) return;

        Morphling.morphTimer = Morphling.duration;
        Morphling.morphTarget = target;
        if (Camouflager.camouflageTimer <= 0f)
            Morphling.morphling.setLook(target.Data.PlayerName, target.Data.DefaultOutfit.ColorId,
                target.Data.DefaultOutfit.HatId, target.Data.DefaultOutfit.VisorId, target.Data.DefaultOutfit.SkinId,
                target.Data.DefaultOutfit.PetId);
    }
}
