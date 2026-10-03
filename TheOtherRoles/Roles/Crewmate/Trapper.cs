using System;
using MiraAPI.GameOptions.OptionTypes;
using System.Collections.Generic;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheOtherRoles.Buttons;
using TheOtherRoles.Objects;
using UnityEngine;
using MiraAPI.Utilities;
using TheOtherRoles.Utilities;

namespace TheOtherRoles.Roles.Crewmate;

public class Trapper(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = new Color32(110, 57, 105, byte.MaxValue);

    public static RoleInfo Info = new(color, RoleId.Trapper);

    public static PlayerControl trapper;

    public static float cooldown = 30f;
    public static int maxCharges = 5;
    public static int rechargeTasksNumber = 3;
    public static int rechargedTasks = 3;
    public static int charges = 1;
    public static int trapCountToReveal = 2;
    public static List<byte> playersOnMap = new();
    public static bool anonymousMap;
    public static int infoType;
    public static float trapDuration = 5f;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Trapper;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<TrapperOptions>.Instance.SpawnRate;

    public static void clearAndReload()
    {
        trapper = null;
        cooldown = OptionGroupSingleton<TrapperOptions>.Instance.Cooldown.Value;
        maxCharges = Mathf.RoundToInt(OptionGroupSingleton<TrapperOptions>.Instance.MaxCharges.Value);
        rechargeTasksNumber = Mathf.RoundToInt(OptionGroupSingleton<TrapperOptions>.Instance.RechargeTasksNumber.Value);
        rechargedTasks = Mathf.RoundToInt(OptionGroupSingleton<TrapperOptions>.Instance.RechargeTasksNumber.Value);
        charges = Mathf.RoundToInt(OptionGroupSingleton<TrapperOptions>.Instance.MaxCharges.Value) / 2;
        trapCountToReveal = Mathf.RoundToInt(OptionGroupSingleton<TrapperOptions>.Instance.TrapNeededTriggerToReveal.Value);
        playersOnMap = new List<byte>();
        anonymousMap = OptionGroupSingleton<TrapperOptions>.Instance.AnonymousMap.Value;
        infoType = OptionGroupSingleton<TrapperOptions>.Instance.InfoType.Selection();
        trapDuration = OptionGroupSingleton<TrapperOptions>.Instance.TrapDuration.Value;
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        if (Trapper.trapper == null || player != Trapper.trapper || Trapper.trapper.Data.IsDead) return;
        var taskInfo = TasksHandler.taskInfo(Trapper.trapper.Data);
        int playerCompleted = taskInfo.Item1;
        if (playerCompleted == Trapper.rechargedTasks)
        {
            Trapper.rechargedTasks += Trapper.rechargeTasksNumber;
            if (Trapper.maxCharges > Trapper.charges) Trapper.charges++;
        }
    }
}

/// <summary>
/// The settings of TrapperOptions. They live next to the role on purpose: the group is bound
/// to Trapper, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class TrapperOptions : TorRoleOptionGroup<Trapper>
{
    public override uint GroupPriority => 380;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Trapper));
    public override Color GroupColor => TorOptionColors.Group(Trapper.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.Trapper), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedNumberOption Cooldown { get; } =
        new ModdedNumberOption("Opt-Trapper,1", 30f, 5f, 120f, 5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption MaxCharges { get; } =
        new ModdedNumberOption("Opt-Trapper,2", 5f, 1f, 15f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption RechargeTasksNumber { get; } =
        new ModdedNumberOption("Opt-Trapper,3", 2f, 1f, 15f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption TrapNeededTriggerToReveal { get; } =
        new ModdedNumberOption("Opt-Trapper,4", 3f, 2f, 10f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedToggleOption AnonymousMap { get; } =
        new ModdedToggleOption("Opt-Trapper,5", false);

    public ModdedStringOption InfoType { get; } =
        new ModdedStringOption("Opt-Trapper,6", "Opt-Trapper,100", ["Opt-Trapper,100", "Opt-Trapper,101", "Opt-Trapper,102"]);

    public ModdedNumberOption TrapDuration { get; } =
        new ModdedNumberOption("Opt-Trapper,7", 5f, 1f, 15f, 1f, MiraNumberSuffixes.None, "0.##");
}

/// <summary>
/// The Trapper's "place trap" button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>trapperButton</c>.
/// </summary>
public sealed class TrapperButton : TorButton
{
    private static TrapperButton trapperButton;

    public TrapperButton()
    {
        trapperButton = this;

        SetSprite(TorAssets.TrapperPlaceButton);
        PositionOffset = TorButtonPositions.LowerRowRight;
        Hotkey = KeyCode.F;
        ShowButtonText = true;

        RealOnClick = () =>
        {
            var pos = PlayerControl.LocalPlayer.transform.position;
            var buff = new byte[sizeof(float) * 2];
            Buffer.BlockCopy(BitConverter.GetBytes(pos.x), 0, buff, 0 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(pos.y), 0, buff, 1 * sizeof(float), sizeof(float));

            PlayerControl.LocalPlayer.RpcSetTrap(buff);

            SoundEffectsManager.play("trapperTrap");
            trapperButton.Timer = trapperButton.MaxTimer;
        };
        HasButton = () =>
        {
            return Trapper.trapper != null && Trapper.trapper == PlayerControl.LocalPlayer &&
                   !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () =>
        {
            SetUsesText($"{Trapper.charges} / {Trapper.maxCharges}");
            return PlayerControl.LocalPlayer.CanMove && Trapper.charges > 0;
        };
        OnMeetingEnds = () => { trapperButton.Timer = trapperButton.MaxTimer; };
    }

    public override float Cooldown => Trapper.cooldown;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        ButtonText = new ButtonText(32,
            FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.UseButton]
                .FontMaterial);
    }
}

public static class TrapperRpcs
{
    [MethodRpc((uint)TorRpc.SetTrap, LocalHandling = RpcLocalHandling.After)]
    public static void RpcSetTrap(this PlayerControl player, byte[] buff)
    {
        if (Trapper.trapper == null) return;
        Trapper.charges -= 1;
        var position = Vector3.zero;
        position.x = BitConverter.ToSingle(buff, 0 * sizeof(float));
        position.y = BitConverter.ToSingle(buff, 1 * sizeof(float));
        new Trap(position);
    }
}
