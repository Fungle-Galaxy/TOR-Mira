using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MiraAPI;
using MiraAPI.Events;
using MiraAPI.Events.Mira;
using MiraAPI.Hud;
using MiraAPI.LocalSettings;
using Reactor.Utilities.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TheOtherRoles.Patches;

// From LasMonjas https://github.com/KiraYamato94/LasMonjas
public enum RoleInfoTeam
{
    Impostor,
    Neutral,
    Crewmate,
    Modifier
}

public static class LobbyRoleInfo
{
    private static readonly Color NeutralTeamColor = new Color32(76, 84, 78, 255);
    private static readonly Color ModifierTeamColor = new Color32(255, 158, 28, 255);
    private static readonly Color MyRoleTeamColor = new Color32(255, 235, 150, 255);

    private static readonly RoleInfoTeam MyRoleTeam = (RoleInfoTeam)99;
    private static readonly RoleInfoTeam SummaryTeam = (RoleInfoTeam)98;

    private const int MaxRolePlates = 20;
    private const int RolesPerRow = 4;

    private static int uiLayer;
    private static string uiSortingLayer = "Default";
    private static int uiOrder;
    private static float modalLocalZ;

    private static GameObject screenRoot;
    private static Action currentScreen;
    private static readonly Stack<Action> history = new();

    /// <summary>
    /// The entry button for the role guide. It is a clone of the vanilla map button parked in Mira
    /// API's extra top right row - the same shape, size and grid cell Town Of Us Mira gives its
    /// wiki button - skinned with our own artwork.
    /// </summary>
    public static GameObject RoleInfoButton;

    private static GameObject oldVanillaWikiButton;
    private static bool lastInMatch;

    private static bool roleInfosEnsured;

    private static Sprite TeamBackground => TorAssets.TeamBackground.LoadAsset();
    private static Sprite MenuBackground => TorAssets.MenuBackground.LoadAsset();

    private static List<(RoleInfoTeam team, string label)> GetEntries()
    {
        var entries = new List<(RoleInfoTeam, string)>
        {
            (RoleInfoTeam.Impostor, Helpers.cs(Palette.ImpostorRed, ModTranslation.GetString("CustomOption-Text", 3))),
            (RoleInfoTeam.Neutral, Helpers.cs(NeutralTeamColor, ModTranslation.GetString("CustomOption-Text", 4))),
            (RoleInfoTeam.Crewmate, Helpers.cs(Palette.CrewmateBlue, ModTranslation.GetString("CustomOption-Text", 5))),
            (RoleInfoTeam.Modifier, Helpers.cs(ModifierTeamColor, ModTranslation.GetString("CustomOption-Text", 6))),
        };
        if (ShipStatus.Instance)
            entries.Add((MyRoleTeam, Helpers.cs(MyRoleTeamColor, ModTranslation.GetString("RoleInfo-Text", 4))));
        else if (!string.IsNullOrEmpty(Helpers.previousEndGameSummary))
            entries.Add((SummaryTeam, Helpers.cs(MyRoleTeamColor, ModTranslation.GetString("RoleInfo-Text", 31))));
        return entries;
    }

    private static string GetTeamLabel(RoleInfoTeam team)
    {
        if (team == MyRoleTeam) return Helpers.cs(MyRoleTeamColor, ModTranslation.GetString("RoleInfo-Text", 4));
        if (team == SummaryTeam) return Helpers.cs(MyRoleTeamColor, ModTranslation.GetString("RoleInfo-Text", 31));
        return GetEntries().Find(t => t.team == team).label;
    }

    private static List<RoleInfo> GetMyRoles()
    {
        EnsureRoleInfos();
        var result = new List<RoleInfo>();
        var p = PlayerControl.LocalPlayer;
        if (p == null || p.Data == null) return result;
        try
        {
            var infos = CustomRoleManager.getRoleInfoForPlayer(p);
            if (infos != null) result.AddRange(infos);
        }
        catch
        {
        }
        return result;
    }

    private static void EnsureRoleInfos()
    {
        if (roleInfosEnsured) return;
        roleInfosEnsured = true;
        try
        {
            foreach (var type in typeof(CustomRoleManager).Assembly.GetTypes())
            {
                try
                {
                    foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (field.FieldType != typeof(RoleInfo)) continue;
                        try
                        {
                            field.GetValue(null);
                        }
                        catch
                        {
                            // ignore
                        }

                        break;
                    }
                }
                catch
                {
                    // ignore types that cannot be initialized
                }
            }
        }
        catch
        {
            // ignore
        }
    }

    private static RoleInfoTeam GetTeam(RoleInfo info)
    {
        if (info.isModifier) return RoleInfoTeam.Modifier;
        if (info.isNeutral) return RoleInfoTeam.Neutral;
        if (info.isImpostor) return RoleInfoTeam.Impostor;
        return RoleInfoTeam.Crewmate;
    }

    private static List<RoleInfo> GetRoles(RoleInfoTeam team)
    {
        EnsureRoleInfos();
        var result = new List<RoleInfo>();
        foreach (RoleId id in Enum.GetValues(typeof(RoleId)))
        {
            if (!RoleInfo.roleInfoById.TryGetValue(id, out var info)) continue;
            if (GetTeam(info) != team) continue;
            result.Add(info);
        }
        return result;
    }

    private static void PrepareUi()
    {
        var hud = HudManager.Instance;
        uiLayer = hud.SettingsButton.layer;
        SpriteRenderer sr = null;
        var mapButton = hud.MapButton;
        if (mapButton)
        {
            sr = mapButton.GetComponent<SpriteRenderer>();
            if (!sr) sr = mapButton.GetComponentInChildren<SpriteRenderer>(true);
        }
        if (!sr) sr = hud.SettingsButton.GetComponentInChildren<SpriteRenderer>(true);
        if (sr)
        {
            uiSortingLayer = sr.sortingLayerName;
            uiOrder = 32000;
        }
        else
        {
            uiSortingLayer = "Default";
            uiOrder = 32000;
        }

        modalLocalZ = -30f;
    }

    private static void StyleRenderer(Renderer renderer, int order)
    {
        renderer.sortingLayerName = uiSortingLayer;
        renderer.sortingOrder = order;
    }

    private static GameObject CreateContainer(Sprite background, float width, float height, float yOffset = 0f)
    {
        PrepareUi();

        var go = new GameObject("RoleInfoScreen");
        go.layer = uiLayer;
        go.transform.SetParent(HudManager.Instance.transform, false);
        go.transform.localPosition = new Vector3(0f, yOffset, modalLocalZ);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        var bgGo = new GameObject("Background");
        bgGo.layer = uiLayer;
        bgGo.transform.SetParent(go.transform, false);
        bgGo.transform.localRotation = Quaternion.identity;
        var sprite = background;
        bgGo.transform.localScale = new Vector3(width / sprite.bounds.size.x, height / sprite.bounds.size.y, 1f);
        var sr = bgGo.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = Color.white;
        StyleRenderer(sr, uiOrder);

        screenRoot = go;
        return go;
    }

    private static TextMeshPro CreateText(Transform parent, string text, Vector3 localPosition, Vector2 size,
        float fontSize, TextAlignmentOptions alignment, bool autoSize = true, float minFontSize = 1.5f)
    {
        var tmp = Object.Instantiate(HudManager.Instance.KillButton.cooldownTimerText, parent);
        tmp.gameObject.SetActive(true);
        tmp.gameObject.layer = uiLayer;
        tmp.transform.localPosition = localPosition;
        tmp.transform.localRotation = Quaternion.identity;
        tmp.transform.localScale = Vector3.one;

        var rt = tmp.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;

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

        if (tmp.TryGetComponent<MeshRenderer>(out var meshRenderer)) StyleRenderer(meshRenderer, uiOrder + 2);
        tmp.text = text;
        return tmp;
    }

    private static PassiveButton CreatePlate(Transform parent, Vector3 localPosition, Vector2 size, Sprite sprite,
        string label, float fontSize, Action onClick)
    {
        var go = new GameObject("Plate");
        go.layer = uiLayer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        var spriteGo = new GameObject("Sprite");
        spriteGo.layer = uiLayer;
        spriteGo.transform.SetParent(go.transform, false);
        spriteGo.transform.localRotation = Quaternion.identity;
        var baseScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1f);
        spriteGo.transform.localScale = baseScale;
        var sr = spriteGo.AddComponent<SpriteRenderer>();
        var baseSprite = sprite;
        var baseColor = Color.white;
        sr.sprite = baseSprite;
        sr.color = baseColor;
        StyleRenderer(sr, uiOrder + 1);

        var collider = go.AddComponent<BoxCollider2D>();
        collider.size = size;

        if (!string.IsNullOrEmpty(label))
            CreateText(go.transform, label, new Vector3(0f, 0f, -0.1f), new Vector2(size.x * 0.9f, size.y * 0.8f),
                fontSize, TextAlignmentOptions.Center, true, 1f);

        var button = go.AddComponent<PassiveButton>();
        button.OnClick = new Button.ButtonClickedEvent();
        button.OnClick.AddListener((Action)(() => onClick()));
        button.OnMouseOver = new UnityEvent();
        button.OnMouseOut = new UnityEvent();
        button.OnMouseOver.AddListener((Action)(() =>
        {
            var hovered = HoverPlateSprite;
            if (!hovered) return;
            sr.sprite = hovered;
            sr.transform.localScale = new Vector3(size.x / hovered.bounds.size.x, size.y / hovered.bounds.size.y, 1f);
        }));
        button.OnMouseOut.AddListener((Action)(() =>
        {
            sr.sprite = baseSprite;
            sr.transform.localScale = baseScale;
            sr.color = baseColor;
        }));
        return button;
    }

    private static Sprite PlateSprite => TorAssets.PlateSprite.LoadAsset();

    private static Sprite hoverPlateSprite;

    private static Sprite HoverPlateSprite =>
        hoverPlateSprite ??= TorAssets.HoverPlateSprite.LoadAsset();

    private static void Show(Action screen, bool pushBack)
    {
        if (screenRoot) Object.Destroy(screenRoot);
        screenRoot = null;
        if (pushBack && currentScreen != null) history.Push(currentScreen);
        currentScreen = screen;
        screen();
    }

    public static void Back()
    {
        if (screenRoot == null) return;
        if (history.Count == 0)
        {
            CloseAll();
            return;
        }
        var screen = history.Pop();
        Object.Destroy(screenRoot);
        screenRoot = null;
        currentScreen = screen;
        screen();
    }

    public static void CloseAll()
    {
        if (screenRoot) Object.Destroy(screenRoot);
        screenRoot = null;
        history.Clear();
        currentScreen = null;
    }

    public static void Open()
    {
        if (!HudManager.Instance) return;
        history.Clear();
        currentScreen = null;
        Show(ShowTeamScreen, false);
    }

    private static void ShowTeamScreen()
    {
        var container = CreateContainer(TeamBackground, 8.2f, 5.5f, -0.2f);
        var plate = PlateSprite;

        CreateText(container.transform, ModTranslation.GetString("CustomOption-Text", 10),
            new Vector3(0f, 2.2f, -1f), new Vector2(6.5f, 0.6f), 5f, TextAlignmentOptions.Center);

        CreatePlate(container.transform, new Vector3(3.45f, 2.2f, -2f), new Vector2(1.1f, 0.4f), plate,
            ModTranslation.GetString("RoleInfo-Text", 2), 2.4f, CloseAll);

        var teams = GetEntries();
        for (var i = 0; i < teams.Count; i++)
        {
            var team = teams[i].team;
            var label = teams[i].label;
            var y = 1.55f - i * 0.79f;
            CreatePlate(container.transform, new Vector3(0f, y, -2f), new Vector2(3f, 0.675f), plate,
                label, 3f, () => Show(() => ShowEntry(team), true));
        }
    }

    private static void ShowEntry(RoleInfoTeam team)
    {
        if (team == MyRoleTeam)
        {
            ShowMyRoleCard();
            return;
        }
        if (team == SummaryTeam)
        {
            ShowSummaryCard();
            return;
        }
        ShowRoleList(team);
    }

    private static void ShowSummaryCard()
    {
        ShowCard(ModTranslation.GetString("RoleInfo-Text", 31), "", Helpers.previousEndGameSummary,
            bodyY: -0.25f, bodyHeight: 2.8f, bodyMinFontSize: 0.9f);
    }

    private static void ShowMyRoleCard()
    {
        var infos = GetMyRoles();
        if (infos.Count == 0)
        {
            ShowCard(ModTranslation.GetString("RoleInfo-Text", 4),
                "",
                ModTranslation.GetString("RoleInfo-Text", 5));
            return;
        }

        var main = infos.Find(x => !x.isModifier);
        if (main == null) main = infos[0];
        var modifiers = infos.FindAll(x => x.isModifier);
        var body = GetRoleBody(main);
        if (modifiers.Count > 0)
        {
            var lines = new List<string>
            {
                Helpers.cs(ModifierTeamColor, $"<b>{ModTranslation.GetString("CustomOption-Text", 6)}</b>")
            };
            foreach (var m in modifiers)
                lines.Add(Helpers.cs(m.color, m.name) + ": " + GetRoleBody(m));
            lines.Add("");
            lines.Add(body);
            body = string.Join("\n", lines);
        }

        ShowCard(Helpers.cs(main.color, main.name), main.introDescription, body,
            bodyY: -0.25f, bodyHeight: 2.8f, bodyMinFontSize: 1.05f);
    }

    private static void ShowRoleList(RoleInfoTeam team)
    {
        var container = CreateContainer(MenuBackground, 7.7f, 5.5f);
        var plate = PlateSprite;

        CreateText(container.transform, GetTeamLabel(team), new Vector3(0f, 2.1f, -1f), new Vector2(4.4f, 0.6f),
                4.5f, TextAlignmentOptions.Center);

        CreatePlate(container.transform, new Vector3(-2.55f, 2.1f, -2f), new Vector2(1f, 0.4f), plate,
            ModTranslation.GetString("RoleInfo-Text", 1), 2.4f, Back);
        CreatePlate(container.transform, new Vector3(2.55f, 2.1f, -2f), new Vector2(1f, 0.4f), plate,
            ModTranslation.GetString("RoleInfo-Text", 2), 2.4f, CloseAll);

        var roles = GetRoles(team);
        var count = Mathf.Min(roles.Count, MaxRolePlates);
        for (var i = 0; i < count; i++)
        {
            var role = roles[i];
            var row = i / RolesPerRow;
            var col = i % RolesPerRow;
            var pos = new Vector3(-2.52f + col * 1.68f, 1.55f - row * 0.473f, -2f);
            CreatePlate(container.transform, pos, new Vector2(1.6f, 0.393f), plate, role.name, 2.4f,
                () => Show(() => ShowCard(role), true));
        }
    }

    private static string GetRoleBody(RoleInfo role)
    {
        var rawDescription = ModTranslation.GetString("Role-Desc", (int)role.roleId);
        return string.IsNullOrEmpty(rawDescription)
            ? role.shortDescription
            : Helpers.cs(Color.white, rawDescription);
    }

    private static void ShowCard(RoleInfo role)
    {
        ShowCard(role.name, role.introDescription, GetRoleBody(role));
    }

    private static void ShowCard(string title, string intro, string body, float bodyY = -0.3f,
        float bodyHeight = 2.6f, float bodyMinFontSize = 1.2f)
    {
        var container = CreateContainer(TorAssets.SummaryScreen.LoadAsset(),6.6f, 4.9f);
        var plate = PlateSprite;

        CreateText(container.transform, title, new Vector3(0f, 1.85f, -1f), new Vector2(5.4f, 0.6f), 5f,
            TextAlignmentOptions.Center);
        CreateText(container.transform, intro, new Vector3(0f, 1.4f, -1f), new Vector2(5.6f, 0.4f), 2.1f,
            TextAlignmentOptions.Center);
        CreateText(container.transform, body, new Vector3(0f, bodyY, -1f), new Vector2(5.9f, bodyHeight), 1.9f,
            TextAlignmentOptions.TopLeft, true, bodyMinFontSize);

        CreatePlate(container.transform, new Vector3(-2.35f, -1.9f, -2f), new Vector2(1.1f, 0.4f), plate,
            ModTranslation.GetString("RoleInfo-Text", 1), 2.4f, Back);
        CreatePlate(container.transform, new Vector3(2.35f, -1.9f, -2f), new Vector2(1.1f, 0.4f), plate,
            ModTranslation.GetString("RoleInfo-Text", 2), 2.4f, CloseAll);
    }

    private static bool CanShowButton =>
        AmongUsClient.Instance != null && PlayerControl.LocalPlayer != null &&
        (LobbyBehaviour.Instance != null || ShipStatus.Instance != null);

    /// <summary>
    /// Builds the entry button the same way Town Of Us Mira builds its wiki button: clone the
    /// vanilla map button - it already ships the Inactive / Active sprites and a
    /// <see cref="PassiveButton"/> - drop it into Mira API's extra top right row and skin it.
    /// </summary>
    private static void CreateRoleInfoButton(HudManager hud)
    {
        if (RoleInfoButton || !hud || !MiraHudHelper.ExtraUiTopRight) return;

        RoleInfoButton = Object.Instantiate(hud.MapButton.gameObject, MiraHudHelper.ExtraUiTopRight.transform);
        RoleInfoButton.name = "RoleInfoButton";

        var button = RoleInfoButton.GetComponent<PassiveButton>();
        button.OnClick = new Button.ButtonClickedEvent();
        button.OnClick.AddListener((UnityAction)(() =>
        {
            if (Minigame.Instance) return;
            if (screenRoot == null) Open();
            else Back();
        }));

        var inactive = RoleInfoButton.transform.Find("Inactive");
        if (inactive)
        {
            inactive.GetComponent<SpriteRenderer>().sprite = TorAssets.RoleInfoButtonSprite.LoadAsset();
            inactive.localPosition = new Vector3(0f, 0.021f, -0.1f);
        }

        var active = RoleInfoButton.transform.Find("Active");
        if (active)
        {
            active.GetComponent<SpriteRenderer>().sprite = TorAssets.RoleInfoButtonActiveSprite.LoadAsset();
            active.localPosition = new Vector3(0f, 0.021f, -0.1f);
        }

        // On the map the button keeps itself in the corner; inside a grid row that would fight the
        // layout, so it goes away - exactly as it does for Town Of Us Mira's wiki button.
        var aspect = RoleInfoButton.GetComponentInChildren<AspectPosition>();
        if (aspect) aspect.Destroy();

        // Hand the slot over the way TOU Mira does: Mira API's own wiki button stops being laid out
        // and ours takes its place in the row.
        oldVanillaWikiButton = MiraHudHelper.VanillaMatchInfoButton;
        MiraHudHelper.VanillaMatchInfoButton = null;
        MiraApiSettings.SetUpButtonPositions();
    }

    /// <summary>Mira API rebuilds the top right rows from scratch; ours has to survive that.</summary>
    [RegisterEvent]
    public static void ResetRoleInfoButton(UiButtonResetEvent @event)
    {
        if (RoleInfoButton) RoleInfoButton.transform.SetParent(null);
    }

    [RegisterEvent(-900)]
    public static void PlaceRoleInfoButton(UiButtonPostResetEvent @event)
    {
        if (!RoleInfoButton) return;
        var bottomRow = LocalSettingsTabSingleton<MiraApiSettings>.Instance.WikiOnBottomRow.Value;
        RoleInfoButton.transform.SetParent(
            bottomRow ? @event.SecondTopUiRow.transform : @event.MainTopUiRow.transform);
    }

    public static class HudManagerRoleInfoUpdate
    {
        public static void Postfix(HudManager __instance)
        {
            if (AmongUsClient.Instance == null || HudManager.Instance == null) return;

            if (!RoleInfoButton) CreateRoleInfoButton(__instance);

            if (!CanShowButton || PlayerControl.LocalPlayer == null)
            {
                if (RoleInfoButton) RoleInfoButton.SetActive(false);
                CloseAll();
                return;
            }

            var inMatch = ShipStatus.Instance != null;
            if (inMatch != lastInMatch)
            {
                lastInMatch = inMatch;
                CloseAll();
            }

            if (oldVanillaWikiButton) oldVanillaWikiButton.SetActive(false);

            if (RoleInfoButton) RoleInfoButton.SetActive(!GameSettingMenu.Instance);

            if ((MapBehaviour.Instance && MapBehaviour.Instance.IsOpen) || MeetingHud.Instance ||
                ExileController.Instance)
                CloseAll();
        }
    }
}
