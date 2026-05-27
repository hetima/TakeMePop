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

    public event EventHandler? CtrlCDoubleTapped;

    public KeyboardHookService(GlobalHookService globalHook, Dispatcher dispatcher, ClipboardService clipboard)
    {
        _dispatcher = dispatcher;
        _clipboard = clipboard;
        globalHook.KeyPressed += OnKeyPressed;
        globalHook.KeyReleased += OnKeyReleased;
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
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
    }
}
