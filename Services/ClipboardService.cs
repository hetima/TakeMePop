using System.Runtime.InteropServices;
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

    // NotifyFirstCtrlC() 呼び出し時点のタイムスタンプ。これより新しいアイテムが来たら変化あり
    private DateTime _snapshotTime = DateTime.MinValue;

    /// <summary>NotifyFirstCtrlC() 以降にクリップボードが更新されたか</summary>
    public bool IsContentChanged { get; private set; }

    public List<ClipboardItem> History { get; } = [];

    public ClipboardService()
    {
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
    /// Ctrl+C 1回目のタイミングで呼ぶ。以降の変化検知をリセットし、スナップショット時刻を記録する
    /// </summary>
    public void NotifyFirstCtrlC()
    {
        _snapshotTime = DateTime.UtcNow;
        IsContentChanged = false;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WM_CLIPBOARDUPDATE) return IntPtr.Zero;

        var item = ClipboardItem.TryCapture();
        if (item == null) return IntPtr.Zero;

        // 直前の履歴と内容が同じテキストなら追加しない
        var last = History.Count > 0 ? History[^1] : null;
        if (last == null || last.Text != item.Text || last.HasFiles != item.HasFiles)
            History.Add(item);

        // スナップショット時刻より新しければ変化あり
        if (item.Timestamp > _snapshotTime)
            IsContentChanged = true;

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        RemoveClipboardFormatListener(_hwndSource.Handle);
        _hwndSource.Dispose();
    }
}
