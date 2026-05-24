using System.Windows.Input;

namespace Listhing.ViewModels;

/// <summary>
/// モーダルコンテンツのViewModel基底クラス
/// </summary>
public abstract class ModalContentViewModel : ObservableObject, IModalContent
{
    protected ModalCloseResult _requestClose;

    /// <summary>
    /// モーダルを閉じる要求
    /// </summary>
    public ModalCloseResult RequestClose
    {
        get => _requestClose;
        set => SetProperty(ref _requestClose, value);
    }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    protected ModalContentViewModel()
    {
    }

    /// <summary>
    /// モーダルが閉じられたときの処理
    /// </summary>
    /// <param name="result">閉じる理由</param>
    public virtual void ModalClosed(ModalCloseResult result)
    {
        // デフォルトでは何もしない
        // 必要に応じて派生クラスでオーバーライド
    }
}
