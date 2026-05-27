using System.Windows;

namespace Listhing.Features.TransparentWindow;

public partial class TransparentWindow : Window
{
    private readonly TransparentWindowViewModel _viewModel;

    public TransparentWindow()
    {
        InitializeComponent();
        _viewModel = new TransparentWindowViewModel();
        DataContext = _viewModel;
    }

    /// <summary>設定値をウィンドウに反映する</summary>
    public void ApplySettings(double size, bool showPattern)
    {
        Width = size;
        Height = size;
        DropTarget.Width = size;
        DropTarget.Height = size;
        DropTarget.Visibility = showPattern ? Visibility.Visible : Visibility.Hidden;
    }

    /// <summary>マウス座標の近くにウィンドウを移動して表示する</summary>
    public void ShowNearPoint(System.Drawing.Point screenPoint)
    {
        Left = screenPoint.X - Width / 2;
        Top = screenPoint.Y - Height / 2;
        Show();
    }
}
