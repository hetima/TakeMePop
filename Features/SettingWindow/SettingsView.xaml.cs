using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Listhing.Helpers;
using Listhing.Models;
using Listhing.Services;
using Listhing.ViewModels;

namespace Listhing.Views;

/// <summary>
/// 設定画面のセクションを表す列挙型
/// </summary>
public enum SettingsSection
{
    /// <summary>
    /// 全般セクション
    /// </summary>
    General = 0,

    /// <summary>
    /// アクションセクション
    /// </summary>
    Action = 1,

    /// <summary>
    /// Pop Window セクション
    /// </summary>
    PopWindow = 2,

    /// <summary>
    /// Quick Text Edit セクション
    /// </summary>
    QuickText = 3,

    /// <summary>
    /// クリップボード履歴セクション
    /// </summary>
    QuickHistory = 4,

    /// <summary>
    /// Transparent Guard セクション
    /// </summary>
    TransparentGuard = 5,

    /// <summary>
    /// その他セクション
    /// </summary>
    Other = 6
}

/// <summary>
/// SettingsView.xaml の相互作用ロジック
/// </summary>
public partial class SettingsView : UserControl
{
    private static readonly int[] ClipboardHistoryLimitValues = [10, 20, 50, 100];

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public SettingsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (DataContext is ViewModels.SettingsViewModel vm)
            SyncClipboardHistoryLimitCombo(vm.ClipboardHistoryLimit);
    }

    /// <summary>
    /// 設定値に対応するコンボボックスのインデックスを選択する
    /// </summary>
    private void SyncClipboardHistoryLimitCombo(int limit)
    {
        var idx = Array.IndexOf(ClipboardHistoryLimitValues, limit);
        ClipboardHistoryLimitCombo.SelectedIndex = idx >= 0 ? idx : 0; // デフォルト10
    }

    /// <summary>
    /// クリップボード履歴上限コンボボックスの選択変更イベント
    /// </summary>
    private void ClipboardHistoryLimitCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ClipboardHistoryLimitCombo.SelectedIndex < 0) return;
        if (DataContext is not ViewModels.SettingsViewModel vm) return;
        vm.ClipboardHistoryLimit = ClipboardHistoryLimitValues[ClipboardHistoryLimitCombo.SelectedIndex];
    }

    /// <summary>
    /// Hyperlinkを開く
    /// </summary>
    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        var url = e.Uri.ToString();
        if (!string.IsNullOrWhiteSpace(url))
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };

            Process.Start(processStartInfo);
        }
    }

    /// <summary>
    /// 設定フォルダを開くボタンクリックイベント
    /// </summary>
    private void OpenSettingsFolder_Click(object sender, RoutedEventArgs e)
    {
        // アプリケーションの設定フォルダを開く
        var settingsFolder = AppConstants.Settings.GetDataDirectory();
        
        if (!FileSystemHelper.OpenFolderInExplorer(settingsFolder))
        {
            System.Windows.MessageBox.Show(
                "Failed to open settings folder",
                "Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// クリップボードの内容を調査してテキストエリアに表示する
    /// </summary>
    private void InspectClipboard_Click(object sender, RoutedEventArgs e)
    {
        ClipboardInspectBox.Text = InspectDataObject(System.Windows.Clipboard.GetDataObject());
    }

    /// <summary>
    /// テキストエリアへのドラッグオーバー。Drop を発火させるため受け入れ可（Copy）にしておく
    /// </summary>
    private void ClipboardInspectBox_PreviewDragOver(object sender, System.Windows.DragEventArgs e)
    {
        // None にすると Drop 自体が発火しないため、ここでは受け入れ可にする
        e.Effects = System.Windows.DragDropEffects.Copy;
        e.Handled = true;
    }

    /// <summary>
    /// テキストエリアへのドロップ。ドロップされたデータの中身を調査して表示しつつ、ドロップ操作自体は不成立にする
    /// </summary>
    private void ClipboardInspectBox_PreviewDrop(object sender, System.Windows.DragEventArgs e)
    {
        ClipboardInspectBox.Text = InspectDataObject(e.Data);
        e.Effects = System.Windows.DragDropEffects.None; // ドロップ元には「コピーされなかった」と通知
        e.Handled = true; // 既定のテキスト挿入を抑止
    }

    /// <summary>
    /// IDataObject の格納フォーマット一覧と各データの概要を文字列化する
    /// </summary>
    private static string InspectDataObject(System.Windows.IDataObject? data)
    {
        var sb = new System.Text.StringBuilder();
        try
        {
            if (data == null)
            {
                return "(empty)";
            }

            var formats = data.GetFormats();
            sb.AppendLine($"Formats ({formats.Length}): {string.Join(", ", formats)}");
            sb.AppendLine(new string('-', 40));

            foreach (var format in formats)
            {
                object? value;
                try
                {
                    value = data.GetData(format);
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"[{format}] <取得エラー: {ex.GetType().Name}>");
                    continue;
                }

                sb.AppendLine($"[{format}] {DescribeValue(value)}");
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine($"<error: {ex.GetType().Name}: {ex.Message}>");
        }
        return sb.ToString();
    }

    /// <summary>
    /// クリップボードデータの型・サイズ・先頭プレビューを1行（必要に応じ複数行）に整形する
    /// </summary>
    private static string DescribeValue(object? value)
    {
        if (value == null)
            return "<null>";

        switch (value)
        {
            case string s:
                return $"(string, {s.Length} chars)\n{Preview(s)}";

            case string[] arr:
                var items = string.Join("\n", arr);
                return $"(string[], {arr.Length} items)\n{items}";

            case System.Collections.Specialized.StringCollection sc:
                var paths = string.Join("\n", sc.Cast<string>());
                return $"(StringCollection, {sc.Count} items)\n{paths}";

            case System.IO.MemoryStream ms:
                return $"(MemoryStream, {ms.Length} bytes)";

            case System.Windows.Interop.InteropBitmap bmp:
                return $"(Bitmap, {bmp.PixelWidth} x {bmp.PixelHeight})";

            case System.Windows.Media.Imaging.BitmapSource src:
                return $"({value.GetType().Name}, {src.PixelWidth} x {src.PixelHeight})";

            default:
                return $"({value.GetType().FullName})";
        }
    }

    /// <summary>
    /// 文字列を最大200文字でトリミングしたプレビューを返す
    /// </summary>
    private static string Preview(string s)
    {
        const int max = 200;
        return s.Length <= max ? s : s.Substring(0, max) + "…";
    }

    /// <summary>
    /// セクション選択ListBoxの選択変更イベント
    /// 選択されたセクションに応じて表示を切り替える
    /// </summary>
    private void SectionListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // コントロール初期化前はnullの場合がある
        if (GeneralSection == null || ActionSection == null || OtherSection == null || TransparentGuardSection == null || PopWindowSection == null || QuickHistorySection == null || QuickTextSection == null)
            return;

        var selectedSection = (SettingsSection)SectionListBox.SelectedIndex;

        // 全てのセクションを非表示にする
        GeneralSection.Visibility = Visibility.Collapsed;
        ActionSection.Visibility = Visibility.Collapsed;
        OtherSection.Visibility = Visibility.Collapsed;
        TransparentGuardSection.Visibility = Visibility.Collapsed;
        PopWindowSection.Visibility = Visibility.Collapsed;
        QuickHistorySection.Visibility = Visibility.Collapsed;
        QuickTextSection.Visibility = Visibility.Collapsed;

        // 選択されたセクションのみを表示する
        switch (selectedSection)
        {
            case SettingsSection.General:
                GeneralSection.Visibility = Visibility.Visible;
                break;
            case SettingsSection.Action:
                ActionSection.Visibility = Visibility.Visible;
                break;
            case SettingsSection.Other:
                OtherSection.Visibility = Visibility.Visible;
                break;
            case SettingsSection.TransparentGuard:
                TransparentGuardSection.Visibility = Visibility.Visible;
                break;
            case SettingsSection.PopWindow:
                PopWindowSection.Visibility = Visibility.Visible;
                break;
            case SettingsSection.QuickText:
                QuickTextSection.Visibility = Visibility.Visible;
                break;
            case SettingsSection.QuickHistory:
                QuickHistorySection.Visibility = Visibility.Visible;
                break;
            default:
                // 未知のセクションの場合は最初のセクションを表示
                GeneralSection.Visibility = Visibility.Visible;
                break;
        }
    }

    /// <summary>
    /// デフォルトのアイテムを開くショートカットキー編集ボタンクリックイベント
    /// </summary>
    private void EditDefaultOpenKey_Click(object sender, RoutedEventArgs e)
    {
        ShowEditShortcutKeyModal(Listhing.Strings.DefaultOpenActionLabel, App.SettingsService.Settings.ShortcutSettings.DefaultOpenKey, (shortcut) =>
        {
            if (shortcut != null)
            {
                App.SettingsService.Settings.ShortcutSettings.DefaultOpenKey = shortcut;
                App.SettingsService.Save();
                App.ApplyHotkeySettings();

                // ListViewの表示を更新
                var viewModel = DataContext as ViewModels.SettingsViewModel;
                viewModel?.RefreshShortcutKeys();
            }
        });
    }

    /// <summary>
    /// Quick Text Edit ショートカットキー編集ボタンクリックイベント
    /// </summary>
    private void EditQuickTextKey_Click(object sender, RoutedEventArgs e)
    {
        ShowEditShortcutKeyModal(Listhing.Strings.QuickTextActionLabel, App.SettingsService.Settings.ShortcutSettings.QuickTextKey, (shortcut) =>
        {
            if (shortcut != null)
            {
                App.SettingsService.Settings.ShortcutSettings.QuickTextKey = shortcut;
                App.SettingsService.Save();
                App.ApplyHotkeySettings();

                var viewModel = DataContext as ViewModels.SettingsViewModel;
                viewModel?.RefreshShortcutKeys();
            }
        });
    }

    /// <summary>
    /// Quick History ショートカットキー編集ボタンクリックイベント
    /// </summary>
    private void EditQuickHistoryKey_Click(object sender, RoutedEventArgs e)
    {
        ShowEditShortcutKeyModal(Listhing.Strings.QuickHistoryActionLabel, App.SettingsService.Settings.ShortcutSettings.QuickHistoryKey, (shortcut) =>
        {
            if (shortcut != null)
            {
                App.SettingsService.Settings.ShortcutSettings.QuickHistoryKey = shortcut;
                App.SettingsService.Save();
                App.ApplyHotkeySettings();

                var viewModel = DataContext as ViewModels.SettingsViewModel;
                viewModel?.RefreshShortcutKeys();
            }
        });
    }

    /// <summary>
    /// ショートカットキー編集モーダルを表示する
    /// </summary>
    /// <param name="actionName">アクション名</param>
    /// <param name="currentShortcut">現在のショートカットキー</param>
    /// <param name="onShortcutChanged">ショートカット変更時のコールバック</param>
    private void ShowEditShortcutKeyModal(string actionName, ShortcutKey currentShortcut, Action<ShortcutKey?> onShortcutChanged)
    {
        // SettingsWindowのIModalOwnerを取得
        if (Window.GetWindow(this) is not SettingsWindow settingsWindow)
            return;

        // ModalServiceを取得
        var modalService = settingsWindow.ModalService;

        // ViewModelを作成
        var editViewModel = new EditShortcutKeyViewModel(
            App.SettingsService,
            actionName,
            currentShortcut,
            onShortcutChanged,
            () =>
            {
                // ショートカットが削除されたら表示を更新
                var settingsViewModel = DataContext as ViewModels.SettingsViewModel;
                settingsViewModel?.RefreshShortcutKeys();
            });

        // モーダルを表示（View は拡張メソッドで自動作成）
        //ModalServiceExtensions.ShowWithViewModel<EditShortcutKeyView, EditShortcutKeyViewModel>(this, editViewModel);
        modalService.ShowWithViewModel<EditShortcutKeyView, EditShortcutKeyViewModel>(editViewModel);
    }
}
