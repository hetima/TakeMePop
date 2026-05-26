using SharpHook;
using SharpHook.Data;
using System.Windows.Threading;

namespace Listhing.Services;

/// <summary>
/// グローバルマウスフックを管理し、ドラッグ状態を追跡するサービス
/// </summary>
public class MouseHookService : IDisposable
{
    private readonly SimpleGlobalHook _hook;
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _earlyCapturTimer;

    // システムのドラッグ開始判定距離（UIスレッドで一度だけ読む）
    private readonly double _minDragX;
    private readonly double _minDragY;

    private bool _isButtonDown = false;
    private bool _isDragging = false;
    private bool _earlyCaptureShown = false;
    private System.Drawing.Point _dragStartPoint;

    // 早期キャプチャウィンドウを自動的に隠すまでの時間
    // TODO: 仮の時間。後でユーザーが設定可能にする
    private static readonly TimeSpan EarlyCaptureHideDelay = TimeSpan.FromMilliseconds(1500);

    /// <summary>ドラッグが開始されたとき（MouseDown 直後）</summary>
    public event EventHandler<System.Drawing.Point>? DragStarted;

    /// <summary>ドラッグが終了したとき（MouseUp）</summary>
    public event EventHandler? DragEnded;

    /// <summary>早期キャプチャウィンドウを表示すべきタイミング（ドラッグ判定距離を超えた直後）</summary>
    public event EventHandler<System.Drawing.Point>? EarlyCaptureRequested;

    /// <summary>現在ドラッグ中かどうか</summary>
    public bool IsDragging => _isDragging;

    public MouseHookService(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;

        // TODO: 後で設定UIから変更可能にする
        _minDragX = 6;
        _minDragY = 6;

        _hook = new SimpleGlobalHook(runAsyncOnBackgroundThread: true);
        _hook.MousePressed += OnMousePressed;
        _hook.MouseReleased += OnMouseReleased;
        _hook.MouseDragged += OnMouseDragged;

        _earlyCapturTimer = new DispatcherTimer { Interval = EarlyCaptureHideDelay };
        _earlyCapturTimer.Tick += OnEarlyCaptureTimerTick;
    }

    /// <summary>フックを開始する</summary>
    public void Start()
    {
        _hook.RunAsync();
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

        // システムのドラッグ判定距離を超えたら表示
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
        _hook.MousePressed -= OnMousePressed;
        _hook.MouseReleased -= OnMouseReleased;
        _hook.MouseDragged -= OnMouseDragged;
        _hook.Dispose();
    }
}
