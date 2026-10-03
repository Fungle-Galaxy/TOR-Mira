using System;
using Object = UnityEngine.Object;
using MiraAPI.GameOptions.OptionTypes;
using System.Collections.Generic;
using TheOtherRoles.Objects;
using TheOtherRoles.Utilities;
using TheOtherRoles.Networking;
using TMPro;
using UnityEngine;
using MiraAPI.Utilities;

namespace TheOtherRoles.Roles.Impostor;

public class BountyHunter(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = Palette.ImpostorRed;

    public static RoleInfo Info = new(color, RoleId.BountyHunter);

    public static PlayerControl bountyHunter;
    public static Arrow arrow;
    public static float bountyDuration = 30f;
    public static bool showArrow = true;
    public static float bountyKillCooldown;
    public static float punishmentTime = 15f;
    public static float arrowUpdateIntervall = 10f;

    public static float arrowUpdateTimer;
    public static float bountyUpdateTimer;
    public static PlayerControl bounty;
    public static TextMeshPro cooldownText;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.BountyHunter;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<BountyHunterOptions>.Instance.SpawnRate;

    public static void clearAndReload()
    {
        bountyHunter = null;
        bounty = null;
        arrowUpdateTimer = 0f;
        bountyUpdateTimer = 0f;
        if (arrow != null && arrow.arrow != null) Object.Destroy(arrow.arrow);
        arrow = null;
        if (cooldownText != null && cooldownText.gameObject != null) Object.Destroy(cooldownText.gameObject);
        cooldownText = null;
        foreach (var p in TORMapOptions.playerIcons.Values)
            if (p != null && p.gameObject != null)
                p.gameObject.SetActive(false);

        bountyDuration = OptionGroupSingleton<BountyHunterOptions>.Instance.BountyDuration.Value;
        bountyKillCooldown = OptionGroupSingleton<BountyHunterOptions>.Instance.ReducedCooldown.Value;
        punishmentTime = OptionGroupSingleton<BountyHunterOptions>.Instance.PunishmentTime.Value;
        showArrow = OptionGroupSingleton<BountyHunterOptions>.Instance.ShowArrow.Value;
        arrowUpdateIntervall = OptionGroupSingleton<BountyHunterOptions>.Instance.ArrowUpdateIntervall.Value;
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        if (BountyHunter.bountyHunter == null || player != BountyHunter.bountyHunter) return;
        if (BountyHunter.bountyHunter.Data.IsDead)
        {
            if (BountyHunter.arrow != null || BountyHunter.arrow.arrow != null)
                Object.Destroy(BountyHunter.arrow.arrow);
            BountyHunter.arrow = null;
            if (BountyHunter.cooldownText != null && BountyHunter.cooldownText.gameObject != null)
                Object.Destroy(BountyHunter.cooldownText.gameObject);
            BountyHunter.cooldownText = null;
            BountyHunter.bounty = null;
            foreach (var p in TORMapOptions.playerIcons.Values)
                if (p != null && p.gameObject != null) p.gameObject.SetActive(false);
            return;
        }
        BountyHunter.arrowUpdateTimer -= Time.fixedDeltaTime;
        BountyHunter.bountyUpdateTimer -= Time.fixedDeltaTime;
        if (BountyHunter.bounty == null || BountyHunter.bountyUpdateTimer <= 0f)
        {
            BountyHunter.bounty = null;
            BountyHunter.arrowUpdateTimer = 0f;
            BountyHunter.bountyUpdateTimer = BountyHunter.bountyDuration;
            var possibleTargets = new List<PlayerControl>();
            foreach (var p in PlayerControl.AllPlayerControls)
                if (!p.Data.IsDead && !p.Data.Disconnected && p != p.Data.Role.IsImpostor && p != Spy.spy &&
                    (p != Sidekick.sidekick || !Sidekick.wasTeamRed) && (p != Jackal.jackal || !Jackal.wasTeamRed) &&
                    (p != Mini.mini || Mini.isGrownUp()) && (Lovers.getPartner(BountyHunter.bountyHunter) == null ||
                                                             p != Lovers.getPartner(BountyHunter.bountyHunter)))
                    possibleTargets.Add(p);
            BountyHunter.bounty = possibleTargets[Helpers.rnd.Next(0, possibleTargets.Count)];
            if (BountyHunter.bounty == null) return;
            PlayerControl.LocalPlayer.RpcGhostBountyTarget(PlayerControl.LocalPlayer.PlayerId,
                BountyHunter.bounty.PlayerId);
            if (FastDestroyableSingleton<HudManager>.Instance != null &&
                FastDestroyableSingleton<HudManager>.Instance.UseButton != null)
            {
                foreach (var pp in TORMapOptions.playerIcons.Values) pp.gameObject.SetActive(false);
                if (TORMapOptions.playerIcons.ContainsKey(BountyHunter.bounty.PlayerId) &&
                    TORMapOptions.playerIcons[BountyHunter.bounty.PlayerId].gameObject != null)
                    TORMapOptions.playerIcons[BountyHunter.bounty.PlayerId].gameObject.SetActive(true);
            }
        }
        if (MeetingHud.Instance && TORMapOptions.playerIcons.ContainsKey(BountyHunter.bounty?.PlayerId ?? byte.MaxValue) &&
            BountyHunter.bounty != null && TORMapOptions.playerIcons[BountyHunter.bounty.PlayerId].gameObject != null)
            TORMapOptions.playerIcons[BountyHunter.bounty.PlayerId].gameObject.SetActive(false);
        if (BountyHunter.cooldownText != null)
        {
            BountyHunter.cooldownText.text = Mathf
                .CeilToInt(Mathf.Clamp(BountyHunter.bountyUpdateTimer, 0, BountyHunter.bountyDuration)).ToString();
            BountyHunter.cooldownText.gameObject.SetActive(!MeetingHud.Instance);
        }
        if (BountyHunter.showArrow && BountyHunter.bounty != null)
        {
            if (BountyHunter.arrow == null) BountyHunter.arrow = new Arrow(Color.red);
            BountyHunter.arrow.arrow.SetActive(true);
            if (BountyHunter.arrowUpdateTimer <= 0f)
            {
                BountyHunter.arrow.Update(BountyHunter.bounty.transform.position);
                BountyHunter.arrowUpdateTimer = BountyHunter.arrowUpdateIntervall;
            }
            BountyHunter.arrow.Update();
        }
        else if (BountyHunter.arrow != null && BountyHunter.arrow.arrow != null)
        {
            BountyHunter.arrow.arrow.SetActive(false);
        }
    }
}

/// <summary>
/// The settings of BountyHunterOptions. They live next to the role on purpose: the group is bound
/// to BountyHunter, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class BountyHunterOptions : TorRoleOptionGroup<BountyHunter>
{
    public override uint GroupPriority => 180;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.BountyHunter));
    public override Color GroupColor => TorOptionColors.Group(BountyHunter.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.BountyHunter), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedNumberOption BountyDuration { get; } =
        new ModdedNumberOption("Opt-BountyHunter,1", 60f, 10f, 180f, 10f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption ReducedCooldown { get; } =
        new ModdedNumberOption("Opt-BountyHunter,2", 2.5f, 0f, 30f, 2.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption PunishmentTime { get; } =
        new ModdedNumberOption("Opt-BountyHunter,3", 20f, 0f, 60f, 2.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedToggleOption ShowArrow { get; } =
        new ModdedToggleOption("Opt-BountyHunter,4", true);

    public ModdedNumberOption ArrowUpdateIntervall { get; } =
        new ModdedNumberOption("Opt-BountyHunter,5", 15f, 2.5f, 60f, 2.5f, MiraNumberSuffixes.None, "0.##")
        {
            Visible = () => OptionGroupSingleton<BountyHunterOptions>.Instance.ShowArrow.Value
        };
}
