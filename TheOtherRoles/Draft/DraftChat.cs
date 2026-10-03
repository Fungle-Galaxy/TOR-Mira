using System.Collections.Generic;
using TheOtherRoles.Options;
using UnityEngine;
using UnityEngine.Rendering;

namespace TheOtherRoles.Draft;

/// <summary>
/// The lobby chat during a draft. The panel is closed the moment the draft starts, and the button
/// that would reopen it only survives - and only shows up over the draft screen - when the option
/// allows chatting.
/// </summary>
internal static class DraftChat
{
    private static readonly Dictionary<Renderer, (string layer, int order)> renderers = new();
    private static readonly Dictionary<SortingGroup, (string layer, int order)> groups = new();
    private static readonly Dictionary<Transform, Vector3> transforms = new();
    private static bool wasVisible;

    public static bool Allowed
    {
        get
        {
            var options = OptionGroupSingleton<DraftOptions>.Instance;
            return options != null && options.CanChat.Value;
        }
    }

    public static bool CanOpen => !DraftManager.IsDraftActive || Allowed;

    public static void OnDraftStart()
    {
        var hud = HudManager.Instance;
        if (!hud || !hud.Chat) return;

        wasVisible = hud.Chat.gameObject.activeSelf;
        hud.Chat.SetVisible(Allowed);
        if (Allowed) boost(hud.Chat);
    }

    public static void OnDraftEnd()
    {
        restore();
        var hud = HudManager.Instance;
        if (hud && hud.Chat && wasVisible) hud.Chat.SetVisible(true);
        wasVisible = false;
    }

    /// <summary>
    /// Kept alive frame by frame while the draft runs: the chat switches its own sprite containers
    /// on and off, re-reads its position and can be closed behind our back, so one push at the
    /// start of the draft is not enough to keep the button on top of the backdrop.
    /// </summary>
    public static void Tick()
    {
        if (!DraftManager.IsDraftActive || !Allowed) return;
        var hud = HudManager.Instance;
        if (!hud || !hud.Chat) return;

        if (!hud.Chat.gameObject.activeSelf) hud.Chat.SetVisible(true);
        boost(hud.Chat);
    }

    // In front of the draft in two independent ways: z is what a click runs into first, the
    // sorting order is what is drawn on top. The z stops short of the camera, because anything
    // behind the near plane is not drawn at all - which reads exactly like "the background is
    // covering it".
    private static void boost(ChatController chat)
    {
        var hud = HudManager.Instance;
        var backdrop = hud.transform.position.z + DraftUi.Depth;
        var camera = Camera.main;
        var limit = camera ? camera.transform.position.z + 6f : backdrop - DraftUi.ChatAhead;
        var front = Mathf.Max(limit, backdrop - DraftUi.ChatAhead);

        var root = chat.transform;
        push(root, front);
        collect(chat.GetComponentsInChildren<Renderer>(true));
        collectGroups(chat.GetComponentsInChildren<SortingGroup>(true));

        // The prefab hangs its parts off serialized references rather than off the controller, so
        // anything parked outside the controller would keep sitting behind the backdrop untouched.
        foreach (var part in parts(chat))
        {
            if (!part.IsChildOf(root)) push(part, front);
            collect(part.GetComponentsInChildren<Renderer>(true));
            collectGroups(part.GetComponentsInChildren<SortingGroup>(true));
        }

        apply(shift());
    }

    private static IEnumerable<Transform> parts(ChatController chat)
    {
        var button = chat.chatButton;
        if (button) yield return button.transform;
        var screen = chat.chatScreen;
        if (screen) yield return screen.transform;
        var keyboard = chat.openKeyboardButton;
        if (keyboard) yield return keyboard.transform;
        var quick = chat.quickChatButton;
        if (quick) yield return quick.transform;
    }

    private static void push(Transform transform, float front)
    {
        if (!transform) return;
        var position = transform.position;
        if (!transforms.ContainsKey(transform)) transforms.Add(transform, position);
        if (Mathf.Abs(position.z - front) > 0.01f)
            transform.position = new Vector3(position.x, position.y, front);
    }

    private static void collect(Renderer[] found)
    {
        foreach (var renderer in found)
        {
            if (!renderer || renderers.ContainsKey(renderer)) continue;
            renderers.Add(renderer, (renderer.sortingLayerName, renderer.sortingOrder));
        }
    }

    private static void collectGroups(SortingGroup[] found)
    {
        foreach (var group in found)
        {
            if (!group || groups.ContainsKey(group)) continue;
            groups.Add(group, (group.sortingLayerName, group.sortingOrder));
        }
    }

    // One fixed step for the whole chat, measured off the values it had before the draft touched
    // them, so re-applying every frame cannot drag its own front to back order along with it.
    private static int shift()
    {
        var lowest = int.MaxValue;
        foreach (var value in renderers.Values) lowest = Mathf.Min(lowest, value.order);
        foreach (var value in groups.Values) lowest = Mathf.Min(lowest, value.order);
        return lowest == int.MaxValue ? 0 : DraftUi.ChatOrder - lowest;
    }

    private static void apply(int step)
    {
        foreach (var (renderer, value) in renderers)
        {
            if (!renderer) continue;
            renderer.sortingLayerName = DraftUi.SortingLayer;
            renderer.sortingOrder = value.order + step;
        }

        foreach (var (group, value) in groups)
        {
            if (!group) continue;
            group.sortingLayerName = DraftUi.SortingLayer;
            group.sortingOrder = value.order + step;
        }
    }

    private static void restore()
    {
        foreach (var (renderer, value) in renderers)
        {
            if (!renderer) continue;
            renderer.sortingLayerName = value.layer;
            renderer.sortingOrder = value.order;
        }

        renderers.Clear();

        foreach (var (group, value) in groups)
        {
            if (!group) continue;
            group.sortingLayerName = value.layer;
            group.sortingOrder = value.order;
        }

        groups.Clear();

        foreach (var (transform, position) in transforms)
        {
            if (transform) transform.position = position;
        }

        transforms.Clear();
    }
}
