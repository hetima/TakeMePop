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
    /// Transparent Guard セクション
    /// </summary>
    TransparentGuard = 3,

    /// <summary>
    /// その他セクション
    /// </summary>
    Other = 4
}

/// <summary>
/// SettingsView.xaml の相互作用ロジック
/// </summary>
public partial class SettingsView : UserControl
{
    /// <summary>
    /// コンストラクタ
    /// </summary>
    public SettingsView()
    {
        InitializeComponent();
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
    /// セクション選択ListBoxの選択変更イベント
    /// 選択されたセクションに応じて表示を切り替える
    /// </summary>
    private void SectionListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // コントロール初期化前はnullの場合がある
        if (GeneralSection == null || ActionSection == null || OtherSection == null || TransparentGuardSection == null || PopWindowSection == null)
            return;

        var selectedSection = (SettingsSection)SectionListBox.SelectedIndex;

        // 全てのセクションを非表示にする
        GeneralSection.Visibility = Visibility.Collapsed;
        ActionSection.Visibility = Visibility.Collapsed;
        OtherSection.Visibility = Visibility.Collapsed;
        TransparentGuardSection.Visibility = Visibility.Collapsed;
        PopWindowSection.Visibility = Visibility.Collapsed;

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
                
                // ListViewの表示を更新
                var viewModel = DataContext as ViewModels.SettingsViewModel;
                viewModel?.RefreshShortcutKeys();
            }
        });
    }

    /// <summary>
    /// アイテムの場所を表示するショートカットキー編集ボタンクリックイベント
    /// </summary>
    private void EditRevealKey_Click(object sender, RoutedEventArgs e)
    {
        ShowEditShortcutKeyModal(Listhing.Strings.RevealActionLabel, App.SettingsService.Settings.ShortcutSettings.RevealKey, (shortcut) =>
        {
            if (shortcut != null)
            {
                App.SettingsService.Settings.ShortcutSettings.RevealKey = shortcut;
                App.SettingsService.Save();
                
                // ListViewの表示を更新
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
