using System;
using System.Linq;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Hud;
using TheOtherRoles.Buttons;
using TheOtherRoles.Utilities;
using UnityEngine;
using MiraAPI.Utilities;
using Object = UnityEngine.Object;

namespace TheOtherRoles.Roles.Crewmate;

public class Hacker(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = new Color32(117, 250, 76, byte.MaxValue);

    public static RoleInfo Info = new(color, RoleId.Hacker);

    public static PlayerControl hacker;
    public static Minigame vitals;
    public static Minigame doorLog;

    public static float cooldown = 30f;
    public static float duration = 10f;
    public static float toolsNumber = 5f;
    public static bool onlyColorType;
    public static float hackerTimer;
    public static int rechargeTasksNumber = 2;
    public static int rechargedTasks = 2;
    public static int chargesVitals = 1;
    public static int chargesAdminTable = 1;
    public static bool cantMove = true;

    private static Sprite vitalsSprite;
    private static Sprite logSprite;
    private static Sprite adminSprite;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Hacker;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<HackerOptions>.Instance.SpawnRate;

    public static Sprite getVitalsSprite()
    {
        if (vitalsSprite) return vitalsSprite;
        vitalsSprite = FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.VitalsButton]
            .Image;
        return vitalsSprite;
    }

    public static Sprite getLogSprite()
    {
        if (logSprite) return logSprite;
        logSprite = FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.DoorLogsButton]
            .Image;
        return logSprite;
    }

    public static Sprite getAdminSprite()
    {
        var mapId = GameOptionsManager.Instance.currentNormalGameOptions.MapId;
        var button =
            FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.PolusAdminButton];
        if (Helpers.isSkeld() || mapId == 3)
            button = FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.AdminMapButton];
        else if (Helpers.isMira())
            button = FastDestroyableSingleton<HudManager>.Instance.UseButton
                .fastUseSettings[ImageNames.MIRAAdminButton];
        else if (Helpers.isAirship())
            button = FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[
                ImageNames.AirshipAdminButton];
        else if (Helpers.isFungle())
            button = FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.AdminMapButton];
        adminSprite = button.Image;
        return adminSprite;
    }

    public static void clearAndReload()
    {
        hacker = null;
        vitals = null;
        doorLog = null;
        hackerTimer = 0f;
        adminSprite = null;
        cooldown = OptionGroupSingleton<HackerOptions>.Instance.Cooldown.Value;
        duration = OptionGroupSingleton<HackerOptions>.Instance.HackeringDuration.Value;
        onlyColorType = OptionGroupSingleton<HackerOptions>.Instance.OnlyColorType.Value;
        toolsNumber = OptionGroupSingleton<HackerOptions>.Instance.ToolsNumber.Value;
        rechargeTasksNumber = Mathf.RoundToInt(OptionGroupSingleton<HackerOptions>.Instance.RechargeTasksNumber.Value);
        rechargedTasks = Mathf.RoundToInt(OptionGroupSingleton<HackerOptions>.Instance.RechargeTasksNumber.Value);
        chargesVitals = Mathf.RoundToInt(OptionGroupSingleton<HackerOptions>.Instance.ToolsNumber.Value) / 2;
        chargesAdminTable = Mathf.RoundToInt(OptionGroupSingleton<HackerOptions>.Instance.ToolsNumber.Value) / 2;
        cantMove = OptionGroupSingleton<HackerOptions>.Instance.NoMove.Value;
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        Hacker.hackerTimer -= Time.deltaTime;
        if (Hacker.hacker == null || player != Hacker.hacker || Hacker.hacker.Data.IsDead) return;
        var taskInfo = TasksHandler.taskInfo(Hacker.hacker.Data);
        int playerCompleted = taskInfo.Item1;
        if (playerCompleted == Hacker.rechargedTasks)
        {
            Hacker.rechargedTasks += Hacker.rechargeTasksNumber;
            if (Hacker.toolsNumber > Hacker.chargesVitals) Hacker.chargesVitals++;
            if (Hacker.toolsNumber > Hacker.chargesAdminTable) Hacker.chargesAdminTable++;
        }
    }
}

/// <summary>
/// The settings of HackerOptions. They live next to the role on purpose: the group is bound
/// to Hacker, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class HackerOptions : TorRoleOptionGroup<Hacker>
{
    public override uint GroupPriority => 490;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Hacker));
    public override Color GroupColor => TorOptionColors.Group(Hacker.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.Hacker), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedNumberOption Cooldown { get; } =
        new ModdedNumberOption("Opt-Hacker,1", 30f, 5f, 60f, 5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption HackeringDuration { get; } =
        new ModdedNumberOption("Opt-Hacker,2", 10f, 2.5f, 60f, 2.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedToggleOption OnlyColorType { get; } =
        new ModdedToggleOption("Opt-Hacker,3", false);

    public ModdedNumberOption ToolsNumber { get; } =
        new ModdedNumberOption("Opt-Hacker,4", 5f, 1f, 30f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption RechargeTasksNumber { get; } =
        new ModdedNumberOption("Opt-Swapper,4", 2f, 1f, 5f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedToggleOption NoMove { get; } =
        new ModdedToggleOption("Opt-Hacker,6", true);
}

/// <summary>
/// The Hacker's hack button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>hackerButton</c>.
/// </summary>
public sealed class HackerButton : TorButton
{
    private static HackerButton hackerButton;

    public HackerButton()
    {
        hackerButton = this;

        SetSprite(TorAssets.HackerButton);
        PositionOffset = TorButtonPositions.UpperRowRight;
        Hotkey = KeyCode.F;
        EffectEnabled = true;
        ShowButtonText = true;

        RealOnClick = () =>
        {
            Hacker.hackerTimer = Hacker.duration;
            SoundEffectsManager.play("hackerHack");
        };
        HasButton = () =>
        {
            return Hacker.hacker != null && Hacker.hacker == PlayerControl.LocalPlayer &&
                   !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () => { return true; };
        OnMeetingEnds = () =>
        {
            hackerButton.Timer = hackerButton.MaxTimer;
            hackerButton.isEffectActive = false;
            hackerButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
        };
    }

    public override float Cooldown => Hacker.cooldown;

    public override float EffectDuration => Hacker.duration;

    public override void OnEffectEnd()
    {
        hackerButton.Timer = hackerButton.MaxTimer;
    }

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        ButtonText = new ButtonText(10,
            FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.UseButton]
                .FontMaterial);
    }
}

/// <summary>
/// The Hacker's admin table tool button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>hackerAdminTableButton</c>.
/// </summary>
public sealed class HackerAdminTableButton : TorButton
{
    private static HackerAdminTableButton hackerAdminTableButton;

    public HackerAdminTableButton()
    {
        hackerAdminTableButton = this;

        PositionOffset = TorButtonPositions.LowerRowRight;
        Hotkey = KeyCode.G;
        EffectEnabled = true;
        ShowButtonText = true;
        ButtonText = new ButtonText(StringNames.Admin);

        RealOnClick = () =>
        {
            if (!MapBehaviour.Instance || !MapBehaviour.Instance.isActiveAndEnabled)
            {
                HudManager.Instance.InitMap();
                MapBehaviour.Instance.ShowCountOverlay(true, true, true);
            }

            if (Hacker.cantMove) PlayerControl.LocalPlayer.moveable = false;
            PlayerControl.LocalPlayer.NetTransform.Halt(); // Stop current movement 
            Hacker.chargesAdminTable--;
        };
        HasButton = () =>
        {
            return Hacker.hacker != null && Hacker.hacker == PlayerControl.LocalPlayer &&
                   !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () =>
        {
            SetUsesText($"{Hacker.chargesAdminTable} / {Hacker.toolsNumber}");
            return Hacker.chargesAdminTable > 0;
        };
        OnMeetingEnds = () =>
        {
            hackerAdminTableButton.Timer = hackerAdminTableButton.MaxTimer;
            hackerAdminTableButton.isEffectActive = false;
            hackerAdminTableButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
        };
    }

    public override float Cooldown => Hacker.cooldown;

    public override float EffectDuration => Hacker.duration;

    public override void OnEffectEnd()
    {
        hackerAdminTableButton.Timer = hackerAdminTableButton.MaxTimer;
        if (!CustomButtonSingleton<HackerVitalsButton>.Instance.isEffectActive)
            PlayerControl.LocalPlayer.moveable = true;
        if (MapBehaviour.Instance && MapBehaviour.Instance.isActiveAndEnabled) MapBehaviour.Instance.Close();
    }

    public override void CreateButton(Transform parent)
    {
        // The admin sprite only exists once the HUD is up.
        SetSprite(Hacker.getAdminSprite());
        base.CreateButton(parent);
        Mirror = GameOptionsManager.Instance.currentNormalGameOptions.MapId == 3;
    }
}

/// <summary>
/// The Hacker's vitals/doorlog tool button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>hackerVitalsButton</c>.
/// </summary>
public sealed class HackerVitalsButton : TorButton
{
    private static HackerVitalsButton hackerVitalsButton;
    
    public HackerVitalsButton()
    {
        hackerVitalsButton = this;

        PositionOffset = TorButtonPositions.LowerRowCenter;
        Hotkey = KeyCode.H;
        EffectEnabled = true;
        ShowButtonText = true;

        RealOnClick = () =>
        {
            if (GameOptionsManager.Instance.currentNormalGameOptions.MapId != 1)
            {
                if (Hacker.vitals == null)
                {
                    var e = Object.FindObjectsOfType<SystemConsole>().FirstOrDefault(x =>
                        x.gameObject.name.Contains("panel_vitals") || x.gameObject.name.Contains("Vitals"));
                    if (e == null || Camera.main == null) return;
                    Hacker.vitals = Object.Instantiate(e.MinigamePrefab, Camera.main.transform, false);
                }

                Hacker.vitals.transform.SetParent(Camera.main.transform, false);
                Hacker.vitals.transform.localPosition = new Vector3(0.0f, 0.0f, -50f);
                Hacker.vitals.Begin(null);
            }
            else
            {
                if (Hacker.doorLog == null)
                {
                    var e = Object.FindObjectsOfType<SystemConsole>()
                        .FirstOrDefault(x => x.gameObject.name.Contains("SurvLogConsole"));
                    if (e == null || Camera.main == null) return;
                    Hacker.doorLog = Object.Instantiate(e.MinigamePrefab, Camera.main.transform, false);
                }

                Hacker.doorLog.transform.SetParent(Camera.main.transform, false);
                Hacker.doorLog.transform.localPosition = new Vector3(0.0f, 0.0f, -50f);
                Hacker.doorLog.Begin(null);
            }

            if (Hacker.cantMove) PlayerControl.LocalPlayer.moveable = false;
            PlayerControl.LocalPlayer.NetTransform.Halt(); // Stop current movement 

            Hacker.chargesVitals--;
        };
        HasButton = () =>
        {
            return Hacker.hacker != null && Hacker.hacker == PlayerControl.LocalPlayer &&
                   !PlayerControl.LocalPlayer.Data.IsDead &&
                   GameOptionsManager.Instance.currentGameOptions.MapId != 0 &&
                   GameOptionsManager.Instance.currentNormalGameOptions.MapId != 3;
        };
        CouldUse = () =>
        {
            SetUsesText($"{Hacker.chargesVitals} / {Hacker.toolsNumber}");
            hackerVitalsButton.actionButton.graphic.sprite =
                Helpers.isMira() ? Hacker.getLogSprite() : Hacker.getVitalsSprite();
            hackerVitalsButton.actionButton.OverrideText(Helpers.isMira() ? "DOORLOG" : "VITALS");
            return Hacker.chargesVitals > 0;
        };
        OnMeetingEnds = () =>
        {
            hackerVitalsButton.Timer = hackerVitalsButton.MaxTimer;
            hackerVitalsButton.isEffectActive = false;
            hackerVitalsButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
        };
    }

    public override float Cooldown => Hacker.cooldown;

    public override float EffectDuration => Hacker.duration;

    public override void OnEffectEnd()
    {
        hackerVitalsButton.Timer = hackerVitalsButton.MaxTimer;
        if (!CustomButtonSingleton<HackerAdminTableButton>.Instance.isEffectActive)
            PlayerControl.LocalPlayer.moveable = true;
        if (Minigame.Instance)
        {
            if (Helpers.isMira()) Hacker.doorLog.ForceClose();
            else Hacker.vitals.ForceClose();
        }
    }

    public override void CreateButton(Transform parent)
    {
        // The vitals sprite only exists once the HUD is up.
        SetSprite(Hacker.getVitalsSprite());
        base.CreateButton(parent);
        // The label depends on the map we are playing on.
        ButtonText = Helpers.isMira() ? new ButtonText(StringNames.DoorlogLabel) : new ButtonText(StringNames.VitalsLabel);
    }
}
