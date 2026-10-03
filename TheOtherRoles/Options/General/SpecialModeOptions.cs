// Converted from CustomOptionHolder.cs by tools/Generate-Options.ps1; this file is now the source of truth.
using System;
using MiraAPI.GameOptions.OptionTypes;
using TheOtherRoles.Utilities;
using UnityEngine;

namespace TheOtherRoles.Options;

public class SpecialModeOptions : TorGameOptionGroup
{
    public override uint GroupPriority => 0;
    public override Func<bool> GroupVisible => () => TorOptionVisibility.ClassicOrGuesser() && EventUtility.canBeEnabled;
    public override string GroupName => TorOptions.Title("Opt-General,10");
    public override Color GroupColor => TorOptionColors.Group(Color.green);

    public ModdedToggleOption EnableEventMode { get; } =
        new ModdedToggleOption("Opt-General,10", true)
        {
            Visible = () => EventUtility.canBeEnabled
        };
}
