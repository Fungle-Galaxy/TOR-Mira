using System.Linq;
using System.Text;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using TheOtherRoles.CustomGameModes;
using TheOtherRoles.Patches;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheOtherRoles.Events;

/// <summary>
/// The end game screen: winners layout, win text and the role summary.
/// Was a Harmony postfix on <see cref="EndGameManager.SetEverythingUp"/> - Mira API raises
/// <see cref="GameEndEvent"/> from a postfix on that exact method, so the body is unchanged.
/// </summary>
public static class GameEndEvents
{
    [RegisterEvent]
    public static void SetupEndGameScreen(GameEndEvent @event)
    {
        var __instance = @event.EndGameManager;
        // Delete and readd PoolablePlayers always showing the name and role of the player
        foreach (var pb in __instance.transform.GetComponentsInChildren<PoolablePlayer>())
            Object.Destroy(pb.gameObject);
        var num = Mathf.CeilToInt(7.5f);
        var list = EndGameResult.CachedWinners.ToArray().ToList().OrderBy(delegate(CachedPlayerData b)
        {
            if (!b.IsYou) return 0;
            return -1;
        }).ToList();
        for (var i = 0; i < list.Count; i++)
        {
            var CachedPlayerData2 = list[i];
            var num2 = i % 2 == 0 ? -1 : 1;
            var num3 = (i + 1) / 2;
            var num4 = num3 / (float)num;
            var num5 = Mathf.Lerp(1f, 0.75f, num4);
            var num6 = (float)(i == 0 ? -8 : -1);
            var poolablePlayer = Object.Instantiate(__instance.PlayerPrefab, __instance.transform);
            poolablePlayer.transform.localPosition = new Vector3(1f * num2 * num3 * num5,
                FloatRange.SpreadToEdges(-1.125f, 0f, num3, num), num6 + num3 * 0.01f) * 0.9f;
            var num7 = Mathf.Lerp(1f, 0.65f, num4) * 0.9f;
            var vector = new Vector3(num7, num7, 1f);
            poolablePlayer.transform.localScale = vector;
            if (CachedPlayerData2.IsDead)
            {
                poolablePlayer.SetBodyAsGhost();
                poolablePlayer.SetDeadFlipX(i % 2 == 0);
            }
            else
            {
                poolablePlayer.SetFlipX(i % 2 == 0);
            }

            poolablePlayer.UpdateFromPlayerOutfit(CachedPlayerData2.Outfit, PlayerMaterial.MaskType.None,
                CachedPlayerData2.IsDead, true);

            poolablePlayer.cosmetics.nameText.color = Color.white;
            poolablePlayer.cosmetics.nameText.transform.localScale =
                new Vector3(1f / vector.x, 1f / vector.y, 1f / vector.z);
            poolablePlayer.cosmetics.nameText.transform.localPosition = new Vector3(
                poolablePlayer.cosmetics.nameText.transform.localPosition.x,
                poolablePlayer.cosmetics.nameText.transform.localPosition.y, -15f);
            poolablePlayer.cosmetics.nameText.text = CachedPlayerData2.PlayerName;

            foreach (var data in AdditionalTempData.playerRoles)
            {
                if (data.PlayerName != CachedPlayerData2.PlayerName) continue;
                var roles =
                    poolablePlayer.cosmetics.nameText.text +=
                        $"\n{string.Join("\n", data.Roles.Select(x => Helpers.cs(x.color, x.name)))}";
            }
        }

        // Additional code
        var bonusText = Object.Instantiate(__instance.WinText.gameObject);
        bonusText.transform.position = new Vector3(__instance.WinText.transform.position.x,
            __instance.WinText.transform.position.y - 0.5f, __instance.WinText.transform.position.z);
        bonusText.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
        var textRenderer = bonusText.GetComponent<TMP_Text>();
        textRenderer.text = "";

        // Win text: map WinCondition/GameOverReason -> (Color, displayText)
        Color winColor = Color.white;
        string winText = "";

        string impName = RoleInfo.roleInfoById[RoleId.Impostor].name;
        string crewName = RoleInfo.roleInfoById[RoleId.Crewmate].name;

        static string Win(string name) => string.Format(ModTranslation.GetString("EndGame-Text" ,1), name);
        static string Died(string name) => string.Format(ModTranslation.GetString("EndGame-Text" ,2), name);
        static string TeamWin(string name) => string.Format(ModTranslation.GetString("EndGame-Text" ,3), name);
        static string CrewAnd(string name) => string.Format(ModTranslation.GetString("EndGame-Text" ,4), name);

        if (AdditionalTempData.winCondition == WinCondition.Default)
        {
            winColor = Color.red;
            winText = OnGameEndPatch.gameOverReason switch
            {
                GameOverReason.ImpostorDisconnect => ModTranslation.GetString("EndGame-Text", 5),
                GameOverReason.ImpostorsByKill => string.Format(ModTranslation.GetString("EndGame-Text", 6), impName),
                GameOverReason.ImpostorsBySabotage => string.Format(ModTranslation.GetString("EndGame-Text", 7), impName),
                GameOverReason.ImpostorsByVote => string.Format(ModTranslation.GetString("EndGame-Text", 8), impName),
                GameOverReason.CrewmatesByTask => string.Format(ModTranslation.GetString("EndGame-Text", 9), crewName),
                GameOverReason.CrewmateDisconnect or GameOverReason.CrewmatesByVote
                    => string.Format(ModTranslation.GetString("EndGame-Text", 10), crewName),
                _ => ""
            };
        }
        else
        {
            (winColor, winText) = AdditionalTempData.winCondition switch
            {
                WinCondition.JesterWin      => (Jester.color,    Win(Jester.Info.name)),
                WinCondition.ArsonistWin    => (Arsonist.color,  Win(Arsonist.Info.name)),
                WinCondition.VultureWin     => (Vulture.color,   Win(Vulture.Info.name)),
                WinCondition.ProsecutorWin  => (Lawyer.color,    Win(RoleInfo.roleInfoById[RoleId.Prosecutor].name)),
                WinCondition.LoversSoloWin  => (Lovers.color,    Win(Lovers.Info.name)),
                WinCondition.JackalWin      => (Jackal.color,    TeamWin(Jackal.Info.name)),
                WinCondition.MiniLose       => (Mini.color,      Died(Mini.Info.name)),
                _ => (Color.white, ""),
            };

            if (AdditionalTempData.winCondition == WinCondition.LoversTeamWin)
            {
                winColor = Lovers.color;
                winText = $"{Lovers.Info.name} {CrewAnd(crewName)}";
                __instance.BackgroundBar.material.SetColor("_Color", Lovers.color);
            }
            else if (AdditionalTempData.winCondition == WinCondition.LoversSoloWin)
            {
                __instance.BackgroundBar.material.SetColor("_Color", Lovers.color);
            }
        }

        textRenderer.text = winText;
        textRenderer.color = winColor;

        foreach (var cond in AdditionalTempData.additionalWinConditions)
        {
            if (cond == WinCondition.AdditionalLawyerBonusWin)
                textRenderer.text += $"\n{Helpers.cs(Lawyer.color, string.Format(ModTranslation.GetString("EndGame-Text", 11), Lawyer.Info.name))}";
            else if (cond == WinCondition.AdditionalAlivePursuerWin)
                textRenderer.text += $"\n{Helpers.cs(Pursuer.color, string.Format(ModTranslation.GetString("EndGame-Text", 12), Pursuer.Info.name))}";
        }

        if (TORMapOptions.showRoleSummary || HideNSeek.isHideNSeekGM || PropHunt.isPropHuntGM)
        {
            var position = Camera.main.ViewportToWorldPoint(new Vector3(0f, 1f, Camera.main.nearClipPlane));
            var roleSummary = Object.Instantiate(__instance.WinText.gameObject);
            roleSummary.transform.position = new Vector3(__instance.Navigation.ExitButton.transform.position.x + 0.1f,
                position.y - 0.1f, -214f);
            roleSummary.transform.localScale = new Vector3(1f, 1f, 1f);

            var roleSummaryText = new StringBuilder();
            if (HideNSeek.isHideNSeekGM || PropHunt.isPropHuntGM)
            {
                var minutes = (int)AdditionalTempData.timer / 60;
                var seconds = (int)AdditionalTempData.timer % 60;
                roleSummaryText.AppendLine($"<color=#FAD934FF>" + string.Format(ModTranslation.GetString("EndGame-Text", 13), minutes, seconds) + "</color> \n");
            }

            roleSummaryText.AppendLine(string.Format(ModTranslation.GetString("EndGame-Text", 14)));
            foreach (var data in AdditionalTempData.playerRoles)
            {
                //var roles = string.Join(" ", data.Roles.Select(x => Helpers.cs(x.color, x.name)));
                var roles = data.RoleNames;
                //if (data.IsGuesser) roles += " (Guesser)";
                var taskInfo = data.TasksTotal > 0
                    ? $" - <color=#FAD934FF>({data.TasksCompleted}/{data.TasksTotal})</color>"
                    : "";
                if (data.Kills != null) taskInfo += $" - <color=#FF0000FF>" + string.Format(ModTranslation.GetString("EndGame-Text", 15), data.Kills) + "</color>";
                roleSummaryText.AppendLine(
                    $"{Helpers.cs(data.IsAlive ? Color.white : new Color(.7f, .7f, .7f), data.PlayerName)} - {roles}{taskInfo}");
            }

            var roleSummaryTextMesh = roleSummary.GetComponent<TMP_Text>();
            roleSummaryTextMesh.alignment = TextAlignmentOptions.TopLeft;
            roleSummaryTextMesh.color = Color.white;
            roleSummaryTextMesh.fontSizeMin = 1.5f;
            roleSummaryTextMesh.fontSizeMax = 1.5f;
            roleSummaryTextMesh.fontSize = 1.5f;

            var roleSummaryTextMeshRectTransform = roleSummaryTextMesh.GetComponent<RectTransform>();
            roleSummaryTextMeshRectTransform.anchoredPosition = new Vector2(position.x + 3.5f, position.y - 0.1f);
            roleSummaryTextMesh.text = roleSummaryText.ToString();
            Helpers.previousEndGameSummary = $"<size=110%>{roleSummaryText}</size>";
        }

        AdditionalTempData.clear();
    }
}
