using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace TheOtherRoles.Draft;

/// <summary>
/// One draft card: the vanilla kill button reduced to a clickable shell (its own art is thrown
/// away), a faction card sprite on top and the role's name underneath - the iconless GMIA look.
/// </summary>
public static class DraftCardFactory
{
    public const float CardHeight = 3f;
    public const float CardAspect = 500f / 620f;
    public const float CardWidth = CardHeight * CardAspect;

    public static GameObject Create(Transform parent, int index, Sprite art, string label, Color labelColor,
        out PassiveButton button)
    {
        var root = new GameObject($"DraftCard_{index}");
        root.layer = DraftUi.Layer;
        root.transform.SetParent(parent, false);

        var shell = Object.Instantiate(HudManager.Instance.KillButton.gameObject, root.transform);
        shell.name = "Card";
        shell.SetActive(true);
        shell.transform.localPosition = Vector3.zero;
        shell.transform.localRotation = Quaternion.identity;
        shell.transform.localScale = Vector3.one;
        foreach (var position in shell.GetComponentsInChildren<AspectPosition>(true)) position.enabled = false;
        foreach (var renderer in shell.GetComponentsInChildren<SpriteRenderer>(true)) renderer.enabled = false;
        foreach (var text in shell.GetComponentsInChildren<TMP_Text>(true)) text.gameObject.SetActive(false);
        foreach (var collider in shell.GetComponentsInChildren<Collider2D>(true)) collider.enabled = false;
        var action = shell.GetComponent<ActionButton>();
        if (action) action.enabled = false;

        var artObject = new GameObject("Art");
        artObject.layer = DraftUi.Layer;
        artObject.transform.SetParent(shell.transform, false);
        var artRenderer = artObject.AddComponent<SpriteRenderer>();
        artRenderer.sprite = art;
        DraftUi.Style(artRenderer, DraftUi.Order + 10);
        artObject.transform.localScale = Vector3.one * (CardHeight / art.bounds.size.y);
        artObject.transform.localPosition = new Vector3(0f, 0f, -0.01f);

        var name = Object.Instantiate(HudManager.Instance.KillButton.cooldownTimerText, shell.transform);
        name.gameObject.SetActive(true);
        name.gameObject.layer = DraftUi.Layer;
        name.transform.localPosition = new Vector3(0f, -CardHeight * 0.36f, -0.02f);
        name.transform.localRotation = Quaternion.identity;
        name.transform.localScale = Vector3.one;

        var rect = name.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(CardWidth * 0.94f, 0.6f);
        rect.localScale = Vector3.one;
        name.alignment = TextAlignmentOptions.Center;
        name.enableWordWrapping = true;
        name.overflowMode = TextOverflowModes.Overflow;
        name.color = labelColor;
        name.fontStyle = FontStyles.Bold;
        name.enableAutoSizing = true;
        name.fontSizeMin = 1.1f;
        name.fontSizeMax = 2.4f;
        name.text = label;
        if (name.TryGetComponent<MeshRenderer>(out var mesh)) DraftUi.Style(mesh, DraftUi.Order + 12);

        var box = shell.AddComponent<BoxCollider2D>();
        box.size = new Vector2(CardWidth, CardHeight);
        box.offset = Vector2.zero;

        button = shell.GetComponent<PassiveButton>();
        if (button)
        {
            button.enabled = true;
            button.ClickMask = null;
            button.Colliders = new[] { box };
            button.OnClick.RemoveAllListeners();
            button.OnMouseOver.RemoveAllListeners();
            button.OnMouseOut.RemoveAllListeners();
        }

        return root;
    }
}
