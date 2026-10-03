using System;
using Object = UnityEngine.Object;
using MiraAPI.GameOptions.OptionTypes;
using System.Collections.Generic;
using AmongUs.GameOptions;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using TheOtherRoles.Buttons;
using TheOtherRoles.Objects;
using UnityEngine;
using MiraAPI.Utilities;
using TheOtherRoles.Utilities;

namespace TheOtherRoles.Roles.Neutral;

public class Vulture(IntPtr cppPtr) : TorRoleBehaviour(cppPtr)
{
    public static Color color = new Color32(139, 69, 19, byte.MaxValue);
    public static RoleInfo Info = new(color, RoleId.Vulture, isNeutral: true);

    public static PlayerControl vulture;
    public static List<Arrow> localArrows = new();
    public static float cooldown = 30f;
    public static int vultureNumberToWin = 4;
    public static int eatenBodies;
    public static bool triggerVultureWin;
    public static bool canUseVents = true;
    public static bool showArrows = true;

    [HideFromIl2Cpp]
    public override RoleId TorRoleId => RoleId.Vulture;

    [HideFromIl2Cpp]
    protected override ModdedStringOption SpawnRateOption =>
        OptionGroupSingleton<VultureOptions>.Instance.SpawnRate;

    public static void clearAndReload()
    {
        vulture = null;
        vultureNumberToWin = Mathf.RoundToInt(OptionGroupSingleton<VultureOptions>.Instance.NumberToWin.Value);
        eatenBodies = 0;
        cooldown = OptionGroupSingleton<VultureOptions>.Instance.Cooldown.Value;
        triggerVultureWin = false;
        canUseVents = OptionGroupSingleton<VultureOptions>.Instance.CanUseVents.Value;
        showArrows = OptionGroupSingleton<VultureOptions>.Instance.ShowArrows.Value;
        if (localArrows != null)
            foreach (var arrow in localArrows)
                if (arrow?.arrow != null)
                    Object.Destroy(arrow.arrow);
        localArrows = new List<Arrow>();
    }

    public override void ClearAndReload()
    {
        clearAndReload();
    }

    [HideFromIl2Cpp]
    public override RoleInfo GetRoleInfo() => Info;

    public override void PlayerFixedUpdate(PlayerControl player)
    {
        if (Vulture.vulture == null || player != Vulture.vulture || Vulture.localArrows == null || !Vulture.showArrows) return;
        if (Vulture.vulture.Data.IsDead)
        {
            foreach (var arrow in Vulture.localArrows) Object.Destroy(arrow.arrow);
            Vulture.localArrows = new List<Arrow>();
            return;
        }
        DeadBody[] deadBodies = Object.FindObjectsOfType<DeadBody>();
        var arrowUpdate = Vulture.localArrows.Count != deadBodies.Length;
        var index = 0;
        if (arrowUpdate)
        {
            foreach (var arrow in Vulture.localArrows) Object.Destroy(arrow.arrow);
            Vulture.localArrows = new List<Arrow>();
        }
        foreach (var db in deadBodies)
        {
            if (arrowUpdate)
            {
                Vulture.localArrows.Add(new Arrow(Color.blue));
                Vulture.localArrows[index].arrow.SetActive(true);
            }
            if (Vulture.localArrows[index] != null) Vulture.localArrows[index].Update(db.transform.position);
            index++;
        }
    }
}

/// <summary>
/// The settings of VultureOptions. They live next to the role on purpose: the group is bound
/// to Vulture, which is what puts it behind the cog on this role's row on Mira API's
/// Role Settings page instead of in the mod's own settings tab.
/// </summary>
public class VultureOptions : TorRoleOptionGroup<Vulture>
{
    public override uint GroupPriority => 340;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Vulture));
    public override Color GroupColor => TorOptionColors.Group(Vulture.color);

    public ModdedStringOption SpawnRate { get; } =
        new ModdedStringOption(TorOptions.RoleKey(RoleId.Vulture), TorOptions.Rates[0], TorOptions.Rates)
        {
            // The chance is the +/- spinner on this role's own row in Role Settings; Mira API writes
            // it back through SetChance(). Showing it here too would put the same number twice.
            Visible = () => false
        };

    public ModdedNumberOption Cooldown { get; } =
        new ModdedNumberOption("Opt-Vulture,1", 15f, 10f, 60f, 2.5f, MiraNumberSuffixes.None, "0.##");

    public ModdedNumberOption NumberToWin { get; } =
        new ModdedNumberOption("Opt-Vulture,2", 4f, 1f, 10f, 1f, MiraNumberSuffixes.None, "0.##");

    public ModdedToggleOption CanUseVents { get; } =
        new ModdedToggleOption("Opt-Vulture,3", true);

    public ModdedToggleOption ShowArrows { get; } =
        new ModdedToggleOption("Opt-Vulture,4", true);
}

/// <summary>
/// The Vulture's "eat body" button. It used to be built centrally in
/// <c>HudManagerStartPatch.createButtonsPostfix</c> as <c>vultureEatButton</c>.
/// </summary>
public sealed class VultureEatButton : TorButton
{
    private static VultureEatButton vultureEatButton;
    
    public VultureEatButton()
    {
        vultureEatButton = this;

        SetSprite(TorAssets.VultureButton);
        PositionOffset = TorButtonPositions.LowerRowCenter;
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

                            PlayerControl.LocalPlayer.RpcCleanBody(playerInfo.PlayerId, Vulture.vulture.PlayerId);

                            Vulture.cooldown = vultureEatButton.Timer = vultureEatButton.MaxTimer;
                            SoundEffectsManager.play("vultureEat");
                            break;
                        }
                    }
                }
        };
        HasButton = () =>
        {
            return Vulture.vulture != null && Vulture.vulture == PlayerControl.LocalPlayer &&
                   !PlayerControl.LocalPlayer.Data.IsDead;
        };
        CouldUse = () =>
        {
            SetUses(Vulture.vultureNumberToWin - Vulture.eatenBodies);
            return HudManager.Instance.ReportButton.graphic.color == Palette.EnabledColor &&
                   PlayerControl.LocalPlayer.CanMove;
        };
        OnMeetingEnds = () => { vultureEatButton.Timer = vultureEatButton.MaxTimer; };
    }

    public override float Cooldown => Vulture.cooldown;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        ButtonText = new ButtonText(24,
            FastDestroyableSingleton<RoleManager>.Instance.GetRole(RoleTypes.Tracker).Ability.FontMaterial);
    }
}

public static class VultureRpcs
{
    [MethodRpc((uint)TorRpc.CleanBody, LocalHandling = RpcLocalHandling.After)]
    public static void RpcCleanBody(this PlayerControl player, byte playerId, byte cleaningPlayerId)
    {
        RPCProcedure.cleanBody(playerId, cleaningPlayerId);
    }
}
