using Listhing.Helpers;
using SharpHook;
using SharpHook.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace Listhing.Services;

public class KeyboardHookService
{
    private readonly Dispatcher _dispatcher;
    private readonly ClipboardService _clipboard;
    private readonly TimeSpan _doublePressInterval = TimeSpan.FromMilliseconds(400);

    private DateTime _lastCtrlCTime = DateTime.MinValue;
    private bool _cKeyReleased = true;

    // Ctrl+X ダブルタップの状態
    private DateTime _lastCtrlXTime = DateTime.MinValue;
    private bool _xKeyReleased = true;

    // グローバルホットキー登録リスト（スナップショット方式でスレッドセーフに読み取る）
    private readonly List<(ShortcutKey key, Action callback)> _hotkeys = new();
    private volatile (ShortcutKey key, Action callback)[] _hotkeySnapshot = [];

    // QuickHistory モデファイキー監視
    private bool _quickHistoryModifierMode = false;
    private ModifierKeys _quickHistoryModifiers = ModifierKeys.None;
    private ShortcutKey? _quickHistoryKey;
    private bool _quickHistoryMainKeyReleased = true;
    private KeyCode _quickHistoryMainKeyCode;
    // 押下を抑制したキャンセルキー。対応する解放イベントも抑制するために記録する。
    private KeyCode? _suppressReleaseFor;
    // 各修飾キーの押下状態（SharpHookスレッドからのみアクセス）
    private bool _ctrlPressed;
    private bool _altPressed;
    private bool _shiftPressed;
    private bool _winPressed;

    public Action? QuickHistorySelectNext { get; set; }
    public Action? QuickHistoryCommit { get; set; }
    public Action? QuickHistoryCancel { get; set; }

    public event EventHandler? CtrlCDoubleTapped;
    public event EventHandler? CtrlXDoubleTapped;

    public KeyboardHookService(GlobalHookService globalHook, Dispatcher dispatcher, ClipboardService clipboard)
    {
        _dispatcher = dispatcher;
        _clipboard = clipboard;
        globalHook.KeyPressed += OnKeyPressed;
        globalHook.KeyReleased += OnKeyReleased;
        globalHook.MousePressed += OnMousePressed;
    }

    /// <summary>
    /// QuickHistory 用ホットキーを登録する。修飾キーありの場合は SelectNext/Commit モードで動作する。
    /// </summary>
    public void RegisterQuickHistoryHotkey(ShortcutKey shortcut, Action toggleCallback)
    {
        _quickHistoryKey = shortcut;
        RegisterHotkey(shortcut, toggleCallback);
    }

    /// <summary>
    /// グローバルホットキーを登録する。同一キーが既に登録済みの場合は上書きする。
    /// </summary>
    public void RegisterHotkey(ShortcutKey shortcut, Action callback)
    {
        if (shortcut.IsEmpty) return;
        _hotkeys.RemoveAll(h => h.key.Equals(shortcut));
        _hotkeys.Add((shortcut, callback));
        _hotkeySnapshot = _hotkeys.ToArray();
    }

    /// <summary>
    /// グローバルホットキーの登録を解除する。
    /// </summary>
    public void UnregisterHotkey(ShortcutKey shortcut)
    {
        _hotkeys.RemoveAll(h => h.key.Equals(shortcut));
        _hotkeySnapshot = _hotkeys.ToArray();
    }

    /// <summary>
    /// 登録済みのグローバルホットキーをすべて解除する。設定の再適用前に呼ぶ。
    /// </summary>
    public void ClearHotkeys()
    {
        _hotkeys.Clear();
        _hotkeySnapshot = [];
        _quickHistoryKey = null;
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
#if false
        // JIS キー診断用の一時ログ。必要なときが来たら有効にする
        System.Diagnostics.Debug.WriteLine($"[KeyHook] KeyCode={e.Data.KeyCode} RawCode={e.Data.RawCode} Mask={e.RawEvent.Mask}");
#endif

        // 修飾キー押下状態を追跡
        TrackModifierPressed(e.Data.KeyCode);

        // グローバルホットキー照合（スナップショットを使ってスレッドセーフに読み取る）
        bool hotkeyMatched = false;
        foreach (var (key, callback) in _hotkeySnapshot)
        {
            if (key.MatchesHook(e))
            {
                hotkeyMatched = true;
                e.SuppressEvent = true;
                bool modifierSelect = App.SettingsService.Settings.QuickHistory.ModifierSelect;
                if (modifierSelect && key.HasModifiers && key.Equals(_quickHistoryKey) && QuickHistorySelectNext != null)
                {
                    if (!_quickHistoryMainKeyReleased && e.Data.KeyCode == _quickHistoryMainKeyCode)
                    {
                        break;
                    }

                    // 修飾キーありの QuickHistory ホットキー → SelectNext モード
                    _quickHistoryModifiers = key.Modifiers;
                    _quickHistoryModifierMode = true;
                    _quickHistoryMainKeyReleased = false;
                    _quickHistoryMainKeyCode = e.Data.KeyCode;
                    _dispatcher.BeginInvoke(QuickHistorySelectNext);
                }
                else
                {
                    var cb = callback;
                    _dispatcher.BeginInvoke(cb);
                }
                break;
            }
        }

        // モデファイキーセレクト中に対象キー以外を押したらキャンセル（設定が ON のとき）
        if (!hotkeyMatched
            && _quickHistoryModifierMode
            && App.SettingsService.Settings.QuickHistory.CancelOnOtherKey
            && !IsModifierKey(e.Data.KeyCode))
        {
            _quickHistoryModifierMode = false;
            // キャンセルキーは押下・解放とも他プロセスへ流さない
            e.SuppressEvent = true;
            _suppressReleaseFor = e.Data.KeyCode;
            if (QuickHistoryCancel != null)
                _dispatcher.BeginInvoke(QuickHistoryCancel);
            return;
        }

        // Ctrl+X ダブルタップ検出
        bool isCtrl = e.RawEvent.Mask.HasFlag(EventMask.LeftCtrl) || e.RawEvent.Mask.HasFlag(EventMask.RightCtrl);
        if (isCtrl && e.Data.KeyCode == KeyCode.VcX && _xKeyReleased)
        {
            _xKeyReleased = false;
            var nowX = DateTime.UtcNow;
            if (nowX - _lastCtrlXTime <= _doublePressInterval)
            {
                _lastCtrlXTime = DateTime.MinValue;
                _dispatcher.BeginInvoke(() => CtrlXDoubleTapped?.Invoke(this, EventArgs.Empty));
            }
            else
            {
                _lastCtrlXTime = nowX;
            }
        }

        if (e.Data.KeyCode != KeyCode.VcC) return;
        if (!e.RawEvent.Mask.HasFlag(EventMask.LeftCtrl) &&
            !e.RawEvent.Mask.HasFlag(EventMask.RightCtrl)) return;

        if (!_cKeyReleased) return;
        _cKeyReleased = false;

        var now = DateTime.UtcNow;
        var elapsed = now - _lastCtrlCTime;

        if (elapsed <= _doublePressInterval)
        {
            _lastCtrlCTime = DateTime.MinValue;
            // クリップボードの内容変化に関わらず発火する
            _dispatcher.BeginInvoke(() => CtrlCDoubleTapped?.Invoke(this, EventArgs.Empty));
        }
        else
        {
            _lastCtrlCTime = now;
        }
    }

    private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
    {
        // キャンセルで押下を抑制したキーは、対応する解放も抑制する
        if (_suppressReleaseFor == e.Data.KeyCode)
        {
            _suppressReleaseFor = null;
            e.SuppressEvent = true;
        }

        if (e.Data.KeyCode == KeyCode.VcC)
            _cKeyReleased = true;

        if (e.Data.KeyCode == KeyCode.VcX)
            _xKeyReleased = true;

        if (e.Data.KeyCode == _quickHistoryMainKeyCode)
            _quickHistoryMainKeyReleased = true;

        // 修飾キー押下状態を追跡
        TrackModifierReleased(e.Data.KeyCode);

        // QuickHistory モデファイキー監視
        if (_quickHistoryModifierMode && IsModifierKey(e.Data.KeyCode))
        {
            if (AreRequiredModifiersReleased(_quickHistoryModifiers))
            {
                _quickHistoryModifierMode = false;
                if (QuickHistoryCommit != null)
                    _dispatcher.BeginInvoke(QuickHistoryCommit);
            }
        }
    }

    private void TrackModifierPressed(KeyCode code)
    {
        if (code is KeyCode.VcLeftControl or KeyCode.VcRightControl) _ctrlPressed = true;
        else if (code is KeyCode.VcLeftAlt or KeyCode.VcRightAlt) _altPressed = true;
        else if (code is KeyCode.VcLeftShift or KeyCode.VcRightShift) _shiftPressed = true;
        else if (code is KeyCode.VcLeftMeta or KeyCode.VcRightMeta) _winPressed = true;
    }

    private void TrackModifierReleased(KeyCode code)
    {
        if (code is KeyCode.VcLeftControl or KeyCode.VcRightControl) _ctrlPressed = false;
        else if (code is KeyCode.VcLeftAlt or KeyCode.VcRightAlt) _altPressed = false;
        else if (code is KeyCode.VcLeftShift or KeyCode.VcRightShift) _shiftPressed = false;
        else if (code is KeyCode.VcLeftMeta or KeyCode.VcRightMeta) _winPressed = false;
    }

    private static bool IsModifierKey(KeyCode code) =>
        code is KeyCode.VcLeftControl or KeyCode.VcRightControl
             or KeyCode.VcLeftAlt or KeyCode.VcRightAlt
             or KeyCode.VcLeftShift or KeyCode.VcRightShift
             or KeyCode.VcLeftMeta or KeyCode.VcRightMeta;

    private bool AreRequiredModifiersReleased(ModifierKeys required)
    {
        if (required.HasFlag(ModifierKeys.Control) && _ctrlPressed) return false;
        if (required.HasFlag(ModifierKeys.Alt) && _altPressed) return false;
        if (required.HasFlag(ModifierKeys.Shift) && _shiftPressed) return false;
        if (required.HasFlag(ModifierKeys.Windows) && _winPressed) return false;
        return true;
    }

    private void OnMousePressed(object? sender, MouseHookEventArgs e)
    {
        foreach (var (key, callback) in _hotkeySnapshot)
        {
            if (key.MatchesHook(e))
            {
                e.SuppressEvent = true;
                var cb = callback;
                _dispatcher.BeginInvoke(cb);
                break;
            }
        }
    }
}
