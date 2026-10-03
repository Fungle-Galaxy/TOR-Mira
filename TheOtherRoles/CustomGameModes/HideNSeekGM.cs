using System;
using System.Collections.Generic;
using MiraAPI.GameModes;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheOtherRoles.Buttons;
using TheOtherRoles.Objects;
using TheOtherRoles.Utilities;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheOtherRoles.CustomGameModes;

public static class HideNSeek
{
    // HideNSeek Gamemode
    /// <summary>
    /// Live view of "is a Hide N Seek mode the selected one", read straight from Mira API's
    /// <see cref="CustomGameModeManager.ActiveMode"/>. Mira API's own <c>HideAndSeekMode</c> counts
    /// too - it was mapped onto TOR's HideNSeek flag by the old settings bridge as well.
    /// </summary>
    public static bool isHideNSeekGM => CustomGameModeManager.ActiveMode is TorHideNSeekMode or HideAndSeekMode;
    public static TMP_Text timerText;
    public static Vent polusVent;
    public static bool isWaitingTimer = true;
    public static DateTime startTime = DateTime.UtcNow;

    public static float timer = 300f;
    public static float hunterVision = 0.5f;
    public static float huntedVision = 2f;
    public static bool taskWinPossible;
    public static float taskPunish = 10f;
    public static int impNumber = 2;
    public static bool canSabotage;
    public static float killCooldown = 10f;
    public static float hunterWaitingTime = 15f;

    public static bool isHunter()
    {
        return isHideNSeekGM && PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data.Role.IsImpostor;
    }

    public static List<PlayerControl> getHunters()
    {
        var hunters = new List<PlayerControl>(PlayerControl.AllPlayerControls.ToArray());
        hunters.RemoveAll(x => !x.Data.Role.IsImpostor);
        return hunters;
    }

    public static bool isHunted()
    {
        return isHideNSeekGM && PlayerControl.LocalPlayer != null && !PlayerControl.LocalPlayer.Data.Role.IsImpostor;
    }

    public static void ClearAndReload()
    {
        if (timerText != null) Object.Destroy(timerText);
        timerText = null;
        if (polusVent != null) Object.Destroy(polusVent);
        polusVent = null;
        isWaitingTimer = true;
        startTime = DateTime.UtcNow;

        timer = OptionGroupSingleton<HideNSeekOptions>.Instance.Timer.Value * 60;
        hunterVision = OptionGroupSingleton<HideNSeekOptions>.Instance.HunterVision.Value;
        huntedVision = OptionGroupSingleton<HideNSeekOptions>.Instance.HuntedVision.Value;
        taskWinPossible = OptionGroupSingleton<HideNSeekOptions>.Instance.TaskWin.Value;
        taskPunish = OptionGroupSingleton<HideNSeekOptions>.Instance.TaskPunish.Value;
        impNumber = Mathf.RoundToInt(OptionGroupSingleton<HideNSeekOptions>.Instance.HunterCount.Value);
        canSabotage = OptionGroupSingleton<HideNSeekOptions>.Instance.CanSabotage.Value;
        killCooldown = OptionGroupSingleton<HideNSeekOptions>.Instance.KillCooldown.Value;
        hunterWaitingTime = OptionGroupSingleton<HideNSeekOptions>.Instance.HunterWaiting.Value;

        Hunter.clearAndReload();
        Hunted.clearAndReload();
    }
}

public static class Hunter
{
    public static List<Arrow> localArrows = new();
    public static List<byte> lightActive = new();
    public static bool arrowActive;
    public static Dictionary<byte, int> playerKillCountMap = new();

    public static float lightCooldown = 30f;
    public static float lightDuration = 5f;
    public static float lightVision = 2f;
    public static float lightPunish = 5f;
    public static float AdminCooldown = 30f;
    public static float AdminDuration = 5f;
    public static float AdminPunish = 5f;
    public static float ArrowCooldown = 30f;
    public static float ArrowDuration = 5f;
    public static float ArrowPunish = 5f;

    public static bool isLightActive(byte playerId)
    {
        return lightActive.Contains(playerId);
    }
    
    public static void clearAndReload()
    {
        if (localArrows != null)
            foreach (var arrow in localArrows)
                if (arrow?.arrow != null)
                    Object.Destroy(arrow.arrow);
        localArrows = new List<Arrow>();
        lightActive = new List<byte>();
        arrowActive = false;

        lightCooldown = OptionGroupSingleton<HunterSettingsOptions>.Instance.LightCooldown.Value;
        lightDuration = OptionGroupSingleton<HunterSettingsOptions>.Instance.LightDuration.Value;
        lightVision = OptionGroupSingleton<HunterSettingsOptions>.Instance.LightVision.Value;
        lightPunish = OptionGroupSingleton<HunterSettingsOptions>.Instance.LightPunish.Value;
        AdminCooldown = OptionGroupSingleton<HunterSettingsOptions>.Instance.AdminCooldown.Value;
        AdminDuration = OptionGroupSingleton<HunterSettingsOptions>.Instance.AdminDuration.Value;
        AdminPunish = OptionGroupSingleton<HunterSettingsOptions>.Instance.AdminPunish.Value;
        ArrowCooldown = OptionGroupSingleton<HunterSettingsOptions>.Instance.ArrowCooldown.Value;
        ArrowDuration = OptionGroupSingleton<HunterSettingsOptions>.Instance.ArrowDuration.Value;
        ArrowPunish = OptionGroupSingleton<HunterSettingsOptions>.Instance.ArrowPunish.Value;
    }
}

public static class Hunted
{
    public static List<byte> timeshieldActive = new();
    public static int shieldCount = 3;

    public static float shieldCooldown = 30f;
    public static float shieldDuration = 5f;
    public static float shieldRewindTime = 3f;
    public static bool taskPunish;

    public static void clearAndReload()
    {
        timeshieldActive = new List<byte>();
        taskPunish = false;

        shieldCount = Mathf.RoundToInt(OptionGroupSingleton<HuntedSettingsOptions>.Instance.ShieldNumber.Value);
        shieldCooldown = OptionGroupSingleton<HuntedSettingsOptions>.Instance.ShieldCooldown.Value;
        shieldDuration = OptionGroupSingleton<HuntedSettingsOptions>.Instance.ShieldDuration.Value;
        shieldRewindTime = OptionGroupSingleton<HuntedSettingsOptions>.Instance.ShieldRewindTime.Value;
    }
}

/// <summary>
/// The Hunter's light button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>hunterLighterButton</c>.
/// </summary>
public sealed class HunterLighterButton : TorButton
{
    private static HunterLighterButton hunterLighterButton;

    public HunterLighterButton()
    {
        hunterLighterButton = this;

        SetSprite(TorAssets.LighterButton);
        PositionOffset = TorButtonPositions.UpperRowFarLeft;
        Hotkey = KeyCode.F;
        EffectEnabled = true;
        ShowButtonText = true;

        RealOnClick = () =>
        {
            Hunter.lightActive.Add(PlayerControl.LocalPlayer.PlayerId);
            SoundEffectsManager.play("lighterLight");

            PlayerControl.LocalPlayer.RpcShareTimer(Hunter.lightPunish);
        };
        HasButton = () => { return HideNSeek.isHunter() && !PlayerControl.LocalPlayer.Data.IsDead; };
        CouldUse = () => { return true; };
        OnMeetingEnds = () =>
        {
            hunterLighterButton.Timer = 30f;
            hunterLighterButton.isEffectActive = false;
            hunterLighterButton.actionButton.graphic.color = Palette.EnabledColor;
        };
    }

    public override float Cooldown => Hunter.lightCooldown;

    public override float EffectDuration => Hunter.lightDuration;

    public override void OnEffectEnd()
    {
        Hunter.lightActive.Remove(PlayerControl.LocalPlayer.PlayerId);
        hunterLighterButton.Timer = hunterLighterButton.MaxTimer;
        SoundEffectsManager.play("lighterLight");
    }

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        ButtonText = new ButtonText(38,
            FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.UseButton]
                .FontMaterial);
    }
}

/// <summary>
/// The Hunter's admin table button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>hunterAdminTableButton</c>.
/// </summary>
public sealed class HunterAdminTableButton : TorButton
{
    private static HunterAdminTableButton hunterAdminTableButton;

    public HunterAdminTableButton()
    {
        hunterAdminTableButton = this;

        PositionOffset = TorButtonPositions.LowerRowCenter;
        Hotkey = KeyCode.G;
        EffectEnabled = true;
        EffectLength = Hunter.AdminDuration;
        ShowButtonText = true;
        ButtonText = new ButtonText(StringNames.Admin);

        RealOnClick = () =>
        {
            if (!MapBehaviour.Instance || !MapBehaviour.Instance.isActiveAndEnabled)
            {
                HudManager.Instance.InitMap();
                MapBehaviour.Instance.ShowCountOverlay(true, true, false);
            }

            PlayerControl.LocalPlayer.NetTransform.Halt(); // Stop current movement 

            PlayerControl.LocalPlayer.RpcShareTimer(Hunter.AdminPunish);
        };
        HasButton = () => { return HideNSeek.isHunter() && !PlayerControl.LocalPlayer.Data.IsDead; };
        CouldUse = () => { return true; };
        OnMeetingEnds = () =>
        {
            hunterAdminTableButton.Timer = hunterAdminTableButton.MaxTimer;
            hunterAdminTableButton.isEffectActive = false;
            hunterAdminTableButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
        };
    }

    public override float Cooldown => Hunter.AdminCooldown;

    public override void OnEffectEnd()
    {
        hunterAdminTableButton.Timer = hunterAdminTableButton.MaxTimer;
        if (MapBehaviour.Instance && MapBehaviour.Instance.isActiveAndEnabled) MapBehaviour.Instance.Close();
    }

    public override void CreateButton(Transform parent)
    {
        // The admin sprite only exists once the HUD is up.
        SetSprite(Hacker.getAdminSprite());
        base.CreateButton(parent);
    }
}

/// <summary>
/// The Hunter's arrow button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>hunterArrowButton</c>.
/// </summary>
public sealed class HunterArrowButton : TorButton
{
    private static HunterArrowButton hunterArrowButton;

    public HunterArrowButton()
    {
        hunterArrowButton = this;

        SetSprite(TorAssets.HideNSeekArrowButton);
        PositionOffset = TorButtonPositions.UpperRowLeft;
        Hotkey = KeyCode.R;
        EffectEnabled = true;
        ShowButtonText = true;

        RealOnClick = () =>
        {
            Hunter.arrowActive = true;
            SoundEffectsManager.play("trackerTrackPlayer");

            PlayerControl.LocalPlayer.RpcShareTimer(Hunter.ArrowPunish);
        };
        HasButton = () => { return HideNSeek.isHunter() && !PlayerControl.LocalPlayer.Data.IsDead; };
        CouldUse = () => { return true; };
        OnMeetingEnds = () =>
        {
            hunterArrowButton.Timer = 30f;
            hunterArrowButton.isEffectActive = false;
            hunterArrowButton.actionButton.graphic.color = Palette.EnabledColor;
        };
    }

    public override float Cooldown => Hunter.ArrowCooldown;

    public override float EffectDuration => Hunter.ArrowDuration;

    public override void OnEffectEnd()
    {
        Hunter.arrowActive = false;
        hunterArrowButton.Timer = hunterArrowButton.MaxTimer;
        SoundEffectsManager.play("trackerTrackPlayer");
    }

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        ButtonText = new ButtonText(39,
            FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.UseButton]
                .FontMaterial);
    }
}

/// <summary>
/// The Hunted's time shield button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>huntedShieldButton</c>.
/// </summary>
public sealed class HuntedShieldButton : TorButton
{
    private static HuntedShieldButton huntedShieldButton;
    
    public HuntedShieldButton()
    {
        huntedShieldButton = this;

        SetSprite(TorAssets.TimeShieldButton);
        PositionOffset = TorButtonPositions.LowerRowRight;
        Hotkey = KeyCode.F;
        EffectEnabled = true;
        ShowButtonText = true;

        RealOnClick = () =>
        {
            PlayerControl.LocalPlayer.RpcHuntedShield(PlayerControl.LocalPlayer.PlayerId);
            SoundEffectsManager.play("timemasterShield");

            Hunted.shieldCount--;
        };
        HasButton = () => { return HideNSeek.isHunted() && !PlayerControl.LocalPlayer.Data.IsDead; };
        CouldUse = () =>
        {
            SetUses(Hunted.shieldCount);
            return PlayerControl.LocalPlayer.CanMove && Hunted.shieldCount > 0;
        };
        OnMeetingEnds = () =>
        {
            huntedShieldButton.Timer = huntedShieldButton.MaxTimer;
            huntedShieldButton.isEffectActive = false;
            huntedShieldButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
        };
    }

    public override float Cooldown => Hunted.shieldCooldown;

    public override float EffectDuration => Hunted.shieldDuration;

    public override void OnEffectEnd()
    {
        huntedShieldButton.Timer = huntedShieldButton.MaxTimer;
        SoundEffectsManager.stop("timemasterShield");
    }

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        ButtonText = new ButtonText(5,
            FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.UseButton]
                .FontMaterial);
    }
}

public static class HideNSeekRpcs
{
    [MethodRpc((uint)TorRpc.ShareTimer, LocalHandling = RpcLocalHandling.After)]
    public static void RpcShareTimer(this PlayerControl player, float punish)
    {
        HideNSeek.timer -= punish;
    }

    [MethodRpc((uint)TorRpc.HuntedShield, LocalHandling = RpcLocalHandling.After)]
    public static void RpcHuntedShield(this PlayerControl player, byte playerId)
    {
        if (!Hunted.timeshieldActive.Contains(playerId)) Hunted.timeshieldActive.Add(playerId);
        FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(Hunted.shieldDuration,
            new Action<float>(p =>
            {
                if (p == 1f) Hunted.timeshieldActive.Remove(playerId);
            })));
    }

    [MethodRpc((uint)TorRpc.HuntedRewindTime, LocalHandling = RpcLocalHandling.After)]
    public static void RpcHuntedRewindTime(this PlayerControl player, byte playerId)
    {
        Hunted.timeshieldActive.Remove(playerId); // Shield is no longer active when rewinding
        SoundEffectsManager.stop("timemasterShield"); // Shield sound stopped when rewinding
        if (playerId == PlayerControl.LocalPlayer.PlayerId) HudManagerStartPatch.resetHuntedRewindButton();
        // The hunter rewinds until the position history is empty, as there is no time limit
        TimeMaster.rewindEndTime = 0f;
        FastDestroyableSingleton<HudManager>.Instance.FullScreen.color = new Color(0f, 0.5f, 0.8f, 0.3f);
        FastDestroyableSingleton<HudManager>.Instance.FullScreen.enabled = true;
        FastDestroyableSingleton<HudManager>.Instance.FullScreen.gameObject.SetActive(true);
        FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(Hunted.shieldRewindTime,
            new Action<float>(p =>
            {
                if (p == 1f) FastDestroyableSingleton<HudManager>.Instance.FullScreen.enabled = false;
            })));

        if (!PlayerControl.LocalPlayer.Data.Role.IsImpostor) return; // only rewind hunter

        TimeMaster.isRewinding = true;

        if (MapBehaviour.Instance)
            MapBehaviour.Instance.Close();
        if (Minigame.Instance)
            Minigame.Instance.ForceClose();
        PlayerControl.LocalPlayer.moveable = false;
    }
}