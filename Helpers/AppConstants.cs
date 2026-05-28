using System.IO;

namespace Listhing.Helpers;

/// <summary>
/// アプリケーション全体で使用する定数
/// </summary>
public static class AppConstants
{
    /// <summary>
    /// アプリの名前
    /// </summary>
    public const string AppName = "TakeMePop";

    /// <summary>
    /// データベース関連の定数
    /// </summary>
    public static class Database
    {
        /// <summary>
        /// データベースルートフォルダ名
        /// </summary>
#if DEBUG
        public const string DatabaseRoot = "debug_Workspaces";
#else
        public const string DatabaseRoot = "Workspaces";
#endif

        /// <summary>
        /// データベースファイル名
        /// </summary>
        public const string FileName = "data.sqlite";

        /// <summary>
        /// アイコンデータベースファイル名
        /// </summary>
        public const string IconDbFileName = "icons.sqlite";

        /// <summary>
        /// デザイン時用データベースファイル名
        /// </summary>
        public const string DesignFileName = "design.db";

        /// <summary>
        /// デザイン時用アイコンデータベースファイル名
        /// </summary>
        public const string IconDesignFileName = "icons_design.db";
        /// <summary>
        /// ウィンドウ設定ファイル名
        /// </summary>
        public const string WindowSettingsFileName = "window_settings.json";
    }

    /// <summary>
    /// 設定関連の定数
    /// </summary>
    public static class Settings
    {
        /// <summary>
        /// 設定ルートフォルダ名
        /// </summary>
        public const string SettingRoot = "TakeMePop";

        /// <summary>
        /// 設定ファイル名
        /// </summary>
#if DEBUG
        public const string FileName = "debug_settings.json";
#else
        public const string FileName = "settings.json";
#endif

        /// <summary>
        /// アプリケーションデータディレクトリのパスを取得します
        /// </summary>
        /// <returns>データディレクトリのパス</returns>
        public static string GetDataDirectory()
        {
            if (_dataDirectory != null)
            {
                return _dataDirectory;
            }
            var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _dataDirectory = Path.Combine(localAppDataPath, SettingRoot);

            Directory.CreateDirectory(_dataDirectory);

            return _dataDirectory;
        }
        private static string _dataDirectory = GetDataDirectory();

        /// <summary>
        /// データベースルートパスを組み立てます
        /// </summary>
        /// <param name="name">データベース名</param>
        /// <returns>データベースルートパス</returns>
        public static string GetWorkspacePath(string name)
        {
            var dataDirectory = GetDataDirectory();
            var databaseRootPath = Path.Combine(
                dataDirectory,
                AppConstants.Database.DatabaseRoot,
                name);

            try
            {
                Directory.CreateDirectory(databaseRootPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create folder: {databaseRootPath}", ex);
            }

            return databaseRootPath;
        }

        /// <summary>
        /// デフォルトのワークスペース名
        /// </summary>
        public const string DefaultWorkspaceName = "Default";

        /// <summary>
        /// VS Code のインストールパスを取得します
        /// </summary>
        /// <returns>VS Code の実行ファイルパス。見つからない場合は null</returns>
        public static string? GetVSCodeExePath()
        {
            // 検索する候補パス
            var candidatePaths = new[]
            {
                @"C:\Program Files\Microsoft VS Code\Code.exe",
                @"C:\Program Files (x86)\Microsoft VS Code\Code.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Microsoft VS Code\Code.exe"),
                @"C:\Program Files\Microsoft VS Code Insiders\Code - Insiders.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Microsoft VS Code Insiders\Code - Insiders.exe")
            };

            foreach (var path in candidatePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// アイコンテキスト関連の定数
    /// </summary>
    public static class IconTexts
    {
        // x
        // public const string Close = "\uE8BB";
        // ●の中にx
        public const string Close = "\uEB90";
        public const string Copy = "\uE8C8";
        public const string Pin = "\uECCA";
        public const string Pinned = "\uECCB";
        public const string Stack = "\uF156";

    }

    /// <summary>
    /// UI関連の定数
    /// </summary>
    public static class Ui
    {
        /// <summary>
        /// デフォルトのリストタブ名
        /// </summary>
        public const string DefaultListTabName = "List";

        /// <summary>
        /// デフォルトのターミナルタブ名
        /// </summary>
        public const string DefaultTerminalTabName = "Terminal";

        /// <summary>
        /// 情報Pane
        /// </summary>
        public const string InfoPaneName = "Info";

        /// <summary>
        /// タブPane
        /// </summary>
        public const string TabPaneName = "Tab";
    }

    /// <summary>
    /// ショートカットアクション関連の定数
    /// </summary>
    public static class ShortcutActions
    {
        /// <summary>
        /// デフォルトのアイテムを開くアクション
        /// </summary>
        public const string DefaultOpen = "default";

        /// <summary>
        /// アイテムの場所を表示するアクション
        /// </summary>
        public const string Reveal = "reveal";

    }
}
