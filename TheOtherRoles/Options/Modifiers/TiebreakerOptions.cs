// Converted from CustomOptionHolder.cs by tools/Generate-Options.ps1; this file is now the source of truth.

using MiraAPI.GameOptions.OptionTypes;
using UnityEngine;

namespace TheOtherRoles.Options;

public class TiebreakerOptions : TorModifierOptionGroup
{
    public override uint GroupPriority => 30;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Tiebreaker));
    public override Color GroupColor => TorOptionColors.Group(Color.yellow);

    public ModdedStringOption TieBreaker { get; } = new ModdedStringOption(TorOptions.RoleKey(RoleId.Tiebreaker), TorOptions.Rates[0], TorOptions.Rates);
}
