using SharpHook;
using SharpHook.Data;
using System.Windows.Threading;

namespace Listhing.Services;

public class KeyboardHookService
{
    private readonly Dispatcher _dispatcher;
    private readonly TimeSpan _doublePressInterval = TimeSpan.FromMilliseconds(400);

    private DateTime _lastCtrlCTime = DateTime.MinValue;
    private bool _cKeyReleased = true;

    public event EventHandler? CtrlCDoubleTapped;

    public KeyboardHookService(GlobalHookService globalHook, Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
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
        if (now - _lastCtrlCTime <= _doublePressInterval)
        {
            _lastCtrlCTime = DateTime.MinValue;
            _dispatcher.BeginInvoke(() => CtrlCDoubleTapped?.Invoke(this, EventArgs.Empty));
        }
        else
        {
            _lastCtrlCTime = now;
        }
    }

    private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
    {
        if (e.Data.KeyCode == KeyCode.VcC)
            _cKeyReleased = true;
    }
}
