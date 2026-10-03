using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace TheOtherRoles.Draft;

/// <summary>The turn screen of whoever is picking right now: a row of cards and one click.</summary>
public static class DraftSelectionScreen
{
    private const float CardScale = 0.62f;
    private const float Gap = 0.2f;

    private static GameObject root;
    private static int showingSlot = -1;
    private static bool picked;
    private static float hideAt;
    private static float cardScale = CardScale;

    private static readonly List<CardAnim> anims = new();

    private sealed class CardAnim
    {
        public Transform transform;
        public Vector3 pos;
        public float appearAt;
        public bool done;
    }

    public static void Show(int slot, byte[] roleIds)
    {
        Hide();
        var hud = HudManager.Instance;
        if (!hud) return;
        DraftUi.Prepare();

        showingSlot = slot;
        picked = false;
        hideAt = 0f;

        root = new GameObject("TorDraftScreen");
        root.layer = DraftUi.Layer;
        root.transform.SetParent(hud.transform, false);
        root.transform.localPosition = new Vector3(0f, 0f, DraftUi.Depth);
        root.transform.localScale = Vector3.one;

        DraftStatusOverlay.SetBackgroundOnly();

        var halfH = DraftUi.Viewport.y * 0.5f;
        var prompt = DraftUi.CreateText(root.transform, ModTranslation.GetString("Draft-Text", 5),
            new Vector3(0f, halfH - 1.85f, -0.6f), new Vector2(9f, 0.7f), 3.2f, TextAlignmentOptions.Center,
            DraftUi.Order + 20, true, 1.8f);
        prompt.fontStyle = FontStyles.Bold;

        var cards = new List<(Sprite art, string label, Color color)>();
        foreach (var id in roleIds)
        {
            var info = RoleInfo.roleInfoById.TryGetValue((RoleId)id, out var found) ? found : null;
            cards.Add((artFor(info), info == null ? TorOptions.RoleName((RoleId)id) : info.name,
                info == null ? Color.white : info.color));
        }

        if (OptionGroupSingleton<DraftOptions>.Instance.ShowRandomCard.Value)
            cards.Add((TorAssets.DraftCardRandom.LoadAsset(), ModTranslation.GetString("Draft-Text", 6),
                Color.white));

        layout(cards);
    }

    public static void Hide()
    {
        if (root) Object.Destroy(root);
        root = null;
        showingSlot = -1;
        picked = false;
        anims.Clear();
        DraftStatusOverlay.SetWaiting();
    }

    public static void OnPickConfirmed(DraftSlotState state)
    {
        if (state == null || state.SlotNumber != showingSlot || !root) return;
        picked = true;
        hideAt = Time.time + 1f;
    }

    public static void Tick()
    {
        if (!root) return;

        if (!DraftManager.IsDraftActive || (showingSlot != DraftManager.CurrentSlot && DraftManager.CurrentSlot > 0))
        {
            Hide();
            return;
        }

        if (picked && Time.time >= hideAt)
        {
            Hide();
            return;
        }

        tickReveal();
    }

    private static void tickReveal()
    {
        foreach (var a in anims)
        {
            if (a.done || !a.transform) continue;
            var t = Mathf.Clamp01((Time.time - a.appearAt) / 0.26f);
            if (t <= 0f) continue;
            a.transform.localScale = Vector3.one * (cardScale * easeOutBack(t));
            a.transform.localPosition = a.pos + new Vector3(0f, -0.5f * (1f - easeOutCubic(t)), 0f);
            if (t < 1f) continue;
            a.transform.localScale = Vector3.one * cardScale;
            a.transform.localPosition = a.pos;
            a.done = true;
        }
    }

    private static float easeOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        var u = t - 1f;
        return 1f + c3 * u * u * u + c1 * u * u;
    }

    private static float easeOutCubic(float t) => 1f - (1f - t) * (1f - t) * (1f - t);

    private static void layout(List<(Sprite art, string label, Color color)> cards)
    {
        var total = cards.Count;
        var view = DraftUi.Viewport;
        var halfW = view.x * 0.5f;
        var halfH = view.y * 0.5f;
        var colW = DraftUi.ColumnWidth(view);

        // The feed and the pick number keep their columns, the cards live in between them.
        var availableW = 2f * (halfW - DraftUi.Margin - colW) - 0.1f;
        var top = halfH - 2.25f;
        var bottom = -halfH + 0.2f;
        var availableH = top - bottom;

        var rows = 1;
        var best = 0f;
        for (var r = 1; r <= Mathf.Min(3, total); r++)
        {
            var columns = Mathf.CeilToInt(total / (float)r);
            var fitW = (availableW - (columns - 1) * Gap) / (columns * DraftCardFactory.CardWidth);
            var fitH = (availableH - (r - 1) * Gap) / (r * DraftCardFactory.CardHeight);
            var fit = Mathf.Min(CardScale, fitW, fitH);
            if (fit <= best) continue;
            best = fit;
            rows = r;
        }

        cardScale = Mathf.Max(0.2f, best);
        var cols = Mathf.CeilToInt(total / (float)rows);
        var stepX = DraftCardFactory.CardWidth * cardScale + Gap;
        var stepY = DraftCardFactory.CardHeight * cardScale + Gap;
        var blockHeight = rows * stepY - Gap;
        var cardHeight = DraftCardFactory.CardHeight * cardScale;
        var firstY = (top + bottom) * 0.5f + (blockHeight - cardHeight) * 0.5f;

        for (var i = 0; i < total; i++)
        {
            var row = i / cols;
            var column = i % cols;
            var inRow = row == rows - 1 ? total - row * cols : cols;
            var x = (column - (inRow - 1) * 0.5f) * stepX;
            var y = firstY - row * stepY;
            var index = i;

            var card = DraftCardFactory.Create(root.transform, index, cards[i].art, cards[i].label, cards[i].color,
                out var button);
            var pos = new Vector3(x, y, -1f - i * 0.05f);
            card.transform.localPosition = pos;
            card.transform.localScale = Vector3.zero;
            anims.Add(new CardAnim
            {
                transform = card.transform,
                pos = pos,
                appearAt = Time.time + 0.15f + i * 0.07f
            });
            if (!button) continue;

            var target = card;
            button.OnClick.AddListener((UnityAction)(() => onCardClicked(index)));
            button.OnMouseOver.AddListener((UnityAction)(() =>
                target.transform.localScale = Vector3.one * (cardScale * 1.075f)));
            button.OnMouseOut.AddListener((UnityAction)(() =>
                target.transform.localScale = Vector3.one * cardScale));
        }
    }

    private static void onCardClicked(int index)
    {
        if (picked || !DraftManager.IsDraftActive) return;
        picked = true;
        hideAt = Time.time + 1f;
        DraftRpcs.SendPick((byte)index);
    }

    private static Sprite artFor(RoleInfo info)
    {
        if (info == null) return TorAssets.DraftCardRandom.LoadAsset();
        if (info.isNeutral) return TorAssets.DraftCardNeutral.LoadAsset();
        if (info.isImpostor) return TorAssets.DraftCardImpostor.LoadAsset();
        return TorAssets.DraftCardCrew.LoadAsset();
    }
}
