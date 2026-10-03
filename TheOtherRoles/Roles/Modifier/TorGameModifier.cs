using System.Linq;
using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Modifiers;
using MiraAPI.Modifiers.Types;
using MiraAPI.PluginLoading;

namespace TheOtherRoles.Roles.Modifier;

/// <summary>
/// Mira API owns assignment, sync and the modifier HUD. TOR keeps answering "who has this"
/// through the statics on each concrete class, which <see cref="OnActivate"/> /
/// <see cref="OnDeactivate"/> fill in, so nothing downstream had to change.
/// </summary>
[MiraIgnore] // Mira API's plugin scan does not skip abstract types.
public abstract class TorGameModifier : GameModifier
{
    public abstract RoleInfo TorInfo { get; }

    public override string ModifierName => TorInfo.name;

    public override string GetDescription() => TorInfo.shortDescription;

    public abstract void ClearAndReload();

    public static void ClearAndReloadAll()
    {
        foreach (var modifier in ModifierManager.Modifiers.OfType<TorGameModifier>())
            modifier.ClearAndReload();
    }

    protected static int ChanceOf(ModdedStringOption option) => option.Selection() * TorOptions.RateStep;

    protected static PlayerControl PlayerOf(RoleBehaviour role)
    {
        if (role == null) return null;
        foreach (var player in PlayerControl.AllPlayerControls)
            if (player != null && player.Data != null && player.Data.Role == role)
                return player;
        return null;
    }

    protected static bool IsCrewSeat(RoleBehaviour role)
    {
        var player = PlayerOf(role);
        return player != null && !player.Data.Role.IsImpostor && !Helpers.isNeutral(player);
    }
}
