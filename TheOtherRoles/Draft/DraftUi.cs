using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheOtherRoles.Draft;

/// <summary>The handful of primitives both draft screens are built from.</summary>
internal static class DraftUi
{
    public const int Order = 32000;
    public const int ChatOrder = 32600;

    // The lobby draws over anything that sits at the usual z, so the whole draft lives down here.
    public const float Depth = -300f;

    // How much further in front of the draft the chat has to sit to stay clickable.
    public const float ChatAhead = 10f;

    public const float Margin = 0.2f;

    public static int Layer;
    public static string SortingLayer = "Default";

    private static Sprite white;

    public static void Prepare()
    {
        var hud = HudManager.Instance;
        if (!hud) return;

        Layer = hud.SettingsButton.layer;
        SpriteRenderer renderer = null;
        if (hud.MapButton)
        {
            renderer = hud.MapButton.GetComponent<SpriteRenderer>();
            if (!renderer) renderer = hud.MapButton.GetComponentInChildren<SpriteRenderer>(true);
        }

        if (!renderer) renderer = hud.SettingsButton.GetComponentInChildren<SpriteRenderer>(true);
        if (renderer) SortingLayer = renderer.sortingLayerName;
    }

    public static void Style(Renderer renderer, int order)
    {
        renderer.sortingLayerName = SortingLayer;
        renderer.sortingOrder = order;
    }

    // The HUD sits in world space at scale 1, so the part of it that is actually on screen is
    // exactly the main camera's view. Every draft position is measured against this.
    public static Vector2 Viewport
    {
        get
        {
            var camera = Camera.main;
            if (!camera) return new Vector2(16f, 9f);
            var height = camera.orthographicSize * 2f;
            return new Vector2(height * camera.aspect, height);
        }
    }

    public static float ColumnWidth(Vector2 view) => Mathf.Min(2.1f, view.x * 0.22f);

    public static Sprite White
    {
        // A runtime sprite dies with the scene it was made in, so never hand out a dead one.
        get
        {
            if (!white) white = createWhite();
            return white;
        }
    }

    private static Sprite createWhite()
    {
        var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        for (var y = 0; y < 4; y++)
        for (var x = 0; x < 4; x++)
            texture.SetPixel(x, y, Color.white);
        texture.Apply();
        // 4 px at 4 ppu = one world unit, so CreateBackdrop can size it with localScale alone.
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
        texture.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
        sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
        return sprite;
    }

    public static GameObject CreateBackdrop(Transform parent, int order)
    {
        var view = Viewport;

        var go = new GameObject("Backdrop");
        go.layer = Layer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = new Vector3(view.x, view.y, 1f);

        var baseRenderer = go.AddComponent<SpriteRenderer>();
        baseRenderer.sprite = White;
        baseRenderer.color = Color.black;
        Style(baseRenderer, order);

        addBar(go.transform, order + 2, 0.49f);
        addBar(go.transform, order + 2, -0.49f);
        return go;
    }

    private static void addBar(Transform parent, int order, float y)
    {
        var bar = new GameObject("Bar");
        bar.layer = Layer;
        bar.transform.SetParent(parent, false);
        bar.transform.localPosition = new Vector3(0f, y, -0.1f);
        bar.transform.localScale = new Vector3(0.86f, 0.0055f, 1f);
        var renderer = bar.AddComponent<SpriteRenderer>();
        renderer.sprite = White;
        renderer.color = new Color(1f, 0.82f, 0.15f, 0.7f);
        Style(renderer, order);
    }

    public static TextMeshPro CreateText(Transform parent, string text, Vector3 localPosition, Vector2 size,
        float fontSize, TextAlignmentOptions alignment, int order, bool autoSize = true, float minFontSize = 1.5f)
    {
        var tmp = Object.Instantiate(HudManager.Instance.KillButton.cooldownTimerText, parent);
        tmp.gameObject.SetActive(true);
        tmp.gameObject.layer = Layer;
        tmp.transform.localPosition = localPosition;
        tmp.transform.localRotation = Quaternion.identity;
        tmp.transform.localScale = Vector3.one;

        var rect = tmp.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;

        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.color = Color.white;
        tmp.fontSize = fontSize;
        tmp.enableAutoSizing = autoSize;
        if (autoSize)
        {
            tmp.fontSizeMin = minFontSize;
            tmp.fontSizeMax = fontSize;
        }

        if (tmp.TryGetComponent<MeshRenderer>(out var mesh)) Style(mesh, order);
        tmp.text = text;
        return tmp;
    }
}
