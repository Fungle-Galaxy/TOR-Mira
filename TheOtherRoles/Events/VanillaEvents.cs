using System;
using System.Linq;
using AmongUs.GameOptions;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Map;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events.Vanilla.Usables;
using TheOtherRoles.Networking;
using TheOtherRoles.Utilities;
using UnityEngine;
using Random = System.Random;

namespace TheOtherRoles.Events;

/// <summary>
/// Vanilla facing behaviour that used to be a Harmony prefix / postfix and now runs through Mira
/// API's <see cref="MiraEventManager"/> instead. Every handler below is invoked from a patch on the
/// very method the old TOR patch targeted, and Mira API turns "cancelled" into the same
/// <c>return false</c> the old prefix produced, so the observable behaviour is unchanged - only the
/// way TOR hooks into it.
/// </summary>
public static class VanillaEvents
{
    /// <summary>
    /// Was part of <c>ShowSabotageMapPatch</c>; only the rule that is independent of the game mode
    /// lives here. <see cref="PlayerOpenSabotageEvent"/> is fired from a prefix on
    /// <see cref="MapBehaviour.ShowSabotageMap"/> which returns <c>!IsCancelled</c> - the exact
    /// allow / deny contract the old prefix expressed through its own return value.
    /// The mode specific half (hunters need <c>HideNSeek.canSabotage</c>, props never get it) moved
    /// to <see cref="CustomGameModes.TorHideNSeekMode.ShouldShowSabotageMap"/> /
    /// <see cref="CustomGameModes.TorPropHuntMode.ShouldShowSabotageMap"/>, which Mira API invokes
    /// from its own prefix on the same method.
    /// </summary>
    [RegisterEvent]
    public static void ShowSabotageMap(PlayerOpenSabotageEvent @event)
    {
        if (PlayerControl.LocalPlayer.Data.IsDead &&
            OptionGroupSingleton<GameSettingsOptions>.Instance.DeadImpsBlockSabotage.Value)
        {
            @event.MapBehaviour.ShowNormalMap();
            @event.Cancel();
        }
    }

    /// <summary>
    /// Was <c>ConsoleCanUsePatch</c>. Mira API fires <see cref="PlayerCanUseEvent"/> from a prefix
    /// on <c>Console.CanUse</c> - and on every console type that overrides it - after zeroing
    /// <c>canUse</c> / <c>couldUse</c>, so cancelling here reproduces what returning <c>false</c>
    /// from the old prefix did. The event carries neither the <c>NetworkedPlayerInfo</c> argument
    /// nor the distance out parameter; the console is always queried for the local player, and the
    /// distance is only consumed while <c>canUse</c> is true.
    /// </summary>
    [RegisterEvent]
    public static void ConsoleCanUse(PlayerCanUseEvent @event)
    {
        var console = @event.Usable.TryCast<Console>();
        var localPlayer = PlayerControl.LocalPlayer;
        if (console == null || localPlayer == null) return;

        if (Swapper.swapper != null && Swapper.swapper == localPlayer)
        {
            if (console.TaskTypes.Any(x => x == TaskTypes.FixLights || x == TaskTypes.FixComms))
                @event.Cancel();
            return;
        }

        if (console.AllowImpostor) return;
        if (!localPlayer.hasFakeTasks()) return;

        @event.Cancel();
    }

    /// <summary>
    /// Was <c>VentUsePatch</c>. Mira API raises <see cref="PlayerUseEvent"/> from its prefix on
    /// <c>Vent.Use</c>, and cancelling here is exactly what the old prefix expressed with
    /// <c>return false</c> - TOR replaced the vanilla method outright, so only Hide'n'Seek lets it run.
    /// </summary>
    [RegisterEvent]
    public static void VentUse(PlayerUseEvent @event)
    {
        if (!@event.IsVent) return;
        if (GameOptionsManager.Instance.currentGameOptions.GameMode == GameModes.HideNSeek) return;

        var vent = @event.Usable.TryCast<Vent>();
        if (vent == null) return;

        if (Deputy.handcuffedPlayers.Contains(PlayerControl.LocalPlayer.PlayerId))
        {
            Deputy.setHandcuffedKnows();
            @event.Cancel();
            return;
        }

        if (Trapper.playersOnMap.Contains(PlayerControl.LocalPlayer.PlayerId))
        {
            @event.Cancel();
            return;
        }

        bool canUse;
        bool couldUse;
        vent.CanUse(PlayerControl.LocalPlayer.Data, out canUse, out couldUse);
        var canMoveInVents = PlayerControl.LocalPlayer != Spy.spy &&
                             !Trapper.playersOnMap.Contains(PlayerControl.LocalPlayer.PlayerId);
        if (!canUse)
        {
            @event.Cancel();
            return;
        }

        var isEnter = !PlayerControl.LocalPlayer.inVent;

        if (vent.name.StartsWith("JackInTheBoxVent_"))
        {
            vent.SetButtons(isEnter && canMoveInVents);
            PlayerControl.LocalPlayer.RpcUseUncheckedVent(vent.Id, PlayerControl.LocalPlayer.PlayerId,
                isEnter ? byte.MaxValue : (byte)0);
            SoundEffectsManager.play("tricksterUseBoxVent");
            @event.Cancel();
            return;
        }

        if (isEnter)
            PlayerControl.LocalPlayer.MyPhysics.RpcEnterVent(vent.Id);
        else
            PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(vent.Id);
        vent.SetButtons(isEnter && canMoveInVents);
        @event.Cancel();
    }

    /// <summary>
    /// Was <c>ShowHost.Setup</c>: replaces the proceed button with the host badge in online games.
    /// <see cref="StartMeetingEvent"/> is fired from a postfix on <see cref="MeetingHud.Start"/>,
    /// i.e. exactly where the old postfix ran.
    /// </summary>
    [RegisterEvent]
    public static void ShowMeetingHostIcon(StartMeetingEvent @event)
    {
        if (AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame) return;

        var __instance = @event.MeetingHud;
        __instance.ProceedButton.gameObject.transform.localPosition = new Vector3(-2.5f, 2.2f, 0);
        __instance.ProceedButton.gameObject.GetComponent<SpriteRenderer>().enabled = false;
        __instance.ProceedButton.GetComponent<PassiveButton>().enabled = false;
        __instance.HostIcon.gameObject.SetActive(true);
        __instance.ProceedButton.gameObject.SetActive(true);
    }

    private static int seed;

    /// <summary>
    /// Mira API hands out roles through <see cref="RoleManager.SetRole"/>, which raises
    /// <see cref="SetRoleEvent"/>. TOR keeps its per role state in statics, so this is what turns
    /// Mira API's assignment into an actual TOR role; without it the RoleTypes are set but every
    /// TOR static stays null.
    /// </summary>
    [RegisterEvent]
    public static void ApplyTorRole(SetRoleEvent @event)
    {
        RPCProcedure.setTorRole(@event.Role, @event.Player);
    }

    /// <summary>
    /// Was <c>SetUpRoleTextPatch</c>. TOR used to prefix the compiler generated state machine of
    /// <see cref="IntroCutscene.ShowRole"/> by name, which differs between platforms
    /// (<c>_ShowRole_d__40</c> vs <c>_ShowRole_d__41</c>); Mira API resolves that state machine by
    /// method name and raises <see cref="IntroRoleRevealEvent"/> once, at the first step, so the
    /// platform ifdefs go away with it.
    /// </summary>
    [RegisterEvent]
    public static void SetUpIntroRoleText(IntroRoleRevealEvent @event)
    {
        seed = Helpers.rnd.Next(5000);
        var introCutscene = @event.IntroCutscene;
        FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(1f,
            new Action<float>(_ => SetRoleTexts(introCutscene))));
    }

    private static void SetRoleTexts(IntroCutscene __instance)
    {
        // Don't override the intro of the vanilla roles
        var infos = CustomRoleManager.getRoleInfoForPlayer(PlayerControl.LocalPlayer);
        var roleInfo = infos.Where(info => !info.isModifier).FirstOrDefault();
        var modifierInfo = infos.Where(info => info.isModifier).FirstOrDefault();

        if (EventUtility.isEnabled)
        {
            var roleInfos = CustomRoleManager.Instance.allRoleInfos.Where(x => !x.isModifier).ToList();
            if (roleInfo.isNeutral) roleInfos.RemoveAll(x => !x.isNeutral);
            if (roleInfo.color == Palette.ImpostorRed) roleInfos.RemoveAll(x => x.color != Palette.ImpostorRed);
            if (!roleInfo.isNeutral && roleInfo.color != Palette.ImpostorRed)
                roleInfos.RemoveAll(x => x.color == Palette.ImpostorRed || x.isNeutral);
            var localRandom = new Random(seed);
            roleInfo = roleInfos[localRandom.Next(roleInfos.Count)];
        }

        __instance.RoleBlurbText.text = "";
        if (roleInfo != null)
        {
            __instance.RoleText.text = roleInfo.name;
            __instance.RoleText.color = roleInfo.color;
            __instance.RoleBlurbText.text = roleInfo.introDescription;
            __instance.RoleBlurbText.color = roleInfo.color;
        }

        if (modifierInfo != null)
        {
            if (modifierInfo.roleId != RoleId.Lover)
            {
                __instance.RoleBlurbText.text +=
                    Helpers.cs(modifierInfo.color, $"\n{modifierInfo.introDescription}");
            }
            else
            {
                var otherLover = PlayerControl.LocalPlayer == Lovers.lover1 ? Lovers.lover2 : Lovers.lover1;
                __instance.RoleBlurbText.text += Helpers.cs(Lovers.color,
                    string.Format("\n" + ModTranslation.GetString("Intro-Text", 2), otherLover?.Data?.PlayerName ?? ""));
            }
        }

        if (Deputy.knowsSheriff && Deputy.deputy != null && Sheriff.sheriff != null)
        {
            if (infos.Any(info => info.roleId == RoleId.Sheriff))
                __instance.RoleBlurbText.text += Helpers.cs(Sheriff.color,
                    $"\n" + string.Format(ModTranslation.GetString("Intro-Text", 3), Deputy.deputy?.Data?.PlayerName ?? ""));
            else if (infos.Any(info => info.roleId == RoleId.Deputy))
                __instance.RoleBlurbText.text += Helpers.cs(Sheriff.color,
                    $"\n" + string.Format(ModTranslation.GetString("Intro-Text", 4), Sheriff.sheriff?.Data?.PlayerName ?? ""));
        }
    }
}
