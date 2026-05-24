namespace Listhing.ViewModels;

/// <summary>
/// モーダルコンテンツのインターフェース
/// </summary>
public interface IModalContent
{
    /// <summary>
    /// モーダルを閉じる要求（閉じる理由）
    /// </summary>
    ModalCloseResult RequestClose { get; set; }

    /// <summary>
    /// モーダルが閉じられたときに呼び出されるメソッド
    /// </summary>
    /// <param name="result">閉じる理由</param>
    void ModalClosed(ModalCloseResult result);
}
