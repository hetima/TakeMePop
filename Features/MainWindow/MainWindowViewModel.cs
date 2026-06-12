using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Controls;
using System.Windows.Input;
using Listhing.Services;


namespace Listhing.ViewModels;

/// <summary>
/// MainWindowのViewModel
/// </summary>
public class MainWindowViewModel : ObservableObject, IModalOwner
{
    private readonly ModalService _modalService;
    private bool _isModalVisible;
    private UserControl? _currentModalContent;
    private readonly AppMenuViewModel _appMenuViewModel;

    private bool _isRestoring = false;

    /// <summary>
    /// 保存コマンド
    /// </summary>
    public ICommand SaveCommand { get; private set; }

    /// <summary>
    /// 終了コマンド
    /// </summary>
    public ICommand ExitCommand { get; private set; }

    /// <summary>
    /// モーダルサービス
    /// </summary>
    public ModalService ModalService => _modalService;

    /// <summary>
    /// モーダルが表示されているかどうか
    /// </summary>
    public bool IsModalVisible
    {
        get => _isModalVisible;
        set => SetProperty(ref _isModalVisible, value);
    }

    /// <summary>
    /// 現在表示中のモーダルコンテンツ
    /// </summary>
    public UserControl? CurrentModalContent
    {
        get => _currentModalContent;
        set => SetProperty(ref _currentModalContent, value);
    }


    /// <summary>
    /// アプリケーションメニュービューモデル
    /// </summary>
    public AppMenuViewModel AppMenuViewModel => _appMenuViewModel;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="tabService">タブサービス</param>
    /// <param name="settingsService">設定サービス</param>
    public MainWindowViewModel(MainWindow win)
    {

        // WindowSettingsの読み込み
        LoadWindowSettings();

        // ModalServiceをWindow単位で作成
        _modalService = new ModalService(this);
        _modalService.ModalOpened += (s, e) =>
        {
            if (Application.Current.Dispatcher.CheckAccess())
            {
                IsModalVisible = true;
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() => IsModalVisible = true);
            }
        };
        _modalService.ModalClosed += (s, e) =>
        {
            if (Application.Current.Dispatcher.CheckAccess())
            {
                IsModalVisible = false;
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() => IsModalVisible = false);
            }
        };

        // メニューコマンドの初期化
        SaveCommand = new RelayCommand(OnSave);
        ExitCommand = new RelayCommand(OnExit);

        // AppMenuViewModelの初期化
        _appMenuViewModel = new AppMenuViewModel(this);

    }


    /// <summary>
    /// 保存コマンドの実行
    /// </summary>
    private void OnSave()
    {
        App.SaveAll();
    }

    /// <summary>
    /// 終了コマンドの実行
    /// </summary>
    private void OnExit()
    {
        Application.Current.Shutdown();
    }


    /// <summary>
    /// WindowSettingsをファイルから読み込む
    /// </summary>
    private void LoadWindowSettings()
    {

    }

    /// <summary>
    /// WindowSettingsをファイルに保存する
    /// </summary>
    private void SaveWindowSettingsToFile()
    {

    }

    /// <summary>
    /// ウィンドウの状態を復元
    /// </summary>
    public void RestoreWindowSettings(MainWindow win)
    {
        _isRestoring = true;

        _isRestoring = false;
    }

    /// <summary>
    /// ウィンドウの状態を保存
    /// </summary>
    public void SaveWindowSettings(MainWindow win)
    {
        if (_isRestoring)
        {
            return;
        }
    }

}
