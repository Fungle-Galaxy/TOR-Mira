using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.GameModes;
using MiraAPI.Hud;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using Reactor.Utilities.Extensions;
using TheOtherRoles.Buttons;
using TheOtherRoles.Patches;
using TheOtherRoles.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.Video;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace TheOtherRoles.CustomGameModes;

[HarmonyPatch]
internal class PropHunt
{
    /// <summary>
    /// Live view of "is the Prop Hunt mode the selected one", read straight from Mira API's
    /// <see cref="MiraAPI.GameModes.CustomGameModeManager.ActiveMode"/> instead of being cached in
    /// <c>clearAndReload()</c>. That cache had to be re-seeded every game start by TOR's own
    /// ShareGamemode RPC; this cannot go stale.
    /// </summary>
    public static bool isPropHuntGM => CustomGameModeManager.ActiveMode is TorPropHuntMode;

    public static Dictionary<byte, int> remainingShots = new();
    public static float timer = 20f;
    public static bool timerRunning;
    public static float blackOutTimer;

    public static int numberOfHunters;
    public static float initialBlackoutTime;
    public static float killCooldownHit;
    public static float killCooldownMiss;
    public static float hunterVision;
    public static float propVision;
    public static float revealCooldown = 5f;
    public static float revealDuration = 5f;
    public static float unstuckDuration = 5f;
    public static float unstuckCooldown = 5f;
    public static float revealPunish;

    public static float invisCooldown;
    public static float invisDuration;
    public static float speedboostCooldown;
    public static float speedboostDuration;
    public static float speedboostRatio;

    public static float adminCooldown = 5f;
    public static float adminDuration = 10f;

    public static float findCooldown = 10f;
    public static float findDuration = 10f;

    public static bool enableSpeedboost = true;
    public static bool enableInvis = true;

    public static bool propBecomesHunterWhenFound;
    public static DateTime startTime = DateTime.UtcNow;
    public static TMP_Text timerText;
    public static List<string> whitelistedObjects = new();

    public static Dictionary<byte, Tuple<string, float>> currentObject = new();
    public static Dictionary<byte, float> isCurrentlyRevealed = new();
    private static Dictionary<byte, GameObject> revealRenderer = new();
    public static Dictionary<byte, float> invisPlayers = new();

    public static Dictionary<byte, float> speedboostActive = new();

    public static GameObject currentTarget;
    private static GameObject poolablesBackground;

    public static float dangerMeterActive = 0f;

    private static List<GameObject> duplicatedCollider = new();

    public static void ClearAndReload()
    {
        remainingShots.Clear();
        numberOfHunters = OptionGroupSingleton<PropHuntHunterOptions>.Instance.NumberOfHunters.Quantity();
        initialBlackoutTime = OptionGroupSingleton<PropHuntHunterOptions>.Instance.HunterInitialBlackoutTime.Value;
        propBecomesHunterWhenFound = OptionGroupSingleton<PropHuntPropOptions>.Instance.BecomesHunterWhenFound.Value;
        killCooldownMiss = OptionGroupSingleton<PropHuntHunterOptions>.Instance.HunterMissCooldown.Value;
        killCooldownHit = OptionGroupSingleton<PropHuntHunterOptions>.Instance.HunterHitCooldown.Value;
        hunterVision = OptionGroupSingleton<PropHuntGeneralOptions>.Instance.PropHunterVision.Value;
        propVision = OptionGroupSingleton<PropHuntGeneralOptions>.Instance.PropVision.Value;
        timer = OptionGroupSingleton<PropHuntGeneralOptions>.Instance.Timer.Value * 60;
        revealDuration = OptionGroupSingleton<PropHuntHunterOptions>.Instance.RevealDuration.Value;
        revealCooldown = OptionGroupSingleton<PropHuntHunterOptions>.Instance.RevealCooldown.Value;
        unstuckDuration = OptionGroupSingleton<PropHuntGeneralOptions>.Instance.UnstuckDuration.Value;
        unstuckCooldown = OptionGroupSingleton<PropHuntGeneralOptions>.Instance.UnstuckCooldown.Value;
        revealPunish = OptionGroupSingleton<PropHuntHunterOptions>.Instance.RevealPunish.Value;
        invisCooldown = OptionGroupSingleton<PropHuntInvisibilityOptions>.Instance.InvisCooldown.Value;
        invisDuration = OptionGroupSingleton<PropHuntInvisibilityOptions>.Instance.InvisDuration.Value;
        speedboostCooldown = OptionGroupSingleton<PropHuntSpeedboostOptions>.Instance.SpeedboostCooldown.Value;
        speedboostDuration = OptionGroupSingleton<PropHuntSpeedboostOptions>.Instance.SpeedboostDuration.Value;
        speedboostRatio = OptionGroupSingleton<PropHuntSpeedboostOptions>.Instance.SpeedboostSpeed.Value;
        enableSpeedboost = OptionGroupSingleton<PropHuntSpeedboostOptions>.Instance.SpeedboostEnabled.Value;
        enableInvis = OptionGroupSingleton<PropHuntInvisibilityOptions>.Instance.InvisEnabled.Value;
        adminCooldown = OptionGroupSingleton<PropHuntHunterOptions>.Instance.AdminCooldown.Value;
        findCooldown = OptionGroupSingleton<PropHuntHunterOptions>.Instance.FindCooldown.Value;
        findDuration = OptionGroupSingleton<PropHuntHunterOptions>.Instance.FindDuration.Value;
        timerRunning = false;
        timerText?.Destroy();
        timerText = null;
        currentObject = new Dictionary<byte, Tuple<string, float>>();
        isCurrentlyRevealed = new Dictionary<byte, float>();
        poolablesBackground?.Destroy();
        foreach (var go in revealRenderer.Values) go.Destroy();
        revealRenderer = new Dictionary<byte, GameObject>();
        speedboostActive = new Dictionary<byte, float>();
        invisPlayers = new Dictionary<byte, float>();
        foreach (var go in duplicatedCollider) go.Destroy();
        duplicatedCollider = new List<GameObject>();
    }

    public static void updateWhitelistedObjects(bool debug = false)
    {
        var allNames = Helpers.readTextFromResources("TheOtherRoles.Resources.Txt.Props.txt");
        if (debug) allNames = Helpers.readTextFromFile(Directory.GetCurrentDirectory() + "\\Props.txt");

        whitelistedObjects = allNames.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ToList();
    }


    public static void propTargetAndTimerDisplayUpdate()
    {
        if (!PlayerControl.LocalPlayer.Data.Role.IsImpostor)
            currentTarget = FindClosestDisguiseObject(PlayerControl.LocalPlayer.gameObject, 1f);

        if (timerText == null)
        {
            var roomTracker = FastDestroyableSingleton<HudManager>.Instance?.roomTracker;
            if (roomTracker != null)
            {
                var gameObject = Object.Instantiate(roomTracker.gameObject);

                gameObject.transform.SetParent(FastDestroyableSingleton<HudManager>.Instance.transform);
                Object.DestroyImmediate(gameObject.GetComponent<RoomTracker>());
                timerText = gameObject.GetComponent<TMP_Text>();

                // Use local position to place it in the player's view instead of the world location
                gameObject.transform.localPosition = new Vector3(0, -1.8f, gameObject.transform.localPosition.z);
                if (DataManager.Settings.Gameplay.StreamerMode)
                    gameObject.transform.localPosition = new Vector3(0, 2f, gameObject.transform.localPosition.z);
            }
        }
        else
        {
            if (timerRunning || blackOutTimer > 0f)
            {
                var relevantTimer = timerRunning ? timer : blackOutTimer;
                var minutes = (int)relevantTimer / 60;
                var seconds = (int)relevantTimer % 60;
                var suffix = $" {minutes:00}:{seconds:00}";
                timerText.text = Helpers.cs(timerRunning ? Color.blue : Color.red, suffix);
                timerText.outlineColor = Color.white;
                timerText.outlineWidth = 0.1f;
                timerText.color = timerRunning ? Color.blue : Color.red;
            }
        }

        var disguiseButton = CustomButtonSingleton<PropDisguiseButton>.Instance;
        if (disguiseButton.Timer > disguiseButton.MaxTimer)
            disguiseButton.Timer = disguiseButton.MaxTimer;
    }

    public static void poolablePlayerUpdate()
    {
        if (poolablesBackground == null)
        {
            poolablesBackground = new GameObject("poolablesBackground");
            poolablesBackground.AddComponent<SpriteRenderer>();
            poolablesBackground.layer = LayerMask.NameToLayer("UI");
        }

        poolablesBackground.transform.SetParent(HudManager.Instance.transform);
        poolablesBackground.transform.localPosition = IntroEndEvents.bottomLeft +
                                                      new Vector3(-1.45f, -0.05f, 0) + Vector3.right *
                                                      PlayerControl.AllPlayerControls.Count * 0.2f;
        var backgroundSizeX = PlayerControl.AllPlayerControls.Count * 0.4f + 0.2f;
        poolablesBackground.GetComponent<SpriteRenderer>().sprite = TorAssets.PoolablesBackground.LoadAsset();
        poolablesBackground.transform.localScale = new Vector3(
            poolablesBackground.transform.localScale.x * backgroundSizeX /
            poolablesBackground.GetComponent<SpriteRenderer>().bounds.size.x,
            poolablesBackground.transform.localScale.y, poolablesBackground.transform.localScale.z);

        foreach (var pc in PlayerControl.AllPlayerControls)
        {
            if (!TORMapOptions.playerIcons.ContainsKey(pc.PlayerId)) continue;
            var poolablePlayer = TORMapOptions.playerIcons[pc.PlayerId];
            if (pc.Data.IsDead)
            {
                poolablePlayer.setSemiTransparent(true);
                poolablePlayer.cosmetics.nameText.text = Helpers.cs(Palette.DisabledGrey, pc.Data.PlayerName);
                ;
            }
            else if (pc.Data.Role.IsImpostor)
            {
                poolablePlayer.cosmetics.nameText.text = Helpers.cs(Palette.ImpostorRed, pc.Data.PlayerName);
                poolablePlayer.cosmetics.currentBodySprite.BodySprite.material.SetFloat("_Outline", 2f);
                poolablePlayer.cosmetics.currentBodySprite.BodySprite.material.SetColor("_OutlineColor",
                    Palette.ImpostorRed);
                poolablePlayer.cosmetics.nameText.fontSize = 4;
            }
            else
            {
                // Display Prop
                poolablePlayer.cosmetics.nameText.text = Helpers.cs(Palette.CrewmateBlue, pc.Data.PlayerName);
            }

            // update currently revealed:
            if (isCurrentlyRevealed.ContainsKey(pc.PlayerId))
            {
                if (!revealRenderer.ContainsKey(pc.PlayerId))
                {
                    var go = new GameObject($"reveal_renderer_{pc.PlayerId}");
                    go.layer = LayerMask.NameToLayer("UI");
                    go.AddComponent<SpriteRenderer>();
                    go.transform.SetParent(poolablePlayer.transform.parent, false);
                    go.SetActive(true);
                    go.transform.localPosition = poolablePlayer.transform.localPosition + new Vector3(0, 0, -50f);
                    poolablePlayer.gameObject.SetActive(false);
                    revealRenderer.Add(pc.PlayerId, go);
                }

                var revealTimer = isCurrentlyRevealed[pc.PlayerId] - Time.deltaTime;
                isCurrentlyRevealed[pc.PlayerId] = revealTimer;
                if (revealTimer > 0)
                {
                    // get sprite:
                    if (currentObject.ContainsKey(pc.PlayerId))
                    {
                        revealRenderer[pc.PlayerId].GetComponent<SpriteRenderer>().sprite =
                            pc.GetComponent<SpriteRenderer>().sprite;
                        revealRenderer[pc.PlayerId].transform.localScale *= 0.5f / revealRenderer[pc.PlayerId]
                            .GetComponent<SpriteRenderer>().bounds.size.magnitude;
                    }
                }
                else
                {
                    revealRenderer[pc.PlayerId].Destroy();
                    isCurrentlyRevealed.Remove(pc.PlayerId);
                    revealRenderer.Remove(pc.PlayerId);
                    poolablePlayer.gameObject.SetActive(true);
                    SoundEffectsManager.play("morphlingMorph");
                }
            }
        }
    }

    public static void invisUpdate()
    {
        foreach (var playerId in invisPlayers.Keys)
        {
            var pc = Helpers.playerById(playerId);
            if (pc == null || pc.Data.IsDead) continue;
            var timeLeft = invisPlayers[playerId] - Time.deltaTime;
            invisPlayers[playerId] = timeLeft;
            if (timeLeft > 0)
            {
                pc.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f,
                    PlayerControl.LocalPlayer.Data.IsDead || PlayerControl.LocalPlayer.PlayerId == playerId
                        ? 0.1f
                        : 0f);
            }
            else
            {
                pc.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 1f);
                invisPlayers.Remove(playerId);
            }

            if (isCurrentlyRevealed.ContainsKey(playerId))
                revealRenderer[playerId].GetComponent<SpriteRenderer>().color = pc.GetComponent<SpriteRenderer>().color;
        }
    }

    public static void speedboostUpdate()
    {
        foreach (var key in speedboostActive.Keys)
        {
            var speedboostTimer = speedboostActive[key] - Time.deltaTime;
            speedboostActive[key] = speedboostTimer;
            if (speedboostTimer < 0)
                speedboostActive.Remove(key);
        }
    }

    public static void dangerMeterUpdate()
    {
        if (!HudManager.Instance || !HudManager.Instance.DangerMeter) return;
        if (HudManager.Instance.DangerMeter.gameObject.active)
        {
            var dist = 55f;
            var dist2 = 15f;
            var curr = float.MaxValue;
            try
            {
                foreach (var playerControl in PlayerControl.AllPlayerControls.ToArray().Where(x =>
                             !x.Data.IsDead && (PlayerControl.LocalPlayer.Data.Role.IsImpostor
                                 ? !x.Data.Role.IsImpostor
                                 : x.Data.Role.IsImpostor)))
                {
                    if (invisPlayers.ContainsKey(playerControl.PlayerId))
                        continue; // Dont light up for invisible players
                    if (!(playerControl == null))
                    {
                        var sqrMagnitude =
                            (playerControl.transform.position - PlayerControl.LocalPlayer.transform.position)
                            .sqrMagnitude;
                        if (sqrMagnitude < dist && curr > sqrMagnitude) curr = sqrMagnitude;
                    }
                }
            }
            catch
            {
            }

            var dangerLevel1 = Mathf.Clamp01((dist - curr) / (dist - dist2));
            var dangerLevel2 = Mathf.Clamp01((dist2 - curr) / dist2);
            HudManager.Instance.DangerMeter.SetDangerValue(dangerLevel1, dangerLevel2);
        }

        HudManager.Instance.DangerMeter?.gameObject.SetActive(!PlayerControl.LocalPlayer.Data.IsDead &&
                                                              (!PlayerControl.LocalPlayer.Data.Role.IsImpostor ||
                                                               CustomButtonSingleton<PropHuntFindButton>.Instance
                                                                   .isEffectActive));
    }


    public static void update()
    {
        if (!isPropHuntGM)
        {
            // Make sure the DangerMeter is not displayed in TOR HideNSeek, Classic or Guesser Game mode.
            if (GameOptionsManager.Instance.currentGameOptions.GameMode != GameModes.HideNSeek)
                HudManager.Instance.DangerMeter?.gameObject.SetActive(false);
            return;
        }

        if (timerRunning) timer = Math.Clamp(timer -= Time.deltaTime, 0, timer >= 0 ? timer : 0);
        else if (blackOutTimer > 0f) blackOutTimer -= Time.deltaTime;

        // Local player find prop Target
        propTargetAndTimerDisplayUpdate();

        poolablePlayerUpdate();

        speedboostUpdate();

        invisUpdate();

        dangerMeterUpdate();
    }

    public static void transformLayers()
    {
        // A bit of a hacky way to make sure that props as well as propable objects are not visible in the dark, while keeping collisions enabled.
        PlayerControl.LocalPlayer.clearAllTasks();
        foreach (var collider in Physics2D.OverlapCircleAll(PlayerControl.LocalPlayer.transform.position, 500))
        {
            var whiteListed = false;
            foreach (var whiteListedWord in whitelistedObjects)
                if (collider.gameObject.name.Contains(whiteListedWord) &&
                    collider.gameObject.GetComponent<SpriteRenderer>() != null)
                    whiteListed = true;
            if (collider.GetComponent<Console>() != null || whiteListed)
            {
                if (whiteListed)
                {
                    var newgo = GameObject.Instantiate(collider.gameObject, collider.transform.parent);
                    newgo.name = "DONTUSE";
                    duplicatedCollider.Add(newgo);
                }

                collider.gameObject.layer = PlayerControl.LocalPlayer.gameObject.layer;
            }
        }
    }

    /// <summary>
    /// Nearest disguise target (console or prop) around <paramref name="origin"/>, or
    /// <see langword="null"/> when nothing valid is inside <paramref name="radius"/>.
    ///
    /// "Nothing nearby" is the normal state for a prop standing in the middle of a corridor, and
    /// this runs every frame for the local player - it must not go through the exception path or
    /// the log fills up and every call costs an IL2CPP throw.
    /// </summary>
    public static GameObject FindClosestDisguiseObject(GameObject origin, float radius, bool verbose = false)
    {
        try
        {
            Collider2D bestCollider = null;
            float bestDist = 9999;
            if (whitelistedObjects == null || whitelistedObjects.Count == 0 || verbose)
                updateWhitelistedObjects(verbose);
            foreach (var collider in Physics2D.OverlapCircleAll(origin.transform.position, radius))
            {
                if (verbose) TheOtherRolesPlugin.Logger.LogMessage($"Nearby Object: {collider.gameObject.name}");
                var whiteListed = false;
                foreach (var whiteListedWord in whitelistedObjects)
                    if (collider.gameObject != null && collider.gameObject.name.Contains(whiteListedWord))
                        whiteListed = true;
                if (collider.GetComponent<Console>() != null || whiteListed)
                {
                    var dist = Vector2.Distance(origin.transform.position, collider.transform.position);
                    if (dist < bestDist)
                    {
                        bestCollider = collider;
                        bestDist = dist;
                    }
                }
            }

            // No valid target in range - that is a normal result, not an error.
            return bestCollider == null ? null : bestCollider.gameObject;
        }
        catch (Exception e)
        {
            TheOtherRolesPlugin.Logger.LogError($"Error in find closest disguise object: {e}");
            return null;
        }
    }

    public static GameObject FindPropByNameAndPos(string propName, float posX)
    {
        var candidates = GameObject.FindObjectsOfType<GameObject>();
        GameObject prop = null;
        foreach (var candidate in candidates)
            if (candidate.name == propName && candidate.transform.position.x == posX)
                prop = candidate;
        return prop;
    }

    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.FixedUpdate))]
    [HarmonyPostfix]
    public static void SpeedbostPostfix(PlayerPhysics __instance)
    {
        if (!__instance.AmOwner || !speedboostActive.ContainsKey(__instance.myPlayer.PlayerId)) return;
        if (GameData.Instance && __instance.myPlayer.CanMove)
            __instance.body.velocity *= speedboostRatio;
    }

    [HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.FixedUpdate))]
    [HarmonyPostfix]
    public static void PostfixNetworkSpeed(CustomNetworkTransform __instance)
    {
        if (__instance.AmOwner || !speedboostActive.ContainsKey(__instance.myPlayer.PlayerId)) return;
        if (GameData.Instance && __instance.myPlayer.CanMove)
            __instance.body.velocity *= speedboostRatio;
    }

    [RegisterEvent]
    public static void IntroCutsceneDestroy(IntroEndEvent @event)
    {
        if (!isPropHuntGM || !PlayerControl.LocalPlayer.Data.Role.IsImpostor) return;
        PlayerControl.LocalPlayer.moveable = false;
        PlayerControl.LocalPlayer.RpcPropHuntStartTimer(true);


        // Play mp4 video in Full Screen:
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();
        var resourceBundle = assembly.GetManifestResourceStream("TheOtherRoles.Resources.IntroAnimation.intro");
        var assetBundle = AssetBundle.LoadFromMemory(resourceBundle.ReadFully());
        var introVid = assetBundle.LoadAsset<VideoClip>("Assets/Video/intro.webm");
        var camera = GameObject.Find("Main Camera");
        var videoPlayer = camera.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
        videoPlayer.targetCameraAlpha = 1F;
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = introVid;
        videoPlayer.aspectRatio = VideoAspectRatio.FitVertically;
        // Skip the first 100 frames.
        videoPlayer.frame = (21 - (int)initialBlackoutTime) * 25;
        videoPlayer.isLooping = false;
        videoPlayer.Play();


        FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(initialBlackoutTime + 10f / 25,
            new Action<float>(p =>
            {
                if (p == 1f)
                {
                    // start timer
                    PlayerControl.LocalPlayer.RpcPropHuntStartTimer();
                    PlayerControl.LocalPlayer.moveable = true;
                    HudManager.Instance.FullScreen.enabled = false;
                    videoPlayer.Destroy();
                    assetBundle.Unload(false);
                }
                else
                {
                    HudManager.Instance.FullScreen.enabled = true;
                    HudManager.Instance.FullScreen.gameObject.SetActive(true);
                }
            })));
    }

    public static void PlayerControlFixedUpdatePatch(PlayerControl __instance)
    {
        if (!isPropHuntGM) return;
        if (__instance.Data.Role.IsImpostor)
        {
            __instance.GetComponent<CircleCollider2D>().radius = 0.2234f;
            return;
        }

        if (__instance.GetComponent<SpriteRenderer>() != null || __instance.Data.IsDead) return;

        __instance.gameObject.AddComponent<SpriteRenderer>();
        __instance.GetComponent<CircleCollider2D>().radius = 0.00001f;
    }


    // Runs periodically, resets animation data for players
    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleAnimation))]
    [HarmonyPostfix]
    public static void PlayerPhysicsAnimationPatch(PlayerPhysics __instance)
    {
        if (!AmongUsClient.Instance.IsGameStarted || !isPropHuntGM)
            return;
        try
        {
            if (__instance.GetComponent<SpriteRenderer>().sprite != null && !__instance.myPlayer.Data.Role.IsImpostor)
            {
                __instance.myPlayer.Visible = false;
                __instance.GetComponent<SpriteRenderer>().flipX =
                    __instance.myPlayer.cosmetics.currentBodySprite.BodySprite.flipX;
                __instance.myPlayer.cosmetics.currentPet?.Destroy();
            }

            if (__instance.myPlayer.Data.IsDead)
            {
                __instance.myPlayer.Visible = PlayerControl.LocalPlayer.Data.IsDead;
                GameObject.Destroy(__instance.GetComponent<SpriteRenderer>());
            }
        }
        catch
        {
        }
    }

    public static void MapSetPostfix()
    {
        // Make sure the map in the settings is in sync with the map from li
        if ((!isPropHuntGM && !HideNSeek.isHideNSeekGM) || AmongUsClient.Instance.IsGameStarted) return;
        int? map = GameOptionsManager.Instance?.currentGameOptions?.MapId;
        if (map == null) return;
        if (map > 3) map--;
        if (HideNSeek.isHideNSeekGM)
        {
            var mapOption = OptionGroupSingleton<HideNSeekOptions>.Instance.Map;
            var wanted = Mathf.Clamp((int)map, 0, mapOption.Values.Length - 1);
            if (mapOption.Selection() != wanted) mapOption.SetValue(mapOption.Values[wanted]);
        }

        if (isPropHuntGM)
        {
            var mapOption = OptionGroupSingleton<PropHuntMapOptions>.Instance.Map;
            var wanted = Mathf.Clamp((int)map, 0, mapOption.Values.Length - 1);
            if (mapOption.Selection() != wanted) mapOption.SetValue(mapOption.Values[wanted]);
        }
    }


    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    [HarmonyPostfix]
    public static void MurderPlayerPostfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        if (!isPropHuntGM || target != PlayerControl.LocalPlayer) return;
        try
        {
            target.NetTransform.RpcSnapTo(__instance.transform.position);
        }
        catch
        {
        }
    }

    // Make it so that the kill button doesn't light up when near a player
    [HarmonyPatch(typeof(VentButton), nameof(VentButton.SetTarget))]
    [HarmonyPatch(typeof(KillButton), nameof(KillButton.SetTarget))]
    [HarmonyPostfix]
    public static void KillButtonHighlightPatch(ActionButton __instance)
    {
        if (!isPropHuntGM) return;
        __instance.SetEnabled();
    }


    // Penalize the impostor if there is no prop killed
    [HarmonyPatch(typeof(KillButton), nameof(KillButton.CheckClick))]
    [HarmonyPrefix]
    public static bool CheckClickPatch(KillButton __instance)
    {
        if (!isPropHuntGM) return true;
        __instance.DoClick();
        return false;
    }

    // Penalize the impostor if there is no prop killed
    [HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
    [HarmonyPriority(Priority.First)]
    [HarmonyPrefix]
    public static bool KillButtonClickPatch(KillButton __instance)
    {
        if (!isPropHuntGM) return true;
        KillAnimationCoPerformKillPatch.hideNextAnimation = true; // dont jump out of bounds!
        if (__instance.isCoolingDown || PlayerControl.LocalPlayer.Data.IsDead ||
            PlayerControl.LocalPlayer.inVent) return false;
        var targets = PlayerControl.LocalPlayer.Data.Role
            .GetPlayersInAbilityRangeSorted(RoleBehaviour.GetTempPlayerList(), true).ToArray();
        __instance.SetTarget(PlayerControl.LocalPlayer.Data.Role
            .GetPlayersInAbilityRangeSorted(RoleBehaviour.GetTempPlayerList(), true).ToArray().FirstOrDefault());

        if (__instance.currentTarget == null)
        {
            PlayerControl.LocalPlayer.SetKillTimer(killCooldownMiss);
        }
        else
        {
            // There is a target, execute kill!
            var res = Helpers.checkMurderAttemptAndKill(PlayerControl.LocalPlayer, __instance.currentTarget);
            __instance.SetTarget(null);
            PlayerControl.LocalPlayer.SetKillTimer(killCooldownHit);
        }

        return false;
    }

    [HarmonyPatch(typeof(RoleBehaviour), nameof(RoleBehaviour.IsValidTarget))]
    [HarmonyPrefix]
    public static bool IsValidTarget(RoleBehaviour __instance, NetworkedPlayerInfo target, ref bool __result)
    {
        if (!isPropHuntGM) return true;
        __result = !(target == null) && !target.Disconnected && !target.IsDead &&
                   target.PlayerId != __instance.Player.PlayerId && !(target.Role == null) &&
                   !(target.Object == null) && !target.Object.inVent && !target.Object.inMovingPlat;
        return false;
    }

    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.Show))]
    [HarmonyPrefix]
    public static void MapBehaviourShowPatch(MapBehaviour __instance, ref MapOptions opts)
    {
        if (!isPropHuntGM) return;
        if (opts.Mode == MapOptions.Modes.Sabotage) opts.Mode = MapOptions.Modes.Normal;
    }


    // Disable a lot of stuff
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
    [HarmonyPatch(typeof(Vent), nameof(Vent.SetOutline))]
    [HarmonyPrefix]
    public static bool DisableFunctions()
    {
        if (!isPropHuntGM) return true;
        return false;
    }
}

/// <summary>
/// The prop's disguise button (Prop Hunt). It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>propDisguiseButton</c>.
/// </summary>
public sealed class PropDisguiseButton : TorButton
{
    private static PropDisguiseButton propDisguiseButton;

    public PropDisguiseButton()
    {
        propDisguiseButton = this;

        // The old constructor passed a null sprite - this button never wears an icon of its own,
        // only the prop preview child rendered by propSpriteRenderer.
        SetSprite(TorLazySprite.Wrap(null));
        PositionOffset = TorButtonPositions.LowerRowRight;
        Hotkey = KeyCode.F;
        ShowButtonText = true;

        RealOnClick = () =>
        {
            // Prop stuff
            var player = PlayerControl.LocalPlayer;
            var disguiseTarget = PropHunt.currentTarget;
            if (disguiseTarget != null)
            {
                player.transform.localScale = disguiseTarget.transform.lossyScale;
                PlayerControl.LocalPlayer.RpcPropHuntSetProp(PlayerControl.LocalPlayer.PlayerId,
                    disguiseTarget.gameObject.name, disguiseTarget.gameObject.transform.position.x);
                SoundEffectsManager.play("morphlingMorph");
                propDisguiseButton.Timer = 1f;
            }
        };
        HasButton = () =>
        {
            return PropHunt.isPropHuntGM && !PlayerControl.LocalPlayer.Data.Role.IsImpostor &&
                   !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () =>
        {
            HudManagerStartPatch.propSpriteRenderer.sprite =
                PropHunt.currentTarget?.GetComponent<SpriteRenderer>()?.sprite;
            if (HudManagerStartPatch.propSpriteRenderer.sprite == null)
                HudManagerStartPatch.propSpriteRenderer.sprite = PropHunt.currentTarget?.transform
                    .GetComponentInChildren<SpriteRenderer>()?.sprite;
            if (HudManagerStartPatch.propSpriteRenderer.sprite != null)
                HudManagerStartPatch.propSpriteHolder.transform.localScale *=
                    1 / HudManagerStartPatch.propSpriteRenderer.bounds.size.magnitude;
            return PropHunt.currentTarget != null &&
                   PropHunt.currentTarget?.GetComponent<SpriteRenderer>()?.sprite != null;
        };
        OnMeetingEnds = () => { };
    }

    public override float Cooldown => 1f;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        ButtonText = new ButtonText(40,
            FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.UseButton]
                .FontMaterial);

        if (actionButton == null)
        {
            return;
        }

        // The prop preview lives on a child of the button (the lines that used to follow the
        // new CustomButton(...) block in createButtonsPostfix).
        HudManagerStartPatch.propSpriteHolder = new GameObject("TORPropButtonPropSpritePreview");
        HudManagerStartPatch.propSpriteRenderer =
            HudManagerStartPatch.propSpriteHolder.AddComponent<SpriteRenderer>();
        HudManagerStartPatch.propSpriteHolder.transform.SetParent(actionButton.gameObject.transform, false);
        HudManagerStartPatch.propSpriteHolder.transform.localPosition = new Vector3(0, 0, -2f);
    }
}

/// <summary>
/// The prop's un-stuck button (Prop Hunt). It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>propHuntUnstuckButton</c>.
/// </summary>
public sealed class PropHuntUnstuckButton : TorButton
{
    private static PropHuntUnstuckButton propHuntUnstuckButton;

    public PropHuntUnstuckButton()
    {
        propHuntUnstuckButton = this;

        SetSprite(TorAssets.UnStuck);
        PositionOffset = TorButtonPositions.UpperRowLeft;
        Hotkey = KeyCode.LeftShift;
        EffectEnabled = true;
        ShowButtonText = true;

        RealOnClick = () => { PlayerControl.LocalPlayer.Collider.enabled = false; };
        HasButton = () => { return PropHunt.isPropHuntGM && !PlayerControl.LocalPlayer.Data.IsDead; };
        CouldUse = () => { return true; };
        OnMeetingEnds = () => { };
    }

    public override float Cooldown => PropHunt.unstuckCooldown;

    public override float EffectDuration => PropHunt.unstuckDuration;

    public override void OnEffectEnd()
    {
        PlayerControl.LocalPlayer.Collider.enabled = true;
        propHuntUnstuckButton.Timer = propHuntUnstuckButton.MaxTimer;
    }

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        ButtonText = new ButtonText(41,
            FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.UseButton]
                .FontMaterial);
    }
}

/// <summary>
/// The hunter's reveal button (Prop Hunt). It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>propHuntRevealButton</c>.
/// </summary>
public sealed class PropHuntRevealButton : TorButton
{
    private static PropHuntRevealButton propHuntRevealButton;

    public PropHuntRevealButton()
    {
        propHuntRevealButton = this;

        SetSprite(TorAssets.Reveal);
        PositionOffset = TorButtonPositions.UpperRowFarLeft;
        Hotkey = KeyCode.R;
        EffectEnabled = true;
        ShowButtonText = true;

        RealOnClick = () =>
        {
            // select a random crewplayer to reveal.
            var candidates = PlayerControl.AllPlayerControls.ToArray().Where(x =>
                    !x.Data.Role.IsImpostor && !x.Data.IsDead &&
                    !PropHunt.isCurrentlyRevealed.ContainsKey(x.PlayerId))
                .ToList();
            var rng = new Random();
            var selectedPlayer = candidates[rng.Next(candidates.Count)];
            PlayerControl.LocalPlayer.RpcPropHuntSetRevealed(selectedPlayer.PlayerId);
        };
        HasButton = () =>
        {
            return PropHunt.isPropHuntGM && !PlayerControl.LocalPlayer.Data.IsDead &&
                   PlayerControl.LocalPlayer.Data.Role.IsImpostor;
        };
        CouldUse = () => { return PropHunt.timer - PropHunt.revealPunish > 0; };
        OnMeetingEnds = () => { };
    }

    public override float Cooldown => PropHunt.revealCooldown;

    public override float EffectDuration => PropHunt.revealDuration;

    public override void OnEffectEnd()
    {
        propHuntRevealButton.Timer = propHuntRevealButton.MaxTimer;
    }

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        ButtonText = new ButtonText(42,
            FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.UseButton]
                .FontMaterial);
    }
}

/// <summary>
/// The prop's invisibility button (Prop Hunt). It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>propHuntInvisButton</c>.
/// </summary>
public sealed class PropHuntInvisButton : TorButton
{
    private static PropHuntInvisButton propHuntInvisButton;

    public PropHuntInvisButton()
    {
        propHuntInvisButton = this;

        SetSprite(TorAssets.InvisButton);
        PositionOffset = TorButtonPositions.UpperRowFarLeft;
        Hotkey = KeyCode.I;
        EffectEnabled = true;
        ShowButtonText = true;

        RealOnClick = () =>
        {
            PlayerControl.LocalPlayer.RpcPropHuntSetInvis(PlayerControl.LocalPlayer.PlayerId);
            SoundEffectsManager.play("morphlingMorph");
        };
        HasButton = () =>
        {
            return PropHunt.isPropHuntGM && !PlayerControl.LocalPlayer.Data.IsDead &&
                   !PlayerControl.LocalPlayer.Data.Role.IsImpostor && PropHunt.enableInvis;
        };
        CouldUse = () => { return PropHunt.currentObject.ContainsKey(PlayerControl.LocalPlayer.PlayerId); };
        OnMeetingEnds = () => { };
    }

    public override float Cooldown => PropHunt.invisCooldown;

    public override float EffectDuration => PropHunt.invisDuration;

    public override void OnEffectEnd()
    {
        SoundEffectsManager.play("morphlingMorph");
        propHuntInvisButton.Timer = propHuntInvisButton.MaxTimer;
    }

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        ButtonText = new ButtonText(43,
            FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.UseButton]
                .FontMaterial);
    }
}

/// <summary>
/// The prop's speed boost button (Prop Hunt). It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>propHuntSpeedboostButton</c>.
/// </summary>
public sealed class PropHuntSpeedboostButton : TorButton
{
    private static PropHuntSpeedboostButton propHuntSpeedboostButton;

    public PropHuntSpeedboostButton()
    {
        propHuntSpeedboostButton = this;

        SetSprite(TorAssets.SpeedboostButton);
        PositionOffset = TorButtonPositions.LowerRowCenter;
        Hotkey = KeyCode.G;
        EffectEnabled = true;
        ShowButtonText = true;

        RealOnClick = () =>
        {
            PlayerControl.LocalPlayer.RpcPropHuntSetSpeedboost(PlayerControl.LocalPlayer.PlayerId);
            SoundEffectsManager.play("timemasterShield");
        };
        HasButton = () =>
        {
            return PropHunt.isPropHuntGM && !PlayerControl.LocalPlayer.Data.IsDead &&
                   !PlayerControl.LocalPlayer.Data.Role.IsImpostor && PropHunt.enableSpeedboost;
        };
        CouldUse = () => { return true; };
        OnMeetingEnds = () => { };
    }

    public override float Cooldown => PropHunt.speedboostCooldown;

    public override float EffectDuration => PropHunt.speedboostDuration;

    public override void OnEffectEnd()
    {
        SoundEffectsManager.stop("timemasterShield");
        propHuntSpeedboostButton.Timer = propHuntSpeedboostButton.MaxTimer;
    }

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        ButtonText = new ButtonText(44,
            FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.UseButton]
                .FontMaterial);
    }
}

/// <summary>
/// The hunter's admin button (Prop Hunt). It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>propHuntAdminButton</c>.
/// </summary>
public sealed class PropHuntAdminButton : TorButton
{
    private static PropHuntAdminButton propHuntAdminButton;

    public PropHuntAdminButton()
    {
        propHuntAdminButton = this;

        PositionOffset = TorButtonPositions.UpperRowCenter;
        Hotkey = KeyCode.G;
        EffectEnabled = true;
        ShowButtonText = true;
        ButtonText = new ButtonText(StringNames.Admin);

        RealOnClick = () =>
        {
            if (!MapBehaviour.Instance || !MapBehaviour.Instance.isActiveAndEnabled)
            {
                HudManager.Instance.InitMap();
                MapBehaviour.Instance.ShowCountOverlay(true, true, false);
            }

            PlayerControl.LocalPlayer.NetTransform.Halt(); // Stop current movement
        };
        HasButton = () =>
        {
            return PropHunt.isPropHuntGM && !PlayerControl.LocalPlayer.Data.IsDead &&
                   PlayerControl.LocalPlayer.Data.Role.IsImpostor;
        };
        CouldUse = () =>
        {
            propHuntAdminButton.PositionOffset = PlayerControl.LocalPlayer.inVent
                ? TorButtonPositions.LowerRowRight
                : TorButtonPositions.UpperRowCenter;
            return !PlayerControl.LocalPlayer.inVent;
        };
        OnMeetingEnds = () =>
        {
            propHuntAdminButton.Timer = CustomButtonSingleton<HunterAdminTableButton>.Instance.MaxTimer;
            propHuntAdminButton.isEffectActive = false;
            propHuntAdminButton.actionButton.cooldownTimerText.color = Palette.EnabledColor;
        };
    }

    public override float Cooldown => PropHunt.adminCooldown;

    public override float EffectDuration => PropHunt.adminDuration;

    public override void OnEffectEnd()
    {
        propHuntAdminButton.Timer = propHuntAdminButton.MaxTimer;
        if (MapBehaviour.Instance && MapBehaviour.Instance.isActiveAndEnabled) MapBehaviour.Instance.Close();
    }

    public override void CreateButton(Transform parent)
    {
        // The admin sprite only exists once the HUD is up.
        SetSprite(Hacker.getAdminSprite());
        base.CreateButton(parent);
    }
}

/// <summary>
/// The hunter's find button (Prop Hunt). It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>propHuntFindButton</c>.
/// </summary>
public sealed class PropHuntFindButton : TorButton
{
    private static PropHuntFindButton propHuntFindButton;

    public PropHuntFindButton()
    {
        propHuntFindButton = this;

        SetSprite(TorAssets.FindButton);
        PositionOffset = TorButtonPositions.LowerRowCenter;
        Hotkey = KeyCode.F;
        EffectEnabled = true;
        ShowButtonText = true;

        RealOnClick = () => { SoundEffectsManager.play("timemasterShield"); };
        HasButton = () =>
        {
            return PropHunt.isPropHuntGM && !PlayerControl.LocalPlayer.Data.IsDead &&
                   PlayerControl.LocalPlayer.Data.Role.IsImpostor;
        };
        CouldUse = () => { return true; };
        OnMeetingEnds = () => { };
    }

    public override float Cooldown => PropHunt.findCooldown;

    public override float EffectDuration => PropHunt.findDuration;

    public override void OnEffectEnd()
    {
        SoundEffectsManager.stop("timemasterShield");
        propHuntFindButton.Timer = propHuntFindButton.MaxTimer;
        propHuntFindButton.isEffectActive = false;
    }

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        ButtonText = new ButtonText(45,
            FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.UseButton]
                .FontMaterial);
    }
}

public static class PropHuntRpcs
{
    [MethodRpc((uint)TorRpc.PropHuntStartTimer, LocalHandling = RpcLocalHandling.After)]
    public static void RpcPropHuntStartTimer(this PlayerControl player, bool blackout = false)
    {
        if (blackout)
        {
            PropHunt.blackOutTimer = PropHunt.initialBlackoutTime;
            PropHunt.transformLayers();
        }
        else
        {
            PropHunt.timerRunning = true;
            PropHunt.blackOutTimer = 0f;
        }

        PropHunt.startTime = DateTime.UtcNow;
        foreach (var pc in PlayerControl.AllPlayerControls.ToArray().Where(x => x.Data.Role.IsImpostor))
            pc.MyPhysics.SetBodyType(PlayerBodyTypes.Seeker);
    }

    [MethodRpc((uint)TorRpc.SetProp, LocalHandling = RpcLocalHandling.After)]
    public static void RpcPropHuntSetProp(this PlayerControl player, byte playerId, string propName, float posX)
    {
        var target = Helpers.playerById(playerId);
        var prop = PropHunt.FindPropByNameAndPos(propName, posX);
        if (prop == null) return;
        try
        {
            target.GetComponent<SpriteRenderer>().sprite = prop.GetComponent<SpriteRenderer>().sprite;
        }
        catch
        {
            target.GetComponent<SpriteRenderer>().sprite =
                prop.transform.GetComponentInChildren<SpriteRenderer>().sprite;
        }

        target.transform.localScale = prop.transform.lossyScale;
        target.Visible = false;
        PropHunt.currentObject[target.PlayerId] = new Tuple<string, float>(propName, posX);
    }

    [MethodRpc((uint)TorRpc.SetRevealed, LocalHandling = RpcLocalHandling.After)]
    public static void RpcPropHuntSetRevealed(this PlayerControl player, byte playerId)
    {
        SoundEffectsManager.play("morphlingMorph");
        PropHunt.isCurrentlyRevealed.Add(playerId, PropHunt.revealDuration);
        PropHunt.timer -= PropHunt.revealPunish;
    }

    [MethodRpc((uint)TorRpc.PropHuntSetInvis, LocalHandling = RpcLocalHandling.After)]
    public static void RpcPropHuntSetInvis(this PlayerControl player, byte playerId)
    {
        PropHunt.invisPlayers.Add(playerId, PropHunt.invisDuration);
    }

    [MethodRpc((uint)TorRpc.PropHuntSetSpeedboost, LocalHandling = RpcLocalHandling.After)]
    public static void RpcPropHuntSetSpeedboost(this PlayerControl player, byte playerId)
    {
        PropHunt.speedboostActive.Add(playerId, PropHunt.speedboostDuration);
    }
}