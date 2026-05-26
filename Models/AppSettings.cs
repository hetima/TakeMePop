 using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Listhing.Helpers;
using System.Windows;

namespace Listhing.Models;

/// <summary>
/// アプリケーション設定を表すクラス
/// </summary>
public class AppSettings
{
    /// <summary>
    /// テーマ設定（Auto, Light, Dark）
    /// </summary>
    [JsonPropertyName("theme")]
    public ThemeMode Theme { get; set; } = ThemeMode.System;

    /// <summary>
    /// 言語設定（En, Ja）
    /// nullの場合はWindowsの言語設定に基づいて自動判定
    /// </summary>
    [JsonPropertyName("language")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AppLanguage? Language { get; set; } = null;

    /// <summary>
    /// フォントサイズ（8-48）
    /// </summary>
    [JsonPropertyName("fontSize")]
    public double FontSize { get; set; } = 14;

    /// <summary>
    /// ショートカットキーの設定
    /// </summary>
    [JsonPropertyName("shortcutSettings")]
    public ShortcutSetting ShortcutSettings { get; set; } = new ShortcutSetting();

    /// <summary>
    /// 開いているワークスペース名のリスト
    /// </summary>
    [JsonPropertyName("openedWorkspaceNames")]
    public List<string> OpenedWorkspaceNames { get; set; } = new List<string>();
}

/// <summary>
/// ウィンドウの設定
/// </summary>
public class WindowSettings
{
    /// <summary>
    /// ウィンドウの幅
    /// </summary>
    [JsonPropertyName("width")]
    public double Width { get; set; } = 800;

    /// <summary>
    /// ウィンドウの高さ
    /// </summary>
    [JsonPropertyName("height")]
    public double Height { get; set; } = 600;

    /// <summary>
    /// ウィンドウの左位置
    /// </summary>
    [JsonPropertyName("left")]
    public double Left { get; set; } = 100;

    /// <summary>
    /// ウィンドウの上位置
    /// </summary>
    [JsonPropertyName("top")]
    public double Top { get; set; } = 100;

    /// <summary>
    /// ウィンドウが最大化されているかどうか
    /// </summary>
    [JsonPropertyName("isMaximized")]
    public bool IsMaximized { get; set; } = false;

    //以下独自設定


}


/// <summary>
/// 言語の種類
/// </summary>
public enum AppLanguage
{
    /// <summary>
    /// 英語
    /// </summary>
    En,

    /// <summary>
    /// 日本語
    /// </summary>
    Ja
}

/// <summary>
/// ショートカットマッチ結果
/// </summary>
public class ShortcutMatchResult
{
    /// <summary>
    /// マッチしたアクションの種類
    /// </summary>
    public string? Action { get; }

    /// <summary>
    /// 追加のパラメータ（例: アプリケーションパス）
    /// </summary>
    public string? Parameter { get; }

    /// <summary>
    /// マッチしたかどうか
    /// </summary>
    public bool IsMatch => Action != null;

    private ShortcutMatchResult(string? action, string? parameter = null)
    {
        Action = action;
        Parameter = parameter;
    }

    /// <summary>
    /// マッチしなかった場合
    /// </summary>
    public static ShortcutMatchResult None => new ShortcutMatchResult(null);

    /// <summary>
    /// デフォルトのアイテムを開くアクション
    /// </summary>
    public static ShortcutMatchResult DefaultOpen => new ShortcutMatchResult(AppConstants.ShortcutActions.DefaultOpen);

    /// <summary>
    /// アイテムの場所を表示するアクション
    /// </summary>
    public static ShortcutMatchResult Reveal => new ShortcutMatchResult(AppConstants.ShortcutActions.Reveal);
}

/// <summary>
/// ショートカットキーの設定
/// </summary>
public class ShortcutSetting
{
    /// <summary>
    /// デフォルトのアイテムを開くショートカットキー（デフォルト: Enter）
    /// </summary>
    [JsonPropertyName("defaultOpenKey")]
    public ShortcutKey DefaultOpenKey { get; set; } = new ShortcutKey(System.Windows.Input.Key.Enter);

    /// <summary>
    /// アイテムの場所を表示するショートカットキー（デフォルト: Ctrl+Enter）
    /// </summary>
    [JsonPropertyName("revealKey")]
    public ShortcutKey RevealKey { get; set; } = new ShortcutKey(System.Windows.Input.Key.Enter, System.Windows.Input.ModifierKeys.Control);


}


