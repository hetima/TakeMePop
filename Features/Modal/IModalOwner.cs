using System.Windows.Controls;
using Listhing.Services;

namespace Listhing.ViewModels;

/// <summary>
/// モーダルの所有者インターフェース
/// </summary>
public interface IModalOwner
{
    /// <summary>
    /// モーダルが表示されているかどうか
    /// </summary>
    bool IsModalVisible { get; set; }

    /// <summary>
    /// 現在表示中のモーダルコンテンツ
    /// </summary>
    UserControl? CurrentModalContent { get; set; }

    /// <summary>
    /// モーダルサービス
    /// </summary>
    ModalService ModalService { get; }
}
