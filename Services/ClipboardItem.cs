using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows;

namespace Listhing.Services;

/// <summary>
/// クリップボードの内容を表すイミュータブルなスナップショット。
/// TempFiles を持つ場合は Dispose で一時ファイルを削除する。
/// </summary>
public class ClipboardItem : IDisposable
{
    public DateTime Timestamp { get; }
    public string? Text { get; }
    public IReadOnlyList<string>? Files { get; }

    /// <summary>仮想ファイルから生成した一時ファイルのパスリスト。Dispose 時に削除される。</summary>
    public IReadOnlyList<string>? TempFiles { get; }

    public bool HasImage { get; }

    public bool HasText => Text != null;
    public bool HasFiles => Files is { Count: > 0 } || TempFiles is { Count: > 0 };

    /// <summary>ドラッグ出力に使うファイルリスト（Files と TempFiles を合わせたもの）</summary>
    public IReadOnlyList<string> AllFiles
    {
        get
        {
            if (TempFiles == null) return Files ?? [];
            if (Files == null) return TempFiles;
            return [.. Files, .. TempFiles];
        }
    }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed || TempFiles == null) return;
        _disposed = true;
        foreach (var path in TempFiles)
        {
            try { File.Delete(path); } catch { }
        }
    }

    /// <summary>
    /// トースト等の短い表示用に先頭1行を取り出す。テキストがなければファイル名、それもなければ "?"
    /// </summary>
    public string GetHeadline(int maxLength = 100)
    {
        string text;
        if (HasText && Text != null)
        {
            text = Text.TrimStart();
            var newline = text.IndexOfAny(['\r', '\n']);
            if (newline >= 0) text = text[..newline] + "…";
        }
        else if (Files?[0] is string f)
        {
            text = System.IO.Path.GetFileName(f);
        }
        else
        {
            return "?";
        }
        if (text.Length > maxLength) text = text[..maxLength] + "…";
        return text;
    }

    private ClipboardItem(DateTime timestamp, string? text, IReadOnlyList<string>? files, IReadOnlyList<string>? tempFiles, bool hasImage)
    {
        Timestamp = timestamp;
        Text = text;
        Files = files;
        TempFiles = tempFiles;
        HasImage = hasImage;
    }

    /// <summary>デバッグ用：利用可能なフォーマット一覧をDebug出力する</summary>
    public static void DebugDumpFormats(System.Windows.IDataObject data)
    {
        foreach (var fmt in data.GetFormats())
            System.Diagnostics.Debug.WriteLine($"[DragDrop] format: {fmt}");
    }

    /// <summary>DragEventArgsのDataからClipboardItemを作成する。対応データがなければ null を返す</summary>
    public static ClipboardItem? TryCreateFromDragData(System.Windows.IDataObject data)
    {
        try
        {
            var now = DateTime.UtcNow;
            string? text = null;
            IReadOnlyList<string>? files = null;

            if (data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var dropped = data.GetData(System.Windows.DataFormats.FileDrop) as string[];
                if (dropped is { Length: > 0 })
                    files = dropped;
            }
            // ファイルがない場合のみURL検出。
            // FileGroupDescriptorW があり拡張子が .url でない場合は仮想ファイルなのでURLとみなさない
            bool isUrlDrop = files == null;
            if (isUrlDrop && data.GetDataPresent("FileGroupDescriptorW"))
            {
                var firstName = GetFirstFileDescriptorName(data);
                isUrlDrop = firstName != null &&
                    Path.GetExtension(firstName).Equals(".url", StringComparison.OrdinalIgnoreCase);
            }
            var urlInfo = isUrlDrop ? Helpers.DragDropHelper.GetUrlFromDropData(data) : null;
            if (urlInfo != null)
            {
                var (url, title) = urlInfo.Value;
                // タイトルがURLと同じ（Chrome/Edge の text/uri-list）なら FileGroupDescriptorW のファイル名で補完
                if (title == url && data.GetDataPresent("FileGroupDescriptorW"))
                {
                    var fgdTitle = GetFirstFileDescriptorName(data);
                    if (fgdTitle != null)
                        title = Path.GetFileNameWithoutExtension(fgdTitle);
                }
                text = string.IsNullOrEmpty(title) || title == url
                    ? url
                    : $"{title}\n{url}";
            }
            else if (data.GetDataPresent(System.Windows.DataFormats.UnicodeText))
                text = data.GetData(System.Windows.DataFormats.UnicodeText) as string;
            else if (data.GetDataPresent(System.Windows.DataFormats.Text))
                text = data.GetData(System.Windows.DataFormats.Text) as string;

            // CF_HDROP がなく仮想ファイル（ブラウザからの画像等）の場合は一時ファイルに書き出す
            // URL の場合は .url ファイルを一時展開しない
            IReadOnlyList<string>? tempFiles = null;
            if (files == null && urlInfo == null && data.GetDataPresent("FileGroupDescriptorW"))
                tempFiles = ExtractVirtualFiles(data);

            if (text == null && files == null && tempFiles == null) return null;
            return new ClipboardItem(now, text, files, tempFiles, false);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// FileGroupDescriptorW + FileContents から一時ファイルを生成してパスリストを返す。
    /// COM IDataObject を直接呼んで IStream / HGLOBAL でデータを取得する。
    /// </summary>
    private static IReadOnlyList<string>? ExtractVirtualFiles(System.Windows.IDataObject wpfData)
    {
        // WPF の IDataObject ラッパーの裏にある COM IDataObject を取得
        if (wpfData is not System.Runtime.InteropServices.ComTypes.IDataObject comData)
            return null;

        // FileGroupDescriptorW を取得してファイル名リストを読む
        var fgdFormat = new FORMATETC
        {
            cfFormat = (short)RegisterClipboardFormat("FileGroupDescriptorW"),
            dwAspect = DVASPECT.DVASPECT_CONTENT,
            lindex = -1,
            tymed = TYMED.TYMED_HGLOBAL,
        };

        comData.GetData(ref fgdFormat, out var fgdMedium);
        if (fgdMedium.unionmember == IntPtr.Zero) return null;

        List<string> fileNames;
        try
        {
            fileNames = ReadFileGroupDescriptor(fgdMedium.unionmember);
        }
        finally
        {
            ReleaseStgMedium(ref fgdMedium);
        }

        if (fileNames.Count == 0) return null;

        var tempDir = Path.Combine(Path.GetTempPath(), "TakeMePop");
        Directory.CreateDirectory(tempDir);

        var result = new List<string>();
        for (int i = 0; i < fileNames.Count; i++)
        {
            var tempPath = Path.Combine(tempDir, fileNames[i]);
            if (TryExtractFileContents(comData, i, tempPath))
                result.Add(tempPath);
        }

        return result.Count > 0 ? result : null;
    }

    /// <summary>FileGroupDescriptorW から最初のファイル名を取得する（拡張子あり）</summary>
    private static string? GetFirstFileDescriptorName(System.Windows.IDataObject wpfData)
    {
        if (wpfData is not System.Runtime.InteropServices.ComTypes.IDataObject comData) return null;
        var fmt = new FORMATETC
        {
            cfFormat = (short)RegisterClipboardFormat("FileGroupDescriptorW"),
            dwAspect = DVASPECT.DVASPECT_CONTENT,
            lindex = -1,
            tymed = TYMED.TYMED_HGLOBAL,
        };
        comData.GetData(ref fmt, out var medium);
        if (medium.unionmember == IntPtr.Zero) return null;
        try
        {
            var names = ReadFileGroupDescriptor(medium.unionmember);
            return names.Count == 0 ? null : names[0];
        }
        finally
        {
            ReleaseStgMedium(ref medium);
        }
    }

    /// <summary>FileContents を取得して指定パスに書き出す</summary>
    private static bool TryExtractFileContents(System.Runtime.InteropServices.ComTypes.IDataObject comData, int index, string destPath)
    {
        var fcFormat = new FORMATETC
        {
            cfFormat = (short)RegisterClipboardFormat("FileContents"),
            dwAspect = DVASPECT.DVASPECT_CONTENT,
            lindex = index,
            tymed = TYMED.TYMED_ISTREAM | TYMED.TYMED_HGLOBAL,
        };

        try
        {
            comData.GetData(ref fcFormat, out var medium);
            try
            {
                // ディレクトリを作成（サブフォルダ付きファイル名の場合）
                var dir = Path.GetDirectoryName(destPath);
                if (dir != null) Directory.CreateDirectory(dir);

                if (medium.tymed == TYMED.TYMED_ISTREAM && medium.unionmember != IntPtr.Zero)
                {
                    var stream = (IStream)Marshal.GetObjectForIUnknown(medium.unionmember);
                    WriteStreamToFile(stream, destPath);
                }
                else if (medium.tymed == TYMED.TYMED_HGLOBAL && medium.unionmember != IntPtr.Zero)
                {
                    WriteHGlobalToFile(medium.unionmember, destPath);
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                ReleaseStgMedium(ref medium);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void WriteStreamToFile(IStream stream, string path)
    {
        using var fs = File.Create(path);
        var buf = new byte[65536];
        var pRead = Marshal.AllocHGlobal(Marshal.SizeOf<int>());
        try
        {
            while (true)
            {
                stream.Read(buf, buf.Length, pRead);
                int read = Marshal.ReadInt32(pRead);
                if (read <= 0) break;
                fs.Write(buf, 0, read);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(pRead);
        }
    }

    private static void WriteHGlobalToFile(IntPtr hGlobal, string path)
    {
        var ptr = GlobalLock(hGlobal);
        try
        {
            var size = (int)GlobalSize(hGlobal);
            var bytes = new byte[size];
            Marshal.Copy(ptr, bytes, 0, size);
            File.WriteAllBytes(path, bytes);
        }
        finally
        {
            GlobalUnlock(hGlobal);
        }
    }

    /// <summary>HGLOBAL から FILEGROUPDESCRIPTOR を読んでファイル名リストを返す</summary>
    private static List<string> ReadFileGroupDescriptor(IntPtr hGlobal)
    {
        var ptr = GlobalLock(hGlobal);
        var names = new List<string>();
        try
        {
            int count = Marshal.ReadInt32(ptr);
            // FILEDESCRIPTORW は 592 バイト、cItems の後に配列が続く
            var descPtr = ptr + 4;
            const int descSize = 592;
            for (int i = 0; i < count; i++)
            {
                // cFileName は offset 72 の 260 文字 Unicode 文字列
                var namePtr = descPtr + i * descSize + 72;
                var name = Marshal.PtrToStringUni(namePtr, 260)!.TrimEnd('\0');
                names.Add(name);
            }
        }
        finally
        {
            GlobalUnlock(hGlobal);
        }
        return names;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClipboardFormat(string lpszFormat);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll")]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll")]
    private static extern UIntPtr GlobalSize(IntPtr hMem);

    [DllImport("ole32.dll")]
    private static extern void ReleaseStgMedium(ref STGMEDIUM pMedium);

    /// <summary>現在のクリップボードからスナップショットを作成する。取得失敗時は null を返す</summary>
    public static ClipboardItem? TryCapture()
    {
        try
        {
            var now = DateTime.UtcNow;
            var text = Clipboard.ContainsText() ? Clipboard.GetText() : null;
            var files = Clipboard.ContainsFileDropList()
                ? Clipboard.GetFileDropList().Cast<string>().ToList()
                : null;
            var hasImage = Clipboard.ContainsImage();

            // 何も入っていなければ null
            if (text == null && files == null && !hasImage) return null;

            return new ClipboardItem(now, text, files, null, hasImage);
        }
        catch
        {
            return null;
        }
    }
}
