// Converted from CustomOptionHolder.cs by tools/Generate-Options.ps1; this file is now the source of truth.

using MiraAPI.GameOptions.OptionTypes;
using UnityEngine;

namespace TheOtherRoles.Options;

public class ArmoredOptions : TorModifierOptionGroup
{
    public override uint GroupPriority => 110;
    public override string GroupName => TorOptions.Title(TorOptions.RoleKey(RoleId.Armored));
    public override Color GroupColor => TorOptionColors.Group(Color.yellow);

    public ModdedStringOption Armored { get; } = new ModdedStringOption(TorOptions.RoleKey(RoleId.Armored), TorOptions.Rates[0], TorOptions.Rates);
}
