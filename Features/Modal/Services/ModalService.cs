using System;
using System.Windows.Controls;
using System.Windows.Input;
using Listhing.ViewModels;

namespace Listhing.Services;

/// <summary>
/// モーダルダイアログサービス
/// </summary>
public class ModalService
{
    private readonly IModalOwner _owner;
    private readonly RelayCommand _closeModalCommand;
    private IModalContent? _currentModalContent;
    private ObservableObject? _currentObservable;

    /// <summary>
    /// モーダルが開いているかどうか
    /// </summary>
    public bool IsModalOpen { get; private set; }

    /// <summary>
    /// モーダルを閉じるコマンド
    /// </summary>
    public ICommand CloseModalCommand => _closeModalCommand;

    /// <summary>
    /// モーダルが開かれたときに発生するイベント
    /// </summary>
    public event EventHandler? ModalOpened;

    /// <summary>
    /// モーダルが閉じられたときに発生するイベント
    /// </summary>
    public event EventHandler? ModalClosed;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="owner">モーダルの所有者</param>
    public ModalService(IModalOwner owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _closeModalCommand = new RelayCommand(() => CloseModal(ModalCloseResult.Cancel));
    }

    /// <summary>
    /// モーダルを表示する（Viewのみ指定）
    /// </summary>
    public void Show(UserControl view)
    {
        // 既にモーダルが開いている場合は閉じる
        if (_currentModalContent != null)
        {
            CloseModal(ModalCloseResult.Cancel);
        }

        _owner.CurrentModalContent = view ?? throw new ArgumentNullException(nameof(view));

        // ViewModel が IModalContent を実装している場合は監視を開始
        if (view.DataContext is IModalContent modalContent)
        {
            _currentModalContent = modalContent;

            // INotifyPropertyChanged を実装している場合は変更を監視
            if (modalContent is ObservableObject observable)
            {
                _currentObservable = observable;
                observable.PropertyChanged += OnPropertyChanged;
            }
        }

        IsModalOpen = true;
        ModalOpened?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// PropertyChanged イベントハンドラ
    /// </summary>
    private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IModalContent.RequestClose) && _currentModalContent != null)
        {
            var result = _currentModalContent.RequestClose;
            if (result != ModalCloseResult.None)
            {
                CloseModal(result);
            }
        }
    }

    /// <summary>
    /// モーダルを閉じる
    /// </summary>
    /// <param name="result">閉じる理由（デフォルトはCancel）</param>
    public void CloseModal(ModalCloseResult result = ModalCloseResult.Cancel)
    {
        if (!IsModalOpen)
            return;

        if (_currentModalContent != null)
        {
            // ViewModel に閉じる通知を送る
            _currentModalContent.ModalClosed(result);
            _currentModalContent.RequestClose = ModalCloseResult.None;
        }

        // PropertyChanged イベントを解除
        if (_currentObservable != null)
        {
            _currentObservable.PropertyChanged -= OnPropertyChanged;
            _currentObservable = null;
        }

        IsModalOpen = false;
        _owner.CurrentModalContent = null;
        _currentModalContent = null;
        ModalClosed?.Invoke(this, EventArgs.Empty);
    }
}
