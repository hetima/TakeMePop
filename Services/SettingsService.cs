using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Listhing.Converters;
using Listhing.Helpers;
using Listhing.Models;

namespace Listhing.Services;

/// <summary>
/// 設定管理サービス
/// </summary>
public class SettingsService
{
    private readonly string _appDataPath;
    private readonly string _settingsFilePath;
    private AppSettings _settings = new();

    /// <summary>
    /// 現在のアプリケーション設定
    /// </summary>
    public AppSettings Settings => _settings;

    /// <summary>TransparentGuard 設定が変更されたとき</summary>
    public event EventHandler? TransparentGuardChanged;

    /// <summary>
    /// 設定ファイルのパス
    /// </summary>
    public string SettingsFilePath => _settingsFilePath;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public SettingsService()
    {
        _appDataPath = AppConstants.Settings.GetDataDirectory();
        _settingsFilePath = Path.Combine(_appDataPath, AppConstants.Settings.FileName);
    }

    /// <summary>
    /// 設定をファイルから読み込む
    /// </summary>
    public void Load()
    {
        try
        {
            // 設定フォルダが存在しない場合は作成
            if (!Directory.Exists(_appDataPath))
            {
                Directory.CreateDirectory(_appDataPath);
            }

            // メイン設定ファイルから読み込み
            if (!File.Exists(_settingsFilePath))
            {
                _settings = new AppSettings();
                // 言語が未設定の場合、Windowsの言語設定に基づいて初期値を設定
                SetDefaultLanguageFromSystem();
                Save();
                return;
            }

            // JSONファイルから読み込み
            var json = File.ReadAllText(_settingsFilePath, Encoding.UTF8);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter(), new ShortcutKeyJsonConverter(), new ThemeModeJsonConverter() }
            };
            
            var loadedSettings = JsonSerializer.Deserialize<AppSettings>(json, options);
            if (loadedSettings != null)
            {
                _settings = loadedSettings;
                
                // 言語が未設定の場合、Windowsの言語設定に基づいて設定
                if (_settings.Language == null)
                {
                    SetDefaultLanguageFromSystem();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load settings: {ex.Message}");
            _settings = new AppSettings();
            SetDefaultLanguageFromSystem();
        }
    }

    /// <summary>TransparentGuard 設定の変更を通知する</summary>
    public void NotifyTransparentGuardChanged() => TransparentGuardChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// 設定をファイルに保存する
    /// </summary>
    public void Save()
    {
        try
        {
            // 設定フォルダが存在しない場合は作成
            Directory.CreateDirectory(_appDataPath);

            // JSONファイルに書き込み
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter(), new ShortcutKeyJsonConverter(), new ThemeModeJsonConverter() }
            };
            
            var json = JsonSerializer.Serialize(_settings, options);
            File.WriteAllText(_settingsFilePath, json, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Windowsの言語設定に基づいてデフォルト言語を設定
    /// </summary>
    private void SetDefaultLanguageFromSystem()
    {
        var currentCulture = System.Globalization.CultureInfo.CurrentUICulture;
        _settings.Language = currentCulture.Name.StartsWith("ja", StringComparison.OrdinalIgnoreCase)
            ? AppLanguage.Ja
            : AppLanguage.En;
    }

    /// <summary>
    /// キーイベントがショートカットキーに一致するか判定する
    /// </summary>
    /// <param name="e">キーイベント引数</param>
    /// <returns>マッチしたショートカットの結果</returns>
    public ShortcutMatchResult MatchesShortcut(System.Windows.Input.KeyEventArgs e)
    {
        if (e.SystemKey == System.Windows.Input.Key.LeftAlt || e.SystemKey == System.Windows.Input.Key.RightAlt)
        {
            return ShortcutMatchResult.None;
        }

        var shortcutSettings = _settings.ShortcutSettings;

        // デフォルトのアイテムを開くショートカットキーをチェック
        if (shortcutSettings.DefaultOpenKey.Matches(e))
        {
            return ShortcutMatchResult.DefaultOpen;
        }

        // マッチしなかった場合
        return ShortcutMatchResult.None;
    }

}
