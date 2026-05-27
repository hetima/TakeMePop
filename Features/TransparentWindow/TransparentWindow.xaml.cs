using System.Windows;

namespace Listhing.Features.TransparentWindow;

public partial class TransparentWindow : Window
{
    private readonly TransparentWindowViewModel _viewModel;

    /// <summary>ドロップされたデータ</summary>
    public IDataObject? DroppedData { get; private set; }

    /// <summary>ドロップを受け取ったとき</summary>
    public event EventHandler<IDataObject>? DataDropped;

    public TransparentWindow()
    {
        InitializeComponent();
        _viewModel = new TransparentWindowViewModel();
        DataContext = _viewModel;

        Drop += OnDrop;
        DragEnter += OnDragEnter;
        DragLeave += OnDragLeave;
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
        // ウィンドウの中心がポインタに重なるよう配置
        Left = screenPoint.X - Width / 2;
        Top = screenPoint.Y - Height / 2;
        Show();
    }

    private void OnDragEnter(object sender, DragEventArgs e)
    {
        // ドロップ可能なデータが来たら視覚フィードバック
        if (e.Data != null)
        {
            e.Effects = DragDropEffects.Copy;
            DropTarget.Fill = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(0x88, 0xFF, 0xFF, 0xFF));
        }
        e.Handled = true;
    }

    private void OnDragLeave(object sender, DragEventArgs e)
    {
        DropTarget.Fill = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF));
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data == null) return;

        DroppedData = e.Data;
        DataDropped?.Invoke(this, e.Data);
        Hide();
        e.Handled = true;
    }
}
