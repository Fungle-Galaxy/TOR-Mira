using System;
using AmongUs.Data;
using Assets.InnerNet;
using HarmonyLib;
using Il2CppSystem.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using static UnityEngine.UI.Button;
using Object = UnityEngine.Object;

namespace TheOtherRoles.Patches;

[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
public class MainMenuPatch
{
    private static AnnouncementPopUp popUp;

    private static void Prefix(MainMenuManager __instance)
    {
        // Force Reload of SoundEffectHolder
        SoundEffectsManager.Load();

        var template2 = GameObject.Find("CreditsButton");
        if (template2 == null) return;

        template2.transform.localScale = new Vector3(0.42f, 0.84f, 0.84f);
        template2.GetComponent<AspectPosition>().anchorPoint = new Vector2(0.378f, 0.5f);
        template2.transform.FindChild("FontPlacer").transform.localScale = new Vector3(1.8f, 0.9f, 0.9f);
        template2.transform.FindChild("FontPlacer").transform.localPosition = new Vector3(-1.1f, 0f, 0f);


        // TOR credits button
        if (template2 == null) return;
        var creditsButton = Object.Instantiate(template2, template2.transform.parent);

        creditsButton.transform.localScale = new Vector3(0.42f, 0.84f, 0.84f);
        creditsButton.GetComponent<AspectPosition>().anchorPoint = new Vector2(0.462f, 0.5f);

        var textCreditsButton = creditsButton.transform.GetComponentInChildren<TMP_Text>();
        __instance.StartCoroutine(Effects.Lerp(0.5f,
            new Action<float>(p => { textCreditsButton.SetText("TOR Credits"); })));
        var passiveCreditsButton = creditsButton.GetComponent<PassiveButton>();

        passiveCreditsButton.OnClick = new ButtonClickedEvent();

        passiveCreditsButton.OnClick.AddListener((Action)delegate
        {
            // do stuff
            if (popUp != null) Object.Destroy(popUp);
            var popUpTemplate = Object.FindObjectOfType<AnnouncementPopUp>(true);
            if (popUpTemplate == null)
            {
                TheOtherRolesPlugin.Logger.LogError("couldnt show credits, popUp is null");
                return;
            }

            popUp = Object.Instantiate(popUpTemplate);

            popUp.gameObject.SetActive(true);
            var creditsString = @"<align=""center""><b>Fungle-Galaxy Team:</b>
FangkuaiYa    HayashiUme    Farewell

<b>TheOtherRolesAU Contributors:</b>
Mallöris    K3ndo    Bavari    Gendelo
Eisbison (GOAT)    Thunderstorm584    EndOfFile
EnoPM    twix    NesTT
Alex2911    amsyarasyiq    MaximeGillot
Psynomit    probablyadnf    JustASysAdmin

Thanks to miniduikboot & GD for hosting modded servers (and so much more)

";
            creditsString += @"<size=60%> <b>Other Credits & Resources:</b>
OxygenFilter - For the versions v2.3.0 to v2.6.1, we were using the OxygenFilter for automatic deobfuscation
Reactor - The framework used for all versions before v2.0.0, and again since 4.2.0
BepInEx - Used to hook game functions
Essentials - Custom game options by DorCoMaNdO:
Before v1.6: We used the default Essentials release
v1.6-v1.8: We slightly changed the default Essentials.
v2.0.0 and later: As we were not using Reactor anymore, we are using our own implementation, inspired by the one from DorCoMaNdO
Jackal and Sidekick - Original idea for the Jackal and Sidekick came from Dhalucard
Among-Us-Love-Couple-Mod - Idea for the Lovers modifier comes from Woodi-dev
Jester - Idea for the Jester role came from Maartii
ExtraRolesAmongUs - Idea for the Engineer and Medic role came from NotHunter101. Also some code snippets from their implementation were used.
Among-Us-Sheriff-Mod - Idea for the Sheriff role came from Woodi-dev
TooManyRolesMods - Idea for the Detective and Time Master roles comes from Hardel-DW. Also some code snippets from their implementation were used.
TownOfUs - Idea for the Swapper, Shifter, Arsonist and a similar Mayor role came from Slushiegoose
Ottomated - Idea for the Morphling, Snitch and Camouflager role came from Ottomated
Crowded-Mod - Our implementation for 10+ player lobbies was inspired by the one from the Crowded Mod Team
Goose-Goose-Duck - Idea for the Vulture role came from Slushiegoose
TheEpicRoles - Idea for the first kill shield (partly) and the (old) tabbed option menu (fully + some code), by LaicosVK DasMonschta Nova
ugackMiner53 - Idea and core code for the Prop Hunt game mode
Role Draft Music: [https://www.youtube.com/watch?v=9STiQ8cCIo0]Unreal Superhero 3 by Kenët & Rez[]
MiraAPI - The framework used for TOR-Mira

License: TheOtherRoles is licensed under the [https://github.com/TheOtherRolesAU/TheOtherRoles?tab=GPL-3.0-1-ov-file#readme]GPLv3[]
</size>";
            creditsString += "</align>";

            Announcement creditsAnnouncement = new()
            {
                Id = "torCredits",
                Language = 0,
                Number = 500,
                Title = "The Other Roles: Mira\nCredits & Resources",
                ShortTitle = "TOR Credits",
                SubTitle = "",
                PinState = false,
                Date = "10.02.2026",
                Text = creditsString
            };
            __instance.StartCoroutine(Effects.Lerp(0.1f, new Action<float>(p =>
            {
                if (p == 1)
                {
                    var backup = DataManager.Player.Announcements.allAnnouncements;
                    DataManager.Player.Announcements.allAnnouncements = new List<Announcement>();
                    popUp.Init(false);
                    DataManager.Player.Announcements.SetAnnouncements(new[] { creditsAnnouncement });
                    popUp.CreateAnnouncementList();
                    popUp.UpdateAnnouncementText(creditsAnnouncement.Number);
                    popUp.visibleAnnouncements._items[0].PassiveButton.OnClick.RemoveAllListeners();
                    DataManager.Player.Announcements.allAnnouncements = backup;
                }
            })));
        });
    }
}

[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Awake))]
public static class MainMenuCoffeeButtonPatch
{
    public static void Postfix(MainMenuManager __instance)
    {
        var go = new GameObject("CoffeeButton");
        go.transform.SetParent(__instance.transform, false);
        go.transform.localPosition = Vector3.zero;
        go.layer = LayerMask.NameToLayer("UI");

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = TorAssets.CoffeeButton.LoadAsset();
        sr.sortingOrder = 32767;

        var aspect = go.AddComponent<AspectPosition>();
        aspect.Alignment = AspectPosition.EdgeAlignments.RightBottom;
        aspect.parentCam = HudManager.InstanceExists ? HudManager.Instance.UICamera : Camera.main;
        aspect.DistanceFromEdge = new Vector3(0.34f, 1.6f, -6f);
        aspect.AdjustPosition();

        var button = go.AddComponent<PassiveButton>();
        button.OnClick = new ButtonClickedEvent();
        button.OnClick.AddListener((Action)(() =>
        {
            var url = Helpers.isChinese()
                ? "https://amongusclub.cn/archives/co-fi"
                : "https://ko-fi.com/fangkuaiya";
            Constants.OpenURL(url);
        }));
        button.OnMouseOver = new UnityEvent();
        button.OnMouseOver.AddListener((Action)(() => sr.color = Color.green));
        button.OnMouseOut = new UnityEvent();
        button.OnMouseOut.AddListener((Action)(() => sr.color = Color.white));

        // Collider for click detection
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.25f;
    }
}
