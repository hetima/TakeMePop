using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.System.Com.StructuredStorage;

namespace Listhing.Helpers;

/// <summary>
/// ファイルシステム操作に関するヘルパークラス
/// </summary>
public static class FileSystemHelper
{
    /// <summary>
    /// 特定のファイルを選択した状態でフォルダを開きます
    /// </summary>
    /// <param name="pidlFolder">フォルダのPIDL</param>
    /// <param name="cidl">選択するアイテムの数</param>
    /// <param name="apidl">アイテムのPIDL配列</param>
    /// <param name="dwFlags">フラグ</param>
    /// <returns>実行結果（0: 成功）</returns>
    [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int SHOpenFolderAndSelectItems(
        IntPtr pidlFolder,
        uint cidl,
        [MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl,
        uint dwFlags);

    /// <summary>
    /// パス名からPIDLを取得します
    /// </summary>
    /// <param name="pszName">パス名</param>
    /// <param name="pbc">バインドコンテキスト</param>
    /// <param name="ppidl">出力PIDL</param>
    /// <param name="sfgaoIn">属性フラグ</param>
    /// <param name="psfgaoOut">出力属性フラグ</param>
    /// <returns>実行結果（0: 成功）</returns>
    [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int SHParseDisplayName(
        [MarshalAs(UnmanagedType.LPWStr)] string pszName,
        IntPtr pbc,
        out IntPtr ppidl,
        uint sfgaoIn,
        out uint psfgaoOut);

    /// <summary>
    /// メモリを解放します
    /// </summary>
    /// <param name="pv">解放するメモリのポインタ</param>
    [DllImport("ole32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern void CoTaskMemFree(IntPtr pv);

    /// <summary>
    /// 指定したフォルダをエクスプローラーで開きます
    /// </summary>
    /// <param name="folderPath">開くフォルダのパス</param>
    /// <returns>成功した場合はtrue、失敗した場合はfalse</returns>
    public static bool OpenFolderInExplorer(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return false;
        }

        if (!Directory.Exists(folderPath))
        {
            return false;
        }

        // エクスプローラーでフォルダを開く
        // TODO: symbolic linkやショートカットの場合、タブではなく新規ウィンドウで開いてしまう
        var processStartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = folderPath.EndsWith('\\') ? folderPath : folderPath + "\\",
            UseShellExecute = true
        };
        System.Diagnostics.Process.Start(processStartInfo);
        return true;
    }

    /// <summary>
    /// 指定したファイルまたはフォルダを含むフォルダをエクスプローラーで開き、選択します
    /// </summary>
    /// <param name="path">ファイルまたはフォルダのパス</param>
    /// <returns>成功した場合はtrue、失敗した場合はfalse</returns>
    public static bool OpenFolderAndSelect(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var pidlFolder = IntPtr.Zero;
        var pidlFile = IntPtr.Zero;

        try
        {
            // ファイルかフォルダかを判定
            bool isFile = File.Exists(path);
            bool isFolder = Directory.Exists(path);

            if (!isFile && !isFolder)
            {
                return false;
            }

            string? folderPath;
            string targetPath;

            if (isFile)
            {
                // ファイルの場合: 親フォルダを開いてファイルを選択
                folderPath = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(folderPath))
                {
                    return false;
                }
                targetPath = path;
            }
            else
            {
                // フォルダの場合: 親フォルダを開いてフォルダを選択
                folderPath = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(folderPath))
                {
                    return false;
                }
                targetPath = path;
            }

            uint psfgaoOut;
            var hr = SHParseDisplayName(folderPath, IntPtr.Zero, out pidlFolder, 0, out psfgaoOut);
            if (hr != 0)
            {
                return false;
            }

            // ターゲット（ファイルまたはフォルダ）のPIDLを取得
            hr = SHParseDisplayName(targetPath, IntPtr.Zero, out pidlFile, 0, out psfgaoOut);
            if (hr != 0)
            {
                return false;
            }

            // 特定のアイテムを選択した状態でフォルダを開く
            IntPtr[] fileArray = { pidlFile };
            hr = SHOpenFolderAndSelectItems(pidlFolder, (uint)fileArray.Length, fileArray, 0);
            if (hr != 0)
            {
                return false;
            }

            return true;
        }
        finally
        {
            // PIDLを解放
            if (pidlFolder != IntPtr.Zero)
            {
                CoTaskMemFree(pidlFolder);
            }
            if (pidlFile != IntPtr.Zero)
            {
                CoTaskMemFree(pidlFile);
            }
        }
    }

    /// <summary>
    /// アプリケーションデータディレクトリのパスを取得します
    /// </summary>
    /// <returns>データディレクトリのパス</returns>
    public static string GetDataDirectory()
    {
        return AppConstants.Settings.GetDataDirectory();
    }

    /// <summary>
    /// .urlファイル（インターネットショートカット）からURLを抽出する
    /// </summary>
    /// <param name="filePath">.urlファイルのパス</param>
    /// <returns>抽出されたURL、見つからない場合はnull</returns>
    public static string? ExtractUrlFromInternetShortcut(string filePath)
    {
        try
        {
            var lines = File.ReadAllLines(filePath);
            bool inInternetShortcutSection = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // セクション判定
                if (trimmedLine.Equals("[InternetShortcut]", StringComparison.OrdinalIgnoreCase))
                {
                    inInternetShortcutSection = true;
                    continue;
                }

                // 次のセクションに入ったら終了
                if (trimmedLine.StartsWith("[") && inInternetShortcutSection)
                {
                    break;
                }

                // URL=の行を探す
                if (inInternetShortcutSection && trimmedLine.StartsWith("URL=", StringComparison.OrdinalIgnoreCase))
                {
                    return trimmedLine.Substring(4).Trim();
                }
            }
        }
        catch
        {
            // ファイル読み込みエラーは無視
        }

        return null;
    }

    /// <summary>
    /// 実行ファイル名からフルパスを取得します
    /// </summary>
    /// <param name="exeName">実行ファイル名（例: pwsh.exe）</param>
    /// <returns>フルパス、見つからない場合はnull</returns>
    public static string? GetExecutablePath(string exeName)
    {
        if (string.IsNullOrWhiteSpace(exeName))
        {
            return null;
        }

        // PATH環境変数から検索
        var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator);
        if (paths == null) return null;

        foreach (var path in paths)
        {
            var fullPath = Path.Combine(path, exeName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }
        return null;
    }

    /// <summary>
    /// アプリケーションを指定した引数で起動します
    /// </summary>
    /// <param name="appPath">アプリケーションの実行ファイルパス</param>
    /// <param name="arguments">引数文字列。ファイルパスの場合は存在すれば引用符で囲み、存在しなければそのまま使用。nullまたは空の場合は引数なしで起動</param>
    /// <param name="wdPath">作業ディレクトリのパス（nullまたは空の場合はappPathのディレクトリを使用）</param>
    /// <returns>成功した場合はtrue、失敗した場合はfalse</returns>
    public static bool OpenApplicationWithArguments(string appPath, string? arguments = null, string? wdPath = null)
    {
        if (string.IsNullOrWhiteSpace(appPath))
        {
            return false;
        }

        if (!File.Exists(appPath))
        {
            return false;
        }

        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = appPath,
                UseShellExecute = false,
                WorkingDirectory = string.IsNullOrWhiteSpace(wdPath) ? Path.GetDirectoryName(appPath) : wdPath
            };

            // 引数が指定されている場合
            if (!string.IsNullOrWhiteSpace(arguments))
            {
                // 引数がファイルとして存在する場合は引用符で囲む
                if (File.Exists(arguments) || Directory.Exists(arguments))
                {
                    startInfo.Arguments = $"\"{arguments}\"";
                }
                else
                {
                    // 存在しない場合はそのまま使用
                    startInfo.Arguments = arguments;
                }
            }

            if (Path.GetExtension(appPath).Equals(".lnk", StringComparison.OrdinalIgnoreCase))
            {
                startInfo.UseShellExecute = true;
            }

            System.Diagnostics.Process.Start(startInfo);
            return true;
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"カスタムアプリの起動に失敗しました: {ex.Message}");
            return false;
        }
    }

}