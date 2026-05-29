using System.Windows;

namespace Listhing.Services;

/// <summary>
/// クリップボードの内容を表すイミュータブルなスナップショット
/// </summary>
public class ClipboardItem
{
    public DateTime Timestamp { get; }
    public string? Text { get; }
    public IReadOnlyList<string>? Files { get; }
    public bool HasImage { get; }

    public bool HasText => Text != null;
    public bool HasFiles => Files is { Count: > 0 };

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

    private ClipboardItem(DateTime timestamp, string? text, IReadOnlyList<string>? files, bool hasImage)
    {
        Timestamp = timestamp;
        Text = text;
        Files = files;
        HasImage = hasImage;
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
            if (data.GetDataPresent(System.Windows.DataFormats.UnicodeText))
                text = data.GetData(System.Windows.DataFormats.UnicodeText) as string;
            else if (data.GetDataPresent(System.Windows.DataFormats.Text))
                text = data.GetData(System.Windows.DataFormats.Text) as string;

            if (text == null && files == null) return null;
            return new ClipboardItem(now, text, files, false);
        }
        catch
        {
            return null;
        }
    }

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

            return new ClipboardItem(now, text, files, hasImage);
        }
        catch
        {
            return null;
        }
    }
}
