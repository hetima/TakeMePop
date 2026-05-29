using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace Listhing.Helpers;

/// <summary>
/// ドラッグ・アンド・ドロップ操作のヘルパークラス
/// </summary>
public static class DragDropHelper
{
    /// <summary>
    /// ドロップデータからURLとタイトルを抽出します
    /// </summary>
    /// <param name="data">ドロップデータ</param>
    /// <returns>URLとタイトルのタプル。抽出できない場合はnull</returns>
    public static (string Url, string Title)? GetUrlFromDropData(IDataObject data)
    {
        if (!ContainsUrl(data))
            return null;

        string url = string.Empty;
        string title = string.Empty;

        // Firefox: text/x-moz-url (UTF-16形式)
        if (data.GetDataPresent("text/x-moz-url"))
        {
            var rawData = data.GetData("text/x-moz-url");
            string? dataString = null;

            // MemoryStreamとして取得される場合（UTF-16）
            if (rawData is MemoryStream memoryStream)
            {
                var bytes = memoryStream.ToArray();
                dataString = Encoding.Unicode.GetString(bytes);
            }
            // 文字列として取得される場合
            else if (rawData is string str)
            {
                dataString = str;
            }

            if (!string.IsNullOrEmpty(dataString))
            {
                // 改行文字で分割（URL[改行]タイトルの形式）
                var parts = dataString.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                url = parts.Length > 0 ? parts[0].Trim('\0').Trim() : string.Empty;
                title = parts.Length > 1 ? parts[1].Trim('\0').Trim() : url;
            }
        }
        // Chrome/Edge: text/uri-list
        else if (data.GetDataPresent("text/uri-list"))
        {
            var dataString = data.GetData("text/uri-list") as string;
            if (!string.IsNullOrEmpty(dataString))
            {
                // 複数URLの場合は最初の1つのみ取得（コメント行は除外）
                var lines = dataString.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                url = lines.FirstOrDefault(l => !l.StartsWith("#")) ?? string.Empty;
                title = url;
            }
        }

        // URLの有効性チェック
        if (string.IsNullOrEmpty(url))
            return null;

        if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult) &&
            (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
        {
            return (url, title);
        }

        return null;
    }

    /// <summary>
    /// ドロップデータがURLを含んでいるかどうかを判定します
    /// </summary>
    /// <param name="data">ドロップデータ</param>
    /// <returns>URLを含んでいる場合はtrue</returns>
    public static bool ContainsUrl(IDataObject data)
    {
        if (data == null)
            return false;

        return data.GetDataPresent("text/x-moz-url") ||
               data.GetDataPresent("text/uri-list");
    }

    /// <summary>
    /// ドロップデータがファイルを含んでいるかどうかを判定します
    /// </summary>
    /// <param name="data">ドロップデータ</param>
    /// <returns>ファイルを含んでいる場合はtrue</returns>
    public static bool ContainsFile(IDataObject data)
    {
        if (data == null)
            return false;

        return data.GetDataPresent(DataFormats.FileDrop);
    }

    /// <summary>
    /// ドロップデータからファイルパスの配列を取得します
    /// </summary>
    /// <param name="data">ドロップデータ</param>
    /// <returns>ファイルパスの配列。取得できない場合はnull</returns>
    public static string[]? GetFilesFromDropData(IDataObject data)
    {
        if (!ContainsFile(data))
            return null;

        return data.GetData(DataFormats.FileDrop) as string[];
    }

    /// <summary>
    /// クリップボードからファイルパスの配列を取得します
    /// </summary>
    /// <returns>ファイルパスの配列。取得できない場合はnull</returns>
    public static string[]? GetFilesFromClipboard()
    {
        var data = Clipboard.GetDataObject();
        if (data == null)
            return null;

        return GetFilesFromDropData(data);
    }
}
