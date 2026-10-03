using MiraAPI.GameOptions.OptionTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;
using TheOtherRoles.Buttons;
using TheOtherRoles.Networking;
using TheOtherRoles.Utilities;
using UnityEngine;
using MiraAPI.Utilities;

namespace TheOtherRoles.Roles.Crewmate;

public class Medium(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = new Color32(98, 120, 115, byte.MaxValue);

    public static RoleInfo Info = new(color, RoleId.Medium);

    public static PlayerControl medium;
    public static DeadPlayer target;
    public static DeadPlayer soulTarget;
    public static List<Tuple<DeadPlayer, Vector3>> deadBodies = new();
    public static List<Tuple<DeadPlayer, Vector3>> futureDeadBodies = new();
    public static List<SpriteRenderer> souls = new();
    public static DateTime meetingStartTime = DateTime.UtcNow;

    public static float cooldown = 30f;
    public static float duration = 3f;
    public static bool oneTimeUse;
    public static float chanceAdditionalInfo;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Medium;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<MediumOptions>.Instance.SpawnRate;

    public static void clearAndReload()
    {
        medium = null;
        target = null;
        soulTarget = null;
        deadBodies = new List<Tuple<DeadPlayer, Vector3>>();
        futureDeadBodies = new List<Tuple<DeadPlayer, Vector3>>();
        souls = new List<SpriteRenderer>();
        meetingStartTime = DateTime.UtcNow;
        cooldown = OptionGroupSingleton<MediumOptions>.Instance.Cooldown.Value;
        duration = OptionGroupSingleton<MediumOptions>.Instance.Duration.Value;
        oneTimeUse = OptionGroupSingleton<MediumOptions>.Instance.OneTimeUse.Value;
        chanceAdditionalInfo = OptionGroupSingleton<MediumOptions>.Instance.ChanceAdditionalInfo.Selection() / (float)TorOptions.MaxRate;
    }

    public static string getInfo(PlayerControl target, PlayerControl killer, DeadPlayer.CustomDeathReason deathReason)
    {
        string msg = "";

        List<SpecialMediumInfo> infos = new List<SpecialMediumInfo>();
        // collect fitting death info types.
        // suicides:
        if (killer == target)
        {
            if ((target == Sheriff.sheriff || target == Sheriff.formerSheriff) && deathReason != DeadPlayer.CustomDeathReason.LoverSuicide) infos.Add(SpecialMediumInfo.SheriffSuicide); if (target == Lovers.lover1 || target == Lovers.lover2) infos.Add(SpecialMediumInfo.PassiveLoverSuicide);
            if (target == Lovers.lover1 || target == Lovers.lover2) infos.Add(SpecialMediumInfo.PassiveLoverSuicide);
            if (target == Thief.thief && deathReason != DeadPlayer.CustomDeathReason.LoverSuicide) infos.Add(SpecialMediumInfo.ThiefSuicide);
            if (target == Warlock.warlock && deathReason != DeadPlayer.CustomDeathReason.LoverSuicide) infos.Add(SpecialMediumInfo.WarlockSuicide);
        }
        else
        {
            if (target == Lovers.lover1 || target == Lovers.lover2) infos.Add(SpecialMediumInfo.ActiveLoverDies);
            if (target.Data.Role.IsImpostor && killer.Data.Role.IsImpostor && Thief.formerThief != killer) infos.Add(SpecialMediumInfo.ImpostorTeamkill);
        }
        if (target == Sidekick.sidekick && (killer == Jackal.jackal || Jackal.formerJackals.Any(x => x.PlayerId == killer.PlayerId))) infos.Add(SpecialMediumInfo.JackalKillsSidekick);
        if (target == Lawyer.lawyer && killer == Lawyer.target) infos.Add(SpecialMediumInfo.LawyerKilledByClient);
        if (Medium.target.wasCleaned) infos.Add(SpecialMediumInfo.BodyCleaned);

        if (infos.Count > 0)
        {
            var selectedInfo = infos[Helpers.rnd.Next(infos.Count)];
            switch (selectedInfo)
            {
                case SpecialMediumInfo.SheriffSuicide:
                    msg = ModTranslation.GetString("Opt-Medium", 5);
                    break;
                case SpecialMediumInfo.WarlockSuicide:
                    msg = ModTranslation.GetString("Opt-Medium", 6);
                    break;
                case SpecialMediumInfo.ThiefSuicide:
                    msg = ModTranslation.GetString("Opt-Medium", 7);
                    break;
                case SpecialMediumInfo.ActiveLoverDies:
                    msg = ModTranslation.GetString("Opt-Medium", 8);
                    break;
                case SpecialMediumInfo.PassiveLoverSuicide:
                    msg = ModTranslation.GetString("Opt-Medium", 9);
                    break;
                case SpecialMediumInfo.LawyerKilledByClient:
                    msg = ModTranslation.GetString("Opt-Medium", 10);
                    break;
                case SpecialMediumInfo.JackalKillsSidekick:
                    msg = ModTranslation.GetString("Opt-Medium", 11);
                    break;
                case SpecialMediumInfo.ImpostorTeamkill:
                    msg = ModTranslation.GetString("Opt-Medium", 12);
                    break;
                case SpecialMediumInfo.BodyCleaned:
                    msg = ModTranslation.GetString("Opt-Medium", 13);
                    break;
            }
        }
        else
        {
            var randomNumber = Helpers.rnd.Next(4);
            var typeOfColor = Helpers.isLighterColor(Medium.target.killerIfExisting) ? ModTranslation.GetString("Opt-Medium", 18) : ModTranslation.GetString("Opt-Medium", 19);
            var timeSinceDeath = (float)(meetingStartTime - Medium.target.timeOfDeath).TotalMilliseconds;
            var roleString = CustomRoleManager.GetRolesString(Medium.target.player, false);
            if (randomNumber == 0)
            {
                if (!roleString.Contains(ModTranslation.GetRoleName(RoleId.Impostor).GetString()) && !roleString.Contains(ModTranslation.GetRoleName(RoleId.Crewmate).GetString()))
                    msg = string.Format(ModTranslation.GetString("Opt-Medium", 14), roleString);
                else
                    msg = string.Format(ModTranslation.GetString("Opt-Medium", 15), typeOfColor);
            }
            else if (randomNumber == 1) msg = string.Format(ModTranslation.GetString("Opt-Medium", 20), typeOfColor);
            else if (randomNumber == 2) msg = string.Format(ModTranslation.GetString("Opt-Medium", 16), Math.Round(timeSinceDeath / 1000));
            else msg = string.Format(ModTranslation.GetString("Opt-Medium", 17), CustomRoleManager.GetRolesString(Medium.target.killerIfExisting, false, false, true));
        }

        if (Helpers.rnd.NextDouble() < chanceAdditionalInfo)
        {
            var count = 0;
            var alivePlayersList = PlayerControl.AllPlayerControls.ToArray().Where(pc => !pc.Data.IsDead);
            string msgTemplate = "";
            switch (Helpers.rnd.Next(3))
            {
                case 0:
                    count = alivePlayersList.Where(pc =>
                        pc.Data.Role.IsImpostor ||
                        new List<RoleInfo> { Jackal.Info, Sidekick.Info, Sheriff.Info, Thief.Info }.Contains(
                            CustomRoleManager.getRoleInfoForPlayer(pc, false).FirstOrDefault())).Count();
                    msgTemplate = count == 1
                        ? ModTranslation.GetString("Opt-Medium", 21)
                        : ModTranslation.GetString("Opt-Medium", 22);
                    break;
                case 1:
                    count = alivePlayersList.Where(Helpers.roleCanUseVents).Count();
                    msgTemplate = count == 1
                        ? ModTranslation.GetString("Opt-Medium", 23)
                        : ModTranslation.GetString("Opt-Medium", 24);
                    break;
                case 2:
                    count = alivePlayersList.Where(pc =>
                            Helpers.isNeutral(pc) && pc != Jackal.jackal && pc != Sidekick.sidekick &&
                            pc != Thief.thief)
                        .Count();
                    msgTemplate = count == 1
                        ? ModTranslation.GetString("Opt-Medium", 25)
                        : ModTranslation.GetString("Opt-Medium", 26);
                    break;
            }
            msg += "\n" + string.Format(msgTemplate, count);
        }

        return string.Format(ModTranslation.GetString("Opt-Medium", 27) + "\n", Medium.target.player.Data.PlayerName) + msg;
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        if (Medium.medium == null || Medium.medium != player || Medium.medium.Data.IsDead ||
            Medium.deadBodies == null || MapUtilities.CachedShipStatus?.AllVents == null) return;
        DeadPlayer target = null;
        var truePosition = player.GetTruePosition();
        var closestDistance = float.MaxValue;
        var usableDistance = MapUtilities.CachedShipStatus.AllVents.FirstOrDefault().UsableDistance;
        foreach (var (dp, ps) in Medium.deadBodies)
        {
            var distance = Vector2.Distance(ps, truePosition);
            if (distance <= usableDistance && distance < closestDistance)
            {
                closestDistance = distance;
                target = dp;
            }
        }
        Medium.target = target;
    }

    private enum SpecialMediumInfo
    {
        SheriffSuicide,
        ThiefSuicide,
        ActiveLoverDies,
        PassiveLoverSuicide,
        LawyerKilledByClient,
        JackalKillsSidekick,
        ImpostorTeamkill,
        SubmergedO2,
        WarlockSuicide,
        BodyCleaned
    }
}

/// <summary>
/// The settings of MediumOptions. They live next to the role on purpose: the group is bound
/// to Medium, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class MediumOptions : TorRoleOptionGroup<Medium>
{
    public override uint GroupPriority => 550;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Medium));
    public override Color GroupColor => TorOptionColors.Group(Medium.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.Medium), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedNumberOption Cooldown { get; } =
        new ModdedNumberOption("Opt-Medium,1", 30f, 5f, 120f, 5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption Duration { get; } =
        new ModdedNumberOption("Opt-Medium,2", 3f, 0f, 15f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedToggleOption OneTimeUse { get; } =
        new ModdedToggleOption("Opt-Medium,3", false);

    public ModdedStringOption ChanceAdditionalInfo { get; } =
        new ModdedStringOption("Opt-Medium,4", TorOptions.Rates[0], TorOptions.Rates);
}

/// <summary>
/// The Medium's "ask soul" button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>mediumButton</c>.
/// </summary>
public sealed class MediumButton : TorButton
{
    private static MediumButton mediumButton;

    public MediumButton()
    {
        mediumButton = this;

        SetSprite(TorAssets.MediumButton);
        PositionOffset = TorButtonPositions.LowerRowRight;
        Hotkey = KeyCode.F;
        EffectEnabled = true;
        ShowButtonText = true;

        RealOnClick = () =>
        {
            if (Medium.target != null)
            {
                Medium.soulTarget = Medium.target;
                mediumButton.EffectEnabled = true;
                SoundEffectsManager.play("mediumAsk");
            }
        };
        HasButton = () =>
        {
            return Medium.medium != null && Medium.medium == PlayerControl.LocalPlayer &&
                   !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () =>
        {
            if (mediumButton.isEffectActive && Medium.target != Medium.soulTarget)
            {
                Medium.soulTarget = null;
                mediumButton.Timer = 0f;
                mediumButton.isEffectActive = false;
            }

            return Medium.target != null && PlayerControl.LocalPlayer.CanMove;
        };
        OnMeetingEnds = () =>
        {
            mediumButton.Timer = mediumButton.MaxTimer;
            mediumButton.isEffectActive = false;
            Medium.soulTarget = null;
        };
    }

    public override float Cooldown => Medium.cooldown;

    public override float EffectDuration => Medium.duration;

    public override void OnEffectEnd()
    {
        mediumButton.Timer = mediumButton.MaxTimer;
        if (Medium.target == null || Medium.target.player == null) return;
        var msg = Medium.getInfo(Medium.target.player, Medium.target.killerIfExisting,
            Medium.target.deathReason);
        FastDestroyableSingleton<HudManager>.Instance.Chat.AddChat(PlayerControl.LocalPlayer, msg);

        // Ghost Info
        PlayerControl.LocalPlayer.RpcGhostMediumInfo(Medium.target.player.PlayerId, msg);

        // Remove soul
        if (Medium.oneTimeUse)
        {
            var closestDistance = float.MaxValue;
            SpriteRenderer target = null;

            foreach (var (db, ps) in Medium.deadBodies)
                if (db == Medium.target)
                {
                    var deadBody = Tuple.Create(db, ps);
                    Medium.deadBodies.Remove(deadBody);
                    break;
                }

            foreach (var rend in Medium.souls)
            {
                var distance = Vector2.Distance(rend.transform.position,
                    PlayerControl.LocalPlayer.GetTruePosition());
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    target = rend;
                }
            }

            FastDestroyableSingleton<HudManager>.Instance.StartCoroutine(Effects.Lerp(5f, new Action<float>(p =>
            {
                if (target != null)
                {
                    var tmp = target.color;
                    tmp.a = Mathf.Clamp01(1 - p);
                    target.color = tmp;
                }

                if (p == 1f && target != null && target.gameObject != null) Object.Destroy(target.gameObject);
            })));

            Medium.souls.Remove(target);
        }

        SoundEffectsManager.stop("mediumAsk");
    }

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        ButtonText = new ButtonText(25,
            FastDestroyableSingleton<HudManager>.Instance.UseButton.fastUseSettings[ImageNames.UseButton]
                .FontMaterial);
    }
}
