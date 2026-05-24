using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Listhing.Helpers;
using Listhing.Services;

namespace Listhing.ViewModels;

    /// <summary>
    /// アプリケーションメニューのViewModel
    /// </summary>
    public class AppMenuViewModel : ObservableObject
    {
        private readonly MainWindowViewModel _mainWindowViewModel;

        /// <summary>
        /// メニュー項目のコレクション
        /// </summary>
        public ObservableCollection<Control> MenuItems { get; }

        /// <summary>
        /// モーダルサービス
        /// </summary>
        public ModalService ModalService => _mainWindowViewModel.ModalService;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="mainWindowViewModel">MainWindowViewModel</param>
    public AppMenuViewModel(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;


        MenuItems = new ObservableCollection<Control>
        {
            MenuHelper.CreateMenuItem(
                Strings.MenuPreferences,
                "Ctrl+,",
                new RelayCommand(() => App.ShowSettingsWindow()),
                iconText: "\uE713"
            ),
            new Separator(),
            MenuHelper.CreateMenuItem(
                Strings.MenuExit,
                "Ctrl+Q",
                mainWindowViewModel.ExitCommand,
                iconText: "\uF3B1"
            )
        };
    }

    /// <summary>
    /// メニューが開かれる直前にメニュー項目を更新する
    /// </summary>
    public void UpdateMenuItems()
    {
        
        // メニュー項目を動的に更新
        foreach (var item in MenuItems.OfType<MenuItem>())
        {
            // 保存メニューの有効/無効を切り替え
            if (item.Header?.ToString() == Strings.MenuSave)
            {
                item.IsEnabled = HasUnsavedChanges();
            }
        }
    }

    /// <summary>
    /// 未保存の変更があるかどうかを判定する
    /// </summary>
    /// <returns>未保存の変更がある場合はtrue、それ以外はfalse</returns>
    private bool HasUnsavedChanges()
    {
        // データベースコンテキストの変更をチェック
        return false;
    }
}
