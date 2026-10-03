using System;
using MiraAPI.GameOptions.OptionTypes;
using System.Collections.Generic;
using System.Linq;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheOtherRoles.Buttons;
using TheOtherRoles.Patches;
using UnityEngine;
using MiraAPI.Utilities;

namespace TheOtherRoles.Roles.Impostor;

public class Witch(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = Palette.ImpostorRed;

    public static RoleInfo Info = new(color, RoleId.Witch);

    public static PlayerControl witch;
    public static List<PlayerControl> futureSpelled = new();
    public static PlayerControl currentTarget;
    public static PlayerControl spellCastingTarget;
    public static float cooldown = 30f;
    public static float spellCastingDuration = 2f;
    public static float cooldownAddition = 10f;
    public static float currentCooldownAddition;
    public static bool canSpellAnyone;
    public static bool triggerBothCooldowns = true;
    public static bool witchVoteSavesTargets = true;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Witch;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<WitchOptions>.Instance.SpawnRate;

    public static void clearAndReload()
    {
        witch = null;
        futureSpelled = new List<PlayerControl>();
        currentTarget = spellCastingTarget = null;
        cooldown = OptionGroupSingleton<WitchOptions>.Instance.Cooldown.Value;
        cooldownAddition = OptionGroupSingleton<WitchOptions>.Instance.AdditionalCooldown.Value;
        currentCooldownAddition = 0f;
        canSpellAnyone = OptionGroupSingleton<WitchOptions>.Instance.CanSpellAnyone.Value;
        spellCastingDuration = OptionGroupSingleton<WitchOptions>.Instance.SpellCastingDuration.Value;
        triggerBothCooldowns = OptionGroupSingleton<WitchOptions>.Instance.TriggerBothCooldowns.Value;
        witchVoteSavesTargets = OptionGroupSingleton<WitchOptions>.Instance.VoteSavesTargets.Value;
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        if (Witch.witch == null || Witch.witch != player) return;
        List<PlayerControl> untargetables;
        if (Witch.spellCastingTarget != null)
        {
            untargetables = PlayerControl.AllPlayerControls.ToArray()
                .Where(x => x.PlayerId != Witch.spellCastingTarget.PlayerId)
                .ToList();
        }
        else
        {
            untargetables = new List<PlayerControl>();
            if (Spy.spy != null && !Witch.canSpellAnyone) untargetables.Add(Spy.spy);
            if (Sidekick.wasTeamRed && !Witch.canSpellAnyone) untargetables.Add(Sidekick.sidekick);
            if (Jackal.wasTeamRed && !Witch.canSpellAnyone) untargetables.Add(Jackal.jackal);
        }
        Witch.currentTarget = PlayerControlFixedUpdatePatch.setTarget(!Witch.canSpellAnyone, untargetablePlayers: untargetables);
        PlayerControlFixedUpdatePatch.setPlayerOutline(Witch.currentTarget, Witch.color);
    }
}

/// <summary>
/// The settings of WitchOptions. They live next to the role on purpose: the group is bound
/// to Witch, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class WitchOptions : TorRoleOptionGroup<Witch>
{
    public override uint GroupPriority => 190;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Witch));
    public override Color GroupColor => TorOptionColors.Group(Witch.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.Witch), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedNumberOption Cooldown { get; } =
        new ModdedNumberOption("Opt-Witch,1", 30f, 10f, 120f, 5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption AdditionalCooldown { get; } =
        new ModdedNumberOption("Opt-Witch,2", 10f, 0f, 60f, 5f, MiraNumberSuffixes.None, "0.##");

    public ModdedToggleOption CanSpellAnyone { get; } =
        new ModdedToggleOption("Opt-Witch,3", false);

    public ModdedNumberOption SpellCastingDuration { get; } =
        new ModdedNumberOption("Opt-Witch,4", 1f, 0f, 10f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedToggleOption TriggerBothCooldowns { get; } =
        new ModdedToggleOption("Opt-Witch,5", true);

    public ModdedToggleOption VoteSavesTargets { get; } =
        new ModdedToggleOption("Opt-Witch,6", true);
}

/// <summary>
/// The Witch's "cast spell" button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>witchSpellButton</c>.
/// </summary>
public sealed class WitchSpellButton : TorButton
{
    private static WitchSpellButton witchSpellButton;

    public WitchSpellButton()
    {
        witchSpellButton = this;

        SetSprite(TorAssets.SpellButton);
        PositionOffset = TorButtonPositions.UpperRowLeft;
        Hotkey = KeyCode.F;
        EffectEnabled = true;
        ShowButtonText = true;
        ButtonText = new ButtonText(27);

        RealOnClick = () =>
        {
            if (Witch.currentTarget != null)
            {
                Witch.spellCastingTarget = Witch.currentTarget;
                SoundEffectsManager.play("witchSpell");
            }
        };
        HasButton = () =>
        {
            return Witch.witch != null && Witch.witch == PlayerControl.LocalPlayer &&
                   !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () =>
        {
            if (witchSpellButton.isEffectActive && Witch.spellCastingTarget != Witch.currentTarget)
            {
                Witch.spellCastingTarget = null;
                witchSpellButton.Timer = 0f;
                witchSpellButton.isEffectActive = false;
            }

            return PlayerControl.LocalPlayer.CanMove && Witch.currentTarget != null;
        };
        OnMeetingEnds = () =>
        {
            witchSpellButton.Timer = witchSpellButton.MaxTimer;
            witchSpellButton.isEffectActive = false;
            Witch.spellCastingTarget = null;
        };
    }

    public override float Cooldown => Witch.cooldown;

    public override float EffectDuration => Witch.spellCastingDuration;

    public override void OnEffectEnd()
    {
        if (Witch.spellCastingTarget == null) return;
        var attempt = Helpers.checkMuderAttempt(Witch.witch, Witch.spellCastingTarget);
        if (attempt == MurderAttemptResult.PerformKill)
        {
            PlayerControl.LocalPlayer.RpcSetFutureSpelled(Witch.currentTarget.PlayerId);
        }
        if (attempt == MurderAttemptResult.BlankKill || attempt == MurderAttemptResult.PerformKill)
        {
            Witch.currentCooldownAddition += Witch.cooldownAddition;
            witchSpellButton.MaxTimer = Witch.cooldown + Witch.currentCooldownAddition;
            if (Mini.mini != null && PlayerControl.LocalPlayer == Mini.mini)
                witchSpellButton.MaxTimer *= Mini.isGrownUp() ? 0.66f : 2f;
            witchSpellButton.Timer = witchSpellButton.MaxTimer;
            if (Witch.triggerBothCooldowns)
            {
                var multiplier = Mini.mini != null && PlayerControl.LocalPlayer == Mini.mini
                    ? Mini.isGrownUp() ? 0.66f : 2f
                    : 1f;
                Witch.witch.killTimer = GameOptionsManager.Instance.currentNormalGameOptions.KillCooldown *
                                        multiplier;
            }
        }
        else
        {
            witchSpellButton.Timer = 0f;
        }

        Witch.spellCastingTarget = null;
    }
}

public static class WitchRpcs
{
    [MethodRpc((uint)TorRpc.SetFutureSpelled, LocalHandling = RpcLocalHandling.After)]
    public static void RpcSetFutureSpelled(this PlayerControl player, byte playerId)
    {
        var target = Helpers.playerById(playerId);
        if (Witch.futureSpelled == null)
            Witch.futureSpelled = new List<PlayerControl>();
        if (target != null) Witch.futureSpelled.Add(target);
    }
}
