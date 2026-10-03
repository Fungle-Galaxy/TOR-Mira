using System;
using System.Collections.Generic;
using System.Linq;
using MiraAPI.Hud;
using Rewired;
using UnityEngine;

namespace TheOtherRoles.Buttons;

public static class TorButtons
{
    /// <summary>Every converted TOR button that is currently registered with Mira.</summary>
    public static IEnumerable<TorButton> All =>
        CustomButtonManager.Buttons.OfType<TorButton>().Where(button => button != null);

    /// <summary>Buttons whose HUD object actually exists yet.</summary>
    public static IEnumerable<TorButton> Live => All.Where(button => button.actionButton != null);

    public static void MeetingEndedUpdate()
    {
        foreach (var button in Live)
        {
            try
            {
                button.MeetingEnd();
                button.Refresh();
            }
            catch (NullReferenceException)
            {
                TheOtherRolesPlugin.Logger.LogWarning(
                    "NullReferenceException in MeetingEndedUpdate(), usually harmless.");
            }
        }
    }

    public static void ResetAllCooldowns()
    {
        foreach (var button in Live)
        {
            try
            {
                button.Timer = button.MaxTimer;
                button.DeputyTimer = button.MaxTimer;
                button.Refresh();
            }
            catch (NullReferenceException)
            {
                TheOtherRolesPlugin.Logger.LogWarning(
                    "NullReferenceException in ResetAllCooldowns(), usually harmless.");
            }
        }
    }

    public static void ReloadHotkeys()
    {
        var player = ReInput.players.GetPlayer(0);
        if (player == null)
        {
            return;
        }

        foreach (var button in Live)
        {
            if (button.OriginalHotkey == KeyCode.Q)
            {
                Remap(player, button, 8);
            }
            else if (button.OriginalHotkey == KeyCode.F)
            {
                Remap(player, button, 49);
            }
        }
    }

    public static void ResetCooldownOffsets()
    {
        foreach (var button in All)
        {
            button.ResetCooldownOffset();
        }
    }

    private static void Remap(Player player, TorButton button, int actionId)
    {
        var map = player.controllers.maps.GetFirstButtonMapWithAction(actionId, true);
        if (map == null)
        {
            return;
        }

        if (Enum.TryParse(map.elementIdentifierName, out KeyCode keycode))
        {
            button.Hotkey = keycode;
        }
    }
}
