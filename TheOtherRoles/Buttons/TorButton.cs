using System;
using InnerNet;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.PluginLoading;
using MiraAPI.Utilities.Assets;
using TMPro;
using UnityEngine;

namespace TheOtherRoles.Buttons;

public static class TorButtonPositions
{
    public static readonly Vector3 LowerRowRight = new(-2f, -0.06f, 0);
    public static readonly Vector3 LowerRowCenter = new(-3f, -0.06f, 0);
    public static readonly Vector3 LowerRowLeft = new(-4f, -0.06f, 0);
    public static readonly Vector3 UpperRowRight = new(0f, 1f, 0f);
    public static readonly Vector3 UpperRowCenter = new(-1f, 1f, 0f);
    public static readonly Vector3 UpperRowLeft = new(-2f, 1f, 0f);
    public static readonly Vector3 UpperRowFarLeft = new(-3f, 1f, 0f);
    public static readonly Vector3 HighRowRight = new(0f, 2.06f, 0f);
}

[MiraIgnore]
public abstract class TorButton : CustomActionButton
{
    private static readonly int Desat = Shader.PropertyToID("_Desat");

    /// <summary>Drawn over a button while the local player is handcuffed (old replacement buttons).</summary>
    private static readonly LoadableAsset<Sprite> HandcuffSprite = TorAssets.DeputyHandcuffed;
    private LoadableAsset<Sprite> _sprite;
    private float _effectDuration;
    private bool _hasEffect;

    /// <summary>Mira needs a non-empty name for the GameObject; the visible text comes from <see cref="ButtonText"/>.</summary>
    public override string Name => GetType().Name;

    /// <summary>Sprite the button wears. Set it in the constructor or override it.</summary>
    public override LoadableAsset<Sprite> Sprite => _sprite!;

    /// <summary>
    /// Mira lays every custom button out with its own GridArrange, so the old
    /// <c>UseButton.localPosition + PositionOffset</c> arithmetic in <see cref="UpdateTor"/> had to go -
    /// it fought the grid and put the buttons on top of the vanilla ones.
    /// </summary>
    public override ButtonLocation Location => ButtonLocation.BottomRight;

    /// <summary>
    /// Drives the key badge Mira draws above the button and the Rewired press that clicks it.
    /// <see cref="TorButton.Hotkey"/> only survives for keys with no <c>Keybind</c>.
    /// </summary>
    public override BaseKeybind Keybind => KeybindFor(Hotkey);

    private static BaseKeybind KeybindFor(KeyCode? key)
    {
        try
        {
            return key switch
            {
                KeyCode.Q => VanillaKeybinding<KillButton>.Instance,
                KeyCode.F => VanillaKeybinding<AbilityButton>.Instance,
                KeyCode.G => TorKeybinds.Secondary,
                KeyCode.J => TorKeybinds.Tertiary,
                KeyCode.H => TorKeybinds.Quaternary,
                KeyCode.R => TorKeybinds.Quinary,
                KeyCode.I => TorKeybinds.Senary,
                KeyCode.K => TorKeybinds.Septenary,
                KeyCode.LeftShift => TorKeybinds.Crouch,
                KeyCode.KeypadPlus => TorKeybinds.Zoom,
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Cooldown in seconds - the old <c>MaxTimer</c>. Every concrete button overrides this with
    /// its role's option, so the base implementation is never the one that gets read.
    /// </summary>
    public override float Cooldown => 0f;

    /// <summary>
    /// Old <c>MaxTimer</c>. Reads as <see cref="Cooldown"/> plus whatever
    /// <see cref="CooldownOffset"/> has piled on top, and writes back by solving for that offset -
    /// which is what keeps <c>button.MaxTimer += 10</c> (Eraser) and
    /// <c>button.MaxTimer = Witch.cooldown + Witch.currentCooldownAddition</c> (Witch) meaningful now
    /// that <see cref="Cooldown"/> is a live per-role override instead of a plain field.
    /// </summary>
    public float MaxTimer
    {
        get => Cooldown + CooldownOffset;
        set => CooldownOffset = value - Cooldown;
    }

    /// <summary>
    /// Seconds added on top of <see cref="Cooldown"/> by code that used to mutate the old
    /// <c>MaxTimer</c> field directly. Cleared on every <c>clearAndReload</c> so a new game starts
    /// from the role's base cooldown again.
    /// </summary>
    public float CooldownOffset { get; set; }

    /// <summary>Forgets any accumulated <see cref="CooldownOffset"/> (Eraser's per-erase penalty etc.).</summary>
    public void ResetCooldownOffset() => CooldownOffset = 0f;

    public override float EffectDuration => _effectDuration;

    public override bool HasEffect => _hasEffect;

    /// <summary>Old <c>HasEffect</c> - usually driven by the role's option, so it has to be settable.</summary>
    public bool EffectEnabled
    {
        get => _hasEffect;
        set => _hasEffect = value;
    }

    public float EffectLength
    {
        get => EffectDuration;
        set => _effectDuration = value;
    }

    public Func<bool> HasButton { get; set; } = () => false;
    public Func<bool> CouldUse { get; set; } = () => true;
    public Action OnMeetingEnds { get; set; } = () => { };
    public Vector3 PositionOffset { get; set; }
    private KeyCode? _hotkey;

    /// <summary>
    /// Bound key. The first assignment also records <see cref="OriginalHotkey"/>, which
    /// <c>ReloadHotkeys</c> needs to know which vanilla action (Q kill / F ability) this button
    /// was originally standing in for. Later assignments rebind without losing that origin.
    /// </summary>
    public KeyCode? Hotkey
    {
        get => _hotkey;
        set
        {
            if (_originalHotkey == null && value != null)
            {
                _originalHotkey = value;
            }

            _hotkey = value;
        }
    }

    private KeyCode? _originalHotkey;

    /// <summary>The key the button was created with, before <c>ReloadHotkeys</c> rebinding.</summary>
    public KeyCode? OriginalHotkey
    {
        get => _originalHotkey;
        set
        {
            _originalHotkey = value;
            if (value != null)
            {
                _hotkey = value;
            }
        }
    }

    public bool Mirror { get; set; }
    public bool ForceActive { get; set; }
    public bool ShowButtonText { get; set; }
    public ButtonText ButtonText { get; set; }
    public bool IsHandcuffed { get; set; }
    public bool IsEffectActive
    {
        get => EffectActive;
        set => EffectActive = value;
    }

    /// <summary>Handcuffs run their own timer so the underlying cooldown keeps ticking underneath.</summary>
    public float DeputyTimer { get; set; }

    /// <summary>Old <c>actionButton</c>.</summary>
    public ActionButton TorAction => Button;

    public TextMeshPro TorLabel => Button?.buttonLabelText;
    public SpriteRenderer TorRenderer => Button?.graphic;
    public Material TorMaterial => Button != null ? Button.graphic.material : null;

    /// <summary>Old <c>actionButton</c> under its original name, so external call sites stay untouched.</summary>
    public ActionButton actionButton => Button;

    /// <summary>Old <c>isEffectActive</c>.</summary>
    public bool isEffectActive
    {
        get => EffectActive;
        set => EffectActive = value;
    }

    /// <summary>Old <c>onClickEvent()</c>.</summary>
    public void onClickEvent() => ClickHandler();

    protected TorButton()
    {
        // Give subclasses a chance to fill in sprite/cooldown via their own property initialisers.
    }

    /// <summary>Convenience for the converted buttons - the old constructor's sprite argument.</summary>
    public void SetSprite(LoadableAsset<Sprite> sprite) => _sprite = sprite;

    /// <summary>Old <c>Sprite = someRawSprite</c>.</summary>
    public void SetSprite(Sprite sprite) => _sprite = TorLazySprite.Wrap(sprite);

    /// <summary>
    /// Writes a formatted label such as <c>3 / 5</c> into Mira's uses counter. Plain counts should
    /// use <see cref="CustomActionButton.SetUses"/> instead; TOU-M fills this slot the same way.
    /// </summary>
    public void SetUsesText(string text)
    {
        if (!Button || !Button.usesRemainingText)
        {
            return;
        }

        Button.usesRemainingText.enableWordWrapping = false;
        Button.usesRemainingText.text = text;
        Button.usesRemainingText.gameObject.SetActive(true);
        if (Button.usesRemainingSprite)
        {
            Button.usesRemainingSprite.gameObject.SetActive(true);
        }
    }

    // ── Mira lifecycle ───────────────────────────────────────────────────────────────────────

    public override void CreateButton(Transform parent)
    {
        if (Button)
        {
            return;
        }

        // Mira hands us BottomRight/BottomLeft itself; parenting anywhere else drops the button
        // out of the GridArrange that positions it.
        base.CreateButton(parent);
        if (!Button)
        {
            return;
        }

        if (ButtonText != null)
        {
            ShowButtonText = true;
        }

        DeputyTimer = EffectDuration;
        Timer = AmongUsClient.Instance != null &&
                AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay
            ? 0
            : InitialCooldown;
    }

    /// <summary>
    /// Mira calls this both when the HUD toggles and when <see cref="Enabled"/> turns false.
    /// The old button decided visibility itself inside <see cref="UpdateTor"/>, so only react to
    /// the "hide" direction here; showing is left to the next tick.
    /// </summary>
    public override void SetActive(bool visible, RoleBehaviour role)
    {
        if (!visible)
        {
            SetActive(false);
        }
    }

    /// <summary>
    /// Forces one <see cref="UpdateTor"/> pass. Replaces the old <c>CustomButton.Update()</c>
    /// calls that TOR made by hand (meeting end, cooldown reset, handcuff state changes).
    /// </summary>
    public void Refresh()
    {
        if (!Button)
        {
            return;
        }

        UpdateTor(PlayerControl.LocalPlayer);
    }

    /// <summary>
    /// Mira ticks every button from plugin load on, so the lobby is fair game. The old buttons only
    /// ever existed once <c>HudManager.Start</c> could read the game options, which meant "a game is
    /// running" - every hook below re-establishes that gate before touching role state.
    /// </summary>
    public static bool IsInGame =>
        AmongUsClient.Instance != null &&
        AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started &&
        PlayerControl.LocalPlayer != null &&
        PlayerControl.LocalPlayer.Data != null;

    public override bool Enabled(RoleBehaviour role) => IsInGame && HasButton();

    public override bool CanUse() => IsInGame && HasButton() && CouldUse();

    public override bool CanClick() => IsInGame && Timer < 0f && HasButton() && CouldUse();

    /// <summary>
    /// Old <c>CustomButton.onClickEvent</c>: the handcuffs swallow the effect start, everything else
    /// is the same ordering as before.
    /// </summary>
    public override void ClickHandler()
    {
        if (!CanClick())
        {
            return;
        }

        if (Button)
        {
            Button.graphic.color = new Color(1f, 1f, 1f, 0.3f);
        }

        var local = PlayerControl.LocalPlayer;
        if (local != null &&
            Deputy.handcuffedKnows.ContainsKey(local.PlayerId) &&
            Deputy.handcuffedKnows[local.PlayerId] > 0f)
        {
            // Deputy: a handcuffed player's click does not start the effect.
            OnClick();
            return;
        }

        OnClick();

        if (HasEffect && !EffectActive)
        {
            DeputyTimer = EffectDuration;
            Timer = EffectDuration;
            if (Button)
            {
                Button.cooldownTimerText.color = new Color(0f, 0.8f, 0f);
            }

            EffectActive = true;
        }
    }

    public override void FixedUpdateHandler(PlayerControl playerControl)
    {
        if (!Button)
        {
            return;
        }

        UpdateTor(playerControl);

        // Mira already dispatches the Rewired press for anything with a Keybind; only fall back to
        // raw input for the handful of hotkeys that have no keybind to draw a badge for.
        if (Keybind == null && Hotkey.HasValue && Input.GetKeyDown(Hotkey.Value))
        {
            ClickHandler();
        }
    }

    // ── the old CustomButton.Update(), verbatim ──────────────────────────────────────────────

    private void UpdateTor(PlayerControl localPlayer)
    {
        if (!IsInGame || localPlayer == null || localPlayer.Data == null)
        {
            return;
        }

        var moveable = localPlayer.moveable;

        if (MeetingHud.Instance || ExileController.Instance || !HasButton())
        {
            SetActive(false);
            return;
        }

        SetActive(ForceActive || HudManager.Instance.UseButton.isActiveAndEnabled ||
                  HudManager.Instance.PetButton.isActiveAndEnabled);

        if (DeputyTimer >= 0)
        {
            // Reordered so handcuffs do not stop the underlying timers from running.
            if (HasEffect && EffectActive)
            {
                DeputyTimer -= Time.deltaTime;
            }
            else if (!localPlayer.inVent && moveable)
            {
                DeputyTimer -= Time.deltaTime;
            }
        }

        if (DeputyTimer <= 0 && HasEffect && EffectActive)
        {
            EffectActive = false;
            Button.cooldownTimerText.color = Palette.EnabledColor;
            OnEffectEnd();
        }

        // Position is Mira's GridArrange's job now (see Location).
        if (IsHandcuffed)
        {
            DrawHandcuffed(localPlayer);
            return;
        }

        Button.graphic.sprite = Sprite.LoadAsset();
        if (ShowButtonText && ButtonText != null)
        {
            Button.OverrideText(ButtonText.GetText());
            ButtonText.ApplyMaterial(Button.buttonLabelText);
        }

        Button.buttonLabelText.enabled = ShowButtonText;

        var usable = CouldUse();
        var label = Button.buttonLabelText;
        if (usable)
        {
            Button.graphic.color = label.color = Palette.EnabledColor;
            Button.graphic.material.SetFloat(Desat, 0f);
        }
        else
        {
            Button.graphic.color = label.color = Palette.DisabledClear;
            Button.graphic.material.SetFloat(Desat, 1f);
        }

        if (Timer >= 0)
        {
            if (HasEffect && EffectActive)
            {
                Timer -= Time.deltaTime;
            }
            else if (!localPlayer.inVent && moveable)
            {
                Timer -= Time.deltaTime;
            }
        }

        if (Timer <= 0 && HasEffect && EffectActive)
        {
            EffectActive = false;
            Button.cooldownTimerText.color = Palette.EnabledColor;
            OnEffectEnd();
        }

        Button.SetCoolDown(Timer, HasEffect && EffectActive ? EffectDuration : MaxTimer);

        // Handcuffed players press their ability into the "you are handcuffed" response instead.
        if (Deputy.handcuffedPlayers.Contains(localPlayer.PlayerId))
        {
            OnClickOverride = TorHandcuffedClick;
        }
        else
        {
            OnClickOverride = null;
        }
    }

    /// <summary>
    /// The old system hid every real button and spawned a throw-away "you are handcuffed" clone in
    /// its place (they could not be registered at runtime under Mira, and the clones carried no
    /// gameplay). Same result, one button: keep the real one in its slot, swap the art, tick the
    /// handcuff clock down, and swallow the click.
    /// </summary>
    private void DrawHandcuffed(PlayerControl localPlayer)
    {
        Button.graphic.sprite = HandcuffSprite.LoadAsset();
        Button.buttonLabelText.enabled = false;
        Button.graphic.color = Palette.DisabledClear;
        Button.graphic.material.SetFloat(Desat, 1f);

        // Clicking a handcuffed button does nothing - the clone's OnClick used to be empty.
        OnClickOverride = () => { };

        var duration = Deputy.handcuffDuration;
        var remaining = Deputy.handcuffedKnows.TryGetValue(localPlayer.PlayerId, out var left) ? left : 0f;
        Button.cooldownTimerText.color = new Color(0f, 0.8f, 0f);
        Button.SetCoolDown(Mathf.Max(0f, remaining), Mathf.Max(0.01f, duration));
    }

    private static void TorHandcuffedClick() => Deputy.setHandcuffedKnows();

    /// <summary>Set by <see cref="UpdateTor"/> while the local player is handcuffed.</summary>
    private Action OnClickOverride;

    /// <summary>The button's real click handler - the old <c>OnClick</c> delegate.</summary>
    public Action RealOnClick { get; set; } = () => { };

    /// <summary>Dispatches to the handcuff stub while restrained, otherwise to the real click.</summary>
    protected override void OnClick() => (OnClickOverride ?? RealOnClick)();

    private void SetActive(bool active)
    {
        if (!Button)
        {
            return;
        }

        Button.gameObject.SetActive(active);
        Button.graphic.enabled = active;
    }

    /// <summary>Called by TOR when a meeting closes, replaces the old <c>MeetingEndedUpdate</c>.</summary>
    public void MeetingEnd()
    {
        OnMeetingEnds();
    }

}

/// <summary>Targeted counterpart of <see cref="TorButton"/> (old target-aware buttons).</summary>
[MiraIgnore]
public abstract class TorTargetButton<T>(float distance) : CustomActionButton<T> where T : MonoBehaviour
{
    // Intentionally thin for now - targeted TOR buttons keep their own target bookkeeping in the
    // converted class. The base is here so those buttons still register with Mira's button manager.
    public override float Distance => distance;

    public override bool Enabled(RoleBehaviour role) => true;
    protected override void OnClick()
    {
    }
}

/// <summary>
/// Mira's <see cref="CustomActionButton.Sprite"/> wants a <see cref="LoadableAsset{T}"/>, but half of
/// TOR's buttons take a sprite that only exists once the HUD is up (e.g. <c>KillButton.graphic.sprite</c>).
/// This resolves it lazily on first use and then caches it.
/// </summary>
public class TorLazySprite(Func<Sprite> resolver) : LoadableAsset<Sprite>
{
    public static LoadableAsset<Sprite> Wrap(Sprite sprite) => new TorLazySprite(() => sprite);

    public override Sprite LoadAsset()
    {
        if (LoadedAsset != null && LoadedAsset)
        {
            return LoadedAsset;
        }

        var resolved = resolver();
        LoadedAsset = resolved;
        return resolved;
    }

    // Never destroy - the sprite belongs to the game (or to a TORAssets entry).
    public override bool UnloadAsset()
    {
        LoadedAsset = null;
        return false;
    }
}
