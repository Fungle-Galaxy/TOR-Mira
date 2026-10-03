using MiraAPI.Utilities.Assets;
using UnityEngine;

namespace TheOtherRoles;

public static class TorAssets
{
    public static LoadableAsset<Sprite> MenuBackground { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.RoleListScreen.png", 110f);
    public static LoadableAsset<Sprite> TeamBackground { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.TeamScreen.png", 110f);
    public static LoadableAsset<Sprite> PlateSprite { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.RolePlate2.png");
    public static LoadableAsset<Sprite> HoverPlateSprite { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.RolePlate.png");
    public static LoadableAsset<Sprite> SummaryScreen { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.SummaryScreen.png", 110f);
    public static LoadableAsset<Sprite> RoleInfoButtonSprite { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.RoleInfoButton.png", 101f);
    public static LoadableAsset<Sprite> RoleInfoButtonActiveSprite { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.RoleInfoButtonActive.png", 101f);
    public static LoadableAsset<Sprite> HideNSeekArrowButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.HideNSeekArrowButton.png", 115f);
    public static LoadableAsset<Sprite> LighterButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.LighterButton.png", 115f);
    public static LoadableAsset<Sprite> PoolablesBackground { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.poolablesBackground.jpg", 200f);
    public static LoadableAsset<Sprite> UnStuck { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.UnStuck.png", 115f);
    public static LoadableAsset<Sprite> Reveal { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.Reveal.png", 115f);
    public static LoadableAsset<Sprite> InvisButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.InvisButton.png", 115f);
    public static LoadableAsset<Sprite> FindButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.FindButton.png", 115f);
    public static LoadableAsset<Sprite> SpeedboostButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.SpeedboostButton.png", 115f);
    public static LoadableAsset<Sprite> Arrow { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.Arrow.png", 200f);
    public static LoadableAsset<Sprite> Banner2 { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.Banner2.png", 300f);
    public static LoadableAsset<Sprite> Banner { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.Banner.png", 300f);
    public static LoadableAsset<Sprite> Blood1 { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.Blood1.png", 700f);
    public static LoadableAsset<Sprite> Blood2 { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.Blood2.png", 500f);
    public static LoadableAsset<Sprite> Blood3 { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.Blood3.png", 300);
    public static LoadableAsset<Sprite> Bomb { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.Bomb.png", 300f);
    public static LoadableAsset<Sprite> BombBackground { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.BombBackground.png", 110f);
    public static LoadableAsset<Sprite> BombButtonDefuse { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.BombButtonDefuse.png", 115f);
    public static LoadableAsset<Sprite> BombButtonPlant { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.BombButtonPlant.png", 115f);
    public static LoadableAsset<Sprite> Footprint { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.Footprint.png", 600f);
    public static LoadableAsset<Sprite> Garlic { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.Garlic.png", 300f);
    public static LoadableAsset<Sprite> GarlicBackground { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.GarlicBackground.png", 60f);
    public static LoadableAsset<Sprite> NinjaTraceW { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.NinjaTraceW.png", 225f);
    public static LoadableAsset<Sprite> PortalPlattform { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.PortalPlattform.png", 115f);
    public static LoadableAsset<Sprite> Silhouette { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.Silhouette.png", 225f);
    public static LoadableAsset<Sprite> TrapperTrapIngame { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.TrapperTrapIngame.png", 300f);
    public static LoadableAsset<Sprite> CoffeeButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.CoffeeButton.png");
    public static LoadableAsset<Sprite> Vent { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.Vent.png", 150f);
    public static LoadableAsset<Sprite> NightVisionOverlay { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.NightVisionOverlay.png", 350f);
    public static LoadableAsset<Sprite> PlusButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.PlusButton.png");
    public static LoadableAsset<Sprite> PlusButtonActive { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.PlusButtonActive.png");
    public static LoadableAsset<Sprite> MinusButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.MinusButton.png");
    public static LoadableAsset<Sprite> MinusButtonActive { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.MinusButtonActive.png");
    public static LoadableAsset<Sprite> DeputyHandcuffButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.DeputyHandcuffButton.png", 115f);
    public static LoadableAsset<Sprite> DeputyHandcuffed { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.DeputyHandcuffed.png", 115f);
    public static LoadableAsset<Sprite> RepairButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.RepairButton.png", 115f);
    public static LoadableAsset<Sprite> HackerButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.HackerButton.png", 115f);
    public static LoadableAsset<Sprite> EmergencyButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.EmergencyButton.png", 550f);
    public static LoadableAsset<Sprite> ShieldButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.ShieldButton.png", 115f);
    public static LoadableAsset<Sprite> Soul { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.Soul.png", 500f);
    public static LoadableAsset<Sprite> MediumButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.MediumButton.png", 115f);
    public static LoadableAsset<Sprite> PlacePortalButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.PlacePortalButton.png", 115f);
    public static LoadableAsset<Sprite> UsePortalButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.UsePortalButton.png", 115f);
    public static LoadableAsset<Sprite> CloseVentButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.CloseVentButton.png", 115f);
    public static LoadableAsset<Sprite> PlaceCameraButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.PlaceCameraButton.png", 115f);
    public static LoadableAsset<Sprite> StaticVentSealed { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.StaticVentSealed.png", 160f);
    public static LoadableAsset<Sprite> FungleVentSealed { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.FungleVentSealed.png", 1660f);
    public static LoadableAsset<Sprite> CentralUpperBlocked { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.CentralUpperBlocked.png", 145f);
    public static LoadableAsset<Sprite> CentralLowerBlocked { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.CentralLowerBlocked.png", 145f);
    public static LoadableAsset<Sprite> SwapperCheck { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.SwapperCheck.png", 150f);
    public static LoadableAsset<Sprite> TimeShieldButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.TimeShieldButton.png", 115f);
    public static LoadableAsset<Sprite> RewindButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.RewindButton.png", 115f);
    public static LoadableAsset<Sprite> PathfindButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.PathfindButton.png", 115f);
    public static LoadableAsset<Sprite> TrackerButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.TrackerButton.png", 115f);
    public static LoadableAsset<Sprite> CamoButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.CamoButton.png", 115f);
    public static LoadableAsset<Sprite> CleanButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.CleanButton.png", 115f);
    public static LoadableAsset<Sprite> EraserButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.EraserButton.png", 115f);
    public static LoadableAsset<Sprite> SampleButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.SampleButton.png", 115f);
    public static LoadableAsset<Sprite> MorphButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.MorphButton.png", 115f);
    public static LoadableAsset<Sprite> NinjaMarkButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.NinjaMarkButton.png", 115f);
    public static LoadableAsset<Sprite> NinjaAssassinateButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.NinjaAssassinateButton.png", 115f);
    public static LoadableAsset<Sprite> PlaceJackInTheBoxButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.PlaceJackInTheBoxButton.png", 115f);
    public static LoadableAsset<Sprite> LightsOutButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.LightsOutButton.png", 115f);
    public static LoadableAsset<Sprite> TricksterVentButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.TricksterVentButton.png", 115f);
    public static LoadableAsset<Sprite> VampireButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.VampireButton.png", 115f);
    public static LoadableAsset<Sprite> GarlicButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.GarlicButton.png", 115f);
    public static LoadableAsset<Sprite> CurseButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.CurseButton.png", 115f);
    public static LoadableAsset<Sprite> CurseKillButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.CurseKillButton.png", 115f);
    public static LoadableAsset<Sprite> SpellButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.SpellButton.png", 115f);
    public static LoadableAsset<Sprite> SpellButtonMeeting { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.SpellButtonMeeting.png", 225f);
    public static LoadableAsset<Sprite> YoyoMarkButtonSprite { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.YoyoMarkButtonSprite.png", 115f);
    public static LoadableAsset<Sprite> YoyoBlinkButtonSprite { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.YoyoBlinkButtonSprite.png", 115f);
    public static LoadableAsset<Sprite> ShiftButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.ShiftButton.png", 115f);
    public static LoadableAsset<Sprite> VultureButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.VultureButton.png", 115f);
    public static LoadableAsset<Sprite> DouseButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.DouseButton.png", 115f);
    public static LoadableAsset<Sprite> IgniteButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.IgniteButton.png", 115f);
    public static LoadableAsset<Sprite> SidekickButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.SidekickButton.png", 115f);
    public static LoadableAsset<Sprite> PursuerButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.PursuerButton.png", 115f);
    public static LoadableAsset<Sprite> EventKickButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.EventKickButton.png", 115f);
    public static LoadableAsset<Sprite> TrapperPlaceButton { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.TrapperPlaceButton.png", 115f);
    public static LoadableAsset<Sprite> TargetIcon { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.TargetIcon.png", 150f);

    public static LoadableAsset<Sprite> DraftCardCrew { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.RoleDraft.DraftRoleCardCrew.png", 250f);
    public static LoadableAsset<Sprite> DraftCardImpostor { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.RoleDraft.DraftRoleCardImpostor.png", 250f);
    public static LoadableAsset<Sprite> DraftCardNeutral { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.RoleDraft.DraftRoleCardNeutral.png", 250f);
    public static LoadableAsset<Sprite> DraftCardRandom { get; } =
        new LoadableResourceAsset("TheOtherRoles.Resources.RoleDraft.DraftRoleCardRandom.png", 250f);
}