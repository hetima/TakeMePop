using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Listhing.ViewModels;

namespace Listhing.Views;

/// <summary>
/// EditShortcutKeyView.xaml の相互作用ロジック
/// </summary>
public partial class EditShortcutKeyView : UserControl
{
    /// <summary>
    /// ViewModel
    /// </summary>
    private EditShortcutKeyViewModel? ViewModel => DataContext as EditShortcutKeyViewModel;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public EditShortcutKeyView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Load
    /// </summary>
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // TextBoxにフォーカスを設定
        var textBox = this.FindName("ShortcutInputTextBox") as TextBox;
        textBox?.Focus();
    }

    /// <summary>
    /// ショートカット入力TextBoxのPreviewKeyDownイベント
    /// </summary>
    private void ShortcutInputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        ViewModel?.OnKeyDown(e);
    }

    /// <summary>
    /// ショートカット入力TextBoxのGotFocusイベント
    /// </summary>
    private void ShortcutInputTextBox_GotFocus(object sender, System.Windows.RoutedEventArgs e)
    {
        // フォーカスが当たったときに選択状態にする
        if (sender is TextBox textBox)
        {
            textBox.SelectAll();
        }
    }

    /// <summary>
    /// ショートカット入力TextBoxのLostFocusイベント
    /// </summary>
    private void ShortcutInputTextBox_LostFocus(object sender, System.Windows.RoutedEventArgs e)
    {
        // 必要に応じて処理を追加
    }
}
