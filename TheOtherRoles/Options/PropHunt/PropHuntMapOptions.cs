// Converted from CustomOptionHolder.cs by tools/Generate-Options.ps1; this file is now the source of truth.

using MiraAPI.GameOptions.OptionTypes;
using UnityEngine;

namespace TheOtherRoles.Options;

public class PropHuntMapOptions : TorPropHuntOptionGroup
{
    public override uint GroupPriority => 0;
    public override string GroupName => TorOptions.Title("Opt-Snitch,101");
    public override Color GroupColor => TorOptionColors.Group(Color.yellow);

    public ModdedStringOption Map { get; } =
        new ModdedStringOption("Opt-Snitch,101", "Opt-General,100", ["Opt-General,100", "Opt-General,101", "Opt-General,102", "Opt-General,103", "Opt-General,104", "Opt-General,105", "Opt-General,106"])
        {
            ChangedEvent = _ =>
            {
                var map = OptionGroupSingleton<PropHuntMapOptions>.Instance.Map.Selection();
                if (map >= 3) map++;
                GameOptionsManager.Instance.currentNormalGameOptions.MapId = (byte)map;
            }
        };
}
