using System.Windows.Controls;
using Listhing.Services;
using Listhing.ViewModels;
using Listhing.Views;

namespace Listhing.ViewModels;

/// <summary>
/// SettingsWindowのViewModel
/// </summary>
public class SettingsWindowViewModel : ObservableObject, IModalOwner
{
    private readonly SettingsViewModel _settingsViewModel;
    private readonly ModalService _modalService;
    private bool _isModalVisible;
    private UserControl? _currentModalContent;

    /// <summary>
    /// SettingsViewModel
    /// </summary>
    public SettingsViewModel SettingsViewModel => _settingsViewModel;

    /// <summary>
    /// ModalService
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
    /// コンストラクタ
    /// </summary>
    public SettingsWindowViewModel()
    {
        // ModalServiceを作成（先に作成する必要がある）
        _modalService = new ModalService(this);
        
        // SettingsViewModelを作成
        _settingsViewModel = new SettingsViewModel(App.SettingsService, _modalService);
    }
}
