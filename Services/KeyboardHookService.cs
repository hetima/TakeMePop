using Listhing.Helpers;
using SharpHook;
using SharpHook.Data;
using System.Windows.Threading;

namespace Listhing.Services;

public class KeyboardHookService
{
    private readonly Dispatcher _dispatcher;
    private readonly ClipboardService _clipboard;
    private readonly TimeSpan _doublePressInterval = TimeSpan.FromMilliseconds(400);
    private readonly TimeSpan _clipboardLagMargin = TimeSpan.FromMilliseconds(50);

    private DateTime _lastCtrlCTime = DateTime.MinValue;
    private bool _cKeyReleased = true;

    // Ctrl+V → Ctrl+X シーケンスの状態
    private readonly TimeSpan _ctrlVXInterval = TimeSpan.FromMilliseconds(750);
    private bool _ctrlVPending = false;
    private System.Windows.Threading.DispatcherTimer? _ctrlVXTimer;

    // グローバルホットキー登録リスト（スナップショット方式でスレッドセーフに読み取る）
    private readonly List<(ShortcutKey key, Action callback)> _hotkeys = new();
    private volatile (ShortcutKey key, Action callback)[] _hotkeySnapshot = [];

    public event EventHandler? CtrlCDoubleTapped;
    public event EventHandler? CtrlVXTriggered;

    public KeyboardHookService(GlobalHookService globalHook, Dispatcher dispatcher, ClipboardService clipboard)
    {
        _dispatcher = dispatcher;
        _clipboard = clipboard;
        globalHook.KeyPressed += OnKeyPressed;
        globalHook.KeyReleased += OnKeyReleased;
        globalHook.MousePressed += OnMousePressed;
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

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        // グローバルホットキー照合（スナップショットを使ってスレッドセーフに読み取る）
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

        // Ctrl+V / Ctrl+X シーケンス検出
        bool isCtrl = e.RawEvent.Mask.HasFlag(EventMask.LeftCtrl) || e.RawEvent.Mask.HasFlag(EventMask.RightCtrl);
        if (isCtrl)
        {
            if (e.Data.KeyCode == KeyCode.VcV)
            {
                _ctrlVPending = true;
                _dispatcher.BeginInvoke(StartCtrlVXTimer);
            }
            else if (e.Data.KeyCode == KeyCode.VcX && _ctrlVPending)
            {
                e.SuppressEvent = true;
                _ctrlVPending = false;
                _dispatcher.BeginInvoke(CancelCtrlVXTimer);
                _dispatcher.BeginInvoke(() => CtrlVXTriggered?.Invoke(this, EventArgs.Empty));
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
            _dispatcher.BeginInvoke(() => TryFireAfterClipboardSettle(elapsed));
        }
        else
        {
            _lastCtrlCTime = now;
            _dispatcher.BeginInvoke(() => _clipboard.NotifyFirstCtrlC());
        }
    }

    private void TryFireAfterClipboardSettle(TimeSpan elapsedBetweenPresses)
    {
        if (_clipboard.IsContentChanged)
        {
            CtrlCDoubleTapped?.Invoke(this, EventArgs.Empty);
            return;
        }

        // 1回目から十分な時間が経っていない場合は余裕を持って再チェック
        var waitMs = (int)(_clipboardLagMargin - elapsedBetweenPresses).TotalMilliseconds;
        if (waitMs <= 0)
        {
            // 既に50ms以上経過しているのに変化なし → コピーなし
            return;
        }

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(waitMs) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            if (_clipboard.IsContentChanged)
                CtrlCDoubleTapped?.Invoke(this, EventArgs.Empty);
        };
        timer.Start();
    }

    private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
    {
        if (e.Data.KeyCode == KeyCode.VcC)
            _cKeyReleased = true;

        if (e.Data.KeyCode == KeyCode.VcLeftControl || e.Data.KeyCode == KeyCode.VcRightControl)
        {
            if (_ctrlVPending)
            {
                _ctrlVPending = false;
                _dispatcher.BeginInvoke(CancelCtrlVXTimer);
            }
        }
    }

    /// <summary>
    /// Ctrl+V 検出後の待機タイマーを開始する。750ms 経過で _ctrlVPending をリセット。
    /// </summary>
    private void StartCtrlVXTimer()
    {
        CancelCtrlVXTimer();
        _ctrlVXTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = _ctrlVXInterval
        };
        _ctrlVXTimer.Tick += (_, _) =>
        {
            _ctrlVXTimer.Stop();
            _ctrlVXTimer = null;
            _ctrlVPending = false;
        };
        _ctrlVXTimer.Start();
    }

    /// <summary>
    /// Ctrl+V 待機タイマーをキャンセルする。
    /// </summary>
    private void CancelCtrlVXTimer()
    {
        _ctrlVXTimer?.Stop();
        _ctrlVXTimer = null;
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
