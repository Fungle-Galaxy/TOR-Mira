using MiraAPI.Hud;
using TheOtherRoles.CustomGameModes;
using TheOtherRoles.Patches;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheOtherRoles;

/// <summary>
///     Shared HUD helpers that used to sit next to the big <c>createButtonsPostfix</c> factory.
///     Every button is now a <see cref="Buttons.TorButton" /> subclass that MiraAPI instantiates at
///     plugin load and ticks through <c>CustomButtonManager</c>, so this type no longer patches
///     <c>HudManager.Start</c> and no longer declares any button fields.
/// </summary>
internal static class HudManagerStartPatch
{
    public static PoolablePlayer targetDisplay;
    public static GameObject propSpriteHolder;
    public static SpriteRenderer propSpriteRenderer;

    public static void resetTimeMasterButton()
    {
        var button = CustomButtonSingleton<TimeMasterShieldButton>.Instance;
        button.Timer = button.MaxTimer;
        button.isEffectActive = false;
        if (button.actionButton != null) button.actionButton.cooldownTimerText.color = Palette.EnabledColor;
        SoundEffectsManager.stop("timemasterShield");
    }

    public static void resetHuntedRewindButton()
    {
        var button = CustomButtonSingleton<HuntedShieldButton>.Instance;
        button.Timer = button.MaxTimer;
        button.isEffectActive = false;
        if (button.actionButton != null) button.actionButton.cooldownTimerText.color = Palette.EnabledColor;
        SoundEffectsManager.stop("timemasterShield");
    }

    /// <summary>
    ///     Marks (or unmarks) every TOR button as handcuffed. <see cref="Buttons.TorButton.DrawHandcuffed" />
    ///     then swaps in the cuff sprite, shows the remaining cuff time and swallows the click, so the old
    ///     runtime-created "replacement" buttons and <c>deputyHandcuffedButtons</c> bookkeeping are gone.
    ///     Vanilla kill / vent / report buttons are still hidden by <c>UpdatePatch</c>.
    /// </summary>
    public static void setAllButtonsHandcuffedStatus(bool handcuffed, bool reset = false)
    {
        var status = reset ? false : handcuffed;
        foreach (var button in Buttons.TorButtons.All) button.IsHandcuffed = status;
    }

    public static void setButtonTargetDisplay(PlayerControl target, Buttons.TorButton button = null, Vector3? offset = null)
    {
        if (target == null || button == null)
        {
            if (targetDisplay != null)
            {
                // Reset the poolable player
                targetDisplay.gameObject.SetActive(false);
                GameObject.Destroy(targetDisplay.gameObject);
                targetDisplay = null;
            }

            return;
        }

        // Add poolable player to the button so that the target outfit is shown
        button.actionButton.cooldownTimerText.transform.localPosition =
            new Vector3(0, 0, -1f); // Before the poolable player
        targetDisplay = Object.Instantiate(IntroEndEvents.playerPrefab, button.actionButton.transform);
        var data = target.Data;
        target.SetPlayerMaterialColors(targetDisplay.cosmetics.currentBodySprite.BodySprite);
        targetDisplay.SetSkin(data.DefaultOutfit.SkinId, data.DefaultOutfit.ColorId);
        targetDisplay.SetHat(data.DefaultOutfit.HatId, data.DefaultOutfit.ColorId);
        targetDisplay.cosmetics.nameText.text = ""; // Hide the name!
        targetDisplay.transform.localPosition = new Vector3(0f, 0.22f, -0.01f);
        if (offset != null) targetDisplay.transform.localPosition += (Vector3)offset;
        targetDisplay.transform.localScale = Vector3.one * 0.33f;
        targetDisplay.setSemiTransparent(false);
        targetDisplay.gameObject.SetActive(true);
    }
}
