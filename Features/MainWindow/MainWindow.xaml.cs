using Listhing.Helpers;
using Listhing.Models;
using Listhing.Services;
using Listhing.ViewModels;
using Listhing.Views;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Listhing;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly SettingsService _settingsService;

    /// <summary>
    /// モーダルサービス
    /// </summary>
    public ModalService ModalService => _viewModel.ModalService;

    /// <summary>
    /// Constructor
    /// </summary>
    public MainWindow(string? title = null)
    {
        InitializeComponent();

        // サービスの初期化
        _settingsService = App.SettingsService;

        // ViewModelの初期化
        _viewModel = new MainWindowViewModel(this);
        DataContext = _viewModel;

        _viewModel.RestoreWindowSettings(this);


        WindowTitleTextBlock.Text = title;
    }

    /// <summary>
    /// デフォルトのタブコンテンツを作成するファクトリメソッド
    /// </summary>
    // private ITabContent CreateDefaultTabContent()
    // {
    //     return new ListTabViewModel();
    // }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    /// <summary>
    /// Menu button click handler - shows context menu
    /// </summary>
    private void MenuButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.ContextMenu is ContextMenu menu)
        {
            menu.PlacementTarget = button;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }
    }

    /// <summary>
    /// AppMenuが開かれる直前にメニュー項目を更新する
    /// </summary>
    private void AppMenu_Opened(object sender, RoutedEventArgs e)
    {
        _viewModel.AppMenuViewModel.UpdateMenuItems();
    }

    /// <summary>
    /// Settings button click handler
    /// </summary>
    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        // 設定画面を表示
        App.ShowSettingsWindow();
    }

    /// <summary>
    /// キーボードショートカットのハンドラ
    /// </summary>
    /// 
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // ESC: モーダルが表示されている場合は閉じる
        if (e.Key == Key.Escape && Keyboard.Modifiers == ModifierKeys.None)
        {
            // モーダルが表示されている場合は閉じる
            if (_viewModel.IsModalVisible)
            {
                _viewModel.ModalService.CloseModal(ModalCloseResult.Cancel);
                e.Handled = true;
            }
            return;
        }

        // Ctrl+Q: アプリケーションを終了
        if (e.Key == Key.Q && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _viewModel.ExitCommand.Execute(null);
            e.Handled = true;
            return;
        }


        if (_viewModel.IsModalVisible)
        {
            return;
        }

        // Alt+矢印キーのチェック
        if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
        {
            // Altキーが押されている場合、e.KeyはKey.Systemになり、実際のキーはe.SystemKeyに格納される
            var eventKey = e.Key == Key.System ? e.SystemKey : e.Key;
            if (eventKey != Key.Up && eventKey != Key.Down && eventKey != Key.Left && eventKey != Key.Right)
            {
                return;
            }

            switch (eventKey)
            {
                case Key.Up:
                    break;
                case Key.Down:
                    break;
                case Key.Left:
                    break;
                case Key.Right:
                    break;
            }
        }

    }

    /// <summary>
    /// キーボードショートカットのハンドラ
    /// </summary>
    /// 
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        // Ctrl+S: 保存
        if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _viewModel.SaveCommand.Execute(null);
            e.Handled = true;
        }

        if (_viewModel.IsModalVisible)
        {
            return;
        }

        // Ctrl+W: 閉じる
        if (e.Key == Key.W && Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;
        }
        // Ctrl+,: 設定画面を表示
        else if (e.Key == Key.OemComma && Keyboard.Modifiers == ModifierKeys.Control)
        {
            App.ShowSettingsWindow();
            e.Handled = true;
        }
        // Ctrl+Z: Undo/Redo（トグル動作）
        else if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
        {
            // e.Handled = true;
        }
        // ショートカットキーのチェック
        else
        {
            var shortcutResult = App.SettingsService.MatchesShortcut(e);
            if (shortcutResult.IsMatch)
            {
                e.Handled = HandleShortcutAction(shortcutResult);
            }
        }
    }

    /// <summary>
    /// ウィンドウが閉じられるときに設定を保存
    /// </summary>
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        App.SaveOpenedWorkspaces();
        _viewModel.SaveWindowSettings(this);
        base.OnClosing(e);
    }



    /// <summary>
    /// カスタムリサイズグリップのドラッグイベントハンドラ
    /// </summary>
    public void ResizeGrip_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
    {
        // ウィンドウのサイズを変更
        double newWidth = Width + e.HorizontalChange;
        double newHeight = Height + e.VerticalChange;

        // 最小サイズを制限
        newWidth = Math.Max(newWidth, MinWidth);
        newHeight = Math.Max(newHeight, MinHeight);

        Width = newWidth;
        Height = newHeight;
    }

    /// <summary>
    /// ウィンドウのコンテンツがレンダリングされた後のイベントハンドラ
    /// </summary>
    private void OnContentRendered(object sender, EventArgs e)
    {
        // コンテンツレンダリング後の処理
    }


    /// <summary>
    /// ショートカットアクションを処理する
    /// </summary>
    /// <param name="result">ショートカットマッチ結果</param>
    /// <param name="item">対象のアイテム</param>
    /// <returns>実行したかどうかの真偽値</returns>
    private bool HandleShortcutAction(ShortcutMatchResult result)
    {
        bool handled = false;
        switch (result.Action)
        {
            case AppConstants.ShortcutActions.DefaultOpen:
                handled = true;

                break;

            case AppConstants.ShortcutActions.Reveal:
                handled = true;

                break;


        }
        return handled;
    }
}
