using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MiraAPI.GameModes;
using MiraAPI.Patches.Options;
using TMPro;
using UnityEngine;

namespace TheOtherRoles.Patches;

internal static class TorSettingsMenu
{
    internal static GameOptionsMenu Menu = null!;

    private static readonly MethodInfo GetAndSetGameMode =
        AccessTools.Method(typeof(CustomGameModeManager), "GetAndSetGameMode");

    private static readonly MethodInfo ToggleGamemodeOptions =
        AccessTools.Method(AccessTools.TypeByName("MiraAPI.Patches.Options.GameOptionsMenuPatch"),
            "ToggleGamemodeOptions");

    private static readonly MethodInfo CustomCreateSettings =
        AccessTools.Method(AccessTools.TypeByName("MiraAPI.Patches.Options.GameOptionsMenuPatch"),
            "CustomCreateSettings");

    private static readonly FieldInfo GamemodeDescriptionField =
        AccessTools.Field(AccessTools.TypeByName("MiraAPI.Patches.Options.GameOptionsMenuPatch"),
            "_gamemodeDescription");

    private static readonly PropertyInfo HeaderProperty =
        AccessTools.Property(typeof(AbstractOptionGroup), "Header");

    private static readonly PropertyInfo ReadyProperty =
        AccessTools.Property(typeof(AbstractOptionGroup), "Ready");

    // Mira API only instantiates mode bound groups while it builds the vanilla Game page, and only
    // GameOptionsMenuPatch.InitPatch knows how to do that.
    private static readonly MethodInfo VanillaInit =
        AccessTools.Method(AccessTools.TypeByName("MiraAPI.Patches.Options.GameOptionsMenuPatch"), "InitPatch");

    private static readonly PropertyInfo CurrentModIdxProperty =
        AccessTools.Property(typeof(MenuState), "CurrentModIdx");

    private static readonly FieldInfo MapPickerField =
        AccessTools.Field(typeof(GameOptionsMenu), "MapPicker");

    private static readonly PropertyInfo MapPickerProperty =
        AccessTools.Property(typeof(GameOptionsMenu), "MapPicker");

    private static int _lastModeValue = int.MinValue;
    private static bool _layoutPending;
    private static int _creationAttempts;
    private static float _nextEnsureTime;
    private static int _vanillaBuildAttempts;
    private static float _nextVanillaBuildTime;
    private static TextMeshPro _fixedDescription;
    private static Transform _descriptionBackground;
    private static Transform _descriptionIcon;
    private static List<AbstractOptionGroup> _torGroups;

    // group -> header / option transforms, cached once the group finished creating so the per frame
    // reparent pass never touches reflection again. The same cache doubles as the list of text that
    // Mira API painted with the group colour, so whitening is a plain loop with no allocation.
    private static readonly Dictionary<AbstractOptionGroup, ReparentEntry> Reparented = new();

    private struct ReparentEntry
    {
        public Transform Header;
        public Transform[] Options;
        public TextMeshPro[] Texts;

        // TOR draws its +/- signs with a TextMeshPro instead of a sprite. Those sit on top of the
        // plate, so they are glyph rather than label: whitening them would erase them.
        public TextMeshPro[] GlyphTexts;
        public SpriteRenderer[] Plates;
        public SpriteRenderer[] Glyphs;
        public ButtonPaint[] Buttons;
    }

    private struct ButtonPaint
    {
        public GameOptionButton Button;

        /// <summary>Whether the sprite this button tints is the plate rather than the glyph.</summary>
        public bool TintsPlate;
    }

    // Mira API paints every sprite of an option row with the group colour's alternate colour,
    // which for TOR's capped group colours comes out near white - the very value the row background
    // itself gets, so the +/- buttons melt into the background and their glyphs disappear.
    //
    // The plate behind the button is the sprite named "ButtonSprite" (Mira API looks it up by that
    // name itself when it builds a role option) and it does not have to be nested under the
    // GameOptionButton, so it is matched by name across the whole row. The plate stays light -
    // the rows themselves are the untouched vanilla panels - and the +/- glyph sitting on top of
    // it goes black. A row without such a sprite keeps whatever Mira API painted, which is light
    // as well, so both prefab layouts come out with a visible plate and a readable glyph.
    private const string PlateSpriteName = "ButtonSprite";
    private static readonly Color PlusMinusPlateColor = Color.white;
    private static readonly Color PlusMinusGlyphColor = Color.black;

    private static void CollectButtonColors(
        Transform option,
        List<SpriteRenderer> plates,
        List<SpriteRenderer> glyphs,
        List<ButtonPaint> buttons)
    {
        foreach (var renderer in option.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer.transform.name == PlateSpriteName && !plates.Contains(renderer))
                plates.Add(renderer);
        }

        foreach (var button in option.GetComponentsInChildren<GameOptionButton>(true))
        {
            var tintsPlate = false;
            foreach (var renderer in button.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.transform.name == PlateSpriteName)
                {
                    if (renderer.transform == button.transform ||
                        renderer.transform.parent == button.transform)
                    {
                        tintsPlate = true;
                    }

                    continue;
                }

                // Everything else that lives under the button is the +/- glyph itself.
                glyphs.Add(renderer);
            }

            buttons.Add(new ButtonPaint { Button = button, TintsPlate = tintsPlate });
        }
    }

    /// <summary>
    /// Re-applied every frame: Mira API repaints an option while it is being created and would
    /// otherwise win the colours back the moment the settings page is rebuilt.
    /// </summary>
    private static void PaintPlusMinus(ReparentEntry entry)
    {
        var plates = entry.Plates;
        if (plates != null)
        {
            for (var i = 0; i < plates.Length; i++)
            {
                var renderer = plates[i];
                if (renderer != null && renderer) renderer.color = PlusMinusPlateColor;
            }
        }

        var glyphs = entry.Glyphs;
        if (glyphs != null)
        {
            for (var i = 0; i < glyphs.Length; i++)
            {
                var renderer = glyphs[i];
                if (renderer != null && renderer) renderer.color = PlusMinusGlyphColor;
            }
        }

        var glyphTexts = entry.GlyphTexts;
        if (glyphTexts != null)
        {
            for (var i = 0; i < glyphTexts.Length; i++)
            {
                var text = glyphTexts[i];
                if (text != null && text) text.color = PlusMinusGlyphColor;
            }
        }

        var buttons = entry.Buttons;
        if (buttons == null) return;

        for (var i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i].Button;
            if (button == null || !button) continue;

            // Mira API drives the state colours from the sprite that sits on the button (or is its
            // direct child), so pinning them to whatever colour that sprite got keeps hover and
            // press from washing the button out again.
            var color = buttons[i].TintsPlate ? PlusMinusPlateColor : PlusMinusGlyphColor;
            button.interactableColor = color;
            button.interactableHoveredColor = color;
        }
    }

    internal static void PaintButtons()
    {
        foreach (var entry in Reparented.Values) PaintPlusMinus(entry);
    }

    internal static void ResetCreationBudget()
    {
        _creationAttempts = 0;
        _nextEnsureTime = 0f;
        _vanillaBuildAttempts = 0;
        _nextVanillaBuildTime = 0f;
    }

    internal static List<AbstractOptionGroup> TorGroups =>
        _torGroups ??= ModdedOptionsManagerGroups()
            .Where(g => g.GetType().Assembly == typeof(TorSettingsMenu).Assembly)
            .ToList();

    private static List<AbstractOptionGroup> ModdedOptionsManagerGroups()
    {
        var value = AccessTools.Field(typeof(ModdedOptionsManager), "Groups")?.GetValue(null)
                    ?? AccessTools.Property(typeof(ModdedOptionsManager), "Groups")?.GetValue(null);
        return value is IEnumerable<AbstractOptionGroup> groups ? groups.ToList() : new List<AbstractOptionGroup>();
    }

    private static Transform HeaderOf(AbstractOptionGroup group)
    {
        // CategoryHeaderMasked is a Unity object; `as Transform` would always fail, so unwrap it.
        if (HeaderProperty.GetValue(group) is not Component header) return null;
        return header ? header.transform : null;
    }

    private static bool Ready(AbstractOptionGroup group) => ReadyProperty.GetValue(group) is true;

    private static TextMeshPro Description =>
        GamemodeDescriptionField?.GetValue(null) as TextMeshPro is { } description && description
            ? description
            : null;

    /// <summary>Called from a prefix on <see cref="GameOptionsMenu.Update"/>, before Mira API lays out.</summary>
    internal static void Tick()
    {
        PollGameMode();
        EnsureVanillaPageBuilt();
        EnsureGroupsCreated();
        ReparentGroups();
        FixDescription();
    }

    /// <summary>
    /// Keeps <see cref="CustomGameModeManager.ActiveMode"/> (and therefore TOR's own game mode flags)
    /// in sync with Mira API's gamemode dropdown. <see cref="GameModeOption.Value"/> is a plain
    /// property, so this costs one int compare per frame.
    /// </summary>
    private static void PollGameMode()
    {
        var value = GameModeOption.Value;
        if (value != _lastModeValue)
        {
            _lastModeValue = value;
            _layoutPending = true;
            GetAndSetGameMode?.Invoke(null, null);
        }

        if (!_layoutPending) return;

        var mode = CustomGameModeManager.ActiveMode;
        if (mode == null || Menu == null || !Menu)
        {
            _layoutPending = false;
            return;
        }

        if (Description == null) return;
        _layoutPending = false;

        ToggleGamemodeOptions.Invoke(null, new object[] { mode, Menu });

        // Mira API only does this inside its own ValueChanged prefix, which is skipped when it hands
        // the RPC off - without it the "Role Settings" button would keep the previous mode's state.
        if (GameSettingMenu.Instance && GameSettingMenu.Instance.RoleSettingsButton)
            GameSettingMenu.Instance.RoleSettingsButton.gameObject.SetActive(mode.ShowNormalRoleSettings);
    }

    private static void EnsureVanillaPageBuilt()
    {
        if (Menu == null || !Menu || Description != null) return;
        if (MenuState.Instance == null || !MenuState.Instance) return;
        if (VanillaInit == null || CurrentModIdxProperty == null) return;
        if (MenuState.Instance.CurrentModIdx == 0) return; // Mira API takes that path by itself

        var setter = CurrentModIdxProperty.GetSetMethod(true);
        if (setter == null) return;

        if (Time.unscaledTime < _nextVanillaBuildTime) return;
        if (_vanillaBuildAttempts >= 4) return;
        _vanillaBuildAttempts++;
        _nextVanillaBuildTime = Time.unscaledTime + 5f;

        var previous = MenuState.Instance.CurrentModIdx;
        try
        {
            setter.Invoke(MenuState.Instance, new object[] { 0 });
            VanillaInit.Invoke(null, new object[] { Menu });
        }
        catch (System.Exception e)
        {
            TheOtherRolesPlugin.Logger.LogWarning($"Could not build the vanilla settings page: {e.Message}");
        }
        finally
        {
            setter.Invoke(MenuState.Instance, new object[] { previous });
            if (previous != 0) HideMapPicker();
        }
    }

    private static void HideMapPicker()
    {
        object mapPicker = MapPickerField != null
            ? MapPickerField.GetValue(Menu)
            : MapPickerProperty?.GetValue(Menu);
        if (mapPicker is Component component && component) component.gameObject.SetActive(false);
    }

    private static void EnsureGroupsCreated()
    {
        if (MenuState.Instance == null || !MenuState.Instance || MenuState.Instance.CurrentModIdx == 0) return;
        if (Time.unscaledTime < _nextEnsureTime) return;

        var menu = MenuState.Instance.CurrentMenu;
        if (!ClassicGroupsReady(menu, out var any))
        {
            // Still being built - do not restart it while it runs.
            if (_nextEnsureTime <= 0f)
            {
                _nextEnsureTime = Time.unscaledTime + 4f;
                return;
            }

            if (_creationAttempts >= 3)
            {
                _nextEnsureTime = Time.unscaledTime + 3f;
                return;
            }

            _creationAttempts++;
            _nextEnsureTime = Time.unscaledTime + 6f;
            CustomCreateSettings?.Invoke(null, new object[] { Menu });
            return;
        }

        _creationAttempts = 0;
        _nextEnsureTime = Time.unscaledTime + (any ? 1f : 5f);
    }

    /// <summary>Reports whether every group Mira API is responsible for on this tab exists.</summary>
    private static bool ClassicGroupsReady(MenuCategory menu, out bool any)
    {
        any = false;
        foreach (var group in TorGroups)
        {
            if (group.OptionableType != null || group.ParentMenu != menu) continue;
            any = true;

            // Deliberately not looking at Ready: Mira API clears it for as long as its creation
            // coroutine runs, and restarting that coroutine from here is what made the page build
            // itself over and over on screen. A missing header is the only real failure.
            if (HeaderOf(group) == null) return false;
        }

        return true;
    }

    /// <summary>Move TOR's groups into the container of the tab that is actually being displayed.</summary>
    private static void ReparentGroups()
    {
        if (MenuState.Instance == null || !MenuState.Instance) return;
        if (MenuState.Instance.CurrentModIdx == 0 && MenuState.Instance.CurrentMenu != MenuCategory.Game) return;

        var container = MenuState.Instance.CurrentContainer;
        if (container == null || !container) return;
        var containerTransform = container.transform;

        // On Mira API's own Game page only mode bound groups belong there; the mod's own groups must
        // stay on the mod's Game tab.
        var vanillaPage = MenuState.Instance.CurrentModIdx == 0;
        var menu = MenuState.Instance.CurrentMenu;

        foreach (var group in TorGroups)
        {
            if (group.ParentMenu != menu) continue;
            if (vanillaPage && group.OptionableType == null) continue;

            var header = HeaderOf(group);
            if (header == null) continue;

            if (!Reparented.TryGetValue(group, out var entry) || entry.Header == null || !entry.Header)
            {
                // Only cache once every option of the group has been instantiated, otherwise the
                // creation coroutine would still be filling them in.
                var options = group.Children;
                if (options.Count == 0 || options.Any(o => o.OptionBehaviour == null)) continue;

                // Mira API only lays a group out once it reports Ready; until then every option
                // still sits where it was created. Activating the group before that piles the whole
                // group up in a single spot and only lets it fly into place a frame later, which is
                // what shows up as the settings blinking while they appear to load.
                if (!Ready(group)) continue;

                var texts = new List<TextMeshPro>();
                foreach (var text in header.GetComponentsInChildren<TextMeshPro>(true)) texts.Add(text);
                var glyphTexts = new List<TextMeshPro>();
                var plates = new List<SpriteRenderer>();
                var glyphs = new List<SpriteRenderer>();
                var buttons = new List<ButtonPaint>();
                foreach (var option in options)
                {
                    var root = option.OptionBehaviour.transform;
                    foreach (var text in option.OptionBehaviour.GetComponentsInChildren<TextMeshPro>(true))
                    {
                        // A label belongs to the row; a text under one of its +/- buttons is a
                        // glyph. The walk stops below the row root, so a prefab that makes the
                        // whole row a button still has its label whitened.
                        GameOptionButton host = null;
                        for (var t = text.transform; t != null && t != root; t = t.parent)
                        {
                            host = t.GetComponent<GameOptionButton>();
                            if (host != null) break;
                        }

                        if (host != null) glyphTexts.Add(text);
                        else texts.Add(text);
                    }

                    CollectButtonColors(root, plates, glyphs, buttons);
                }

                entry = new ReparentEntry
                {
                    Header = header,
                    Options = options.Select(o => o.OptionBehaviour.transform).ToArray(),
                    Texts = texts.ToArray(),
                    GlyphTexts = glyphTexts.ToArray(),
                    Plates = plates.ToArray(),
                    Glyphs = glyphs.ToArray(),
                    Buttons = buttons.ToArray(),
                };
                Reparented[group] = entry;

                // Mira API tints the text while the group is being created; undo it immediately so a
                // group is never on screen with coloured labels for a frame.
                foreach (var text in entry.Texts)
                {
                    if (text != null && text) text.color = Color.white;
                }

                // ... and give the +/- buttons their plate and glyph colours back.
                PaintPlusMinus(entry);
            }

            // Mira API only hands a group to UpdateGroup when it belongs to the mode that is currently
            // selected. Groups of a mode that was deselected therefore never get hidden by Mira API
            // and would stay on screen, stacked on top of the settings of the newly selected mode.
            var activeMode = CustomGameModeManager.ActiveMode;
            var selectedMode = group.OptionableType == null ||
                               (activeMode != null && group.OptionableType.IsInstanceOfType(activeMode));

            if (!selectedMode || !group.GroupVisible.Invoke())
            {
                SetActive(entry, false);
                continue;
            }

            // Restore whatever a previous mode selection hid. Mira API only ever hands the groups of
            // the currently selected mode to UpdateGroup, so nothing else would bring them back -
            // which is why Guesser kept showing classic settings only.
            SetActive(entry, true);

            if (entry.Header.parent != containerTransform) entry.Header.SetParent(containerTransform, false);
            foreach (var option in entry.Options)
            {
                if (option != null && option && option.parent != containerTransform)
                    option.SetParent(containerTransform, false);
            }
        }
    }

    private static void SetActive(ReparentEntry entry, bool active)
    {
        if (entry.Header != null && entry.Header) entry.Header.gameObject.SetActive(active);
        foreach (var option in entry.Options)
        {
            if (option != null && option) option.gameObject.SetActive(active);
        }
    }

    /// <summary>
    /// Moves mode bound groups back into Mira API's own Game container before it re-initialises.
    /// <c>GameOptionsMenu.Initialize</c> decides whether to build the page from scratch by looking at
    /// the container's child count, so a group that was borrowed by the mod's tab has to be back
    /// home first or the page would be built a second time and every option would be duplicated.
    /// </summary>
    internal static void RestoreModeGroups()
    {
        if (MenuState.Instance == null || !MenuState.Instance) return;
        if (MenuState.Instance.CurrentModIdx != 0 || MenuState.Instance.CurrentMenu != MenuCategory.Game) return;

        var container = MenuState.Instance.CurrentContainer;
        if (container == null || !container) return;
        var containerTransform = container.transform;

        foreach (var pair in Reparented)
        {
            if (pair.Key.OptionableType == null) continue;
            var entry = pair.Value;
            if (entry.Header == null || !entry.Header) continue;
            if (entry.Header.parent != containerTransform) entry.Header.SetParent(containerTransform, false);
            foreach (var option in entry.Options)
            {
                if (option != null && option && option.parent != containerTransform)
                    option.SetParent(containerTransform, false);
            }
        }
    }

    /// <summary>
    /// The gamemode description is a single non wrapping line that also starts underneath the mode
    /// icon. Wrap it, and pull it to the right of the icon while keeping it inside the background.
    /// Re-applied every frame because Mira API repositions (and re-aligns) the text whenever the
    /// selected mode changes.
    /// </summary>
    private static void FixDescription()
    {
        var description = Description;
        if (description == null || !description) return;

        var holder = description.transform.parent;
        if (holder == null || !holder) return;

        if (_fixedDescription != description)
        {
            _fixedDescription = description;
            _descriptionBackground = holder.Find("Background");
            _descriptionIcon = holder.Find("ModIcon");

            description.enableWordWrapping = true;
            description.enableAutoSizing = true;
            description.fontSizeMin = 0.45f;
            description.overflowMode = TextOverflowModes.Ellipsis;
        }

        if (_descriptionBackground == null || !_descriptionBackground ||
            !_descriptionBackground.TryGetComponent<SpriteRenderer>(out var background) ||
            background.bounds.size.x < 0.01f)
        {
            return;
        }

        // The background, the icon and the text all live under the same "GamemodeInfo" holder, so
        // measuring in that space keeps the text independent of the menu's own transform.
        const float padding = 0.1f;
        var left = holder.InverseTransformPoint(background.bounds.min).x + padding;
        var right = holder.InverseTransformPoint(background.bounds.max).x - padding;

        if (_descriptionIcon != null && _descriptionIcon && _descriptionIcon.gameObject.activeSelf &&
            _descriptionIcon.TryGetComponent<SpriteRenderer>(out var icon) && icon.sprite)
        {
            left = Mathf.Max(left, holder.InverseTransformPoint(icon.bounds.max).x + padding);
        }

        if (right - left < 0.3f) return;

        var holderScale = Mathf.Abs(holder.lossyScale.x);
        var textScale = Mathf.Abs(description.transform.lossyScale.x);
        if (holderScale < 0.0001f || textScale < 0.0001f) return;

        var rectTransform = description.rectTransform;
        if (!Mathf.Approximately(rectTransform.anchorMin.x, rectTransform.anchorMax.x))
        {
            // Equal anchors make sizeDelta the exact rect width, so the maths below stays honest.
            var anchor = rectTransform.anchorMin.x;
            rectTransform.anchorMin = new Vector2(anchor, rectTransform.anchorMin.y);
            rectTransform.anchorMax = new Vector2(anchor, rectTransform.anchorMax.y);
        }

        var size = rectTransform.sizeDelta;
        size.x = (right - left) * holderScale / textScale;
        rectTransform.sizeDelta = size;

        var rect = rectTransform.rect;
        var currentLeft = holder.InverseTransformPoint(
            rectTransform.TransformPoint(new Vector3(rect.xMin, rect.center.y, 0f))).x;
        var delta = left - currentLeft;
        if (Mathf.Abs(delta) < 0.0005f) return;

        var parent = description.transform.parent;
        var parentScale = Mathf.Abs(parent.lossyScale.x);
        if (parentScale < 0.0001f) return;

        var localPosition = description.transform.localPosition;
        localPosition.x += delta * holderScale / parentScale;
        description.transform.localPosition = localPosition;
    }

    internal static void WhitenText()
    {
        foreach (var entry in Reparented.Values)
        {
            var texts = entry.Texts;
            if (texts == null) continue;
            for (var i = 0; i < texts.Length; i++)
            {
                var text = texts[i];
                if (text != null && text && text.color != Color.white) text.color = Color.white;
            }
        }

        var description = Description;
        if (description != null && description && description.color != Color.white)
            description.color = Color.white;
    }
}

[HarmonyPatch]
internal static class TorSettingsMenuPatches
{
    [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.OnEnable))]
    [HarmonyPrefix]
    private static void OnEnablePrefix() => TorSettingsMenu.RestoreModeGroups();

    [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.OnEnable))]
    [HarmonyPostfix]
    private static void OnEnablePostfix(GameOptionsMenu __instance)
    {
        TorSettingsMenu.Menu = __instance;
        TorSettingsMenu.ResetCreationBudget();
    }

    [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Update))]
    [HarmonyPrefix]
    private static void UpdatePrefix() => TorSettingsMenu.Tick();

    [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Update))]
    [HarmonyPostfix]
    private static void UpdatePostfix()
    {
        TorSettingsMenu.WhitenText();
        TorSettingsMenu.PaintButtons();
    }
}
