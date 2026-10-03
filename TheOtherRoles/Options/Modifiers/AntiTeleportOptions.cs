// Converted from CustomOptionHolder.cs by tools/Generate-Options.ps1; this file is now the source of truth.

using MiraAPI.GameOptions.OptionTypes;
using UnityEngine;

namespace TheOtherRoles.Options;

public class AntiTeleportOptions : TorModifierOptionGroup
{
    public override uint GroupPriority => 20;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.AntiTeleport));
    public override Color GroupColor => TorOptionColors.Group(Color.yellow);

    public ModdedStringOption AntiTeleport { get; } = new ModdedStringOption(TorOptions.RoleKey(RoleId.AntiTeleport), TorOptions.Rates[0], TorOptions.Rates);

    public ModdedStringOption AntiTeleportQuantity { get; } =
        new ModdedStringOption("Opt-AntiTeleport,1", TorOptions.Counts[0], TorOptions.Counts)
        {
            Visible = () => OptionGroupSingleton<AntiTeleportOptions>.Instance.AntiTeleport.Enabled()
        };
}
