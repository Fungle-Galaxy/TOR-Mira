using MiraAPI.Keybinds;
using Rewired;

namespace TheOtherRoles.Buttons;

/// <summary>
/// TOR binds most of its buttons to keys that vanilla has no Rewired action for. Mira only draws
/// the key badge (and dispatches the press) for buttons that expose a <c>Keybind</c>, so every
/// key TOR uses gets one here - Q/F/V reuse the vanilla actions so user rebinds are honoured.
/// </summary>
[RegisterCustomKeybinds]
public static class TorKeybinds
{
    public static MiraKeybind Secondary { get; } = new("TOR Secondary", KeyboardKeyCode.G);
    public static MiraKeybind Tertiary { get; } = new("TOR Tertiary", KeyboardKeyCode.J);
    public static MiraKeybind Quaternary { get; } = new("TOR Quaternary", KeyboardKeyCode.H);
    public static MiraKeybind Quinary { get; } = new("TOR Quinary", KeyboardKeyCode.R);
    public static MiraKeybind Senary { get; } = new("TOR Senary", KeyboardKeyCode.I);
    public static MiraKeybind Septenary { get; } = new("TOR Septenary", KeyboardKeyCode.K);
    public static MiraKeybind Crouch { get; } = new("TOR Crouch", KeyboardKeyCode.LeftShift);
    public static MiraKeybind Zoom { get; } = new("TOR Zoom", KeyboardKeyCode.KeypadPlus);
}
