using System;
using MiraAPI.GameOptions.OptionTypes;
using System.Linq;
using MiraAPI.Utilities.Assets;
using PowerTools;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheOtherRoles.Utilities;
using UnityEngine;
using MiraAPI.Utilities;
using TheOtherRoles.Buttons;
using Object = UnityEngine.Object;

namespace TheOtherRoles.Roles.Crewmate;

public class SecurityGuard(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = new Color32(195, 178, 95, byte.MaxValue);

    public static RoleInfo Info = new(color, RoleId.SecurityGuard);

    public static PlayerControl securityGuard;

    public static float cooldown = 30f;
    public static int remainingScrews = 7;
    public static int totalScrews = 7;
    public static int ventPrice = 1;
    public static int camPrice = 2;
    public static int placedCameras;
    public static float duration = 10f;
    public static int maxCharges = 5;
    public static int rechargeTasksNumber = 3;
    public static int rechargedTasks = 3;
    public static int charges = 1;
    public static bool cantMove = true;
    public static Vent ventTarget;
    public static Minigame minigame;

    private static Sprite animatedVentSealedSprite;
    private static float lastPPU;
    private static Sprite camSprite;
    private static Sprite logSprite;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.SecurityGuard;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<SecurityGuardOptions>.Instance.SpawnRate;
    
    public static Sprite getAnimatedVentSealedSprite()
    {
        var ppu = 185f;
        if (SubmergedCompatibility.IsSubmerged) ppu = 120f;
        if (lastPPU != ppu)
        {
            animatedVentSealedSprite = null;
            lastPPU = ppu;
        }

        if (animatedVentSealedSprite) return animatedVentSealedSprite;
        animatedVentSealedSprite =
            new LoadableResourceAsset("TheOtherRoles.Resources.AnimatedVentSealed.png", ppu).LoadAsset();
        return animatedVentSealedSprite;
    }

    public static Sprite getCamSprite()
    {
        if (camSprite) return camSprite;
        camSprite = FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.CamsButton]
            .Image;
        return camSprite;
    }

    public static Sprite getLogSprite()
    {
        if (logSprite) return logSprite;
        logSprite = FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.DoorLogsButton]
            .Image;
        return logSprite;
    }

    public static void clearAndReload()
    {
        securityGuard = null;
        ventTarget = null;
        minigame = null;
        duration = OptionGroupSingleton<SecurityGuardOptions>.Instance.CamDuration.Value;
        maxCharges = Mathf.RoundToInt(OptionGroupSingleton<SecurityGuardOptions>.Instance.CamMaxCharges.Value);
        rechargeTasksNumber = Mathf.RoundToInt(OptionGroupSingleton<SecurityGuardOptions>.Instance.CamRechargeTasksNumber.Value);
        rechargedTasks = Mathf.RoundToInt(OptionGroupSingleton<SecurityGuardOptions>.Instance.CamRechargeTasksNumber.Value);
        charges = Mathf.RoundToInt(OptionGroupSingleton<SecurityGuardOptions>.Instance.CamMaxCharges.Value) / 2;
        placedCameras = 0;
        cooldown = OptionGroupSingleton<SecurityGuardOptions>.Instance.Cooldown.Value;
        totalScrews = remainingScrews = Mathf.RoundToInt(OptionGroupSingleton<SecurityGuardOptions>.Instance.TotalScrews.Value);
        camPrice = Mathf.RoundToInt(OptionGroupSingleton<SecurityGuardOptions>.Instance.CamPrice.Value);
        ventPrice = Mathf.RoundToInt(OptionGroupSingleton<SecurityGuardOptions>.Instance.VentPrice.Value);
        cantMove = OptionGroupSingleton<SecurityGuardOptions>.Instance.NoMove.Value;
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        // securityGuardSetTarget
        if (SecurityGuard.securityGuard == null || SecurityGuard.securityGuard != player ||
            MapUtilities.CachedShipStatus == null || MapUtilities.CachedShipStatus.AllVents == null) return;
        Vent target = null;
        var truePosition = player.GetTruePosition();
        var closestDistance = float.MaxValue;
        for (var i = 0; i < MapUtilities.CachedShipStatus.AllVents.Length; i++)
        {
            var vent = MapUtilities.CachedShipStatus.AllVents[i];
            if (vent.gameObject.name.StartsWith("JackInTheBoxVent_") ||
                vent.gameObject.name.StartsWith("SealedVent_") ||
                vent.gameObject.name.StartsWith("FutureSealedVent_")) continue;
            if (SubmergedCompatibility.IsSubmerged && vent.Id == 9) continue;
            var distance = Vector2.Distance(vent.transform.position, truePosition);
            if (distance <= vent.UsableDistance && distance < closestDistance)
            {
                closestDistance = distance;
                target = vent;
            }
        }
        SecurityGuard.ventTarget = target;

        // securityGuardUpdate
        if (SecurityGuard.securityGuard.Data.IsDead) return;
        var taskInfo = TasksHandler.taskInfo(SecurityGuard.securityGuard.Data);
        int playerCompleted = taskInfo.Item1;
        if (playerCompleted == SecurityGuard.rechargedTasks)
        {
            SecurityGuard.rechargedTasks += SecurityGuard.rechargeTasksNumber;
            if (SecurityGuard.maxCharges > SecurityGuard.charges) SecurityGuard.charges++;
        }
    }
}

/// <summary>
/// The settings of SecurityGuardOptions. They live next to the role on purpose: the group is bound
/// to SecurityGuard, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class SecurityGuardOptions : TorRoleOptionGroup<SecurityGuard>
{
    public override uint GroupPriority => 540;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.SecurityGuard));
    public override Color GroupColor => TorOptionColors.Group(SecurityGuard.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.SecurityGuard), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedNumberOption Cooldown { get; } =
        new ModdedNumberOption("Opt-SecurityGuard,1", 30f, 10f, 60f, 2.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption TotalScrews { get; } =
        new ModdedNumberOption("Opt-SecurityGuard,2", 7f, 1f, 15f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption CamPrice { get; } =
        new ModdedNumberOption("Opt-SecurityGuard,3", 2f, 1f, 15f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption VentPrice { get; } =
        new ModdedNumberOption("Opt-SecurityGuard,4", 1f, 1f, 15f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption CamDuration { get; } =
        new ModdedNumberOption("Opt-SecurityGuard,5", 10f, 2.5f, 60f, 2.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption CamMaxCharges { get; } =
        new ModdedNumberOption("Opt-SecurityGuard,6", 5f, 1f, 30f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption CamRechargeTasksNumber { get; } =
        new ModdedNumberOption("Opt-SecurityGuard,7", 3f, 1f, 10f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedToggleOption NoMove { get; } =
        new ModdedToggleOption("Opt-SecurityGuard,8", true);
}

/// <summary>
/// The Security Guard's "place camera / seal vent" button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>securityGuardButton</c>.
/// </summary>
public sealed class SecurityGuardButton : TorButton
{
    private static SecurityGuardButton securityGuardButton;
    
    public SecurityGuardButton()
    {
        securityGuardButton = this;

        SetSprite(TorAssets.PlaceCameraButton);
        PositionOffset = TorButtonPositions.LowerRowRight;
        Hotkey = KeyCode.F;
        ShowButtonText = true;

        RealOnClick = () =>
        {
            if (SecurityGuard.ventTarget != null)
            {
                // Seal vent
                PlayerControl.LocalPlayer.RpcSealVent(SecurityGuard.ventTarget.Id);
                SecurityGuard.ventTarget = null;
            }
            else if (!Helpers.isMira() && !Helpers.isFungle() && !SubmergedCompatibility.IsSubmerged)
            {
                // Place camera if there's no vent and it's not MiraHQ or Submerged
                var pos = PlayerControl.LocalPlayer.transform.position;
                var buff = new byte[sizeof(float) * 2];
                Buffer.BlockCopy(BitConverter.GetBytes(pos.x), 0, buff, 0 * sizeof(float), sizeof(float));
                Buffer.BlockCopy(BitConverter.GetBytes(pos.y), 0, buff, 1 * sizeof(float), sizeof(float));

                PlayerControl.LocalPlayer.RpcPlaceCamera(buff);
            }

            SoundEffectsManager.play("securityGuardPlaceCam"); // Same sound used for both types (cam or vent)!
            securityGuardButton.Timer = securityGuardButton.MaxTimer;
        };        HasButton = () =>
        {
            return SecurityGuard.securityGuard != null &&
                   SecurityGuard.securityGuard == PlayerControl.LocalPlayer &&
                   !PlayerControl.LocalPlayer.Data.IsDead && SecurityGuard.remainingScrews >=
                   Mathf.Min(SecurityGuard.ventPrice, SecurityGuard.camPrice);
        };
        CouldUse = () =>
        {
            securityGuardButton.actionButton.graphic.sprite =
                SecurityGuard.ventTarget == null && !Helpers.isMira() && !Helpers.isFungle() &&
                !SubmergedCompatibility.IsSubmerged
                    ? TorAssets.PlaceCameraButton.LoadAsset()
                    : TorAssets.CloseVentButton.LoadAsset();
            securityGuardButton.ButtonText =
                SecurityGuard.ventTarget == null && !Helpers.isMira() && !Helpers.isFungle() &&
                !SubmergedCompatibility.IsSubmerged
                    ? new ButtonText(StringNames.SecurityCamsSystem, FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.UseButton].FontMaterial)
                    : new ButtonText(21, FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.UseButton].FontMaterial);
            SetUsesText($"{SecurityGuard.remainingScrews}/{SecurityGuard.totalScrews}");

            if (SecurityGuard.ventTarget != null)
                return SecurityGuard.remainingScrews >= SecurityGuard.ventPrice &&
                       PlayerControl.LocalPlayer.CanMove;
            return !Helpers.isMira() && !Helpers.isFungle() && !SubmergedCompatibility.IsSubmerged &&
                   SecurityGuard.remainingScrews >= SecurityGuard.camPrice && PlayerControl.LocalPlayer.CanMove;
        };
        OnMeetingEnds = () => { securityGuardButton.Timer = securityGuardButton.MaxTimer; };
    }

    public override float Cooldown => SecurityGuard.cooldown;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        ButtonText = new ButtonText(StringNames.SecurityCamsSystem,
            FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.UseButton].FontMaterial);
    }
}

/// <summary>
/// The Security Guard's camera/log viewer button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>securityGuardCamButton</c>.
/// </summary>
public sealed class SecurityGuardCamButton : TorButton
{
    private static SecurityGuardCamButton securityGuardCamButton;

    public SecurityGuardCamButton()
    {
        securityGuardCamButton = this;

        PositionOffset = TorButtonPositions.LowerRowRight;
        Hotkey = KeyCode.G;
        EffectEnabled = true;
        ShowButtonText = true;

        RealOnClick = () =>
        {
            if (!Helpers.isMira())
            {
                if (SecurityGuard.minigame == null)
                {
                    var mapId = GameOptionsManager.Instance.currentNormalGameOptions.MapId;
                    var e = Object.FindObjectsOfType<SystemConsole>().FirstOrDefault(x =>
                        x.gameObject.name.Contains("Surv_Panel") || x.name.Contains("Cam") ||
                        x.name.Contains("BinocularsSecurityConsole"));
                    if (Helpers.isSkeld() || mapId == 3)
                        e = Object.FindObjectsOfType<SystemConsole>()
                            .FirstOrDefault(x => x.gameObject.name.Contains("SurvConsole"));
                    else if (Helpers.isAirship())
                        e = Object.FindObjectsOfType<SystemConsole>()
                            .FirstOrDefault(x => x.gameObject.name.Contains("task_cams"));
                    if (e == null || Camera.main == null) return;
                    SecurityGuard.minigame = Object.Instantiate(e.MinigamePrefab, Camera.main.transform, false);
                }

                SecurityGuard.minigame.transform.SetParent(Camera.main.transform, false);
                SecurityGuard.minigame.transform.localPosition = new Vector3(0.0f, 0.0f, -50f);
                SecurityGuard.minigame.Begin(null);
            }
            else
            {
                if (SecurityGuard.minigame == null)
                {
                    var e = Object.FindObjectsOfType<SystemConsole>()
                        .FirstOrDefault(x => x.gameObject.name.Contains("SurvLogConsole"));
                    if (e == null || Camera.main == null) return;
                    SecurityGuard.minigame = Object.Instantiate(e.MinigamePrefab, Camera.main.transform, false);
                }

                SecurityGuard.minigame.transform.SetParent(Camera.main.transform, false);
                SecurityGuard.minigame.transform.localPosition = new Vector3(0.0f, 0.0f, -50f);
                SecurityGuard.minigame.Begin(null);
            }

            SecurityGuard.charges--;

            if (SecurityGuard.cantMove) PlayerControl.LocalPlayer.moveable = false;
            PlayerControl.LocalPlayer.NetTransform.Halt(); // Stop current movement
        };
        HasButton = () =>
        {
            return SecurityGuard.securityGuard != null &&
                   SecurityGuard.securityGuard == PlayerControl.LocalPlayer &&
                   !PlayerControl.LocalPlayer.Data.IsDead && SecurityGuard.remainingScrews <
                   Mathf.Min(SecurityGuard.ventPrice, SecurityGuard.camPrice)
                   && !SubmergedCompatibility.IsSubmerged;
        };
        CouldUse = () =>
        {
            SetUsesText($"{SecurityGuard.charges} / {SecurityGuard.maxCharges}");
            securityGuardCamButton.actionButton.graphic.sprite =
                Helpers.isMira() ? SecurityGuard.getLogSprite() : SecurityGuard.getCamSprite();
            securityGuardCamButton.ButtonText = (Helpers.isMira() ? new ButtonText(StringNames.DoorlogLabel, FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.UseButton].FontMaterial)
            : new ButtonText(StringNames.SecurityCamsSystem, FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.UseButton].FontMaterial));
            return PlayerControl.LocalPlayer.CanMove && SecurityGuard.charges > 0;
        };
        OnMeetingEnds = () =>
        {
            securityGuardCamButton.Timer = securityGuardCamButton.MaxTimer;
            securityGuardCamButton.isEffectActive = false;
            securityGuardCamButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
        };
    }

    public override float Cooldown => SecurityGuard.cooldown;

    public override float EffectDuration => SecurityGuard.duration;

    public override void OnEffectEnd()
    {
        securityGuardCamButton.Timer = securityGuardCamButton.MaxTimer;
        if (Minigame.Instance) SecurityGuard.minigame.ForceClose();
        PlayerControl.LocalPlayer.moveable = true;
    }

    public override void CreateButton(Transform parent)
    {
        // The cam sprite only exists once the HUD is up.
        SetSprite(SecurityGuard.getCamSprite());
        base.CreateButton(parent);
        // The label depends on the map we are playing on.
        ButtonText = Helpers.isMira()
            ? new ButtonText(StringNames.DoorlogLabel, FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.UseButton].FontMaterial)
            : new ButtonText(StringNames.SecurityCamsSystem, FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.UseButton].FontMaterial);
    }
}

public static class SecurityGuardRpcs
{
    [MethodRpc((uint)TorRpc.SealVent, LocalHandling = RpcLocalHandling.After)]
    public static void RpcSealVent(this PlayerControl player, int ventId)
    {
        var vent = MapUtilities.CachedShipStatus.AllVents.FirstOrDefault(x => x != null && x.Id == ventId);
        if (vent == null) return;

        SecurityGuard.remainingScrews -= SecurityGuard.ventPrice;
        if (PlayerControl.LocalPlayer == SecurityGuard.securityGuard)
        {
            var animator = vent.GetComponent<SpriteAnim>();

            vent.EnterVentAnim = vent.ExitVentAnim = null;
            var newSprite = animator == null
                ? TorAssets.StaticVentSealed.LoadAsset()
                : SecurityGuard.getAnimatedVentSealedSprite();
            var rend = vent.myRend;
            if (Helpers.isFungle())
            {
                newSprite = TorAssets.FungleVentSealed.LoadAsset();
                rend = vent.transform.GetChild(3).GetComponent<SpriteRenderer>();
                animator = vent.transform.GetChild(3).GetComponent<SpriteAnim>();
            }

            animator?.Stop();
            rend.sprite = newSprite;
            if (SubmergedCompatibility.IsSubmerged && vent.Id == 0)
                vent.myRend.sprite = TorAssets.CentralUpperBlocked.LoadAsset();
            if (SubmergedCompatibility.IsSubmerged && vent.Id == 14)
                vent.myRend.sprite = TorAssets.CentralLowerBlocked.LoadAsset();
            rend.color = new Color(1f, 1f, 1f, 0.5f);
            vent.name = "FutureSealedVent_" + vent.name;
        }

        TORMapOptions.ventsToSeal.Add(vent);
    }

    [MethodRpc((uint)TorRpc.PlaceCamera, LocalHandling = RpcLocalHandling.After)]
    public static void RpcPlaceCamera(this PlayerControl player, byte[] buff)
    {
        var referenceCamera = Object.FindObjectOfType<SurvCamera>();
        if (referenceCamera == null) return; // Mira HQ

        SecurityGuard.remainingScrews -= SecurityGuard.camPrice;
        SecurityGuard.placedCameras++;

        var position = Vector3.zero;
        position.x = BitConverter.ToSingle(buff, 0 * sizeof(float));
        position.y = BitConverter.ToSingle(buff, 1 * sizeof(float));

        var camera = Object.Instantiate(referenceCamera);
        camera.transform.position = new Vector3(position.x, position.y, referenceCamera.transform.position.z - 1f);
        camera.CamName = string.Format(ModTranslation.GetString("Game-Normal", 4), SecurityGuard.placedCameras);
        camera.Offset = new Vector3(0f, 0f, camera.Offset.z);
        if (GameOptionsManager.Instance.currentNormalGameOptions.MapId == 2 ||
            GameOptionsManager.Instance.currentNormalGameOptions.MapId == 4)
            camera.transform.localRotation = new Quaternion(0, 0, 1, 1); // Polus and Airship 

        if (SubmergedCompatibility.IsSubmerged)
        {
            // remove 2d box collider of console, so that no barrier can be created. (irrelevant for now, but who knows... maybe we need it later)
            var fixConsole = camera.transform.FindChild("FixConsole");
            if (fixConsole != null)
            {
                var boxCollider = fixConsole.GetComponent<BoxCollider2D>();
                if (boxCollider != null) Object.Destroy(boxCollider);
            }
        }


        if (PlayerControl.LocalPlayer == SecurityGuard.securityGuard)
        {
            camera.gameObject.SetActive(true);
            camera.gameObject.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.5f);
        }
        else
        {
            camera.gameObject.SetActive(false);
        }

        TORMapOptions.camerasToAdd.Add(camera);
    }
}
