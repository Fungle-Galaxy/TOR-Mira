using System.Collections.Generic;
using UnityEngine;

namespace TheOtherRoles;

internal static class TORMapOptions
{
    // Set values
    public static int maxNumberOfMeetings = 10;
    public static bool blockSkippingInEmergencyMeetings;
    public static bool noVoteIsSelfVote;
    public static bool hidePlayerNames;
    public static bool ghostsSeeRoles = true;
    public static bool ghostsSeeModifier = true;
    public static bool ghostsSeeInformation = true;
    public static bool ghostsSeeVotes = true;
    public static bool showRoleSummary = true;
    public static bool allowParallelMedBayScans;
    public static bool showLighterDarker = true;
    public static bool enableSoundEffects = true;
    public static bool shieldFirstKill;
    public static bool ShowVentsOnMap = true;
    public static bool ShowChatNotifications = true;

    // Updating values
    public static int meetingsCount;
    public static List<SurvCamera> camerasToAdd = new();
    public static List<Vent> ventsToSeal = new();
    public static Dictionary<byte, PoolablePlayer> playerIcons = new();
    public static string firstKillName;
    public static PlayerControl firstKillPlayer;

    public static void clearAndReloadMapOptions()
    {
        meetingsCount = 0;
        camerasToAdd = new List<SurvCamera>();
        ventsToSeal = new List<Vent>();
        playerIcons = new Dictionary<byte, PoolablePlayer>();
        ;

        maxNumberOfMeetings = Mathf.RoundToInt(OptionGroupSingleton<GameSettingsOptions>.Instance.MaxNumberOfMeetings.Selection());
        blockSkippingInEmergencyMeetings = OptionGroupSingleton<GameSettingsOptions>.Instance.BlockSkippingInEmergencyMeetings.Value;
        noVoteIsSelfVote = OptionGroupSingleton<GameSettingsOptions>.Instance.NoVoteIsSelfVote.Value;
        hidePlayerNames = OptionGroupSingleton<GameSettingsOptions>.Instance.HidePlayerNames.Value;
        allowParallelMedBayScans = OptionGroupSingleton<GameSettingsOptions>.Instance.AllowParallelMedBayScans.Value;
        shieldFirstKill = OptionGroupSingleton<GameSettingsOptions>.Instance.ShieldFirstKill.Value;
        firstKillPlayer = null;
    }

    public static void reloadPluginOptions()
    {
        ghostsSeeRoles = TheOtherRolesPlugin.GhostsSeeRoles.Value;
        ghostsSeeModifier = TheOtherRolesPlugin.GhostsSeeModifier.Value;
        ghostsSeeInformation = TheOtherRolesPlugin.GhostsSeeInformation.Value;
        ghostsSeeVotes = TheOtherRolesPlugin.GhostsSeeVotes.Value;
        showRoleSummary = TheOtherRolesPlugin.ShowRoleSummary.Value;
        showLighterDarker = TheOtherRolesPlugin.ShowLighterDarker.Value;
        enableSoundEffects = TheOtherRolesPlugin.EnableSoundEffects.Value;
        ShowVentsOnMap = TheOtherRolesPlugin.ShowVentsOnMap.Value;
        ShowChatNotifications = TheOtherRolesPlugin.ShowChatNotifications.Value;

        //Patches.ShouldAlwaysHorseAround.isHorseMode = TheOtherRolesPlugin.EnableHorseMode.Value;
    }
}