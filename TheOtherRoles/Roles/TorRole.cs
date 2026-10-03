using System;
using System.Globalization;
using System.Linq;
using System.Text;
using MiraAPI.GameModes;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.PluginLoading;
using MiraAPI.Roles;
using UnityEngine;

namespace TheOtherRoles.Roles;

public interface ITorRole : ICustomRole
{
    /// <summary>TOR's own role id. Drives TOR's translation keys and TOR's role sync RPC.</summary>
    RoleId TorRoleId { get; }

    /// <summary>TOR's legacy <see cref="RoleInfo"/>, so every existing TOR UI keeps working.</summary>
    RoleInfo GetRoleInfo();
}

public interface ITorRoleLifecycle
{
    RoleInfo GetRoleInfo();

    void ClearAndReload();

    void PlayerFixedUpdate(PlayerControl player);

    void PlayerUpdate(PlayerControl player);

    void OnMeetingStart();

    void OnMeetingEnd();

    void OnPlayerExiled(PlayerControl player);

    void OnPlayerDeath(PlayerControl player);

    void OnMurderPlayer(PlayerControl killer, PlayerControl victim);

    bool CanUseVent(PlayerControl player, Vent vent);

    bool CanKill(PlayerControl killer, PlayerControl target);

    void SetTarget(PlayerControl target);

    void OnClickButton();
}

[MiraIgnore]
public abstract class TorRoleBehaviour : RoleBehaviour, ITorRole, ITorRoleLifecycle
{
    protected TorRoleBehaviour(IntPtr cppPtr)
        : base(cppPtr)
    {
    }

    [HideFromIl2Cpp]
    public abstract RoleId TorRoleId { get; }

    [HideFromIl2Cpp]
    public abstract RoleInfo GetRoleInfo();

    // TOR's string table is fed into MiraLocaleManager as "Category,Id".
    public virtual string IdPrefix => "TheOtherRolesMira.Role";

    public virtual string IdPart => TorRoleId.ToString();

    public virtual string RoleNameLocale => $"Role-Name,{(int)TorRoleId}";

    public virtual string RoleDescriptionLocale => $"Role-IntroDesc,{(int)TorRoleId}";

    public virtual string RoleMedDescriptionLocale => $"Role-ShortDesc,{(int)TorRoleId}";

    public virtual string RoleLongDescriptionLocale => $"Role-Desc,{(int)TorRoleId}";

    public virtual string RoleWikiDescriptionLocale => $"Role-Desc,{(int)TorRoleId}";

    public virtual string RoleDescription => ModTranslation.GetString("Role-IntroDesc", (int)TorRoleId);

    public virtual string RoleMedDescription => ModTranslation.GetString("Role-ShortDesc", (int)TorRoleId);

    public virtual string RoleLongDescription => ModTranslation.GetString("Role-Desc", (int)TorRoleId);

    public virtual string RoleWikiDescription => ModTranslation.GetString("Role-Desc", (int)TorRoleId);

    /// <summary>
    /// The role tab is a narrow panel: the long description overflows the screen, so the tab shows
    /// the short one instead (the header stays Mira API's <c>Your role is &lt;b&gt;...&lt;/b&gt;</c>).
    /// </summary>
    [HideFromIl2Cpp]
    public virtual StringBuilder SetTabText()
    {
        var text = new StringBuilder();
        text.AppendLine(CultureInfo.InvariantCulture,
            $"{RoleColor.ToTextColor()}Your role is <b>{((ICustomRole)this).RoleName}.</b></color>");
        text.Append("<size=70%>");
        text.AppendLine(RoleMedDescription);
        return text;
    }

    public override bool DidWin(GameOverReason reason) => Patches.TorEndGameWinners.DidWin(this, reason);

    public virtual Color RoleColor => GetRoleInfo().color;

    public virtual ModdedRoleTeams Team => GetRoleInfo().isNeutral
        ? ModdedRoleTeams.Custom
        : GetRoleInfo().isImpostor
            ? ModdedRoleTeams.Impostor
            : ModdedRoleTeams.Crewmate;

    [HideFromIl2Cpp]
    protected CustomRoleConfiguration DefaultConfiguration() => new(this)
    {
        CanGetKilled = true,
        MaxRoleCount = 0,
        DefaultRoleCount = 0,
        CanModifyChance = !ChanceControlledByAnotherRole,
        TasksCountForProgress = Team != ModdedRoleTeams.Impostor,
        RoleHintType = RoleHintType.RoleTab,
        HideSettings = HideRoleSettings,
    };

    [HideFromIl2Cpp]
    protected virtual bool HideRoleSettings => false;

    [HideFromIl2Cpp]
    protected virtual bool ChanceControlledByAnotherRole => false;

    [HideFromIl2Cpp]
    public virtual CustomRoleConfiguration Configuration => DefaultConfiguration();

    /// <summary>
    /// Mira API's own answer, redeclared here only so TOR roles can gate themselves on something
    /// other than the game mode (see the dependent roles below). Returning false keeps the role out
    /// of Mira API's roll without touching the chance shown on its row.
    /// </summary>
    [HideFromIl2Cpp]
    public virtual bool CanSpawnOnCurrentMode() =>
        Configuration.AssociatedGameMode.IsInstanceOfType(CustomGameModeManager.ActiveMode);

    [HideFromIl2Cpp]
    public virtual bool? ForceShowRoleOnWiki => null;

    [HideFromIl2Cpp]
    protected virtual ModdedStringOption SpawnRateOption => null;

    [HideFromIl2Cpp]
    public bool HasSpawnRateOption => SpawnRateOption != null;

    public virtual int? GetChance()
    {
        var option = SpawnRateOption;
        return option == null ? 0 : option.Selection() * TorOptions.RateStep;
    }

    public virtual void SetChance(int chance)
    {
        var option = SpawnRateOption;
        if (option == null) return;

        var index = Mathf.Clamp(
            Mathf.RoundToInt(chance / (float)TorOptions.RateStep), 0, TorOptions.Rates.Length - 1);
        var value = TorOptions.Rates[index];

        if (option.Value == value) return;
        option.SetValue(value);
    }

    public virtual int? GetCount() => 1;

    /// <summary>Intentionally ignored - TOR has no per-role count. See <see cref="GetCount"/>.</summary>
    public virtual void SetCount(int count)
    {
    }
    
    // RoleBehaviour defaults to "dead" (it is the base of the ghost roles), TOR roles are alive.
    public override bool IsDead => false;

    public override void OnMeetingStart()
    {
    }

    public virtual void ClearAndReload()
    {
    }

    public virtual void PlayerFixedUpdate(PlayerControl player)
    {
    }

    public virtual void PlayerUpdate(PlayerControl player)
    {
    }

    public virtual void OnMeetingEnd()
    {
    }

    public virtual void OnPlayerExiled(PlayerControl player)
    {
    }

    public virtual void OnPlayerDeath(PlayerControl player)
    {
    }

    public virtual void OnMurderPlayer(PlayerControl killer, PlayerControl victim)
    {
    }

    public virtual bool CanUseVent(PlayerControl player, Vent vent)
    {
        return false;
    }

    public virtual bool CanKill(PlayerControl killer, PlayerControl target)
    {
        return false;
    }

    public virtual void SetTarget(PlayerControl target)
    {
    }

    public virtual void OnClickButton()
    {
    }
}

public static class TorSpawn
{
    /// <summary>
    /// How many impostor seats the current options open up. Readable from the lobby too, where no
    /// player has a role yet - which is what makes it usable from <see cref="TorRoleBehaviour.CanSpawnOnCurrentMode"/>.
    /// </summary>
    public static int ImpostorSeats
    {
        get
        {
            var players = GameData.Instance != null ? GameData.Instance.AllPlayers.Count : 15;
            return Mathf.Max(1, GameOptionsManager.Instance.CurrentGameOptions.GetAdjustedNumImpostors(players));
        }
    }

    /// <summary>Live impostor players, used once the game is running.</summary>
    public static int ImpostorsInGame =>
        GameData.Instance == null
            ? ImpostorSeats
            : GameData.Instance.AllPlayers.ToArray().Count(x => x != null && x.Role != null && x.Role.IsImpostor);
}
