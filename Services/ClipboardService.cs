using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Listhing.Services;

/// <summary>
/// クリップボードの変化を監視し、履歴を管理するサービス
/// </summary>
public class ClipboardService : IDisposable
{
    [DllImport("user32.dll")]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    private const int WM_CLIPBOARDUPDATE = 0x031D;

    private readonly HwndSource _hwndSource;
    private string? _snapshotText;

    public bool IsContentChanged { get; private set; }

    public List<string> History { get; } = new();

    public ClipboardService()
    {
        // メッセージ受信用の不可視ウィンドウを作成
        var p = new HwndSourceParameters("ClipboardWatcher")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0,
        };
        _hwndSource = new HwndSource(p);
        _hwndSource.AddHook(WndProc);
        AddClipboardFormatListener(_hwndSource.Handle);
    }

    /// <summary>
    /// Ctrl+C 1回目のタイミングで呼ぶ。現在のクリップボード内容をスナップショットとして保存し、
    /// 以降の変化検知をリセットする
    /// </summary>
    public void NotifyFirstCtrlC()
    {
        IsContentChanged = false;
        _snapshotText = GetCurrentText();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_CLIPBOARDUPDATE)
        {
            var text = GetCurrentText();
            if (text != null)
            {
                if (History.Count == 0 || History[^1] != text)
                    History.Add(text);
            }

            // スナップショット以降に内容が変化したか判定
            if (_snapshotText != null && text != _snapshotText)
                IsContentChanged = true;
        }
        return IntPtr.Zero;
    }

    private static string? GetCurrentText()
    {
        try
        {
            return Clipboard.ContainsText() ? Clipboard.GetText() : null;
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        RemoveClipboardFormatListener(_hwndSource.Handle);
        _hwndSource.Dispose();
    }
}
