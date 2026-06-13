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
    /// クリップボード履歴の最大保持件数。この件数に達したら古い方から半分削除する。
    /// </summary>
    [JsonPropertyName("clipboardHistoryLimit")]
    public int ClipboardHistoryLimit { get; set; } = 10;

    /// <summary>
    /// ショートカットキーの設定
    /// </summary>
    [JsonPropertyName("shortcutSettings")]
    public ShortcutSetting ShortcutSettings { get; set; } = new ShortcutSetting();

    /// <summary>
    /// Ctrl+C 2回押しでの発動を有効にするかどうか。
    /// </summary>
    [JsonPropertyName("ctrlCDoubleTapEnabled")]
    public bool CtrlCDoubleTapEnabled { get; set; } = true;

    /// <summary>
    /// Ctrl+C 2回押しの判定間隔（ミリ秒）。300〜1000。
    /// </summary>
    [JsonPropertyName("ctrlCDoublePressIntervalMs")]
    public int CtrlCDoublePressIntervalMs { get; set; } = 500;

    /// <summary>
    /// 開いているワークスペース名のリスト
    /// </summary>
    [JsonPropertyName("openedWorkspaceNames")]
    public List<string> OpenedWorkspaceNames { get; set; } = new List<string>();

    [JsonPropertyName("transparentGuard")]
    public TransparentGuardSettings TransparentGuard { get; set; } = new TransparentGuardSettings();

    [JsonPropertyName("popWindow")]
    public PopWindowSettings PopWindow { get; set; } = new PopWindowSettings();

    [JsonPropertyName("quickTextWindow")]
    public QuickTextWindowSettings QuickTextWindow { get; set; } = new QuickTextWindowSettings();

    [JsonPropertyName("quickHistory")]
    public QuickHistorySettings QuickHistory { get; set; } = new QuickHistorySettings();

    /// <summary>
    /// QuickTextWindow・QuickHistoryWindow で使うフォント名。空文字列はシステムデフォルト。
    /// </summary>
    [JsonPropertyName("popupFontFamily")]
    public string PopupFontFamily { get; set; } = "";

    /// <summary>
    /// Windows スタートアップ時に自動実行するかどうか。
    /// </summary>
    [JsonPropertyName("runAtStartup")]
    public bool RunAtStartup { get; set; } = false;

    /// <summary>
    /// 最後に起動したときの実行ファイルパス。スタートアップ登録パスの更新判定に使う。
    /// </summary>
    [JsonPropertyName("lastLaunchedPath")]
    public string? LastLaunchedPath { get; set; } = null;
}

public class QuickHistorySettings
{
    /// <summary>
    /// モデファイキーセレクト。ON のとき、修飾キーを押している間に対象キーで選択移動し、
    /// 修飾キーを離すとペーストする。OFF のときはホットキーでトグル表示するだけ。
    /// </summary>
    [JsonPropertyName("modifierSelect")]
    public bool ModifierSelect { get; set; } = true;

    /// <summary>
    /// モデファイキーセレクト中、対象キー以外を押したら動作をキャンセルしてパネルを閉じる。
    /// </summary>
    [JsonPropertyName("cancelOnOtherKey")]
    public bool CancelOnOtherKey { get; set; } = true;

    /// <summary>履歴リストの表示件数（4〜10）。</summary>
    [JsonPropertyName("displayCount")]
    public int DisplayCount { get; set; } = 5;
}

public class QuickTextWindowSettings
{
    [JsonPropertyName("width")]
    public double Width { get; set; } = 260;

    [JsonPropertyName("height")]
    public double Height { get; set; } = 200;

    /// <summary>閉じる時にテキストの内容をクリップボードへコピーする。</summary>
    [JsonPropertyName("autoCopyOnClose")]
    public bool AutoCopyOnClose { get; set; } = true;

    /// <summary>ESC キーでウィンドウを閉じる。</summary>
    [JsonPropertyName("closeOnEsc")]
    public bool CloseOnEsc { get; set; } = true;
}

public class PopWindowSettings
{
    [JsonPropertyName("width")]
    public double Width { get; set; } = 120;

    [JsonPropertyName("height")]
    public double Height { get; set; } = 120;

    [JsonPropertyName("linkDropFormat")]
    public LinkDropFormat LinkDropFormat { get; set; } = LinkDropFormat.TitleAndUrl;
}

/// <summary>URLドロップ時のテキスト形式</summary>
public enum LinkDropFormat
{
    TitleAndUrl,
    Markdown,
    UrlOnly,
}

public class TransparentGuardSettings
{
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    [JsonPropertyName("size")]
    public double Size { get; set; } = 120;

    [JsonPropertyName("activationPixels")]
    public int ActivationPixels { get; set; } = 10;

    [JsonPropertyName("dismissDelayMs")]
    public int DismissDelayMs { get; set; } = 300;

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
    public ShortcutKey DefaultOpenKey { get; set; } = new ShortcutKey(System.Windows.Input.Key.None);

    /// <summary>
    /// クイック履歴ウィンドウを表示するショートカットキー
    /// </summary>
    [JsonPropertyName("quickHistoryKey")]
    public ShortcutKey QuickHistoryKey { get; set; } = new ShortcutKey(System.Windows.Input.Key.None);

}


