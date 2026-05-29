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

    /// <summary>履歴が追加・削除されたときに発火する</summary>
    public event Action? HistoryChanged;

    /// <summary>外部から履歴変更を通知する（手動削除後などに呼ぶ）</summary>
    public void NotifyHistoryChanged() => HistoryChanged?.Invoke();

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

        // 直前の履歴と内容が同じなら追加しない
        var last = History.Count > 0 ? History[^1] : null;
        bool isDuplicate = last != null &&
            last.Text == item.Text &&
            Enumerable.SequenceEqual(last.Files ?? [], item.Files ?? []);
        if (!isDuplicate)
        {
            History.Add(item);
            TrimHistoryIfNeeded();
            HistoryChanged?.Invoke();
        }

        // スナップショット時刻より新しく、かつ内容が変化していれば変化あり
        if (!isDuplicate && item.Timestamp > _snapshotTime)
            IsContentChanged = true;

        return IntPtr.Zero;
    }

    /// <summary>
    /// 最新の履歴を削除し、2番目の履歴をクリップボードにセットして返す。
    /// 履歴が1件以下の場合は何もせず null を返す。
    /// </summary>
    public ClipboardItem? PopLatestAndRestorePrevious()
    {
        if (History.Count < 2) return null;

        History.RemoveAt(History.Count - 1);
        var prev = History[^1];
        HistoryChanged?.Invoke();

        // クリップボード更新による履歴追加を抑制するためスナップショット時刻を更新
        _snapshotTime = DateTime.UtcNow;
        IsContentChanged = false;

        if (prev.HasFiles && prev.Files != null)
        {
            var coll = new System.Collections.Specialized.StringCollection();
            coll.AddRange(prev.Files.ToArray());
            System.Windows.Clipboard.SetFileDropList(coll);
        }
        else if (prev.HasText && prev.Text != null)
        {
            System.Windows.Clipboard.SetText(prev.Text);
        }

        return prev;
    }

    /// <summary>
    /// 履歴件数が上限に達したら古い方から半分削除する。
    /// </summary>
    private void TrimHistoryIfNeeded()
    {
        var limit = App.SettingsService.Settings.ClipboardHistoryLimit;
        if (History.Count >= limit)
            History.RemoveRange(0, limit / 2);
    }

    public void Dispose()
    {
        RemoveClipboardFormatListener(_hwndSource.Handle);
        _hwndSource.Dispose();
    }
}
