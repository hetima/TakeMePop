using System.Windows;
using Listhing.Helpers;

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
    public void ApplySettings(double size)
    {
        Width = size;
        Height = size;
    }

    /// <summary>マウス座標の近くにウィンドウを移動して表示する</summary>
    public void ShowNearPoint(System.Drawing.Point screenPoint)
    {
        var dipPoint = ScreenCoordinateHelper.PhysicalToDip(screenPoint);
        Left = dipPoint.X - Width / 2;
        Top = dipPoint.Y - Height / 2;
        Show();
    }
}
