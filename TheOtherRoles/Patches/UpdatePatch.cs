using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using InnerNet;
using Rewired;
using RewiredConsts;
using TheOtherRoles.CustomGameModes;
using TheOtherRoles.Utilities;
using UnityEngine;

namespace TheOtherRoles.Patches;

internal class HudManagerUpdatePatch
{
    private static readonly Dictionary<byte, (string name, Color color)> TagColorDict = new();

    private static void resetNameTagsAndColors()
    {
        var localPlayer = PlayerControl.LocalPlayer;
        var myData = PlayerControl.LocalPlayer.Data;
        var amImpostor = myData.Role.IsImpostor;
        var morphTimerNotUp = Morphling.morphTimer > 0f;
        var morphTargetNotNull = Morphling.morphTarget != null;

        var dict = TagColorDict;
        dict.Clear();

        foreach (var data in GameData.Instance.AllPlayers.GetFastEnumerator())
        {
            var player = data.Object;
            var text = data.PlayerName;
            Color color;
            if (player)
            {
                var playerName = text;
                if (morphTimerNotUp && morphTargetNotNull && Morphling.morphling == player)
                    playerName = Morphling.morphTarget.Data.PlayerName;
                var nameText = player.cosmetics.nameText;

                nameText.text = Helpers.hidePlayerName(localPlayer, player) ? "" : playerName;
                nameText.color = color = amImpostor && data.Role.IsImpostor ? Palette.ImpostorRed : Color.white;
                nameText.color = nameText.color.SetAlpha(Chameleon.visibility(player.PlayerId));
            }
            else
            {
                color = Color.white;
            }


            dict.Add(data.PlayerId, (text, color));
        }

        if (MeetingHud.Instance != null)
            foreach (var playerVoteArea in MeetingHud.Instance.playerStates)
            {
                var data = dict[playerVoteArea.PlayerId];
                var text = playerVoteArea.NameText;
                text.text = data.name;
                text.color = data.color;
            }
    }

    private static void setPlayerNameColor(PlayerControl p, Color color)
    {
        p.cosmetics.nameText.color = color.SetAlpha(Chameleon.visibility(p.PlayerId));
        if (MeetingHud.Instance != null)
            foreach (var player in MeetingHud.Instance.playerStates)
                if (player.NameText != null && p.PlayerId == player.PlayerId)
                    player.NameText.color = color;
    }

    private static void setNameColors()
    {
        var localPlayer = PlayerControl.LocalPlayer;
        var localRole = CustomRoleManager.getRoleInfoForPlayer(localPlayer, false).FirstOrDefault();
        setPlayerNameColor(localPlayer, localRole.color);

        if (Deputy.deputy != null && Deputy.deputy == localPlayer)
        {
            setPlayerNameColor(Deputy.deputy, Deputy.color);
            if (Sheriff.sheriff != null && Deputy.knowsSheriff) setPlayerNameColor(Sheriff.sheriff, Sheriff.color);
        }
        else if (Jackal.jackal != null && Jackal.jackal == localPlayer)
        {
            // Jackal can see his sidekick
            setPlayerNameColor(Jackal.jackal, Jackal.color);
            if (Sidekick.sidekick != null) setPlayerNameColor(Sidekick.sidekick, Jackal.color);
            if (Jackal.fakeSidekick != null) setPlayerNameColor(Jackal.fakeSidekick, Jackal.color);
        }

        // No else if here, as a Lover of team Jackal needs the colors
        if (Sidekick.sidekick != null && Sidekick.sidekick == localPlayer)
        {
            // Sidekick can see the jackal
            setPlayerNameColor(Sidekick.sidekick, Sidekick.color);
            if (Jackal.jackal != null) setPlayerNameColor(Jackal.jackal, Jackal.color);
        }

        // Schrödinger's Cat: show team color when cat has joined a team
        if (SchrodingerCat.cat != null && SchrodingerCat.cat == localPlayer && SchrodingerCat.hasTeam())
        {
            Color catColor = SchrodingerCat.team switch
            {
                SchrodingerCat.CatTeam.Impostor => Palette.ImpostorRed,
                SchrodingerCat.CatTeam.Jackal => Jackal.color,
                _ => Color.white
            };
            setPlayerNameColor(SchrodingerCat.cat, catColor);
        }

        // Schrödinger's Cat on Impostor team: Impostors see cat in red
        if (SchrodingerCat.cat != null && SchrodingerCat.team == SchrodingerCat.CatTeam.Impostor && localPlayer.Data.Role.IsImpostor)
            setPlayerNameColor(SchrodingerCat.cat, Palette.ImpostorRed);

        // Schrödinger's Cat on Jackal team: Jackal/Sidekick see cat in Jackal color
        if (SchrodingerCat.cat != null && SchrodingerCat.team == SchrodingerCat.CatTeam.Jackal)
        {
            if (localPlayer == Jackal.jackal || localPlayer == Sidekick.sidekick)
                setPlayerNameColor(SchrodingerCat.cat, Jackal.color);
        }

        // Schrödinger's Cat sees its team members
        if (SchrodingerCat.cat != null && SchrodingerCat.cat == localPlayer && SchrodingerCat.hasTeam())
        {
            switch (SchrodingerCat.team)
            {
                case SchrodingerCat.CatTeam.Impostor:
                    // Cat on Impostor team sees all Impostors in red
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        if (pc != SchrodingerCat.cat && pc.Data.Role.IsImpostor)
                            setPlayerNameColor(pc, Palette.ImpostorRed);
                    }
                    break;
                case SchrodingerCat.CatTeam.Jackal:
                    // Cat on Jackal team sees Jackal and Sidekick
                    if (Jackal.jackal != null) setPlayerNameColor(Jackal.jackal, Jackal.color);
                    if (Sidekick.sidekick != null) setPlayerNameColor(Sidekick.sidekick, Jackal.color);
                    break;
            }
        }

        // No else if here, as the Impostors need the Spy name to be colored
        if (Spy.spy != null && localPlayer.Data.Role.IsImpostor) setPlayerNameColor(Spy.spy, Spy.color);
        if (Sidekick.sidekick != null && Sidekick.wasTeamRed && localPlayer.Data.Role.IsImpostor)
            setPlayerNameColor(Sidekick.sidekick, Spy.color);
        if (Jackal.jackal != null && Jackal.wasTeamRed && localPlayer.Data.Role.IsImpostor)
            setPlayerNameColor(Jackal.jackal, Spy.color);

        // Crewmate roles with no changes: Mini
        // Impostor roles with no changes: Morphling, Camouflager, Vampire, Godfather, Eraser, Janitor, Cleaner, Warlock, BountyHunter,  Witch and Mafioso
    }

    private static void setNameTags()
    {
        // Mafia
        if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data.Role.IsImpostor)
        {
            foreach (var player in PlayerControl.AllPlayerControls)
                if (Godfather.godfather != null && Godfather.godfather == player)
                    player.cosmetics.nameText.text = player.Data.PlayerName + " " + ModTranslation.GetString("Game-Mafia", 1);
                else if (Mafioso.mafioso != null && Mafioso.mafioso == player)
                    player.cosmetics.nameText.text = player.Data.PlayerName + " " + ModTranslation.GetString("Game-Mafia", 2);
                else if (Janitor.janitor != null && Janitor.janitor == player)
                    player.cosmetics.nameText.text = player.Data.PlayerName + " " + ModTranslation.GetString("Game-Mafia", 3);
            if (MeetingHud.Instance != null)
                foreach (var player in MeetingHud.Instance.playerStates)
                    if (Godfather.godfather != null && Godfather.godfather.PlayerId == player.PlayerId)
                        player.NameText.text = Godfather.godfather.Data.PlayerName + " " + ModTranslation.GetString("Game-Mafia", 1);
                    else if (Mafioso.mafioso != null && Mafioso.mafioso.PlayerId == player.PlayerId)
                        player.NameText.text = Mafioso.mafioso.Data.PlayerName + " " + ModTranslation.GetString("Game-Mafia", 2);
                    else if (Janitor.janitor != null && Janitor.janitor.PlayerId == player.PlayerId)
                        player.NameText.text = Janitor.janitor.Data.PlayerName + " " + ModTranslation.GetString("Game-Mafia", 3);
        }

        // Lovers
        if (Lovers.lover1 != null && Lovers.lover2 != null && (Lovers.lover1 == PlayerControl.LocalPlayer ||
                                                               Lovers.lover2 == PlayerControl.LocalPlayer))
        {
            var suffix = Helpers.cs(Lovers.color, " ♥");
            Lovers.lover1.cosmetics.nameText.text += suffix;
            Lovers.lover2.cosmetics.nameText.text += suffix;

            if (MeetingHud.Instance != null)
                foreach (var player in MeetingHud.Instance.playerStates)
                    if (Lovers.lover1.PlayerId == player.PlayerId || Lovers.lover2.PlayerId == player.PlayerId)
                        player.NameText.text += suffix;
        }

        // Lawyer or Prosecutor
        if (Lawyer.lawyer != null && Lawyer.target != null && Lawyer.lawyer == PlayerControl.LocalPlayer)
        {
            var color = Lawyer.color;
            var target = Lawyer.target;
            var suffix = Helpers.cs(color, " §");
            target.cosmetics.nameText.text += suffix;

            if (MeetingHud.Instance != null)
                foreach (var player in MeetingHud.Instance.playerStates)
                    if (player.PlayerId == target.PlayerId)
                        player.NameText.text += suffix;
        }

        // Former Thief
        if (Thief.formerThief != null &&
            (Thief.formerThief == PlayerControl.LocalPlayer || PlayerControl.LocalPlayer.Data.IsDead))
        {
            var suffix = Helpers.cs(Thief.color, " $");
            Thief.formerThief.cosmetics.nameText.text += suffix;
            if (MeetingHud.Instance != null)
                foreach (var player in MeetingHud.Instance.playerStates)
                    if (player.PlayerId == Thief.formerThief.PlayerId)
                        player.NameText.text += suffix;
        }

        // Display lighter / darker color for all alive players
        if (PlayerControl.LocalPlayer != null && MeetingHud.Instance != null && TORMapOptions.showLighterDarker)
            foreach (var player in MeetingHud.Instance.playerStates)
            {
                var target = Helpers.playerById(player.PlayerId);
                if (target != null) player.NameText.text += $" ({(Helpers.isLighterColor(target) ? ModTranslation.GetString("Role-Portalmaker", 2) : ModTranslation.GetString("Role-Portalmaker", 3))})";
            }

        // Add medic shield info:
        if (MeetingHud.Instance != null && Medic.medic != null && Medic.shielded != null &&
            Medic.shieldVisible(Medic.shielded))
            foreach (var player in MeetingHud.Instance.playerStates)
                if (player.PlayerId == Medic.shielded.PlayerId)
                    player.NameText.text = Helpers.cs(Medic.color, "[") + player.NameText.text +
                                           Helpers.cs(Medic.color, "]");
        // player.HighlightedFX.color = Medic.color;
        // player.HighlightedFX.enabled = true;
    }

    private static void updateShielded()
    {
        if (Medic.shielded == null) return;

        if (Medic.shielded.Data.IsDead || Medic.medic == null || Medic.medic.Data.IsDead) Medic.shielded = null;
    }

    private static void timerUpdate()
    {
        HideNSeek.timer -= Time.deltaTime;
    }

    public static void miniUpdate()
    {
        if (Mini.mini == null || Camouflager.camouflageTimer > 0f || Helpers.MushroomSabotageActive() ||
            (Mini.mini == Morphling.morphling && Morphling.morphTimer > 0f) ||
            (Mini.mini == Ninja.ninja && Ninja.isInvisble) || SurveillanceMinigamePatch.nightVisionIsActive) return;

        var growingProgress = Mini.growingProgress();
        var scale = growingProgress * 0.35f + 0.35f;
        var suffix = "";
        if (growingProgress != 1f)
            suffix = " <color=#FAD934FF>(" + Mathf.FloorToInt(growingProgress * 18) + ")</color>";
        if (!Mini.isGrowingUpInMeeting && MeetingHud.Instance != null && Mini.ageOnMeetingStart != 0 &&
            !(Mini.ageOnMeetingStart >= 18))
            suffix = " <color=#FAD934FF>(" + Mini.ageOnMeetingStart + ")</color>";

        Mini.mini.cosmetics.nameText.text += suffix;
        if (MeetingHud.Instance != null)
            foreach (var player in MeetingHud.Instance.playerStates)
                if (player.NameText != null && Mini.mini.PlayerId == player.PlayerId)
                    player.NameText.text += suffix;

        if (Morphling.morphling != null && Morphling.morphTarget == Mini.mini && Morphling.morphTimer > 0f)
            Morphling.morphling.cosmetics.nameText.text += suffix;
    }

    private static void updateImpostorKillButton(HudManager __instance)
    {
        if (!PlayerControl.LocalPlayer.Data.Role.IsImpostor) return;
        if (MeetingHud.Instance)
        {
            __instance.KillButton.Hide();
            return;
        }

        var enabled = true;
        if (Vampire.vampire != null && Vampire.vampire == PlayerControl.LocalPlayer)
            enabled = false;
        else if (Mafioso.mafioso != null && Mafioso.mafioso == PlayerControl.LocalPlayer &&
                 Godfather.godfather != null && !Godfather.godfather.Data.IsDead)
            enabled = false;
        else if (Janitor.janitor != null && Janitor.janitor == PlayerControl.LocalPlayer)
            enabled = false;

        if (enabled) __instance.KillButton.Show();
        else __instance.KillButton.Hide();

        if (Deputy.handcuffedKnows.ContainsKey(PlayerControl.LocalPlayer.PlayerId) &&
            Deputy.handcuffedKnows[PlayerControl.LocalPlayer.PlayerId] > 0) __instance.KillButton.Hide();
    }

    private static void updateReportButton(HudManager __instance)
    {
        if (GameOptionsManager.Instance.currentGameOptions.GameMode == GameModes.HideNSeek) return;
        if ((Deputy.handcuffedKnows.ContainsKey(PlayerControl.LocalPlayer.PlayerId) &&
             Deputy.handcuffedKnows[PlayerControl.LocalPlayer.PlayerId] > 0) ||
            MeetingHud.Instance) __instance.ReportButton.Hide();
        else if (!__instance.ReportButton.isActiveAndEnabled) __instance.ReportButton.Show();
    }

    private static void updateVentButton(HudManager __instance)
    {
        if (GameOptionsManager.Instance.currentGameOptions.GameMode == GameModes.HideNSeek) return;
        if ((Deputy.handcuffedKnows.ContainsKey(PlayerControl.LocalPlayer.PlayerId) &&
             Deputy.handcuffedKnows[PlayerControl.LocalPlayer.PlayerId] > 0) ||
            MeetingHud.Instance) __instance.ImpostorVentButton.Hide();
        else if (PlayerControl.LocalPlayer.roleCanUseVents() && !__instance.ImpostorVentButton.isActiveAndEnabled)
            __instance.ImpostorVentButton.Show();
        if (ReInput.players.GetPlayer(0).GetButtonDown(Action.UseVent) &&
            !PlayerControl.LocalPlayer.Data.Role.IsImpostor &&
            PlayerControl.LocalPlayer.roleCanUseVents()) __instance.ImpostorVentButton.DoClick();
    }

    private static void updateUseButton(HudManager __instance)
    {
        if (MeetingHud.Instance) __instance.UseButton.Hide();
    }

    private static void updateSabotageButton(HudManager __instance)
    {
        if (MeetingHud.Instance || HideNSeek.isHideNSeekGM || PropHunt.isPropHuntGM)
            __instance.SabotageButton.Hide();
        if (PlayerControl.LocalPlayer.Data.IsDead && OptionGroupSingleton<GameSettingsOptions>.Instance.DeadImpsBlockSabotage.Value)
            __instance.SabotageButton.Hide();
    }

    private static void updateMapButton(HudManager __instance)
    {
        if (Trapper.trapper == null || !(PlayerControl.LocalPlayer.PlayerId == Trapper.trapper.PlayerId) ||
            __instance == null || __instance.MapButton.HeldButtonSprite == null) return;
        __instance.MapButton.HeldButtonSprite.color = Trapper.playersOnMap.Any() ? Trapper.color : Color.white;
    }

    internal static void Postfix(HudManager __instance)
    {
        if (AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started ||
            GameOptionsManager.Instance.currentGameOptions.GameMode == GameModes.HideNSeek) return;

        EventUtility.Update();

        // Per-frame button ticking is MiraAPI's now (CustomButtonManager -> FixedUpdateHandler
        // -> TorButton.UpdateTor); the old CustomButton.HudUpdate() hook is gone.
        resetNameTagsAndColors();
        setNameColors();
        updateShielded();
        setNameTags();

        // Impostors
        updateImpostorKillButton(__instance);
        // Timer updates
        timerUpdate();
        // Mini
        miniUpdate();

        // Deputy Sabotage, Use and Vent Button Disabling
        updateReportButton(__instance);
        updateVentButton(__instance);
        // Meeting hide buttons if needed (used for the map usage, because closing the map would show buttons)
        updateSabotageButton(__instance);
        updateUseButton(__instance);
        updateMapButton(__instance);
        if (!MeetingHud.Instance) __instance.AbilityButton?.Update();

        // Fix dead player's pets being visible by just always updating whether the pet should be visible at all.
        foreach (var target in PlayerControl.AllPlayerControls)
        {
            var pet = target.GetPet();
            if (pet != null)
                pet.Visible = ((PlayerControl.LocalPlayer.Data.IsDead && target.Data.IsDead) || !target.Data.IsDead) &&
                              !target.inVent;
        }
    }
}