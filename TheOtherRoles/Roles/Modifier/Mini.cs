using System;
using TheOtherRoles.Utilities;
using UnityEngine;

namespace TheOtherRoles.Roles.Modifier;

public class Mini : TorGameModifier
{
    public const float defaultColliderRadius = 0.2233912f;
    public const float defaultColliderOffset = 0.3636057f;

    public static Color color = Color.yellow;

    public static RoleInfo Info = new(color, RoleId.Mini, isModifier: true);

    public static PlayerControl mini;

    public static float growingUpDuration = 400f;
    public static bool isGrowingUpInMeeting = true;
    public static DateTime timeOfGrowthStart = DateTime.UtcNow;
    public static DateTime timeOfMeetingStart = DateTime.UtcNow;
    public static float ageOnMeetingStart;
    public static bool triggerMiniLose;

    public override RoleInfo TorInfo => Info;

    public override int GetAssignmentChance()
    {
        var option = OptionGroupSingleton<MiniOptions>.Instance;
        var chance = option.Mini.Selection() * TorOptions.RateStep;
        if (EventUtility.isEnabled)
            chance = option.Mini.Selection() == 0 && option.EventReallyNoMini.Value ? 0 : 10;
        return chance;
    }

    public override int GetAmountPerGame() => 1;

    public override bool IsModifierValidOn(RoleBehaviour role) => role is not Spy;

    public override void OnActivate() => mini = Player;

    public override void OnDeactivate()
    {
        if (mini == Player) mini = null;
    }

    public override void ClearAndReload() => clearAndReload();

    public static void clearAndReload()
    {
        mini = null;
        triggerMiniLose = false;
        growingUpDuration = OptionGroupSingleton<MiniOptions>.Instance.MiniGrowingUpDuration.Value;
        isGrowingUpInMeeting = OptionGroupSingleton<MiniOptions>.Instance.MiniGrowingUpInMeeting.Value;
        timeOfGrowthStart = DateTime.UtcNow;
    }

    public static float growingProgress()
    {
        var timeSinceStart = (float)(DateTime.UtcNow - timeOfGrowthStart).TotalMilliseconds;
        return Mathf.Clamp(timeSinceStart / (growingUpDuration * 1000), 0f, 1f);
    }

    public static bool isGrownUp()
    {
        return growingProgress() == 1f;
    }
}
