using SharpHook;
using SharpHook.Data;

namespace Listhing.Services;

/// <summary>
/// libuiohook のグローバルフックインスタンスを唯一保持し、マウス・キーイベントを各サービスへ中継する
/// </summary>
public class GlobalHookService : IDisposable
{
    private readonly SimpleGlobalHook _hook;

    public event EventHandler<MouseHookEventArgs>? MousePressed;
    public event EventHandler<MouseHookEventArgs>? MouseReleased;
    public event EventHandler<MouseHookEventArgs>? MouseDragged;
    public event EventHandler<KeyboardHookEventArgs>? KeyPressed;
    public event EventHandler<KeyboardHookEventArgs>? KeyReleased;

    public GlobalHookService()
    {
        _hook = new SimpleGlobalHook(runAsyncOnBackgroundThread: true);
        _hook.MousePressed += (s, e) => MousePressed?.Invoke(s, e);
        _hook.MouseReleased += (s, e) => MouseReleased?.Invoke(s, e);
        _hook.MouseDragged += (s, e) => MouseDragged?.Invoke(s, e);
        _hook.KeyPressed += (s, e) => KeyPressed?.Invoke(s, e);
        _hook.KeyReleased += (s, e) => KeyReleased?.Invoke(s, e);
    }

    public void Start() => _hook.RunAsync();

    public void Dispose() => _hook.Dispose();
}
