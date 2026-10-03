using TheOtherRoles.Utilities;
using UnityEngine;

namespace TheOtherRoles.Buttons;

/// <summary>
/// April event kick button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>eventKickButton</c>.
/// </summary>
public sealed class EventKickButton : TorButton
{
    private static EventKickButton eventKickButton;

    public EventKickButton()
    {
        eventKickButton = this;

        SetSprite(EventUtility.getKickButtonSprite());
        PositionOffset = TorButtonPositions.HighRowRight;
        Hotkey = KeyCode.K;
        EffectEnabled = true;
        ShowButtonText = true;

        RealOnClick = () => { EventUtility.kickTarget(); };
        HasButton = () =>
        {
            return EventUtility.isEnabled && Mini.mini != null && !Mini.mini.Data.IsDead &&
                   PlayerControl.LocalPlayer != Mini.mini;
        };
        CouldUse = () => { return EventUtility.currentTarget != null; };
        OnMeetingEnds = () => { };
    }

    public override float Cooldown => 0f;

    public override float EffectDuration => 3f;

    public override void OnEffectEnd()
    {
        // onEffectEnds
        eventKickButton.Timer = 69;
    }

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        ButtonText = new ButtonText(46,
            FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.UseButton]
                .FontMaterial);
    }
}
