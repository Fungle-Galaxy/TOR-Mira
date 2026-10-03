global using Il2CppInterop.Runtime;
global using Il2CppInterop.Runtime.Attributes;
global using Il2CppInterop.Runtime.Injection;
global using Il2CppInterop.Runtime.InteropTypes;
global using Il2CppInterop.Runtime.InteropTypes.Arrays;
global using TheOtherRoles.Roles;
global using TheOtherRoles.Roles.Crewmate;
global using TheOtherRoles.Roles.Impostor;
global using TheOtherRoles.Roles.Modifier;
global using TheOtherRoles.Roles.Neutral;
global using TheOtherRoles.Options;
global using MiraAPI.GameOptions;
using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.Data.Player;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppSystem.Security.Cryptography;
using Il2CppSystem.Text;
using MiraAPI;
using MiraAPI.Modifiers;
using MiraAPI.PluginLoading;
using Reactor;
using Reactor.Networking;
using Reactor.Networking.Attributes;
using Reactor.Utilities;
using TheOtherRoles.Modules;
using TheOtherRoles.Networking;
using TheOtherRoles.Patches;
using TheOtherRoles.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace TheOtherRoles;

[BepInPlugin(Id, "The Other Roles: Mira", VersionString)]
[BepInDependency(SubmergedCompatibility.SUBMERGED_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(ReactorPlugin.Id)]
[BepInDependency(MiraApiPlugin.Id)]
[ReactorModFlags(ModFlags.RequireOnAllClients)]
[BepInProcess("Among Us.exe")]
public class TheOtherRolesPlugin : BasePlugin, IMiraPlugin
{
    public const string Id = "aus.theotherroles.mira";
    public const string VersionString = "1.0.0";
    public static bool isBeta = true;

    public static Version Version = Version.Parse(VersionString);
    
    internal static ManualLogSource Logger;
    
    public static TheOtherRolesPlugin Instance;

    public Harmony Harmony { get; } = new(Id);

    public static ConfigEntry<string> DebugMode { get; private set; }
    public static ConfigEntry<bool> GhostsSeeInformation { get; set; }
    public static ConfigEntry<bool> GhostsSeeRoles { get; set; }
    public static ConfigEntry<bool> GhostsSeeModifier { get; set; }
    public static ConfigEntry<bool> GhostsSeeVotes { get; set; }
    public static ConfigEntry<bool> ShowRoleSummary { get; set; }
    public static ConfigEntry<bool> ShowLighterDarker { get; set; }
    public static ConfigEntry<bool> EnableSoundEffects { get; set; }
    public static ConfigEntry<bool> ShowVentsOnMap { get; set; }
    public static ConfigEntry<bool> ShowChatNotifications { get; set; }

    public override void Load()
    {
        Logger = Log;
        Instance = this;

        ReactorCredits.Register("The Other Roles: Mira", VersionString, isBeta, ReactorCredits.AlwaysShow);
        
        ModTranslation.Load();
        ModTranslation.RegisterWithMiraLocale();

        CustomColors.Load();

        _ = CredentialsPatch.MOTD.loadMOTDs();

        DebugMode = Config.Bind("Custom", "Enable Debug Mode", "false");

        GhostsSeeInformation ??= Config.Bind("Custom", "Ghosts See Remaining Tasks", true);
        GhostsSeeRoles ??= Config.Bind("Custom", "Ghosts See Roles", true);
        GhostsSeeModifier ??= Config.Bind("Custom", "Ghosts See Modifier", true);
        GhostsSeeVotes ??= Config.Bind("Custom", "Ghosts See Votes", true);
        ShowRoleSummary ??= Config.Bind("Custom", "Show Role Summary", true);
        ShowLighterDarker ??= Config.Bind("Custom", "Show Lighter / Darker", true);
        EnableSoundEffects ??= Config.Bind("Custom", "Enable Sound Effects", true);
        ShowVentsOnMap ??= Config.Bind("Custom", "Show vent positions on minimap", false);
        ShowChatNotifications ??= Config.Bind("Custom", "Show Chat Notifications", true);

        // The gameplay code reads the TORMapOptions mirrors, so seed them from the config right
        // away instead of waiting for the first round sync.
        TORMapOptions.reloadPluginOptions();

        // Removes vanilla Servers   More extensive testing is needed because I removed the Reactor, so after testing, TOR can temporarily run on the Innerslot server
        ServerManager.DefaultRegions = new Il2CppReferenceArray<IRegionInfo>(new IRegionInfo[0]);

        Harmony.PatchAll();

        CustomOptionHolder.Load();
        EventUtility.Load();
        SubmergedCompatibility.Initialize();
        AddToKillDistanceSetting.addKillDistance();

        // TOR rolls its own pair of lovers before Mira API hands out the rest of the modifiers,
        // so Mira's own SelectRoles pass must not assign them a second time.
        ModifierManager.MiraAssignsModifiers = false;

        Logger.LogInfo("Loading TOR-Mira completed!");
    }

    public ConfigFile GetConfigFile()
    {
        return Config;
    }

    public string OptionsTitleText => "TOR Mira";
}

// Deactivate bans, since I always leave my local testing game and ban myself
[HarmonyPatch(typeof(PlayerBanData), nameof(PlayerBanData.IsBanned), MethodType.Getter)]
public static class IsBannedPatch
{
    public static void Postfix(out bool __result)
    {
        __result = false;
    }
}

// Debugging tools
[HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Update))]
public static class DebugManager
{
    private static readonly string passwordHash = "d1f51dfdfd8d38027fd2ca9dfeb299399b5bdee58e6c0b3b5e9a45cd4e502848";
    private static readonly Random random = new((int)DateTime.Now.Ticks);
    private static readonly List<PlayerControl> bots = new();

    public static void Postfix(KeyboardJoystick __instance)
    {
        // Check if debug mode is active.
        var builder = new StringBuilder();
        var sha = SHA256Managed.Create();
        byte[] hashed = sha.ComputeHash(Encoding.UTF8.GetBytes(TheOtherRolesPlugin.DebugMode.Value));
        foreach (var b in hashed) builder.Append(b.ToString("x2"));
        var enteredHash = builder.ToString();
        if (enteredHash != passwordHash) return;


        // Spawn dummys
        if (Input.GetKeyDown(KeyCode.F))
        {
            var playerControl = Object.Instantiate(AmongUsClient.Instance.PlayerPrefab);
            var i = playerControl.PlayerId = (byte)GameData.Instance.GetAvailableId();

            bots.Add(playerControl);
            GameData.Instance.AddDummy(playerControl);
            AmongUsClient.Instance.Spawn(playerControl);

            playerControl.transform.position = PlayerControl.LocalPlayer.transform.position;
            playerControl.GetComponent<DummyBehaviour>().enabled = true;
            playerControl.NetTransform.enabled = false;
            playerControl.SetName(RandomString(10));
            playerControl.SetColor((byte)random.Next(Palette.PlayerColors.Length));
            playerControl.Data.RpcSetTasks(new byte[0]);
        }

        // Terminate round
        if (Input.GetKeyDown(KeyCode.L))
        {
            PlayerControl.LocalPlayer.RpcForceEnd();
        }
    }

    public static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}