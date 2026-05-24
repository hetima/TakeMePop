using System.Windows;
using Listhing.Services;
using Listhing.ViewModels;

namespace Listhing;

/// <summary>
/// SettingsWindow.xaml の相互作用ロジック
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly SettingsWindowViewModel _viewModel;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public SettingsWindow()
    {
        InitializeComponent();
        
        // ViewModelの初期化
        _viewModel = new SettingsWindowViewModel();
        DataContext = _viewModel;

        // ModalServiceのイベントハンドラーを設定
        _viewModel.ModalService.ModalOpened += (s, e) =>
        {
            if (Dispatcher.CheckAccess())
            {
                _viewModel.IsModalVisible = true;
            }
            else
            {
                Dispatcher.Invoke(() => _viewModel.IsModalVisible = true);
            }
        };
        _viewModel.ModalService.ModalClosed += (s, e) =>
        {
            if (Dispatcher.CheckAccess())
            {
                _viewModel.IsModalVisible = false;
            }
            else
            {
                Dispatcher.Invoke(() => _viewModel.IsModalVisible = false);
            }
        };
    }

    /// <summary>
    /// ModalService
    /// </summary>
    public Services.ModalService ModalService => _viewModel.ModalService;

    /// <summary>
    /// ウィンドウが閉じられるときに設定を保存する
    /// </summary>
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // 設定を保存
        App.SettingsService.Save();
    }
}
