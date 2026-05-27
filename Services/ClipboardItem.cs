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

    private ClipboardItem(DateTime timestamp, string? text, IReadOnlyList<string>? files, bool hasImage)
    {
        Timestamp = timestamp;
        Text = text;
        Files = files;
        HasImage = hasImage;
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
