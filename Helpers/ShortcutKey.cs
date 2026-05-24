using System.Windows.Input;

namespace Listhing.Helpers;

/// <summary>
/// ショートカットキーを表すクラス
/// </summary>
public class ShortcutKey : IEquatable<ShortcutKey>
{
    /// <summary>
    /// メインキー
    /// </summary>
    public Key Key { get; }

    /// <summary>
    /// 修飾キー
    /// </summary>
    public ModifierKeys Modifiers { get; }

    /// <summary>
    /// 修飾キーを持っているかどうか
    /// </summary>
    public bool HasModifiers => Modifiers != ModifierKeys.None;

    /// <summary>
    /// 空のショートカットかどうか
    /// </summary>
    public bool IsEmpty => Key == Key.None;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="key">メインキー</param>
    /// <param name="modifiers">修飾キー（デフォルト: ModifierKeys.None）</param>
    public ShortcutKey(Key key, ModifierKeys modifiers = ModifierKeys.None)
    {
        Key = key;
        Modifiers = modifiers;
    }

    /// <summary>
    /// キーイベントからショートカットキーを作成するコンストラクタ
    /// </summary>
    /// <param name="e">キーイベント引数</param>
    public ShortcutKey(KeyEventArgs e)
    {
        // Key.Systemの場合はRealKeyを取得する
        // Altキーを押した状態で文字キーを押すとKey.Systemになることがある
        Key = e.Key == Key.System ? e.SystemKey : e.Key;
        Modifiers = Keyboard.Modifiers;
    }

    /// <summary>
    /// 文字列からショートカットキーを作成するコンストラクタ
    /// </summary>
    /// <param name="shortcutString">ショートカット文字列（例: "Ctrl+A", "Enter", ""）</param>
    /// <exception cref="ArgumentException">無効なショートカット文字列の場合</exception>
    public ShortcutKey(string shortcutString)
    {
        if (string.IsNullOrWhiteSpace(shortcutString))
        {
            Key = Key.None;
            Modifiers = ModifierKeys.None;
            return;
        }

        var parts = shortcutString.Split('+', StringSplitOptions.RemoveEmptyEntries);
        ModifierKeys modifiers = ModifierKeys.None;
        Key key = Key.None;

        foreach (var part in parts)
        {
            var trimmedPart = part.Trim();

            if (TryParseModifier(trimmedPart, out var modifier))
            {
                modifiers |= modifier;
            }
            else if (TryParseKey(trimmedPart, out var parsedKey))
            {
            if (key != Key.None)
            {
                throw new ArgumentException($"Invalid shortcut string: {shortcutString} - Multiple main keys specified");
            }
            key = parsedKey;
        }
        else
        {
            throw new ArgumentException($"Invalid shortcut string: {shortcutString} - '{trimmedPart}' is not a valid key");
        }
    }

    if (key == Key.None)
    {
        throw new ArgumentException($"Invalid shortcut string: {shortcutString} - No main key specified");
    }

        Key = key;
        Modifiers = modifiers;
    }

    /// <summary>
    /// キーイベントと比較して一致するかどうかを判定する
    /// </summary>
    /// <param name="e">キーイベント引数</param>
    /// <returns>一致する場合は true、それ以外は false</returns>
    public bool Matches(KeyEventArgs e)
    {
        // Key.Systemの場合はRealKeyを取得して比較する
        var eventKey = e.Key == Key.System ? e.SystemKey : e.Key;
        return Key == eventKey && Modifiers == Keyboard.Modifiers;
    }

    /// <summary>
    /// 文字列表現を返す（JSON保存・画面表示用）
    /// </summary>
    /// <returns>ショートカット文字列（例: "Ctrl+Shift+A", "Enter", ""）</returns>
    public override string ToString()
    {
        if (IsEmpty)
        {
            return string.Empty;
        }

        var parts = new List<string>();

        if (Modifiers.HasFlag(ModifierKeys.Control))
        {
            parts.Add("Ctrl");
        }

        if (Modifiers.HasFlag(ModifierKeys.Alt))
        {
            parts.Add("Alt");
        }

        if (Modifiers.HasFlag(ModifierKeys.Shift))
        {
            parts.Add("Shift");
        }

        if (Modifiers.HasFlag(ModifierKeys.Windows))
        {
            parts.Add("Win");
        }

        parts.Add(GetKeyDisplayName(Key));

        return string.Join("+", parts);
    }

    /// <summary>
    /// 文字列からショートカットキーをパースする
    /// </summary>
    /// <param name="shortcutString">ショートカット文字列</param>
    /// <returns>パースされたショートカットキー</returns>
    /// <exception cref="ArgumentException">無効なショートカット文字列の場合</exception>
    public static ShortcutKey Parse(string shortcutString)
    {
        return new ShortcutKey(shortcutString);
    }

    /// <summary>
    /// 文字列からショートカットキーをパースを試みる
    /// </summary>
    /// <param name="shortcutString">ショートカット文字列</param>
    /// <param name="shortcut">パースされたショートカットキー</param>
    /// <returns>パースに成功した場合は true、それ以外は false</returns>
    public static bool TryParse(string shortcutString, out ShortcutKey shortcut)
    {
        try
        {
            shortcut = new ShortcutKey(shortcutString);
            return true;
        }
        catch
        {
            shortcut = new ShortcutKey(Key.None);
            return false;
        }
    }

    /// <summary>
    /// 修飾キー文字列をパースする
    /// </summary>
    private static bool TryParseModifier(string modifierString, out ModifierKeys modifier)
    {
        modifier = modifierString.ToLowerInvariant() switch
        {
            "ctrl" or "control" => ModifierKeys.Control,
            "alt" => ModifierKeys.Alt,
            "shift" => ModifierKeys.Shift,
            "win" or "windows" => ModifierKeys.Windows,
            _ => ModifierKeys.None
        };

        return modifier != ModifierKeys.None;
    }

    /// <summary>
    /// キー文字列をパースする
    /// </summary>
    private static bool TryParseKey(string keyString, out Key key)
    {
        // 大文字小文字を区別せずにパース
        if (Enum.TryParse<Key>(keyString, true, out key))
        {
            return true;
        }

        // 特殊なキー名のマッピング
        key = keyString.ToLowerInvariant() switch
        {
            "esc" or "escape" => Key.Escape,
            "enter" or "return" => Key.Enter,
            "space" or " " => Key.Space,
            "tab" => Key.Tab,
            "backspace" or "bs" => Key.Back,
            "delete" or "del" => Key.Delete,
            "insert" or "ins" => Key.Insert,
            "home" => Key.Home,
            "end" => Key.End,
            "pageup" or "pgup" => Key.PageUp,
            "pagedown" or "pgdn" => Key.PageDown,
            "up" or "arrowup" => Key.Up,
            "down" or "arrowdown" => Key.Down,
            "left" or "arrowleft" => Key.Left,
            "right" or "arrowright" => Key.Right,
            "f1" => Key.F1,
            "f2" => Key.F2,
            "f3" => Key.F3,
            "f4" => Key.F4,
            "f5" => Key.F5,
            "f6" => Key.F6,
            "f7" => Key.F7,
            "f8" => Key.F8,
            "f9" => Key.F9,
            "f10" => Key.F10,
            "f11" => Key.F11,
            "f12" => Key.F12,
            "plus" or "+" => Key.OemPlus,
            "minus" or "-" => Key.OemMinus,
            "," => Key.OemComma,
            "." => Key.OemPeriod,
            "_" => Key.OemBackslash,
            _ => Key.None
        };

        return key != Key.None;
    }

    /// <summary>
    /// キーの表示名を取得する
    /// </summary>
    private static string GetKeyDisplayName(Key key)
    {
        // 特殊なキー名のマッピング
        return key switch
        {
            Key.Escape => "Esc",
            Key.Enter => "Enter",
            Key.Space => "Space",
            Key.Tab => "Tab",
            Key.Back => "Backspace",
            Key.Delete => "Delete",
            Key.Insert => "Insert",
            Key.Home => "Home",
            Key.End => "End",
            Key.PageUp => "PageUp",
            Key.PageDown => "PageDown",
            Key.Up => "Up",
            Key.Down => "Down",
            Key.Left => "Left",
            Key.Right => "Right",
            Key.OemPlus => "+",
            Key.OemMinus => "-",
            Key.D0 => "0",
            Key.D1 => "1",
            Key.D2 => "2",
            Key.D3 => "3",
            Key.D4 => "4",
            Key.D5 => "5",
            Key.D6 => "6",
            Key.D7 => "7",
            Key.D8 => "8",
            Key.D9 => "9",
            Key.OemComma => ",",
            Key.OemPeriod => ".",
            Key.OemBackslash => "_",
            _ => key.ToString()
        };
    }

    /// <summary>
    /// 等価性を判定する
    /// </summary>
    public bool Equals(ShortcutKey? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Key == other.Key && Modifiers == other.Modifiers;
    }

    /// <summary>
    /// 等価性を判定する
    /// </summary>
    public override bool Equals(object? obj)
    {
        return Equals(obj as ShortcutKey);
    }

    /// <summary>
    /// ハッシュコードを取得する
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(Key, Modifiers);
    }

    /// <summary>
    /// 等価演算子
    /// </summary>
    public static bool operator ==(ShortcutKey? left, ShortcutKey? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    /// <summary>
    /// 不等価演算子
    /// </summary>
    public static bool operator !=(ShortcutKey? left, ShortcutKey? right)
    {
        return !(left == right);
    }
}
