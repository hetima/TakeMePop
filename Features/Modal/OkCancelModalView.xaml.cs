using System.Windows;
using System.Windows.Controls;

namespace Listhing.Views;

/// <summary>
/// OkCancelModalView.xaml の相互作用ロジック
/// </summary>
public partial class OkCancelModalView : UserControl
{
    /// <summary>
    /// コンストラクタ
    /// </summary>
    public OkCancelModalView()
    {
        InitializeComponent();
        Loaded += ModalView_Loaded;

    }
    private void ModalView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (CancelButton.Visibility == Visibility.Visible)
        {
            CancelButton.Focus();
        }
        else
        {
            OkButton.Focus();
        }
    }
}
