using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using MiraAPI.Hud;
using MiraAPI.Utilities;
using TheOtherRoles.Objects;
using TheOtherRoles.Utilities;
using TMPro;
using UnityEngine;
using static TheOtherRoles.TORMapOptions;
using Object = UnityEngine.Object;

namespace TheOtherRoles.Patches;

[HarmonyPatch]
internal class MeetingHudPatch
{
    private const float scale = 0.65f;
    private static bool[] selections;
    private static SpriteRenderer[] renderers;
    private static NetworkedPlayerInfo target;
    private static TextMeshPro meetingExtraButtonText;
    private static PassiveButton[] swapperButtonList;
    private static TextMeshPro meetingExtraButtonLabel;
    private static PlayerVoteArea swapped1;
    private static PlayerVoteArea swapped2;

    public static GameObject guesserUI;
    public static PassiveButton guesserUIExitButton;
    public static byte guesserCurrentTarget;


    private static void swapperOnClick(int i, MeetingHud __instance)
    {
        if (__instance.state == MeetingHud.MeetingStates.Results || Swapper.charges <= 0) return;
        if (__instance.playerStates[i].AmDead) return;

        var selectedCount = selections.Where(b => b).Count();
        var renderer = renderers[i];

        if (selectedCount == 0)
        {
            renderer.color = Color.yellow;
            selections[i] = true;
        }
        else if (selectedCount == 1)
        {
            if (selections[i])
            {
                renderer.color = Color.red;
                selections[i] = false;
            }
            else
            {
                selections[i] = true;
                renderer.color = Color.yellow;
                meetingExtraButtonLabel.text = Helpers.cs(Color.yellow, ModTranslation.GetString("Swapper-Text", 1));
            }
        }
        else if (selectedCount == 2)
        {
            if (selections[i])
            {
                renderer.color = Color.red;
                selections[i] = false;
                meetingExtraButtonLabel.text = Helpers.cs(Color.red, ModTranslation.GetString("Swapper-Text", 1));
            }
        }
    }

    private static void swapperConfirm(MeetingHud __instance)
    {
        __instance.playerStates[0].Cancel(); // This will stop the underlying buttons of the template from showing up
        if (__instance.state == MeetingHud.MeetingStates.Results) return;
        if (selections.Where(b => b).Count() != 2) return;
        if (Swapper.charges <= 0 || Swapper.playerId1 != byte.MaxValue) return;

        PlayerVoteArea firstPlayer = null;
        PlayerVoteArea secondPlayer = null;
        for (var A = 0; A < selections.Length; A++)
        {
            if (selections[A])
            {
                if (firstPlayer == null)
                    firstPlayer = __instance.playerStates[A];
                else
                    secondPlayer = __instance.playerStates[A];
                renderers[A].color = Color.green;
            }
            else if (renderers[A] != null)
            {
                renderers[A].color = Color.gray;
            }

            if (swapperButtonList[A] != null)
                swapperButtonList[A].OnClick.RemoveAllListeners(); // Swap buttons can't be clicked / changed anymore
        }

        if (firstPlayer != null && secondPlayer != null)
        {
            PlayerControl.LocalPlayer.RpcSwapperSwap(firstPlayer.PlayerId, secondPlayer.PlayerId);

            meetingExtraButtonLabel.text = Helpers.cs(Color.green, ModTranslation.GetString("Swapper-Text", 2));
            Swapper.charges--;
            meetingExtraButtonText.text = string.Format(ModTranslation.GetString("Swapper-Text", 3), Swapper.charges);
        }
    }

    public static void swapperCheckAndReturnSwap(MeetingHud __instance, byte dyingPlayerId)
    {
        // someone was guessed or dced in the meeting, check if this affects the swapper.
        if (Swapper.swapper == null || __instance.state == MeetingHud.MeetingStates.Results) return;

        // reset swap.
        var reset = false;
        if (dyingPlayerId == Swapper.playerId1 || dyingPlayerId == Swapper.playerId2)
        {
            reset = true;
            Swapper.playerId1 = Swapper.playerId2 = byte.MaxValue;
        }


        // Only for the swapper: Reset all the buttons and charges value to their original state.
        if (PlayerControl.LocalPlayer != Swapper.swapper) return;


        // check if dying player was a selected player (but not confirmed yet)
        for (var i = 0; i < __instance.playerStates.Count; i++)
        {
            reset = reset || (selections[i] && __instance.playerStates[i].PlayerId == dyingPlayerId);
            if (reset) break;
        }

        if (!reset) return;


        for (var i = 0; i < selections.Length; i++)
        {
            selections[i] = false;
            var playerVoteArea = __instance.playerStates[i];
            if (playerVoteArea.AmDead ||
                (playerVoteArea.PlayerId == Swapper.swapper.PlayerId && Swapper.canOnlySwapOthers)) continue;
            renderers[i].color = Color.red;
            Swapper.charges++;
            var copyI = i;
            swapperButtonList[i].OnClick.RemoveAllListeners();
            swapperButtonList[i].OnClick.AddListener((Action)(() => swapperOnClick(copyI, __instance)));
        }

        meetingExtraButtonText.text = string.Format(ModTranslation.GetString("Swapper-Text", 3), Swapper.charges);
        meetingExtraButtonLabel.text = Helpers.cs(Color.red, ModTranslation.GetString("Swapper-Text", 1));
    }

    private static void mayorToggleVoteTwice(MeetingHud __instance)
    {
        __instance.playerStates[0].Cancel(); // This will stop the underlying buttons of the template from showing up
        if (__instance.state == MeetingHud.MeetingStates.Results || Mayor.mayor.Data.IsDead) return;
        if (Mayor.mayorChooseSingleVote == 1)
        {
            // Only accept changes until the mayor voted
            var mayorPVA = __instance.playerStates.FirstOrDefault(x => x.PlayerId == Mayor.mayor.PlayerId);
            if (mayorPVA != null && mayorPVA.DidVote)
            {
                SoundEffectsManager.play("fail");
                return;
            }
        }

        Mayor.voteTwice = !Mayor.voteTwice;

        PlayerControl.LocalPlayer.RpcMayorSetVoteTwice(Mayor.voteTwice);

        meetingExtraButtonLabel.text = Helpers.cs(Mayor.color,
            string.Format(ModTranslation.GetString("Mayor-Text", 1), Mayor.voteTwice ? Helpers.cs(Color.green, ModTranslation.GetString("Mayor-Text", 2)) : Helpers.cs(Color.red, ModTranslation.GetString("Mayor-Text", 3))));
    }

    private static void guesserOnClick(int buttonTarget, MeetingHud __instance)
    {
        if (guesserUI != null || !(__instance.state == MeetingHud.MeetingStates.Voted ||
                                   __instance.state == MeetingHud.MeetingStates.NotVoted)) return;
        __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(false));

        var PhoneUI = Object.FindObjectsOfType<Transform>().FirstOrDefault(x => x.name == "PhoneUI");
        var container = Object.Instantiate(PhoneUI, __instance.transform);
        container.transform.localPosition = new Vector3(0, 0, -5f);
        guesserUI = container.gameObject;

        var i = 0;
        var buttonTemplate = __instance.playerStates[0].transform.FindChild("votePlayerBase");
        var maskTemplate = __instance.playerStates[0].transform.FindChild("MaskArea");
        var smallButtonTemplate = __instance.playerStates[0].Buttons.transform.Find("CancelButton");
        var textTemplate = __instance.playerStates[0].NameText;

        guesserCurrentTarget = __instance.playerStates[buttonTarget].PlayerId;

        var exitButtonParent = new GameObject().transform;
        exitButtonParent.SetParent(container);
        var exitButton = Object.Instantiate(buttonTemplate.transform, exitButtonParent);
        var exitButtonMask = Object.Instantiate(maskTemplate, exitButtonParent);
        exitButton.gameObject.GetComponent<SpriteRenderer>().sprite =
            smallButtonTemplate.GetComponent<SpriteRenderer>().sprite;
        exitButtonParent.transform.localPosition = new Vector3(2.725f, 2.1f, -5);
        exitButtonParent.transform.localScale = new Vector3(0.217f, 0.9f, 1);
        guesserUIExitButton = exitButton.GetComponent<PassiveButton>();
        guesserUIExitButton.OnClick.RemoveAllListeners();
        guesserUIExitButton.OnClick.AddListener((Action)(() =>
        {
            __instance.playerStates.ToList().ForEach(x =>
            {
                x.gameObject.SetActive(true);
                if (PlayerControl.LocalPlayer.Data.IsDead && x.transform.FindChild("ShootButton") != null)
                    Object.Destroy(x.transform.FindChild("ShootButton").gameObject);
            });
            Object.Destroy(container.gameObject);
        }));

        var buttons = new List<Transform>();
        Transform selectedButton = null;

        foreach (var roleInfo in CustomRoleManager.Instance.allRoleInfos)
        {
            var guesserRole =
                Guesser.niceGuesser != null && PlayerControl.LocalPlayer.PlayerId == Guesser.niceGuesser.PlayerId
                    ? RoleId.NiceGuesser
                    : RoleId.EvilGuesser;
            if (roleInfo.isModifier || roleInfo.roleId == guesserRole || (!HandleGuesser.evilGuesserCanGuessSpy &&
                                                                          guesserRole == RoleId.EvilGuesser &&
                                                                          roleInfo.roleId == RoleId.Spy &&
                                                                          !HandleGuesser.isGuesserGm))
                continue; // Not guessable roles & modifier
            if (HandleGuesser.isGuesserGm &&
                (roleInfo.roleId == RoleId.NiceGuesser || roleInfo.roleId == RoleId.EvilGuesser))
                continue; // remove Guesser for guesser game mode
            if (HandleGuesser.isGuesserGm && PlayerControl.LocalPlayer.Data.Role.IsImpostor &&
                !HandleGuesser.evilGuesserCanGuessSpy && roleInfo.roleId == RoleId.Spy) continue;
            // remove all roles that cannot spawn due to the settings from the ui.
            var torRole = MiraAPI.Roles.CustomRoleManager.CustomMiraRoles
                .OfType<TorRoleBehaviour>()
                .FirstOrDefault(x => x.TorRoleId == roleInfo.roleId);
            if (torRole != null && torRole.HasSpawnRateOption && torRole.GetChance() == 0) continue;
            if (new List<RoleId> { RoleId.Janitor, RoleId.Godfather, RoleId.Mafioso }.Contains(roleInfo.roleId) &&
                (OptionGroupSingleton<MafiaOptions>.Instance.SpawnRate.Selection() == 0 ||
                 GameOptionsManager.Instance.currentGameOptions.NumImpostors < 3)) continue;
            if (roleInfo.roleId == RoleId.Sidekick && (!OptionGroupSingleton<JackalOptions>.Instance.CanCreateSidekick.Value ||
                                                       OptionGroupSingleton<JackalOptions>.Instance.SpawnRate.Selection() == 0))
                continue;
            if (roleInfo.roleId == RoleId.Deputy && (!OptionGroupSingleton<SheriffOptions>.Instance.DeputyEnabled.Value ||
                                                     OptionGroupSingleton<DeputyOptions>.Instance.SpawnRate.Selection() == 0)) continue;
            if (roleInfo.roleId == RoleId.Pursuer && OptionGroupSingleton<LawyerOptions>.Instance.SpawnRate.Selection() == 0) continue;
            if (roleInfo.roleId == RoleId.Spy && TorSpawn.ImpostorsInGame <= 1) continue;
            if (roleInfo.roleId == RoleId.Prosecutor &&
                (OptionGroupSingleton<LawyerOptions>.Instance.IsProsecutorChance.Selection() == 0 ||
                 OptionGroupSingleton<LawyerOptions>.Instance.SpawnRate.Selection() == 0)) continue;
            if (roleInfo.roleId == RoleId.Lawyer && (OptionGroupSingleton<LawyerOptions>.Instance.IsProsecutorChance.Selection() == TorOptions.MaxRate ||
                                                     OptionGroupSingleton<LawyerOptions>.Instance.SpawnRate.Selection() == 0)) continue;
            if (Snitch.snitch != null && HandleGuesser.guesserCantGuessSnitch)
            {
                var (playerCompleted, playerTotal) = TasksHandler.taskInfo(Snitch.snitch.Data);
                var numberOfLeftTasks = playerTotal - playerCompleted;
                if (numberOfLeftTasks <= 0 && roleInfo.roleId == RoleId.Snitch) continue;
            }

            var buttonParent = new GameObject().transform;
            buttonParent.SetParent(container);
            var button = Object.Instantiate(buttonTemplate, buttonParent);
            var buttonMask = Object.Instantiate(maskTemplate, buttonParent);
            var label = Object.Instantiate(textTemplate, button);
            button.GetComponent<SpriteRenderer>().sprite =
                ShipStatus.Instance.CosmeticsCache.GetNameplate("nameplate_NoPlate").Image;
            buttons.Add(button);
            int row = i / 5, col = i % 5;
            buttonParent.localPosition = new Vector3(-3.47f + 1.75f * col, 1.5f - 0.45f * row, -5);
            buttonParent.localScale = new Vector3(0.55f, 0.55f, 1f);
            label.text = Helpers.cs(roleInfo.color, roleInfo.name);
            label.alignment = TextAlignmentOptions.Center;
            label.transform.localPosition = new Vector3(0, 0, label.transform.localPosition.z);
            label.transform.localScale *= 1.7f;
            var copiedIndex = i;

            button.GetComponent<PassiveButton>().OnClick.RemoveAllListeners();
            if (!PlayerControl.LocalPlayer.Data.IsDead &&
                !Helpers.playerById(__instance.playerStates[buttonTarget].PlayerId).Data.IsDead)
                button.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() =>
                {
                    if (selectedButton != button)
                    {
                        selectedButton = button;
                        buttons.ForEach(x =>
                            x.GetComponent<SpriteRenderer>().color = x == selectedButton ? Color.red : Color.white);
                    }
                    else
                    {
                        var focusedTarget = Helpers.playerById(__instance.playerStates[buttonTarget].PlayerId);
                        if (!(__instance.state == MeetingHud.MeetingStates.Voted ||
                              __instance.state == MeetingHud.MeetingStates.NotVoted) || focusedTarget == null ||
                            HandleGuesser.remainingShots(PlayerControl.LocalPlayer.PlayerId) <= 0) return;

                        if (!HandleGuesser.killsThroughShield && focusedTarget == Medic.shielded)
                        {
                            // Depending on the options, shooting the shielded player will not allow the guess, notifiy everyone about the kill attempt and close the window
                            __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(true));
                            Object.Destroy(container.gameObject);

                            PlayerControl.LocalPlayer.RpcShieldedMurderAttempt();
                            SoundEffectsManager.play("fail");
                            return;
                        }

                        var mainRoleInfo = CustomRoleManager.getRoleInfoForPlayer(focusedTarget, false)
                            .FirstOrDefault();
                        if (mainRoleInfo == null) return;

                        var dyingTarget = mainRoleInfo == roleInfo ? focusedTarget : PlayerControl.LocalPlayer;

                        // Reset the GUI
                        __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(true));
                        Object.Destroy(container.gameObject);
                        if (HandleGuesser.hasMultipleShotsPerMeeting &&
                            HandleGuesser.remainingShots(PlayerControl.LocalPlayer.PlayerId) > 1 &&
                            dyingTarget != PlayerControl.LocalPlayer)
                            __instance.playerStates.ToList().ForEach(x =>
                            {
                                if (x.PlayerId == dyingTarget.PlayerId && x.transform.FindChild("ShootButton") != null)
                                    Object.Destroy(x.transform.FindChild("ShootButton").gameObject);
                            });
                        else
                            __instance.playerStates.ToList().ForEach(x =>
                            {
                                if (x.transform.FindChild("ShootButton") != null)
                                    Object.Destroy(x.transform.FindChild("ShootButton").gameObject);
                            });

                        // Shoot player and send chat info if activated
                        PlayerControl.LocalPlayer.RpcGuesserShoot(PlayerControl.LocalPlayer.PlayerId,
                            dyingTarget.PlayerId, focusedTarget.PlayerId, (byte)roleInfo.roleId);
                    }
                }));

            i++;
        }

        container.transform.localScale *= 0.75f;
    }

    private static void populateButtonsPostfix(MeetingHud __instance)
    {
        // Add Swapper Buttons
        var addSwapperButtons = Swapper.swapper != null && PlayerControl.LocalPlayer == Swapper.swapper &&
                                !Swapper.swapper.Data.IsDead;
        var addMayorButton = Mayor.mayor != null && PlayerControl.LocalPlayer == Mayor.mayor &&
                             !Mayor.mayor.Data.IsDead && Mayor.mayorChooseSingleVote > 0;
        if (addSwapperButtons)
        {
            selections = new bool[__instance.playerStates.Length];
            renderers = new SpriteRenderer[__instance.playerStates.Length];
            swapperButtonList = new PassiveButton[__instance.playerStates.Length];

            for (var i = 0; i < __instance.playerStates.Length; i++)
            {
                var playerVoteArea = __instance.playerStates[i];
                if (playerVoteArea.AmDead ||
                    (playerVoteArea.PlayerId == Swapper.swapper.PlayerId && Swapper.canOnlySwapOthers)) continue;

                var template = playerVoteArea.Buttons.transform.Find("CancelButton").gameObject;
                var checkbox = Object.Instantiate(template);
                checkbox.transform.SetParent(playerVoteArea.transform);
                checkbox.transform.position = template.transform.position;
                checkbox.transform.localPosition = new Vector3(-0.95f, 0.03f, -1.3f);
                if (HandleGuesser.isGuesserGm && HandleGuesser.isGuesser(PlayerControl.LocalPlayer.PlayerId))
                    checkbox.transform.localPosition = new Vector3(-0.5f, 0.03f, -1.3f);
                var renderer = checkbox.GetComponent<SpriteRenderer>();
                renderer.sprite = TorAssets.SwapperCheck.LoadAsset();
                renderer.color = Color.red;

                if (Swapper.charges <= 0) renderer.color = Color.gray;

                var button = checkbox.GetComponent<PassiveButton>();
                swapperButtonList[i] = button;
                button.OnClick.RemoveAllListeners();
                var copiedIndex = i;
                button.OnClick.AddListener((Action)(() => swapperOnClick(copiedIndex, __instance)));

                selections[i] = false;
                renderers[i] = renderer;
            }
        }

        // Add meeting extra button, i.e. Swapper Confirm Button or Mayor Toggle Double Vote Button. Swapper Button uses ExtraButtonText on the Left of the Button. (Future meeting buttons can easily be added here)
        if (addSwapperButtons || addMayorButton)
        {
            var meetingUI = Object.FindObjectsOfType<Transform>().FirstOrDefault(x => x.name == "PhoneUI");

            var buttonTemplate = __instance.playerStates[0].transform.FindChild("votePlayerBase");
            var maskTemplate = __instance.playerStates[0].transform.FindChild("MaskArea");
            var textTemplate = __instance.playerStates[0].NameText;
            var meetingExtraButtonParent = new GameObject().transform;
            meetingExtraButtonParent.SetParent(meetingUI);
            var meetingExtraButton = Object.Instantiate(buttonTemplate, meetingExtraButtonParent);

            var infoTransform = __instance.playerStates[0].NameText.transform.parent.FindChild("Info");
            var meetingInfo = infoTransform != null ? infoTransform.GetComponent<TextMeshPro>() : null;
            meetingExtraButtonText = Object.Instantiate(__instance.playerStates[0].NameText, meetingExtraButtonParent);
            meetingExtraButtonText.text = addSwapperButtons ? string.Format(ModTranslation.GetString("Swapper-Text", 3), Swapper.charges) : "";
            meetingExtraButtonText.enableWordWrapping = false;
            meetingExtraButtonText.transform.localScale = Vector3.one * 1.7f;
            meetingExtraButtonText.transform.localPosition = new Vector3(-2.5f, 0f, 0f);

            var meetingExtraButtonMask = Object.Instantiate(maskTemplate, meetingExtraButtonParent);
            meetingExtraButtonLabel = Object.Instantiate(textTemplate, meetingExtraButton);
            meetingExtraButton.GetComponent<SpriteRenderer>().sprite =
                ShipStatus.Instance.CosmeticsCache.GetNameplate("nameplate_NoPlate").Image;

            meetingExtraButtonParent.localPosition = new Vector3(0, -2.225f, -5);
            meetingExtraButtonParent.localScale = new Vector3(0.55f, 0.55f, 1f);
            meetingExtraButtonLabel.alignment = TextAlignmentOptions.Center;
            meetingExtraButtonLabel.transform.localPosition =
                new Vector3(0, 0, meetingExtraButtonLabel.transform.localPosition.z);
            if (addSwapperButtons)
            {
                meetingExtraButtonLabel.transform.localScale *= 1.7f;
                meetingExtraButtonLabel.text = Helpers.cs(Color.red, ModTranslation.GetString("Swapper-Text", 1));
            }
            else if (addMayorButton)
            {
                meetingExtraButtonLabel.transform.localScale = new Vector3(
                    meetingExtraButtonLabel.transform.localScale.x * 1.5f,
                    meetingExtraButtonLabel.transform.localScale.x * 1.7f,
                    meetingExtraButtonLabel.transform.localScale.x * 1.7f);
                meetingExtraButtonLabel.text = Helpers.cs(Mayor.color,
                   string.Format(ModTranslation.GetString("Mayor-Text", 1), Mayor.voteTwice ? Helpers.cs(Color.green, ModTranslation.GetString("Mayor-Text", 2)) : Helpers.cs(Color.red, ModTranslation.GetString("Mayor-Text", 3))));
            }

            var passiveButton = meetingExtraButton.GetComponent<PassiveButton>();
            passiveButton.OnClick.RemoveAllListeners();
            if (!PlayerControl.LocalPlayer.Data.IsDead)
            {
                if (addSwapperButtons)
                    passiveButton.OnClick.AddListener((Action)(() => swapperConfirm(__instance)));
                else if (addMayorButton)
                    passiveButton.OnClick.AddListener((Action)(() => mayorToggleVoteTwice(__instance)));
            }

            meetingExtraButton.parent.gameObject.SetActive(false);
            __instance.StartCoroutine(Effects.Lerp(7.27f, new Action<float>(p =>
            {
                // Button appears delayed, so that its visible in the voting screen only!
                if (p == 1f) meetingExtraButton.parent.gameObject.SetActive(true);
            })));
        }


        var isGuesser = HandleGuesser.isGuesser(PlayerControl.LocalPlayer.PlayerId);

        // Add overlay for spelled players
        if (Witch.witch != null && Witch.futureSpelled != null)
            foreach (var pva in __instance.playerStates)
                if (Witch.futureSpelled.Any(x => x.PlayerId == pva.PlayerId))
                {
                    var rend = new GameObject().AddComponent<SpriteRenderer>();
                    rend.transform.SetParent(pva.transform);
                    rend.gameObject.layer = pva.Megaphone.gameObject.layer;
                    rend.transform.localPosition = new Vector3(-0.5f, -0.03f, -1f);
                    if (PlayerControl.LocalPlayer == Swapper.swapper && isGuesser)
                        rend.transform.localPosition = new Vector3(-0.725f, -0.15f, -1f);
                    rend.sprite = TorAssets.SpellButtonMeeting.LoadAsset();
                }

        // Add Guesser Buttons
        var remainingShots = HandleGuesser.remainingShots(PlayerControl.LocalPlayer.PlayerId);
        var (playerCompleted, playerTotal) = TasksHandler.taskInfo(PlayerControl.LocalPlayer.Data);

        if (isGuesser && !PlayerControl.LocalPlayer.Data.IsDead && remainingShots > 0)
            for (var i = 0; i < __instance.playerStates.Length; i++)
            {
                var playerVoteArea = __instance.playerStates[i];
                if (playerVoteArea.AmDead || playerVoteArea.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer == Eraser.eraser &&
                    Eraser.alreadyErased.Contains(playerVoteArea.PlayerId)) continue;
                if (PlayerControl.LocalPlayer != null && !Helpers.isEvil(PlayerControl.LocalPlayer) &&
                    playerCompleted < HandleGuesser.tasksToUnlock) continue;

                var template = playerVoteArea.Buttons.transform.Find("CancelButton").gameObject;
                var targetBox = Object.Instantiate(template, playerVoteArea.transform);
                targetBox.name = "ShootButton";
                targetBox.transform.localPosition = new Vector3(-0.95f, 0.03f, -1.3f);
                var renderer = targetBox.GetComponent<SpriteRenderer>();
                renderer.sprite = HandleGuesser.getTargetSprite();
                var button = targetBox.GetComponent<PassiveButton>();
                button.OnClick.RemoveAllListeners();
                var copiedIndex = i;
                button.OnClick.AddListener((Action)(() => guesserOnClick(copiedIndex, __instance)));
            }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.StartMeeting))]
    public static void MeetingHudIntroPrefix()
    {
        EventUtility.meetingStartsUpdate();
        // RoleBase lifecycle
        CustomRoleManager.Instance.OnMeetingStart();
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
    private class MeetingCalculateVotesPatch
    {
        private static Dictionary<byte, int> CalculateVotes(MeetingHud __instance)
        {
            var dictionary = new Dictionary<byte, int>();
            for (var i = 0; i < __instance.playerStates.Length; i++)
            {
                var playerVoteArea = __instance.playerStates[i];
                if (playerVoteArea.VotedForId != 252 && playerVoteArea.VotedForId != 255 &&
                    playerVoteArea.VotedForId != 254)
                {
                    var player = Helpers.playerById(playerVoteArea.PlayerId);
                    if (player == null || player.Data == null || player.Data.IsDead ||
                        player.Data.Disconnected) continue;

                    int currentVotes;
                    var additionalVotes = Mayor.mayor != null && Mayor.mayor.PlayerId == playerVoteArea.PlayerId &&
                                          Mayor.voteTwice
                        ? 2
                        : 1; // Mayor vote
                    if (dictionary.TryGetValue(playerVoteArea.VotedForId, out currentVotes))
                        dictionary[playerVoteArea.VotedForId] = currentVotes + additionalVotes;
                    else
                        dictionary[playerVoteArea.VotedForId] = additionalVotes;
                }
            }

            // Swapper swap votes
            if (Swapper.swapper != null && !Swapper.swapper.Data.IsDead)
            {
                swapped1 = null;
                swapped2 = null;
                foreach (var playerVoteArea in __instance.playerStates)
                {
                    if (playerVoteArea.PlayerId == Swapper.playerId1) swapped1 = playerVoteArea;
                    if (playerVoteArea.PlayerId == Swapper.playerId2) swapped2 = playerVoteArea;
                }

                if (swapped1 != null && swapped2 != null)
                {
                    if (!dictionary.ContainsKey(swapped1.PlayerId)) dictionary[swapped1.PlayerId] = 0;
                    if (!dictionary.ContainsKey(swapped2.PlayerId)) dictionary[swapped2.PlayerId] = 0;
                    var tmp = dictionary[swapped1.PlayerId];
                    dictionary[swapped1.PlayerId] = dictionary[swapped2.PlayerId];
                    dictionary[swapped2.PlayerId] = tmp;
                }
            }


            return dictionary;
        }


        private static bool Prefix(MeetingHud __instance)
        {
            if (__instance.playerStates.All(ps => ps.AmDead || ps.DidVote))
            {
                // If skipping is disabled, replace skipps/no-votes with self vote
                if (target == null && blockSkippingInEmergencyMeetings && noVoteIsSelfVote)
                    foreach (var playerVoteArea in __instance.playerStates)
                        if (playerVoteArea.VotedForId == byte.MaxValue - 1)
                            playerVoteArea.VotedForId = playerVoteArea.PlayerId; // PlayerId

                var self = CalculateVotes(__instance);
                bool tie;
                var max = self.MaxPair(out tie);
                var exiled = GameData.Instance.AllPlayers.ToArray()
                    .FirstOrDefault(v => !tie && v.PlayerId == max.Key && !v.IsDead);

                // TieBreaker 
                var potentialExiled = new List<NetworkedPlayerInfo>();
                var skipIsTie = false;
                if (self.Count > 0)
                {
                    Tiebreaker.isTiebreak = false;
                    var maxVoteValue = self.Values.Max();
                    PlayerVoteArea tb = null;
                    if (Tiebreaker.tiebreaker != null)
                        tb = __instance.playerStates.ToArray()
                            .FirstOrDefault(x => x.PlayerId == Tiebreaker.tiebreaker.PlayerId);
                    var isTiebreakerSkip = tb == null || tb.VotedForId == 253;
                    if (tb != null && tb.AmDead) isTiebreakerSkip = true;

                    foreach (var pair in self)
                    {
                        if (pair.Value != maxVoteValue || isTiebreakerSkip) continue;
                        if (pair.Key != 253)
                            potentialExiled.Add(GameData.Instance.AllPlayers.ToArray()
                                .FirstOrDefault(x => x.PlayerId == pair.Key));
                        else
                            skipIsTie = true;
                    }
                }

                var array = new MeetingHud.VoterState[__instance.playerStates.Length];
                for (var i = 0; i < __instance.playerStates.Length; i++)
                {
                    var playerVoteArea = __instance.playerStates[i];
                    array[i] = new MeetingHud.VoterState
                    {
                        VoterId = playerVoteArea.PlayerId,
                        VotedForId = playerVoteArea.VotedForId
                    };

                    if (Tiebreaker.tiebreaker == null ||
                        playerVoteArea.PlayerId != Tiebreaker.tiebreaker.PlayerId) continue;

                    byte tiebreakerVote = playerVoteArea.VotedForId;
                    if (swapped1 != null && swapped2 != null)
                    {
                        if (tiebreakerVote == swapped1.PlayerId) tiebreakerVote = swapped2.PlayerId;
                        else if (tiebreakerVote == swapped2.PlayerId) tiebreakerVote = swapped1.PlayerId;
                    }

                    if (potentialExiled.FindAll(x => x != null && x.PlayerId == tiebreakerVote).Count > 0 &&
                        (potentialExiled.Count > 1 || skipIsTie))
                    {
                        exiled = potentialExiled.ToArray().FirstOrDefault(v => v.PlayerId == tiebreakerVote);
                        tie = false;

                        PlayerControl.LocalPlayer.RpcSetTiebreak();
                    }
                }

                // RPCVotingComplete
                __instance.RpcVotingComplete(array, exiled, tie, false, 0);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.BloopAVoteIcon))]
    private class MeetingHudBloopAVoteIconPatch
    {
        public static bool Prefix(MeetingHud __instance, NetworkedPlayerInfo voterPlayer, int index, Transform parent)
        {
            var spriteRenderer = Object.Instantiate(__instance.PlayerVotePrefab);
            var showVoteColors = !GameManager.Instance.LogicOptions.GetAnonymousVotes() ||
                                 (PlayerControl.LocalPlayer.Data.IsDead && ghostsSeeVotes) ||
                                 (Mayor.mayor != null && Mayor.mayor == PlayerControl.LocalPlayer &&
                                  Mayor.canSeeVoteColors &&
                                  TasksHandler.taskInfo(PlayerControl.LocalPlayer.Data).Item1 >=
                                  Mayor.tasksNeededToSeeVoteColors);
            if (showVoteColors)
                PlayerMaterial.SetColors(voterPlayer.DefaultOutfit.ColorId, spriteRenderer);
            else
                PlayerMaterial.SetColors(Palette.DisabledGrey, spriteRenderer);

            var transform = spriteRenderer.transform;
            transform.SetParent(parent);
            transform.localScale = Vector3.zero;
            var component = parent.GetComponent<PlayerVoteArea>();
            if (component != null) spriteRenderer.material.SetInt(PlayerMaterial.MaskLayer, component.MaskLayer);

            __instance.StartCoroutine(Effects.Bloop(index * 0.3f, transform));
            parent.GetComponent<VoteSpreader>().AddVote(spriteRenderer);
            return false;
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.PopulateResults))]
    private class MeetingHudPopulateVotesPatch
    {
        private static bool Prefix(MeetingHud __instance, Il2CppStructArray<MeetingHud.VoterState> states)
        {
            // Swapper swap

            PlayerVoteArea swapped1 = null;
            PlayerVoteArea swapped2 = null;
            foreach (var playerVoteArea in __instance.playerStates)
            {
                if (playerVoteArea.PlayerId == Swapper.playerId1) swapped1 = playerVoteArea;
                if (playerVoteArea.PlayerId == Swapper.playerId2) swapped2 = playerVoteArea;
            }

            var doSwap = swapped1 != null && swapped2 != null && Swapper.swapper != null &&
                         !Swapper.swapper.Data.IsDead;
            if (doSwap)
            {
                __instance.StartCoroutine(Effects.Slide3D(swapped1.transform, swapped1.transform.localPosition,
                    swapped2.transform.localPosition, 1.5f));
                __instance.StartCoroutine(Effects.Slide3D(swapped2.transform, swapped2.transform.localPosition,
                    swapped1.transform.localPosition, 1.5f));
            }


            __instance.TitleText.text =
                FastDestroyableSingleton<TranslationController>.Instance.GetString(StringNames.MeetingVotingResults,
                    new Il2CppReferenceArray<Il2CppSystem.Object>(0));
            var num = 0;
            for (var i = 0; i < __instance.playerStates.Length; i++)
            {
                var playerVoteArea = __instance.playerStates[i];
                byte PlayerId = playerVoteArea.PlayerId;
                // Swapper change playerVoteArea that gets the votes
                if (doSwap && playerVoteArea.PlayerId == swapped1.PlayerId) playerVoteArea = swapped2;
                else if (doSwap && playerVoteArea.PlayerId == swapped2.PlayerId) playerVoteArea = swapped1;

                playerVoteArea.ClearForResults();
                var num2 = 0;
                var mayorFirstVoteDisplayed = false;
                for (var j = 0; j < states.Length; j++)
                {
                    var voterState = states[j];
                    var playerById = GameData.Instance.GetPlayerById(voterState.VoterId);
                    if (playerById == null)
                    {
                        Debug.LogError(string.Format("Couldn't find player info for voter: {0}", voterState.VoterId));
                    }
                    else if (i == 0 && voterState.SkippedVote && !playerById.IsDead)
                    {
                        __instance.BloopAVoteIcon(playerById, num, __instance.SkippedVoting.transform);
                        num++;
                    }
                    else if (voterState.VotedForId == PlayerId && !playerById.IsDead)
                    {
                        __instance.BloopAVoteIcon(playerById, num2, playerVoteArea.transform);
                        num2++;
                    }

                    // Major vote, redo this iteration to place a second vote
                    if (Mayor.mayor != null && voterState.VoterId == (sbyte)Mayor.mayor.PlayerId &&
                        !mayorFirstVoteDisplayed && Mayor.voteTwice)
                    {
                        mayorFirstVoteDisplayed = true;
                        j--;
                    }
                }
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.VotingComplete))]
    private class MeetingHudVotingCompletedPatch
    {
        private static void Postfix(MeetingHud __instance, [HarmonyArgument(0)] byte[] states,
            [HarmonyArgument(1)] NetworkedPlayerInfo exiled, [HarmonyArgument(2)] bool tie)
        {
            // Reset swapper values
            Swapper.playerId1 = byte.MaxValue;
            Swapper.playerId2 = byte.MaxValue;

            // Lovers, Lawyer & Pursuer save next to be exiled, because RPC of ending game comes before RPC of exiled
            Lovers.notAckedExiledIsLover = false;
            Pursuer.notAckedExiled = false;
            if (exiled != null)
            {
                Lovers.notAckedExiledIsLover = (Lovers.lover1 != null && Lovers.lover1.PlayerId == exiled.PlayerId) ||
                                               (Lovers.lover2 != null && Lovers.lover2.PlayerId == exiled.PlayerId);
                Pursuer.notAckedExiled = (Pursuer.pursuer != null && Pursuer.pursuer.PlayerId == exiled.PlayerId) ||
                                         (Lawyer.lawyer != null && Lawyer.target != null &&
                                          Lawyer.target.PlayerId == exiled.PlayerId && Lawyer.target != Jester.jester &&
                                          !Lawyer.isProsecutor);
            }

            // Mini
            if (!Mini.isGrowingUpInMeeting)
                Mini.timeOfGrowthStart = Mini.timeOfGrowthStart.Add(DateTime.UtcNow.Subtract(Mini.timeOfMeetingStart))
                    .AddSeconds(10);

            // Snitch
            if (Snitch.snitch != null && !Snitch.needsUpdate && Snitch.snitch.Data.IsDead && Snitch.text != null)
                Object.Destroy(Snitch.text);

            // RoleBase lifecycle
            CustomRoleManager.Instance.OnMeetingEnd();
        }
    }

    [HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.Select))]
    private class PlayerVoteAreaSelectPatch
    {
        private static bool Prefix(MeetingHud __instance)
        {
            return !(PlayerControl.LocalPlayer != null && HandleGuesser.isGuesser(PlayerControl.LocalPlayer.PlayerId) &&
                     guesserUI != null);
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.ServerStart))]
    private class MeetingServerStartPatch
    {
        private static void Postfix(MeetingHud __instance)
        {
            populateButtonsPostfix(__instance);
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Deserialize))]
    private class MeetingDeserializePatch
    {
        private static void Postfix(MeetingHud __instance, [HarmonyArgument(0)] MessageReader reader,
            [HarmonyArgument(1)] bool initialState)
        {
            // Add swapper buttons
            if (initialState) populateButtonsPostfix(__instance);
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.StartMeeting))]
    private class StartMeetingPatch
    {
        public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] NetworkedPlayerInfo meetingTarget)
        {
            var roomTracker = FastDestroyableSingleton<HudManager>.Instance?.roomTracker;
            var roomId = byte.MinValue;
            if (roomTracker != null && roomTracker.LastRoom != null) roomId = (byte)roomTracker.LastRoom?.RoomId;
            if (Snitch.snitch != null && roomTracker != null)
            {
                PlayerControl.LocalPlayer.RpcShareRoom(PlayerControl.LocalPlayer.PlayerId, roomId);
            }

            // Resett Bait list
            Bait.active = new Dictionary<DeadPlayer, float>();
            // Save AntiTeleport position, if the player is able to move (i.e. not on a ladder or a gap thingy)
            if (PlayerControl.LocalPlayer.MyPhysics.enabled && (PlayerControl.LocalPlayer.moveable ||
                                                                PlayerControl.LocalPlayer.inVent
                                                                || CustomButtonSingleton<HackerVitalsButton>.Instance.isEffectActive ||
                                                                CustomButtonSingleton<HackerAdminTableButton>
                                                                    .Instance.isEffectActive ||
                                                                CustomButtonSingleton<SecurityGuardCamButton>
                                                                    .Instance.isEffectActive
                                                                || (Portal.isTeleporting &&
                                                                    Portal.teleportedPlayers.Last().playerId ==
                                                                    PlayerControl.LocalPlayer.PlayerId)))
                if (!PlayerControl.LocalPlayer.inMovingPlat)
                    AntiTeleport.position = PlayerControl.LocalPlayer.transform.position;

            // Medium meeting start time
            Medium.meetingStartTime = DateTime.UtcNow;
            // Mini
            Mini.timeOfMeetingStart = DateTime.UtcNow;
            Mini.ageOnMeetingStart = Mathf.FloorToInt(Mini.growingProgress() * 18);
            // Reset vampire bitten
            Vampire.bitten = null;
            // Count meetings
            if (meetingTarget == null) meetingsCount++;
            // Save the meeting target
            target = meetingTarget;


            // Add Portal info into Portalmaker Chat:
            if (Portalmaker.portalmaker != null &&
                (PlayerControl.LocalPlayer == Portalmaker.portalmaker || Helpers.shouldShowGhostInfo()) &&
                !Portalmaker.portalmaker.Data.IsDead)
                if (Portal.teleportedPlayers.Count > 0)
                {
                    var msg = ModTranslation.GetString("Portalmaker-Text", 1) + "\n";
                    foreach (var entry in Portal.teleportedPlayers)
                    {
                        var timeBeforeMeeting = (float)(DateTime.UtcNow - entry.time).TotalMilliseconds / 1000;
                        msg += Portalmaker.logShowsTime ? string.Format(ModTranslation.GetString("Portalmaker-Text", 2), (int)timeBeforeMeeting) : "";
                        msg = msg + string.Format(ModTranslation.GetString("Portalmaker-Text", 3), entry.name) + "\n";
                    }

                    FastDestroyableSingleton<HudManager>.Instance.Chat.AddChat(Portalmaker.portalmaker, $"{msg}");
                }

            // Add trapped Info into Trapper chat
            if (Trapper.trapper != null &&
                (PlayerControl.LocalPlayer == Trapper.trapper || Helpers.shouldShowGhostInfo()) &&
                !Trapper.trapper.Data.IsDead)
            {
                if (Trap.traps.Any(x => x.revealed))
                    FastDestroyableSingleton<HudManager>.Instance.Chat.AddChat(Trapper.trapper, ModTranslation.GetString("Trapper-Text", 1));
                foreach (var trap in Trap.traps)
                {
                    if (!trap.revealed) continue;
                    var message = string.Format(ModTranslation.GetString("Trapper-Text", 2), trap.instanceId) + "\n";
                    trap.trappedPlayer = trap.trappedPlayer.OrderBy(x => Helpers.rnd.Next()).ToList();
                    foreach (var playerId in trap.trappedPlayer)
                    {
                        var p = Helpers.playerById(playerId);
                        if (Trapper.infoType == 0)
                        {
                            message += CustomRoleManager.GetRolesString(p, false, false, true) + "\n";
                        }
                        else if (Trapper.infoType == 1)
                        {
                            if (Helpers.isNeutral(p) || p.Data.Role.IsImpostor) message += ModTranslation.GetString("Trapper-Text", 3) + "\n";
                            else message += ModTranslation.GetString("Trapper-Text", 4) + "\n";
                        }
                        else
                        {
                            message += p.Data.PlayerName + "\n";
                        }
                    }

                    FastDestroyableSingleton<HudManager>.Instance.Chat.AddChat(Trapper.trapper, $"{message}");
                }
            }

            // Add Snitch info
            var output = "";

            if (Snitch.snitch != null && Snitch.mode != Snitch.Mode.Map &&
                (PlayerControl.LocalPlayer == Snitch.snitch || Helpers.shouldShowGhostInfo()) &&
                !Snitch.snitch.Data.IsDead)
            {
                var (playerCompleted, playerTotal) = TasksHandler.taskInfo(Snitch.snitch.Data);
                var numberOfTasks = playerTotal - playerCompleted;
                if (numberOfTasks == 0)
                {
                    output = ModTranslation.GetString("Snitch-Text", 1) + "\n \n";
                    FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(0.4f,
                        new Action<float>(x =>
                        {
                            if (x == 1f)
                            {
                                foreach (var p in PlayerControl.AllPlayerControls)
                                {
                                    if (Snitch.targets == Snitch.Targets.Killers && !Helpers.isKiller(p)) continue;
                                    if (Snitch.targets == Snitch.Targets.EvilPlayers && !Helpers.isEvil(p)) continue;
                                    if (!Snitch.playerRoomMap.ContainsKey(p.PlayerId)) continue;
                                    if (p.Data.IsDead) continue;
                                    var room = Snitch.playerRoomMap[p.PlayerId];
                                    var roomName = ModTranslation.GetString("Role-Portalmaker", 1);
                                    if (room != byte.MinValue)
                                        roomName =
                                            DestroyableSingleton<TranslationController>.Instance.GetString(
                                                (SystemTypes)room);
                                    output += string.Format(ModTranslation.GetString("Snitch-Text", 2), CustomRoleManager.GetRolesString(p, false, false, true), roomName) + "\n";
                                }

                                FastDestroyableSingleton<HudManager>.Instance.Chat.AddChat(Snitch.snitch, $"{output}");
                            }
                        })));
                }
            }

            if (PlayerControl.LocalPlayer.Data.IsDead && output != "")
                FastDestroyableSingleton<HudManager>.Instance.Chat.AddChat(PlayerControl.LocalPlayer, $"{output}");

            Trapper.playersOnMap = new List<byte>();
            Snitch.playerRoomMap = new Dictionary<byte, byte>();

            // Remove revealed traps
            Trap.clearRevealedTraps();

            Bomber.clearBomb();

            // Reset zoomed out ghosts
            Helpers.toggleZoom(true);

            // Stop all playing sounds
            SoundEffectsManager.stopAll();
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    private class MeetingHudUpdatePatch
    {
        private static void Postfix(MeetingHud __instance)
        {
            // Deactivate skip Button if skipping on emergency meetings is disabled
            if (target == null && blockSkippingInEmergencyMeetings)
                __instance.SkipVoteButton.gameObject.SetActive(false);

            if (__instance.state >= MeetingHud.MeetingStates.Discussion)
                // Remove first kill shield
                firstKillPlayer = null;
        }
    }

    [HarmonyPatch]
    public class ShowHost
    {
        private static TextMeshPro Text;

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
        [HarmonyPostfix]
        public static void Postfix(MeetingHud __instance)
        {
            var host = GameData.Instance.GetHost();

            if (host != null)
            {
                PlayerMaterial.SetColors(host.DefaultOutfit.ColorId, __instance.HostIcon);
                if (Text == null) Text = __instance.ProceedButton.gameObject.GetComponentInChildren<TextMeshPro>();
                Text.text = string.Format(ModTranslation.GetString("MeetingHud-Text", 1), host.PlayerName);
            }
        }
    }
}