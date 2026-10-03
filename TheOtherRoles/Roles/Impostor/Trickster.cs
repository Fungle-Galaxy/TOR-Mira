using System;
using MiraAPI.GameOptions.OptionTypes;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheOtherRoles.Objects;
using UnityEngine;
using MiraAPI.Utilities;
using TheOtherRoles.Buttons;

namespace TheOtherRoles.Roles.Impostor;

public class Trickster(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = Palette.ImpostorRed;

    public static RoleInfo Info = new(color, RoleId.Trickster);

    public static PlayerControl trickster;
    public static float placeBoxCooldown = 30f;
    public static float lightsOutCooldown = 30f;
    public static float lightsOutDuration = 10f;
    public static float lightsOutTimer;
    
    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Trickster;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<TricksterOptions>.Instance.SpawnRate;
    
    public static void clearAndReload()
    {
        trickster = null;
        lightsOutTimer = 0f;
        placeBoxCooldown = OptionGroupSingleton<TricksterOptions>.Instance.PlaceBoxCooldown.Value;
        lightsOutCooldown = OptionGroupSingleton<TricksterOptions>.Instance.LightsOutCooldown.Value;
        lightsOutDuration = OptionGroupSingleton<TricksterOptions>.Instance.LightsOutDuration.Value;
        JackInTheBox.UpdateStates();
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        Trickster.lightsOutTimer -= Time.deltaTime;
    }
}

/// <summary>
/// The settings of TricksterOptions. They live next to the role on purpose: the group is bound
/// to Trickster, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class TricksterOptions : TorRoleOptionGroup<Trickster>
{
    public override uint GroupPriority => 150;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Trickster));
    public override Color GroupColor => TorOptionColors.Group(Trickster.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.Trickster), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedNumberOption PlaceBoxCooldown { get; } =
        new ModdedNumberOption("Opt-Trickster,1", 10f, 2.5f, 30f, 2.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption LightsOutCooldown { get; } =
        new ModdedNumberOption("Opt-Trickster,2", 30f, 10f, 60f, 5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption LightsOutDuration { get; } =
        new ModdedNumberOption("Opt-Trickster,3", 15f, 5f, 60f, 2.5f, MiraNumberSuffixes.None, "0.##");
}

/// <summary>
/// The Trickster's "place jack-in-the-box" button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>placeJackInTheBoxButton</c>.
/// </summary>
public sealed class PlaceJackInTheBoxButton : TorButton
{
    private static PlaceJackInTheBoxButton placeJackInTheBoxButton;

    public PlaceJackInTheBoxButton()
    {
        placeJackInTheBoxButton = this;

        SetSprite(TorAssets.PlaceJackInTheBoxButton);
        PositionOffset = TorButtonPositions.UpperRowLeft;
        Hotkey = KeyCode.F;
        ShowButtonText = true;
        ButtonText = new ButtonText(18);

        RealOnClick = () =>
        {
            placeJackInTheBoxButton.Timer = placeJackInTheBoxButton.MaxTimer;

            var pos = PlayerControl.LocalPlayer.transform.position;
            var buff = new byte[sizeof(float) * 2];
            Buffer.BlockCopy(BitConverter.GetBytes(pos.x), 0, buff, 0 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(pos.y), 0, buff, 1 * sizeof(float), sizeof(float));

            PlayerControl.LocalPlayer.RpcPlaceJackInTheBox(buff);
            SoundEffectsManager.play("tricksterPlaceBox");
        };
        HasButton = () =>
        {
            return Trickster.trickster != null && Trickster.trickster == PlayerControl.LocalPlayer &&
                   !PlayerControl.LocalPlayer.Data.IsDead && !JackInTheBox.hasJackInTheBoxLimitReached();
        };
        CouldUse = () => { return PlayerControl.LocalPlayer.CanMove && !JackInTheBox.hasJackInTheBoxLimitReached(); };
        OnMeetingEnds = () => { placeJackInTheBoxButton.Timer = placeJackInTheBoxButton.MaxTimer; };
    }

    public override float Cooldown => Trickster.placeBoxCooldown;
}

/// <summary>
/// The Trickster's "lights out" button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>lightsOutButton</c>.
/// </summary>
public sealed class LightsOutButton : TorButton
{
    private static LightsOutButton lightsOutButton;

    public LightsOutButton()
    {
        lightsOutButton = this;

        SetSprite(TorAssets.LightsOutButton);
        PositionOffset = TorButtonPositions.UpperRowLeft;
        Hotkey = KeyCode.F;
        EffectEnabled = true;
        ShowButtonText = true;
        ButtonText = new ButtonText(19);

        RealOnClick = () =>
        {
            PlayerControl.LocalPlayer.RpcLightsOut();
            SoundEffectsManager.play("lighterLight");
        };
        HasButton = () =>
        {
            return Trickster.trickster != null && Trickster.trickster == PlayerControl.LocalPlayer &&
                   !PlayerControl.LocalPlayer.Data.IsDead
                   && JackInTheBox.hasJackInTheBoxLimitReached() && JackInTheBox.boxesConvertedToVents;
        };
        CouldUse = () =>
        {
            return PlayerControl.LocalPlayer.CanMove && JackInTheBox.hasJackInTheBoxLimitReached() &&
                   JackInTheBox.boxesConvertedToVents;
        };
        OnMeetingEnds = () =>
        {
            lightsOutButton.Timer = lightsOutButton.MaxTimer;
            lightsOutButton.isEffectActive = false;
            lightsOutButton.actionButton.graphic.color = Palette.EnabledColor;
        };
    }

    public override float Cooldown => Trickster.lightsOutCooldown;

    public override float EffectDuration => Trickster.lightsOutDuration;

    public override void OnEffectEnd()
    {
        lightsOutButton.Timer = lightsOutButton.MaxTimer;
        SoundEffectsManager.play("lighterLight");
    }

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        Timer = Cooldown;
    }
}

public static class TricksterRpcs
{
    [MethodRpc((uint)TorRpc.PlaceJackInTheBox, LocalHandling = RpcLocalHandling.After)]
    public static void RpcPlaceJackInTheBox(this PlayerControl player, byte[] buff)
    {
        var position = Vector3.zero;
        position.x = BitConverter.ToSingle(buff, 0 * sizeof(float));
        position.y = BitConverter.ToSingle(buff, 1 * sizeof(float));
        new JackInTheBox(position);
    }

    [MethodRpc((uint)TorRpc.LightsOut, LocalHandling = RpcLocalHandling.After)]
    public static void RpcLightsOut(this PlayerControl player)
    {
        Trickster.lightsOutTimer = Trickster.lightsOutDuration;
        // If the local player is impostor indicate lights out
        if (Helpers.hasImpVision(GameData.Instance.GetPlayerById(PlayerControl.LocalPlayer.PlayerId)))
            new CustomMessage("Lights are out", Trickster.lightsOutDuration);
    }
}
