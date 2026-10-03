using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheOtherRoles.Draft;

/// <summary>
/// The screen everyone sits behind while the draft runs: whose turn it is, which number you are,
/// and the running list of picks. It also takes the lobby's own start panel out of the way.
/// </summary>
public static class DraftStatusOverlay
{
    private static GameObject root;
    private static GameObject content;
    private static TextMeshPro nowLabel;
    private static TextMeshPro nowValue;
    private static TextMeshPro yourNumberValue;
    private static TextMeshPro timer;
    private static TextMeshPro picked;
    private static TextMeshPro feedText;
    private static GameObject startManagerGo;

    private static string lastTimer;
    private static string lastFeed;

    private static int myNumber;
    private static int slotCount;
    private static float scrambleEnds;
    private static float nextScramble;

    public static void Show()
    {
        if (root) return;
        build();
        if (!root) return;
        content.SetActive(true);
        hideLobbyPanel();
        Refresh();
        startNumberScramble();
    }

    /// <summary>The old mod's pick number reveal: digits roll for a few seconds, then land on yours.</summary>
    private static void startNumberScramble()
    {
        var local = PlayerControl.LocalPlayer;
        var myState = local == null ? null : DraftManager.GetStateForPlayer(local.PlayerId);
        myNumber = myState == null ? 0 : myState.SlotNumber;
        slotCount = DraftManager.SlotStates.Count;
        scrambleEnds = 0f;
        if (myNumber <= 0 || slotCount <= 1)
        {
            yourNumberValue.text = myNumber <= 0 ? "?" : myNumber.ToString();
            return;
        }

        scrambleEnds = Time.time + 3.5f;
        nextScramble = 0f;
    }

    private static void tickNumberScramble()
    {
        if (scrambleEnds <= 0f) return;
        if (Time.time >= scrambleEnds)
        {
            scrambleEnds = 0f;
            yourNumberValue.text = myNumber.ToString();
            return;
        }

        if (Time.time < nextScramble) return;
        nextScramble = Time.time + 0.15f;

        var left = scrambleEnds - Time.time;
        var min = Mathf.Max(1, myNumber - 1);
        var max = Mathf.Min(slotCount, myNumber + 1);
        if (left > 1f)
        {
            min = 1;
            max = slotCount;
        }
        else if (left > 0.6f)
        {
            min = Mathf.Max(1, myNumber - 2);
            max = Mathf.Min(slotCount, myNumber + 2);
        }
        else if (left <= 0.3f)
        {
            yourNumberValue.text = myNumber.ToString();
            return;
        }

        yourNumberValue.text = Helpers.rnd.Next(min, max + 1).ToString();
    }

    /// <summary>Out of the way while the picker has the cards - everything else stays on screen.</summary>
    public static void SetBackgroundOnly()
    {
        if (nowLabel) nowLabel.gameObject.SetActive(false);
        if (nowValue) nowValue.gameObject.SetActive(false);
    }

    public static void SetWaiting()
    {
        if (nowLabel) nowLabel.gameObject.SetActive(true);
        if (nowValue) nowValue.gameObject.SetActive(true);
    }

    public static void Hide()
    {
        if (root) Object.Destroy(root);
        root = null;
        content = null;
        nowLabel = null;
        nowValue = null;
        yourNumberValue = null;
        timer = null;
        picked = null;
        feedText = null;
        lastTimer = "";
        lastFeed = "";
        scrambleEnds = 0f;
        nextScramble = 0f;
        restoreLobbyPanel();
    }

    public static void Refresh()
    {
        if (!content) return;

        var local = PlayerControl.LocalPlayer;
        var myState = local == null ? null : DraftManager.GetStateForPlayer(local.PlayerId);
        var current = DraftManager.GetStateForSlot(DraftManager.CurrentSlot);
        var isMyTurn = current != null && local != null && current.PlayerId == local.PlayerId;

        if (scrambleEnds <= 0f)
            yourNumberValue.text = myState == null ? "?" : myState.SlotNumber.ToString();

        var picker = current == null ? null : Helpers.playerById(current.PlayerId);
        if (isMyTurn)
        {
            nowLabel.text = ModTranslation.GetString("Draft-Text", 4);
            nowValue.color = new Color(0.2f, 1f, 0.5f);
        }
        else
        {
            nowLabel.text = ModTranslation.GetString("Draft-Text", 3);
            nowValue.color = new Color(1f, 0.85f, 0.1f);
        }

        nowValue.text = picker != null && picker.Data != null ? picker.Data.PlayerName : "?";

        var showPicked = myState != null && myState.HasPicked && myState.ChosenRoleId != 0;
        picked.gameObject.SetActive(showPicked);
        if (showPicked)
        {
            var name = TorOptions.RoleName((RoleId)myState.ChosenRoleId);
            if (RoleInfo.roleInfoById.TryGetValue((RoleId)myState.ChosenRoleId, out var info))
                name = Helpers.cs(info.color, name);
            picked.text = $"{ModTranslation.GetString("Draft-Text", 8)} <b>{name}</b>";
        }

        var feed = string.Join("\n", DraftManager.Feed);
        if (feed != lastFeed)
        {
            feedText.text = feed;
            lastFeed = feed;
        }
    }

    public static void Tick()
    {
        if (DraftManager.IsDraftActive && !root)
        {
            Show();
            if (!root) return;
        }

        if (!root || !content) return;

        tickNumberScramble();

        if (!content.activeSelf) return;

        var state = DraftManager.GetStateForSlot(DraftManager.CurrentSlot);
        var text = "";
        if (state != null && !state.HasPicked)
        {
            var seconds = Mathf.CeilToInt(DraftManager.TimeLeft);
            text = seconds <= 5
                ? $"<color=#FF5555><b>{seconds}</b></color>"
                : $"<color=#FFD700><b>{seconds}</b></color>";
        }

        if (text == lastTimer) return;
        timer.text = text;
        lastTimer = text;
    }

    private static void build()
    {
        var hud = HudManager.Instance;
        if (!hud) return;
        DraftUi.Prepare();

        var view = DraftUi.Viewport;
        var halfW = view.x * 0.5f;
        var halfH = view.y * 0.5f;
        var colW = DraftUi.ColumnWidth(view);
        var colX = halfW - DraftUi.Margin - colW * 0.5f;

        root = new GameObject("TorDraftOverlay");
        root.layer = DraftUi.Layer;
        root.transform.SetParent(hud.transform, false);
        root.transform.localPosition = new Vector3(0f, 0f, DraftUi.Depth);
        root.transform.localScale = Vector3.one;
        DraftUi.CreateBackdrop(root.transform, DraftUi.Order);

        content = new GameObject("Content");
        content.layer = DraftUi.Layer;
        content.transform.SetParent(root.transform, false);
        content.transform.localPosition = new Vector3(0f, 0f, -0.5f);

        var title = DraftUi.CreateText(content.transform, ModTranslation.GetString("Draft-Text", 1),
            new Vector3(0f, halfH - 0.45f, 0f), new Vector2(9f, 0.75f), 4.2f, TextAlignmentOptions.Center,
            DraftUi.Order + 2, true, 2f);
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(1f, 0.82f, 0.15f);

        // The countdown sits up here on purpose: it has to stay readable while the cards are out.
        timer = DraftUi.CreateText(content.transform, "",
            new Vector3(0f, halfH - 1.35f, 0f), new Vector2(6f, 0.75f), 4.5f, TextAlignmentOptions.Center,
            DraftUi.Order + 2, true, 2f);

        nowLabel = DraftUi.CreateText(content.transform, ModTranslation.GetString("Draft-Text", 3),
            new Vector3(0f, 0.85f, 0f), new Vector2(6f, 0.4f), 1.7f, TextAlignmentOptions.Center,
            DraftUi.Order + 2, false);
        nowLabel.color = new Color(1f, 0.85f, 0.1f);

        nowValue = DraftUi.CreateText(content.transform, "?",
            new Vector3(0f, 0.2f, 0f), new Vector2(6f, 0.7f), 3f, TextAlignmentOptions.Center,
            DraftUi.Order + 2, true, 1.5f);
        nowValue.fontStyle = FontStyles.Bold;

        var numberLabel = DraftUi.CreateText(content.transform, ModTranslation.GetString("Draft-Text", 2),
            new Vector3(colX, halfH - 1.35f, 0f), new Vector2(colW, 0.4f), 1.7f, TextAlignmentOptions.Center,
            DraftUi.Order + 2, false);
        numberLabel.color = new Color(0.6f, 0.9f, 1f);

        yourNumberValue = DraftUi.CreateText(content.transform, "?",
            new Vector3(colX, 0.9f, 0f), new Vector2(colW, 0.9f), 5f, TextAlignmentOptions.Center,
            DraftUi.Order + 2, true, 3f);
        yourNumberValue.fontStyle = FontStyles.Bold;

        picked = DraftUi.CreateText(content.transform, "",
            new Vector3(colX, 0.25f, 0f), new Vector2(colW, 0.55f), 2.4f, TextAlignmentOptions.Center,
            DraftUi.Order + 2, true, 1.4f);
        picked.gameObject.SetActive(false);

        var feedHeading = DraftUi.CreateText(content.transform, ModTranslation.GetString("Draft-Text", 7),
            new Vector3(-colX, halfH - 1.35f, 0f), new Vector2(colW, 0.4f), 1.7f, TextAlignmentOptions.Center,
            DraftUi.Order + 2, false);
        feedHeading.color = new Color(0.6f, 0.9f, 1f);

        feedText = DraftUi.CreateText(content.transform, "",
            new Vector3(-colX, halfH - 3.1f, 0f), new Vector2(colW, 2.9f), 1.6f, TextAlignmentOptions.TopLeft,
            DraftUi.Order + 2, true, 1f);
    }

    private static void hideLobbyPanel()
    {
        var startManager = GameStartManager.Instance;
        if (!startManager) return;
        startManagerGo = startManager.gameObject;
        startManagerGo.SetActive(false);
    }

    private static void restoreLobbyPanel()
    {
        if (startManagerGo) startManagerGo.SetActive(true);
        startManagerGo = null;
    }
}
