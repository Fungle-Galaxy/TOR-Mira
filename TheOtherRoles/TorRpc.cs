namespace TheOtherRoles;

public enum TorRpc : uint
{
    // Main Controls

    ResetVaribles,
    ForceEnd,
    SetRole,
    UseUncheckedVent,
    UncheckedMurderPlayer,
    UncheckedCmdReportDeadBody,
    UncheckedExilePlayer,
    DynamicMapOption,
    SetGameStarting,
    StopStart,

    // Role functionality

    EngineerFixLights,
    EngineerFixSubmergedOxygen,
    EngineerUsedRepair,
    CleanBody,
    MedicSetShielded,
    ShieldedMurderAttempt,
    TimeMasterShield,
    TimeMasterRewindTime,
    TimeMasterShieldBreak,
    ShifterShift,
    SwapperSwap,
    MorphlingMorph,
    CamouflagerCamouflage,
    TrackerUsedTracker,
    VampireSetBitten,
    PlaceGarlic,
    DeputyUsedHandcuffs,
    DeputyPromotes,
    JackalCreatesSidekick,
    SidekickPromotes,
    ErasePlayerRoles,
    SetFutureErased,
    SetFutureShifted,
    SetFutureShielded,
    SetFutureSpelled,
    PlaceNinjaTrace,
    PlacePortal,
    UsePortal,
    PlaceJackInTheBox,
    LightsOut,
    PlaceCamera,
    SealVent,
    ArsonistWin,
    GuesserShoot,
    LawyerSetTarget,
    LawyerPromotesToPursuer,
    SetBlanked,
    Bloody,
    SetFirstKill,
    SetTiebreak,
    SetInvisible,
    ThiefStealsRole,
    SetTrap,
    TriggerTrap,
    MayorSetVoteTwice,
    PlaceBomb,
    DefuseBomb,
    ShareRoom,
    YoyoMarkLocation,
    YoyoBlink,
    BreakArmor,
    SchrodingerCatSetTeam,

    // Gamemode

    SetGuesserGm,
    HuntedShield,
    HuntedRewindTime,
    SetProp,
    SetRevealed,
    PropHuntStartTimer,
    PropHuntSetInvis,
    PropHuntSetSpeedboost,

    // Other functionality

    ShareTimer,
    EventKick,

    // Ghost info (one rpc per ghost payload)
    GhostHandcuffNoticed,
    GhostHandcuffOver,
    GhostArsonistDouse,
    GhostBountyTarget,
    GhostNinjaMarked,
    GhostWarlockTarget,
    GhostMediumInfo,
    GhostDetectiveOrMedicInfo,
    GhostVampireTimer,
    GhostDeathReasonAndKiller,

    // Role draft

    DraftStart,
    DraftTurn,
    DraftSubmitPick,
    DraftPickConfirmed,
    DraftCancel,
    DraftEnd,
}
