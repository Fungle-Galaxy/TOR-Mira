using System;
using MiraAPI.GameOptions.OptionTypes;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using UnityEngine;
using MiraAPI.Utilities;
using TheOtherRoles.Buttons;

namespace TheOtherRoles.Roles.Impostor;

public class Camouflager(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = Palette.ImpostorRed;

    public static RoleInfo Info = new(color, RoleId.Camouflager);

    public static PlayerControl camouflager;
    public static float cooldown = 30f;
    public static float duration = 10f;
    public static float camouflageTimer;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Camouflager;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<CamouflagerOptions>.Instance.SpawnRate;

    public static void resetCamouflage()
    {
        camouflageTimer = 0f;
        foreach (var p in PlayerControl.AllPlayerControls)
        {
            if (p == Ninja.ninja && Ninja.isInvisble)
                continue;
            p.setDefaultLook();
        }
    }

    public static void clearAndReload()
    {
        resetCamouflage();
        camouflager = null;
        camouflageTimer = 0f;
        cooldown = OptionGroupSingleton<CamouflagerOptions>.Instance.Cooldown.Value;
        duration = OptionGroupSingleton<CamouflagerOptions>.Instance.Duration.Value;
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;
}

/// <summary>
/// The settings of CamouflagerOptions. They live next to the role on purpose: the group is bound
/// to Camouflager, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class CamouflagerOptions : TorRoleOptionGroup<Camouflager>
{
    public override uint GroupPriority => 120;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Camouflager));
    public override Color GroupColor => TorOptionColors.Group(Camouflager.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.Camouflager), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedNumberOption Cooldown { get; } =
        new ModdedNumberOption("Opt-Camouflager,1", 30f, 10f, 60f, 2.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption Duration { get; } =
        new ModdedNumberOption("Opt-Camouflager,2", 10f, 1f, 20f, 0.5f, MiraNumberSuffixes.None, "0.##");
}

/// <summary>
/// The Camouflager's camouflage button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>camouflagerButton</c>.
/// </summary>
public sealed class CamouflagerButton : TorButton
{
    private static CamouflagerButton camouflagerButton;

    public CamouflagerButton()
    {
        camouflagerButton = this;

        SetSprite(TorAssets.CamoButton);
        PositionOffset = TorButtonPositions.UpperRowLeft;
        Hotkey = KeyCode.F;
        EffectEnabled = true;
        ShowButtonText = true;
        ButtonText = new ButtonText(9);

        RealOnClick = () =>
        {
            PlayerControl.LocalPlayer.RpcCamouflagerCamouflage();
            SoundEffectsManager.play("morphlingMorph");
        };
        HasButton = () =>
        {
            return Camouflager.camouflager != null && Camouflager.camouflager == PlayerControl.LocalPlayer &&
                   !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () => { return PlayerControl.LocalPlayer.CanMove; };
        OnMeetingEnds = () =>
        {
            camouflagerButton.Timer = camouflagerButton.MaxTimer;
            camouflagerButton.isEffectActive = false;
            camouflagerButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
        };
    }

    public override float Cooldown => Camouflager.cooldown;

    public override float EffectDuration => Camouflager.duration;

    public override void OnEffectEnd()
    {
        camouflagerButton.Timer = camouflagerButton.MaxTimer;
        SoundEffectsManager.play("morphlingMorph");
    }
}

public static class CamouflagerRpcs
{
    [MethodRpc((uint)TorRpc.CamouflagerCamouflage, LocalHandling = RpcLocalHandling.After)]
    public static void RpcCamouflagerCamouflage(this PlayerControl player)
    {
        if (Camouflager.camouflager == null) return;

        Camouflager.camouflageTimer = Camouflager.duration;
        if (Helpers.MushroomSabotageActive()) return; // Dont overwrite the fungle "camo"
        foreach (var p in PlayerControl.AllPlayerControls)
            p.setLook("", 6, "", "", "", "");
    }
}
