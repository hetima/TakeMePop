using SharpHook;
using SharpHook.Data;
using System.Windows.Threading;

namespace Listhing.Services;

public class MouseHookService : IDisposable
{
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _earlyCapturTimer;

    private double _minDragX = 6;
    private double _minDragY = 6;
    private TimeSpan _earlyCaptureHideDelay = TimeSpan.FromMilliseconds(1500);

    private bool _isButtonDown = false;
    private bool _isDragging = false;
    private bool _earlyCaptureShown = false;
    private System.Drawing.Point _dragStartPoint;

    /// <summary>ドラッグが開始されたとき（MouseDown 直後）</summary>
    public event EventHandler<System.Drawing.Point>? DragStarted;

    /// <summary>ドラッグが終了したとき（MouseUp）</summary>
    public event EventHandler? DragEnded;

    /// <summary>早期キャプチャウィンドウを表示すべきタイミング（ドラッグ判定距離を超えた直後）</summary>
    public event EventHandler<System.Drawing.Point>? EarlyCaptureRequested;

    /// <summary>現在ドラッグ中かどうか</summary>
    public bool IsDragging => _isDragging;

    public MouseHookService(GlobalHookService globalHook, Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;

        globalHook.MousePressed += OnMousePressed;
        globalHook.MouseReleased += OnMouseReleased;
        globalHook.MouseDragged += OnMouseDragged;

        _earlyCapturTimer = new DispatcherTimer { Interval = _earlyCaptureHideDelay };
        _earlyCapturTimer.Tick += OnEarlyCaptureTimerTick;
    }

    public void ApplySettings(int activationPixels, int dismissDelayMs)
    {
        _minDragX = activationPixels;
        _minDragY = activationPixels;
        _earlyCaptureHideDelay = TimeSpan.FromMilliseconds(dismissDelayMs);
        _earlyCapturTimer.Interval = _earlyCaptureHideDelay;
    }

    private void OnMousePressed(object? sender, MouseHookEventArgs e)
    {
        if (e.Data.Button != MouseButton.Button1) return;

        _isButtonDown = true;
        _isDragging = false;
        _earlyCaptureShown = false;
        _dragStartPoint = new System.Drawing.Point(e.Data.X, e.Data.Y);

        _dispatcher.BeginInvoke(() =>
        {
            _earlyCapturTimer.Stop();
            DragStarted?.Invoke(this, _dragStartPoint);
        });
    }

    private void OnMouseReleased(object? sender, MouseHookEventArgs e)
    {
        if (e.Data.Button != MouseButton.Button1) return;

        _isButtonDown = false;
        _isDragging = false;
        _earlyCaptureShown = false;

        _dispatcher.BeginInvoke(() =>
        {
            _earlyCapturTimer.Stop();
            DragEnded?.Invoke(this, EventArgs.Empty);
        });
    }

    private void OnMouseDragged(object? sender, MouseHookEventArgs e)
    {
        if (!_isButtonDown || _earlyCaptureShown) return;

        double dx = Math.Abs(e.Data.X - _dragStartPoint.X);
        double dy = Math.Abs(e.Data.Y - _dragStartPoint.Y);
        if (dx < _minDragX && dy < _minDragY) return;

        _isDragging = true;
        _earlyCaptureShown = true;
        var point = _dragStartPoint;

        _dispatcher.BeginInvoke(() =>
        {
            _earlyCapturTimer.Stop();
            _earlyCapturTimer.Start();
            EarlyCaptureRequested?.Invoke(this, point);
        });
    }

    private void OnEarlyCaptureTimerTick(object? sender, EventArgs e)
    {
        _earlyCapturTimer.Stop();
        DragEnded?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        _earlyCapturTimer.Stop();
    }
}
