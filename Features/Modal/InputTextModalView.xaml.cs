using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Listhing.Views;

/// <summary>
/// InputTextModalView.xaml の相互作用ロジック
/// </summary>
public partial class InputTextModalView : UserControl
{
    public InputTextModalView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// テキスト入力でEnterキーが押されたときの処理
    /// </summary>
    private void OnInputKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is ViewModels.InputTextModalViewModel viewModel)
        {
            viewModel.EnterKeyCommand?.Execute(null);
            e.Handled = true;
        }
    }
    
    /// <summary>
    /// Load
    /// </summary>
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // TextBoxにフォーカスを設定
        var textBox = this.FindName("InputTextBox") as TextBox;
        textBox?.Focus();
        textBox?.SelectAll();
    }
}
