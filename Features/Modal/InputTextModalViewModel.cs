using System;
using System.Windows.Input;

namespace Listhing.ViewModels;

/// <summary>
/// テキスト入力モーダルのViewModel
/// </summary>
public class InputTextModalViewModel : ModalContentViewModel
{
    private readonly Action<string>? _okCallback;
    private readonly Action? _cancelCallback;
    private string _message;
    private string _inputText;
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
    /// 入力されたテキスト
    /// </summary>
    public string InputText
    {
        get => _inputText;
        set => SetProperty(ref _inputText, value);
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
    /// Enterキー用コマンド
    /// </summary>
    public ICommand EnterKeyCommand { get; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="message">表示するメッセージ</param>
    /// <param name="defaultText">デフォルトのテキスト</param>
    /// <param name="okLabel">OKボタンのラベル（nullの場合はデフォルト値）</param>
    /// <param name="cancelLabel">キャンセルボタンのラベル（nullの場合はデフォルト値）</param>
    /// <param name="okCallback">OKボタンが押されたときのコールバック（入力された文字列を引数に受け取る）</param>
    /// <param name="cancelCallback">キャンセルボタンが押されたときのコールバック</param>
    /// <param name="okOnly">OKボタンのみを表示するかどうか</param>
    public InputTextModalViewModel(
        string message,
        string defaultText = "",
        string? okLabel = null,
        string? cancelLabel = null,
        Action<string>? okCallback = null,
        Action? cancelCallback = null,
        bool okOnly = false)
    {
        _message = message ?? "";
        _inputText = defaultText ?? "";
        // リソースからデフォルト値を設定
        _okLabel = okLabel ?? Strings.OkButton;
        _cancelLabel = cancelLabel ?? Strings.CancelButton;
        _okCallback = okCallback;
        _cancelCallback = cancelCallback;
        _okOnly = okOnly;

        OkCommand = new RelayCommand(OnOk);
        CancelCommand = new RelayCommand(OnCancel);
        EnterKeyCommand = new RelayCommand(OnOk);
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
                _okCallback?.Invoke(InputText);
                break;
            case ModalCloseResult.Cancel:
                _cancelCallback?.Invoke();
                break;
        }
    }
}
