using System;
using MiraAPI.GameOptions.OptionTypes;
using UnityEngine;
using MiraAPI.Utilities;
using TheOtherRoles.Buttons;

namespace TheOtherRoles.Roles.Impostor;

public class Cleaner(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = Palette.ImpostorRed;

    public static RoleInfo Info = new(color, RoleId.Cleaner);

    public static PlayerControl cleaner;
    public static float cooldown = 30f;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Cleaner;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<CleanerOptions>.Instance.SpawnRate;

    public static void clearAndReload()
    {
        cleaner = null;
        cooldown = OptionGroupSingleton<CleanerOptions>.Instance.Cooldown.Value;
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;
}

/// <summary>
/// The settings of CleanerOptions. They live next to the role on purpose: the group is bound
/// to Cleaner, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class CleanerOptions : TorRoleOptionGroup<Cleaner>
{
    public override uint GroupPriority => 160;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Cleaner));
    public override Color GroupColor => TorOptionColors.Group(Cleaner.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.Cleaner), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedNumberOption Cooldown { get; } =
        new ModdedNumberOption("Opt-Cleaner,1", 30f, 10f, 60f, 2.5f, MiraNumberSuffixes.None, "0.##");
}

/// <summary>
/// The Cleaner's clean body button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>cleanerCleanButton</c>.
/// </summary>
public sealed class CleanerCleanButton : TorButton
{
    private static CleanerCleanButton cleanerCleanButton;

    public CleanerCleanButton()
    {
        cleanerCleanButton = this;

        SetSprite(TorAssets.CleanButton);
        PositionOffset = TorButtonPositions.UpperRowLeft;
        Hotkey = KeyCode.F;
        ShowButtonText = true;
        ButtonText = new ButtonText(2);

        RealOnClick = () =>
        {
            foreach (var collider2D in Physics2D.OverlapCircleAll(PlayerControl.LocalPlayer.GetTruePosition(),
                         PlayerControl.LocalPlayer.MaxReportDistance, Constants.PlayersOnlyMask))
                if (collider2D.tag == "DeadBody")
                {
                    var component = collider2D.GetComponent<DeadBody>();
                    if (component && !component.Reported)
                    {
                        var truePosition = PlayerControl.LocalPlayer.GetTruePosition();
                        var truePosition2 = component.TruePosition;
                        if (Vector2.Distance(truePosition2, truePosition) <=
                            PlayerControl.LocalPlayer.MaxReportDistance && PlayerControl.LocalPlayer.CanMove &&
                            !PhysicsHelpers.AnythingBetween(truePosition, truePosition2,
                                Constants.ShipAndObjectsMask, false))
                        {
                            var playerInfo = GameData.Instance.GetPlayerById(component.ParentId);

                            PlayerControl.LocalPlayer.RpcCleanBody(playerInfo.PlayerId, Cleaner.cleaner.PlayerId);

                            Cleaner.cleaner.killTimer = cleanerCleanButton.Timer = cleanerCleanButton.MaxTimer;
                            SoundEffectsManager.play("cleanerClean");
                            break;
                        }
                    }
                }
        };
        HasButton = () =>
        {
            return Cleaner.cleaner != null && Cleaner.cleaner == PlayerControl.LocalPlayer &&
                   !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () =>
        {
            return HudManager.Instance.ReportButton.graphic.color == Palette.EnabledColor &&
                   PlayerControl.LocalPlayer.CanMove;
        };
        OnMeetingEnds = () => { cleanerCleanButton.Timer = cleanerCleanButton.MaxTimer; };
    }

    public override float Cooldown => Cleaner.cooldown;
}
