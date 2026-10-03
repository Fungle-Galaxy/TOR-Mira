using System;
using UnityEngine;

namespace TheOtherRoles.Buttons;

public sealed class ZoomOutButton : TorButton
{
    public ZoomOutButton()
    {
        // The old constructor took no sprite at all - this button is invisible on purpose.
        SetSprite(TorLazySprite.Wrap(null));
        PositionOffset = new Vector3(0.4f, 2.8f, 0);
        Hotkey = KeyCode.KeypadPlus;

        RealOnClick = () => { Helpers.toggleZoom(); };
        HasButton = () =>
        {
            if (PlayerControl.LocalPlayer == null || !PlayerControl.LocalPlayer.Data.IsDead ||
                (PlayerControl.LocalPlayer.Data.Role.IsImpostor &&
                 !OptionGroupSingleton<GameSettingsOptions>.Instance.DeadImpsBlockSabotage.Value)) return false;
            var (playerCompleted, playerTotal) = TasksHandler.taskInfo(PlayerControl.LocalPlayer.Data);
            var numberOfLeftTasks = playerTotal - playerCompleted;
            return numberOfLeftTasks <= 0 || !OptionGroupSingleton<GameSettingsOptions>.Instance.FinishTasksBeforeHauntingOrZoomingOut.Value;
        };
        CouldUse = () => { return true; };
        OnMeetingEnds = () => { };
    }

    public override float Cooldown => 0f;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        Timer = 0f;
    }
}
