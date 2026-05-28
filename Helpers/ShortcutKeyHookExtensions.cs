using SharpHook;
using SharpHook.Data;
using System.Windows.Input;

namespace Listhing.Helpers;

/// <summary>
/// ShortcutKey を SharpHook のイベントと照合するための拡張メソッド
/// </summary>
public static class ShortcutKeyHookExtensions
{
    /// <summary>
    /// SharpHook のキーイベントと一致するか判定する
    /// </summary>
    public static bool MatchesHook(this ShortcutKey shortcut, KeyboardHookEventArgs e)
    {
        if (shortcut.IsEmpty) return false;
        var keyCode = ToKeyCode(shortcut.Key);
        if (keyCode == KeyCode.VcUndefined) return false;
        if (e.Data.KeyCode != keyCode) return false;
        return MatchesMask(shortcut.Modifiers, e.RawEvent.Mask);
    }

    private static bool MatchesMask(ModifierKeys modifiers, EventMask mask)
    {
        // EventMask.Ctrl = LeftCtrl(2) | RightCtrl(32) の複合フラグ
        bool needCtrl  = modifiers.HasFlag(ModifierKeys.Control);
        bool needAlt   = modifiers.HasFlag(ModifierKeys.Alt);
        bool needShift = modifiers.HasFlag(ModifierKeys.Shift);
        bool needWin   = modifiers.HasFlag(ModifierKeys.Windows);

        bool hasCtrl  = mask.HasFlag(EventMask.LeftCtrl)  || mask.HasFlag(EventMask.RightCtrl);
        bool hasAlt   = mask.HasFlag(EventMask.LeftAlt)   || mask.HasFlag(EventMask.RightAlt);
        bool hasShift = mask.HasFlag(EventMask.LeftShift)  || mask.HasFlag(EventMask.RightShift);
        bool hasMeta  = mask.HasFlag(EventMask.LeftMeta)   || mask.HasFlag(EventMask.RightMeta);

        return needCtrl  == hasCtrl
            && needAlt   == hasAlt
            && needShift == hasShift
            && needWin   == hasMeta;
    }

    /// <summary>
    /// WPF Key → SharpHook KeyCode 変換
    /// </summary>
    public static KeyCode ToKeyCode(Key key) => key switch
    {
        Key.A => KeyCode.VcA, Key.B => KeyCode.VcB, Key.C => KeyCode.VcC,
        Key.D => KeyCode.VcD, Key.E => KeyCode.VcE, Key.F => KeyCode.VcF,
        Key.G => KeyCode.VcG, Key.H => KeyCode.VcH, Key.I => KeyCode.VcI,
        Key.J => KeyCode.VcJ, Key.K => KeyCode.VcK, Key.L => KeyCode.VcL,
        Key.M => KeyCode.VcM, Key.N => KeyCode.VcN, Key.O => KeyCode.VcO,
        Key.P => KeyCode.VcP, Key.Q => KeyCode.VcQ, Key.R => KeyCode.VcR,
        Key.S => KeyCode.VcS, Key.T => KeyCode.VcT, Key.U => KeyCode.VcU,
        Key.V => KeyCode.VcV, Key.W => KeyCode.VcW, Key.X => KeyCode.VcX,
        Key.Y => KeyCode.VcY, Key.Z => KeyCode.VcZ,
        Key.D0 => KeyCode.Vc0, Key.D1 => KeyCode.Vc1, Key.D2 => KeyCode.Vc2,
        Key.D3 => KeyCode.Vc3, Key.D4 => KeyCode.Vc4, Key.D5 => KeyCode.Vc5,
        Key.D6 => KeyCode.Vc6, Key.D7 => KeyCode.Vc7, Key.D8 => KeyCode.Vc8,
        Key.D9 => KeyCode.Vc9,
        Key.F1 => KeyCode.VcF1,   Key.F2 => KeyCode.VcF2,   Key.F3 => KeyCode.VcF3,
        Key.F4 => KeyCode.VcF4,   Key.F5 => KeyCode.VcF5,   Key.F6 => KeyCode.VcF6,
        Key.F7 => KeyCode.VcF7,   Key.F8 => KeyCode.VcF8,   Key.F9 => KeyCode.VcF9,
        Key.F10 => KeyCode.VcF10, Key.F11 => KeyCode.VcF11, Key.F12 => KeyCode.VcF12,
        Key.Enter    => KeyCode.VcEnter,
        Key.Escape   => KeyCode.VcEscape,
        Key.Space    => KeyCode.VcSpace,
        Key.Tab      => KeyCode.VcTab,
        Key.Back     => KeyCode.VcBackspace,
        Key.Delete   => KeyCode.VcDelete,
        Key.Insert   => KeyCode.VcInsert,
        Key.Home     => KeyCode.VcHome,
        Key.End      => KeyCode.VcEnd,
        Key.PageUp   => KeyCode.VcPageUp,
        Key.PageDown => KeyCode.VcPageDown,
        Key.Up       => KeyCode.VcUp,
        Key.Down     => KeyCode.VcDown,
        Key.Left     => KeyCode.VcLeft,
        Key.Right    => KeyCode.VcRight,
        Key.OemMinus  => KeyCode.VcMinus,
        Key.OemPlus   => KeyCode.VcEquals,
        Key.OemComma  => KeyCode.VcComma,
        Key.OemPeriod => KeyCode.VcPeriod,
        _ => KeyCode.VcUndefined
    };
}
