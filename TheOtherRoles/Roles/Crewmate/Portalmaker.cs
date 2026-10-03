using System;
using AmongUs.GameOptions;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Hud;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheOtherRoles.Buttons;
using TheOtherRoles.Objects;
using TheOtherRoles.Utilities;
using UnityEngine;
using MiraAPI.Utilities;
using TMPro;

namespace TheOtherRoles.Roles.Crewmate;

public class Portalmaker(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = new Color32(69, 69, 169, byte.MaxValue);

    public static RoleInfo Info = new(color, RoleId.Portalmaker);

    public static PlayerControl portalmaker;

    public static float cooldown;
    public static float usePortalCooldown;
    public static bool logOnlyHasColors;
    public static bool logShowsTime;
    public static bool canPortalFromAnywhere;

    private static Sprite logSprite;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Portalmaker;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<PortalmakerOptions>.Instance.SpawnRate;

    public static Sprite getLogSprite()
    {
        if (logSprite) return logSprite;
        logSprite = FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.DoorLogsButton]
            .Image;
        return logSprite;
    }

    public static void clearAndReload()
    {
        portalmaker = null;
        cooldown = OptionGroupSingleton<PortalmakerOptions>.Instance.Cooldown.Value;
        usePortalCooldown = OptionGroupSingleton<PortalmakerOptions>.Instance.UsePortalCooldown.Value;
        logOnlyHasColors = OptionGroupSingleton<PortalmakerOptions>.Instance.LogOnlyColorType.Value;
        logShowsTime = OptionGroupSingleton<PortalmakerOptions>.Instance.LogHasTime.Value;
        canPortalFromAnywhere = OptionGroupSingleton<PortalmakerOptions>.Instance.CanPortalFromAnywhere.Value;
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;
}

/// <summary>
/// The settings of PortalmakerOptions. They live next to the role on purpose: the group is bound
/// to Portalmaker, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class PortalmakerOptions : TorRoleOptionGroup<Portalmaker>
{
    public override uint GroupPriority => 530;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Portalmaker));
    public override Color GroupColor => TorOptionColors.Group(Portalmaker.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.Portalmaker), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedNumberOption Cooldown { get; } =
        new ModdedNumberOption("Opt-Portalmaker,1", 30f, 10f, 60f, 2.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption UsePortalCooldown { get; } =
        new ModdedNumberOption("Opt-Portalmaker,2", 30f, 10f, 60f, 2.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedToggleOption LogOnlyColorType { get; } =
        new ModdedToggleOption("Opt-Portalmaker,3", true);

    public ModdedToggleOption LogHasTime { get; } =
        new ModdedToggleOption("Opt-Portalmaker,4", true);

    public ModdedToggleOption CanPortalFromAnywhere { get; } =
        new ModdedToggleOption("Opt-Portalmaker,5", true);
}

/// <summary>
/// The Portalmaker's "place portal" button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>portalmakerPlacePortalButton</c>.
/// </summary>
public sealed class PortalmakerPlacePortalButton : TorButton
{
    private static PortalmakerPlacePortalButton portalmakerPlacePortalButton;

    public PortalmakerPlacePortalButton()
    {
        portalmakerPlacePortalButton = this;

        SetSprite(TorAssets.PlacePortalButton);
        PositionOffset = TorButtonPositions.LowerRowRight;
        Hotkey = KeyCode.F;
        ShowButtonText = true;

        RealOnClick = () =>
        {
            portalmakerPlacePortalButton.Timer = portalmakerPlacePortalButton.MaxTimer;

            var pos = PlayerControl.LocalPlayer.transform.position;
            var buff = new byte[sizeof(float) * 2];
            Buffer.BlockCopy(BitConverter.GetBytes(pos.x), 0, buff, 0 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(pos.y), 0, buff, 1 * sizeof(float), sizeof(float));

            PlayerControl.LocalPlayer.RpcPlacePortal(buff);
            SoundEffectsManager.play("tricksterPlaceBox");
        };
        HasButton = () =>
        {
            return Portalmaker.portalmaker != null && Portalmaker.portalmaker == PlayerControl.LocalPlayer &&
                   !PlayerControl.LocalPlayer.Data.IsDead && Portal.secondPortal == null;
        };
        CouldUse = () => { return PlayerControl.LocalPlayer.CanMove && Portal.secondPortal == null; };
        OnMeetingEnds = () => { portalmakerPlacePortalButton.Timer = portalmakerPlacePortalButton.MaxTimer; };
    }

    public override float Cooldown => Portalmaker.cooldown;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        ButtonText = new ButtonText(15,
            FastDestroyableSingleton<RoleManager>.Instance.GetRole(RoleTypes.Engineer).Ability.FontMaterial);
    }
}

/// <summary>
/// The "use portal" button, shared by everyone once both portals are placed.
/// It used to be built centrally in <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>usePortalButton</c>.
/// </summary>
public sealed class UsePortalButton : TorButton
{
    private static UsePortalButton usePortalButton;
    public static TMP_Text portalmakerButtonText1;
    
    public UsePortalButton()
    {
        usePortalButton = this;

        SetSprite(TorAssets.UsePortalButton);
        PositionOffset = new Vector3(0.9f, -0.06f, 0);
        Hotkey = KeyCode.J;
        Mirror = true;
        ShowButtonText = true;

        RealOnClick = () =>
        {
            var didTeleport = false;
            Vector3 exit = Portal.findExit(PlayerControl.LocalPlayer.transform.position);
            Vector3 entry = Portal.findEntry(PlayerControl.LocalPlayer.transform.position);

            var portalMakerSoloTeleport = !Portal.locationNearEntry(PlayerControl.LocalPlayer.transform.position);
            if (portalMakerSoloTeleport)
            {
                exit = Portal.firstPortal.portalGameObject.transform.position;
                entry = PlayerControl.LocalPlayer.transform.position;
            }

            PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(entry);

            if (!PlayerControl.LocalPlayer.Data.IsDead)
            {
                // Ghosts can portal too, but non-blocking and only with a local animation
                PlayerControl.LocalPlayer.RpcUsePortal(PlayerControl.LocalPlayer.PlayerId,
                    portalMakerSoloTeleport ? (byte)1 : (byte)0);
            }

            RPCProcedure.usePortal(PlayerControl.LocalPlayer.PlayerId, portalMakerSoloTeleport ? (byte)1 : (byte)0);
            usePortalButton.Timer = usePortalButton.MaxTimer;
            CustomButtonSingleton<PortalmakerMoveToPortalButton>.Instance.Timer = usePortalButton.MaxTimer;
            SoundEffectsManager.play("portalUse");
            FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(Portal.teleportDuration,
                new Action<float>(p =>
                {
                    // Delayed action
                    PlayerControl.LocalPlayer.moveable = false;
                    PlayerControl.LocalPlayer.NetTransform.Halt();
                    if (p >= 0.5f && p <= 0.53f && !didTeleport && !MeetingHud.Instance)
                    {
                        if (SubmergedCompatibility.IsSubmerged) SubmergedCompatibility.ChangeFloor(exit.y > -7);
                        PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(exit);
                        didTeleport = true;
                    }

                    if (p == 1f) PlayerControl.LocalPlayer.moveable = true;
                })));
        };
        HasButton = () =>
        {
            if (PlayerControl.LocalPlayer == Portalmaker.portalmaker && Portal.bothPlacedAndEnabled &&
                portalmakerButtonText1 != null)
                portalmakerButtonText1.text =
                    Portal.locationNearEntry(PlayerControl.LocalPlayer.transform.position) ||
                    !Portalmaker.canPortalFromAnywhere
                        ? ""
                        : "1. " + Portal.firstPortal.room;
            return Portal.bothPlacedAndEnabled;
        };
        CouldUse = () =>
        {
            return PlayerControl.LocalPlayer.CanMove &&
                   (Portal.locationNearEntry(PlayerControl.LocalPlayer.transform.position) ||
                    (Portalmaker.canPortalFromAnywhere && PlayerControl.LocalPlayer == Portalmaker.portalmaker)) &&
                   !Portal.isTeleporting;
        };
        OnMeetingEnds = () => { usePortalButton.Timer = usePortalButton.MaxTimer; };
    }

    public override float Cooldown => Portalmaker.usePortalCooldown;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        ButtonText = new ButtonText(StringNames.UseLabel,
            FastDestroyableSingleton<RoleManager>.Instance.GetRole(RoleTypes.Engineer).Ability.FontMaterial);

        if (actionButton == null)
        {
            return;
        }

        // "1. <room>" caption that sits on top of the portal button (old createButtonsPostfix).
        portalmakerButtonText1 =
            GameObject.Instantiate(actionButton.cooldownTimerText, actionButton.cooldownTimerText.transform.parent);
        portalmakerButtonText1.text = "";
        portalmakerButtonText1.enableWordWrapping = false;
        portalmakerButtonText1.transform.localScale = Vector3.one * 0.5f;
        portalmakerButtonText1.transform.localPosition += new Vector3(-0.05f, 0.55f, -1f);
    }
}

/// <summary>
/// The Portalmaker's "teleport to the second portal" button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>portalmakerMoveToPortalButton</c>.
/// </summary>
public sealed class PortalmakerMoveToPortalButton : TorButton
{
    private static PortalmakerMoveToPortalButton portalmakerMoveToPortalButton;
    public static TMP_Text portalmakerButtonText2;

    public PortalmakerMoveToPortalButton()
    {
        portalmakerMoveToPortalButton = this;

        SetSprite(TorAssets.UsePortalButton);
        PositionOffset = new Vector3(0.9f, 1f, 0);
        Hotkey = KeyCode.G;
        Mirror = true;
        ShowButtonText = true;

        RealOnClick = () =>
        {
            var didTeleport = false;
            var exit = Portal.secondPortal.portalGameObject.transform.position;

            if (!PlayerControl.LocalPlayer.Data.IsDead)
            {
                // Ghosts can portal too, but non-blocking and only with a local animation
                PlayerControl.LocalPlayer.RpcUsePortal(PlayerControl.LocalPlayer.PlayerId, 2);
            }

            RPCProcedure.usePortal(PlayerControl.LocalPlayer.PlayerId, 2);
            CustomButtonSingleton<UsePortalButton>.Instance.Timer =
                CustomButtonSingleton<UsePortalButton>.Instance.MaxTimer;
            portalmakerMoveToPortalButton.Timer = CustomButtonSingleton<UsePortalButton>.Instance.MaxTimer;
            SoundEffectsManager.play("portalUse");
            FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(Portal.teleportDuration,
                new Action<float>(p =>
                {
                    // Delayed action
                    PlayerControl.LocalPlayer.moveable = false;
                    PlayerControl.LocalPlayer.NetTransform.Halt();
                    if (p >= 0.5f && p <= 0.53f && !didTeleport && !MeetingHud.Instance)
                    {
                        if (SubmergedCompatibility.IsSubmerged) SubmergedCompatibility.ChangeFloor(exit.y > -7);
                        PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(exit);
                        didTeleport = true;
                    }

                    if (p == 1f) PlayerControl.LocalPlayer.moveable = true;
                })));
        };
        HasButton = () =>
        {
            return Portalmaker.canPortalFromAnywhere && Portal.bothPlacedAndEnabled &&
                   PlayerControl.LocalPlayer == Portalmaker.portalmaker;
        };
        CouldUse = () =>
        {
            return PlayerControl.LocalPlayer.CanMove &&
                   !Portal.locationNearEntry(PlayerControl.LocalPlayer.transform.position) && !Portal.isTeleporting;
        };
        OnMeetingEnds = () =>
        {
            portalmakerMoveToPortalButton.Timer = CustomButtonSingleton<UsePortalButton>.Instance.MaxTimer;
        };
    }

    public override float Cooldown => Portalmaker.usePortalCooldown;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        ButtonText = new ButtonText(StringNames.UseLabel,
            FastDestroyableSingleton<RoleManager>.Instance.GetRole(RoleTypes.Engineer).Ability.FontMaterial);

        if (actionButton == null)
        {
            return;
        }

        // "2. <room>" caption (old createButtonsPostfix); Portal.cs keeps its text in sync.
        portalmakerButtonText2 =
            GameObject.Instantiate(actionButton.cooldownTimerText, actionButton.cooldownTimerText.transform.parent);
        portalmakerButtonText2.text = "";
        portalmakerButtonText2.enableWordWrapping = false;
        portalmakerButtonText2.transform.localScale = Vector3.one * 0.5f;
        portalmakerButtonText2.transform.localPosition += new Vector3(-0.05f, 0.55f, -1f);
    }
}

public static class PortalmakerRpcs
{
    [MethodRpc((uint)TorRpc.PlacePortal, LocalHandling = RpcLocalHandling.After)]
    public static void RpcPlacePortal(this PlayerControl player, byte[] buff)
    {
        Vector3 position = Vector2.zero;
        position.x = BitConverter.ToSingle(buff, 0 * sizeof(float));
        position.y = BitConverter.ToSingle(buff, 1 * sizeof(float));
        new Portal(position);
    }

    [MethodRpc((uint)TorRpc.UsePortal, LocalHandling = RpcLocalHandling.None)]
    public static void RpcUsePortal(this PlayerControl player, byte playerId, byte exit)
    {
        Portal.startTeleport(playerId, exit);
    }
}
