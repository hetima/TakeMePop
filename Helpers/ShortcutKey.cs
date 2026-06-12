using System.Windows.Input;

namespace Listhing.Helpers;

/// <summary>
/// ショートカットに使用するマウスボタン（SharpHook.Data.MouseButton と値を一致させる）
/// </summary>
public enum ShortcutMouseButton
{
    None    = 0,
    Button1 = 1,  // 左
    Button2 = 2,  // 右
    Button3 = 3,  // 中（ホイールクリック）
    Button4 = 4,
    Button5 = 5,
}

/// <summary>
/// ショートカットキーを表すクラス
/// </summary>
public class ShortcutKey : IEquatable<ShortcutKey>
{
    /// <summary>
    /// メインキー（マウスボタンショートカットの場合は Key.None）
    /// </summary>
    public Key Key { get; }

    /// <summary>
    /// 修飾キー
    /// </summary>
    public ModifierKeys Modifiers { get; }

    /// <summary>
    /// マウスボタン（キーボードショートカットの場合は None）
    /// </summary>
    public ShortcutMouseButton MouseButton { get; }

    /// <summary>
    /// マウスボタンショートカットかどうか
    /// </summary>
    public bool IsMouseButton => MouseButton != ShortcutMouseButton.None;

    /// <summary>
    /// 修飾キーを持っているかどうか
    /// </summary>
    public bool HasModifiers => Modifiers != ModifierKeys.None;

    /// <summary>
    /// 空のショートカットかどうか
    /// </summary>
    public bool IsEmpty => Key == Key.None && MouseButton == ShortcutMouseButton.None;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="key">メインキー</param>
    /// <param name="modifiers">修飾キー（デフォルト: ModifierKeys.None）</param>
    public ShortcutKey(Key key, ModifierKeys modifiers = ModifierKeys.None)
    {
        Key = key;
        Modifiers = modifiers;
        MouseButton = ShortcutMouseButton.None;
    }

    /// <summary>
    /// マウスボタンショートカット用コンストラクタ
    /// </summary>
    /// <param name="mouseButton">マウスボタン</param>
    /// <param name="modifiers">修飾キー（デフォルト: ModifierKeys.None）</param>
    public ShortcutKey(ShortcutMouseButton mouseButton, ModifierKeys modifiers = ModifierKeys.None)
    {
        Key = Key.None;
        MouseButton = mouseButton;
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
        MouseButton = ShortcutMouseButton.None;
    }

    /// <summary>
    /// 文字列からショートカットキーを作成するコンストラクタ
    /// </summary>
    /// <param name="shortcutString">ショートカット文字列（例: "Ctrl+A", "Enter", "Button1", "Ctrl+Button2"）</param>
    /// <exception cref="ArgumentException">無効なショートカット文字列の場合</exception>
    public ShortcutKey(string shortcutString)
    {
        if (string.IsNullOrWhiteSpace(shortcutString))
        {
            Key = Key.None;
            Modifiers = ModifierKeys.None;
            MouseButton = ShortcutMouseButton.None;
            return;
        }

        var parts = shortcutString.Split('+', StringSplitOptions.RemoveEmptyEntries);
        ModifierKeys modifiers = ModifierKeys.None;
        Key key = Key.None;
        ShortcutMouseButton mouseButton = ShortcutMouseButton.None;

        foreach (var part in parts)
        {
            var trimmedPart = part.Trim();

            if (TryParseModifier(trimmedPart, out var modifier))
            {
                modifiers |= modifier;
            }
            else if (TryParseMouseButton(trimmedPart, out var parsedButton))
            {
                if (mouseButton != ShortcutMouseButton.None)
                    throw new ArgumentException($"Invalid shortcut string: {shortcutString} - Multiple mouse buttons specified");
                mouseButton = parsedButton;
            }
            else if (TryParseKey(trimmedPart, out var parsedKey))
            {
                if (key != Key.None)
                    throw new ArgumentException($"Invalid shortcut string: {shortcutString} - Multiple main keys specified");
                key = parsedKey;
            }
            else
            {
                throw new ArgumentException($"Invalid shortcut string: {shortcutString} - '{trimmedPart}' is not a valid key");
            }
        }

        if (key == Key.None && mouseButton == ShortcutMouseButton.None)
            throw new ArgumentException($"Invalid shortcut string: {shortcutString} - No main key specified");

        if (key != Key.None && mouseButton != ShortcutMouseButton.None)
            throw new ArgumentException($"Invalid shortcut string: {shortcutString} - Cannot mix keyboard key and mouse button");

        Key = key;
        Modifiers = modifiers;
        MouseButton = mouseButton;
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
    /// 文字列表現を返す（JSON保存・照合・重複チェック用）。
    /// Oem 系キーは enum 名（例: "OemQuotes"）のまま出力し、配列差による文字衝突を避けてラウンドトリップを一意に保つ。
    /// 画面表示には <see cref="ToDisplayString"/> を使う。
    /// </summary>
    /// <returns>ショートカット文字列（例: "Ctrl+Shift+A", "Enter", "Button1", "Ctrl+Button2"）</returns>
    public override string ToString()
    {
        return BuildString(GetKeyName(Key));
    }

    /// <summary>
    /// 画面表示用の文字列を返す。Oem 系キーは現在のキーボード配列の刻印（ベストエフォート）で表示する。
    /// 配列により実際の刻印とズレることがあるが、保存・動作には影響しない。
    /// </summary>
    public string ToDisplayString()
    {
        return BuildString(GetKeyDisplayName(Key));
    }

    /// <summary>
    /// 修飾キー + キー名を "+" 連結した文字列を組み立てる。
    /// </summary>
    private string BuildString(string keyName)
    {
        if (IsEmpty) return string.Empty;

        var parts = new List<string>();

        if (Modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(ModifierKeys.Alt))     parts.Add("Alt");
        if (Modifiers.HasFlag(ModifierKeys.Shift))   parts.Add("Shift");
        if (Modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");

        if (IsMouseButton)
            parts.Add(MouseButton.ToString());
        else
            parts.Add(keyName);

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
    /// マウスボタン文字列をパースする
    /// </summary>
    private static bool TryParseMouseButton(string s, out ShortcutMouseButton button)
    {
        button = s.ToLowerInvariant() switch
        {
            "button1" or "leftbutton"   => ShortcutMouseButton.Button1,
            "button2" or "rightbutton"  => ShortcutMouseButton.Button2,
            "button3" or "middlebutton" => ShortcutMouseButton.Button3,
            "button4" or "xbutton1"     => ShortcutMouseButton.Button4,
            "button5" or "xbutton2"     => ShortcutMouseButton.Button5,
            _ => ShortcutMouseButton.None
        };
        return button != ShortcutMouseButton.None;
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
            // Oem 系（OemQuestion 等）は enum 名で保存されるため冒頭の Enum.TryParse が拾う。
            // 記号文字（"/" 等）でのパースは配列差で衝突しうるため、あえて追加しない。
            _ => Key.None
        };

        return key != Key.None;
    }

    /// <summary>
    /// 保存・照合用のキー名を取得する。
    /// Oem 系キーは enum 名（key.ToString()）のまま返し、配列差による文字衝突を避けてラウンドトリップを一意に保つ。
    /// </summary>
    private static string GetKeyName(Key key)
    {
        // 特殊なキー名のマッピング（いずれも単一の Key にのみ対応するため衝突しない）
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
            Key.OemComma => ",",
            Key.OemPeriod => ".",
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
            _ => key.ToString()
        };
    }

    /// <summary>
    /// 画面表示用のキー名を取得する。
    /// Oem 系キーは現在のキーボード配列の刻印（ベストエフォート、US 配列基準）で表示する。
    /// 配列により実際の刻印とズレることがあるが、表示専用なので保存・動作には影響しない。
    /// </summary>
    private static string GetKeyDisplayName(Key key)
    {
        return key switch
        {
            // US 配列の刻印（JIS では刻印が異なるが、内部の Key は共通）
            Key.OemQuestion      => "/",
            Key.OemTilde         => "`",
            Key.OemSemicolon     => ";",
            Key.OemQuotes        => "'",
            Key.OemOpenBrackets  => "[",
            Key.OemCloseBrackets => "]",
            Key.OemPipe          => "\\(|)",
            Key.OemBackslash     => "\\(_)",
            _ => GetKeyName(key)
        };
    }

    /// <summary>
    /// 等価性を判定する
    /// </summary>
    public bool Equals(ShortcutKey? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Key == other.Key && Modifiers == other.Modifiers && MouseButton == other.MouseButton;
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
        return HashCode.Combine(Key, Modifiers, MouseButton);
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
