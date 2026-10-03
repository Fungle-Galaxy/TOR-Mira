using System;
using MiraAPI.GameOptions.OptionTypes;
using System.Collections.Generic;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheOtherRoles.Patches;
using UnityEngine;
using MiraAPI.Utilities;
using TheOtherRoles.Buttons;

namespace TheOtherRoles.Roles.Impostor;

public class Eraser(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = Palette.ImpostorRed;

    public static RoleInfo Info = new(color, RoleId.Eraser);

    public static PlayerControl eraser;
    public static List<byte> alreadyErased = new();
    public static List<PlayerControl> futureErased = new();
    public static PlayerControl currentTarget;
    public static float cooldown = 30f;
    public static bool canEraseAnyone;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Eraser;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<EraserOptions>.Instance.SpawnRate;

    public static void clearAndReload()
    {
        eraser = null;
        futureErased = new List<PlayerControl>();
        currentTarget = null;
        cooldown = OptionGroupSingleton<EraserOptions>.Instance.Cooldown.Value;
        canEraseAnyone = OptionGroupSingleton<EraserOptions>.Instance.CanEraseAnyone.Value;
        alreadyErased = new List<byte>();
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        if (Eraser.eraser == null || Eraser.eraser != player) return;
        var untargetables = new List<PlayerControl>();
        if (Spy.spy != null) untargetables.Add(Spy.spy);
        if (Sidekick.wasTeamRed) untargetables.Add(Sidekick.sidekick);
        if (Jackal.wasTeamRed) untargetables.Add(Jackal.jackal);
        Eraser.currentTarget = PlayerControlFixedUpdatePatch.setTarget(!Eraser.canEraseAnyone,
            untargetablePlayers: Eraser.canEraseAnyone ? new List<PlayerControl>() : untargetables);
        PlayerControlFixedUpdatePatch.setPlayerOutline(Eraser.currentTarget, Eraser.color);
    }
}

/// <summary>
/// The settings of EraserOptions. They live next to the role on purpose: the group is bound
/// to Eraser, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class EraserOptions : TorRoleOptionGroup<Eraser>
{
    public override uint GroupPriority => 140;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Eraser));
    public override Color GroupColor => TorOptionColors.Group(Eraser.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.Eraser), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedNumberOption Cooldown { get; } =
        new ModdedNumberOption("Opt-Eraser,1", 30f, 10f, 120f, 5f, MiraNumberSuffixes.None, "0.##");

    public ModdedToggleOption CanEraseAnyone { get; } =
        new ModdedToggleOption("Opt-Eraser,2", false);
}

/// <summary>
/// The Eraser's erase button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>eraserButton</c>.
/// </summary>
public sealed class EraserButton : TorButton
{
    private static EraserButton eraserButton;

    public EraserButton()
    {
        eraserButton = this;

        SetSprite(TorAssets.EraserButton);
        PositionOffset = TorButtonPositions.UpperRowLeft;
        Hotkey = KeyCode.F;
        ShowButtonText = true;
        ButtonText = new ButtonText(17);

        RealOnClick = () =>
        {
            eraserButton.MaxTimer += 10;
            eraserButton.Timer = eraserButton.MaxTimer;

            PlayerControl.LocalPlayer.RpcSetFutureErased(Eraser.currentTarget.PlayerId);
            SoundEffectsManager.play("eraserErase");
        };
        HasButton = () =>
        {
            return Eraser.eraser != null && Eraser.eraser == PlayerControl.LocalPlayer &&
                   !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () => { return PlayerControl.LocalPlayer.CanMove && Eraser.currentTarget != null; };
        OnMeetingEnds = () => { eraserButton.Timer = eraserButton.MaxTimer; };
    }

    public override float Cooldown => Eraser.cooldown;
}

public static class EraserRpcs
{
    [MethodRpc((uint)TorRpc.SetFutureErased, LocalHandling = RpcLocalHandling.After)]
    public static void RpcSetFutureErased(this PlayerControl player, byte playerId)
    {
        var target = Helpers.playerById(playerId);
        if (Eraser.futureErased == null)
            Eraser.futureErased = new List<PlayerControl>();
        if (target != null) Eraser.futureErased.Add(target);
    }

    [MethodRpc((uint)TorRpc.ErasePlayerRoles, LocalHandling = RpcLocalHandling.After)]
    public static void RpcErasePlayerRoles(this PlayerControl player, byte playerId)
    {
        RPCProcedure.erasePlayerRoles(playerId);
    }
}
