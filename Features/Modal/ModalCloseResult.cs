namespace Listhing.ViewModels;

/// <summary>
/// モーダルの閉じる理由を表す列挙型
/// </summary>
public enum ModalCloseResult
{
    /// <summary>
    /// 閉じる要求なし
    /// </summary>
    None,

    /// <summary>
    /// キャンセル（ESCキー、キャンセルボタンなど）
    /// </summary>
    Cancel,

    /// <summary>
    /// OK（OKボタン、Enterキーなど）
    /// </summary>
    OK
}
