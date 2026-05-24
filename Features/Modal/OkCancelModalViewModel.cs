using System;
using System.Windows.Input;

namespace Listhing.ViewModels;

/// <summary>
/// OK/キャンセルモーダルのViewModel
/// </summary>
public class OkCancelModalViewModel : ModalContentViewModel
{
    private readonly Action? _okCallback;
    private readonly Action? _cancelCallback;
    private string _message;
    private string _okLabel;
    private string _cancelLabel;
    private bool _okOnly;

    /// <summary>
    /// OKボタンのみを表示するかどうか
    /// </summary>
    public bool OkOnly
    {
        get => _okOnly;
        set => SetProperty(ref _okOnly, value);
    }

    /// <summary>
    /// 表示するメッセージ
    /// </summary>
    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    /// <summary>
    /// OKボタンのラベル
    /// </summary>
    public string OkLabel
    {
        get => _okLabel;
        set => SetProperty(ref _okLabel, value);
    }

    /// <summary>
    /// キャンセルボタンのラベル
    /// </summary>
    public string CancelLabel
    {
        get => _cancelLabel;
        set => SetProperty(ref _cancelLabel, value);
    }

    /// <summary>
    /// OKコマンド
    /// </summary>
    public ICommand OkCommand { get; }

    /// <summary>
    /// キャンセルコマンド
    /// </summary>
    public ICommand CancelCommand { get; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="message">表示するメッセージ</param>
    /// <param name="okLabel">OKボタンのラベル（nullの場合はデフォルト値）</param>
    /// <param name="cancelLabel">キャンセルボタンのラベル（nullの場合はデフォルト値）</param>
    /// <param name="okCallback">OKボタンが押されたときのコールバック</param>
    /// <param name="cancelCallback">キャンセルボタンが押されたときのコールバック</param>
    /// <param name="okOnly">OKボタンのみを表示するかどうか</param>
    public OkCancelModalViewModel(
        string message,
        string? okLabel = null,
        string? cancelLabel = null,
        Action? okCallback = null,
        Action? cancelCallback = null,
        bool okOnly = false)
    {
        _message = message ?? "";
        // リソースからデフォルト値を設定
        _okLabel = okLabel ?? Strings.OkButton;
        _cancelLabel = cancelLabel ?? Strings.CancelButton;
        _okCallback = okCallback;
        _cancelCallback = cancelCallback;
        _okOnly = okOnly;

        OkCommand = new RelayCommand(OnOk);
        CancelCommand = new RelayCommand(OnCancel);
    }

    /// <summary>
    /// OKボタンが押されたときの処理
    /// </summary>
    private void OnOk()
    {
        RequestClose = ModalCloseResult.OK;
    }

    /// <summary>
    /// キャンセルボタンが押されたときの処理
    /// </summary>
    private void OnCancel()
    {
        RequestClose = ModalCloseResult.Cancel;
    }

    /// <summary>
    /// モーダルが閉じられたときの処理
    /// </summary>
    /// <param name="result">閉じる理由</param>
    public override void ModalClosed(ModalCloseResult result)
    {
        switch (result)
        {
            case ModalCloseResult.OK:
                _okCallback?.Invoke();
                break;
            case ModalCloseResult.Cancel:
                _cancelCallback?.Invoke();
                break;
        }
    }
}
