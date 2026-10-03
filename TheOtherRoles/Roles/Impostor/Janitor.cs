using System;
using MiraAPI.GameOptions.OptionTypes;
using UnityEngine;

using MiraAPI.Utilities;
using TheOtherRoles.Buttons;
namespace TheOtherRoles.Roles.Impostor;

public class Janitor(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = Palette.ImpostorRed;

    public static RoleInfo Info = new(color, RoleId.Janitor);

    public static PlayerControl janitor;
    public static float cooldown = 30f;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Janitor;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<MafiaOptions>.Instance.SpawnRate;

    [HideFromIl2Cpp]
    protected override bool ChanceControlledByAnotherRole => true;

    // Mira API must not roll the family piecemeal: TOR only ever hands out the Janitor when a
    // Godfather is already sitting at the table.
    public override bool CanSpawnOnCurrentMode() => false;

    public override bool? ForceShowRoleOnWiki => true;

    public static void clearAndReload()
    {
        janitor = null;
        cooldown = OptionGroupSingleton<JanitorOptions>.Instance.JanitorCooldown.Value;
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;
}

/// <summary>
/// The Janitor's settings. They used to live inside <c>MafiaOptions</c>; the Mafia's spawn rate
/// stays there (it is one chance for the whole crew) while the Janitor's own page lives here.
/// </summary>
public class JanitorOptions : TorRoleOptionGroup<Janitor>
{
    public override uint GroupPriority => 101;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Janitor));
    public override Color GroupColor => TorOptionColors.Group(Janitor.color);

    public ModdedNumberOption JanitorCooldown { get; } =
        new ModdedNumberOption("Opt-Mafia,2", 30f, 10f, 60f, 2.5f, MiraNumberSuffixes.None, "0.##");
}

/// <summary>
/// The Janitor's "clean body" button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>janitorCleanButton</c>.
/// </summary>
public sealed class JanitorCleanButton : TorButton
{
    private static JanitorCleanButton janitorCleanButton;

    public JanitorCleanButton()
    {
        janitorCleanButton = this;

        SetSprite(TorAssets.CleanButton);
        PositionOffset = TorButtonPositions.UpperRowLeft;
        Hotkey = KeyCode.F;
        ShowButtonText = true;

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

                            PlayerControl.LocalPlayer.RpcCleanBody(playerInfo.PlayerId, Janitor.janitor.PlayerId);
                            janitorCleanButton.Timer = janitorCleanButton.MaxTimer;
                            SoundEffectsManager.play("cleanerClean");

                            break;
                        }
                    }
                }
        };
        HasButton = () =>
        {
            return Janitor.janitor != null && Janitor.janitor == PlayerControl.LocalPlayer &&
                   !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () =>
        {
            return HudManager.Instance.ReportButton.graphic.color == Palette.EnabledColor &&
                   PlayerControl.LocalPlayer.CanMove;
        };
        OnMeetingEnds = () => { janitorCleanButton.Timer = janitorCleanButton.MaxTimer; };
    }

    public override float Cooldown => Janitor.cooldown;
}
