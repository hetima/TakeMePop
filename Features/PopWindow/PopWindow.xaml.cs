using System.IO;
using System.Windows;
using System.Windows.Input;
using Listhing.Services;

namespace Listhing.Features.PopWindow;

public partial class PopWindow : Window
{
    private PopWindowViewModel _viewModel;

    public PopWindow()
    {
        InitializeComponent();
        _viewModel = new PopWindowViewModel();
        DataContext = _viewModel;
        Closed += (_, _) => App.RemovePopWindow(this);
        SizeChanged += (_, _) => App.SavePopWindowSize(Width, Height);
    }

    public void SetItem(ClipboardItem item) => _viewModel.Item = item;

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ContentArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.Item == null) return;

        var data = BuildDataObject(_viewModel.Item);
        if (data == null) return;

        DragDrop.DoDragDrop(ContentArea, data, DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link);
    }

    private void ContentArea_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop) ||
            e.Data.GetDataPresent(DataFormats.UnicodeText) ||
            e.Data.GetDataPresent(DataFormats.Text))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void ContentArea_Drop(object sender, DragEventArgs e)
    {
        var item = ClipboardItem.TryCreateFromDragData(e.Data);
        if (item != null)
            _viewModel.Item = item;
        e.Handled = true;
    }

    /// <summary>ClipboardItemからDragDrop用DataObjectを生成する</summary>
    private static DataObject? BuildDataObject(ClipboardItem item)
    {
        if (item.HasFiles && item.Files != null)
        {
            var coll = new System.Collections.Specialized.StringCollection();
            coll.AddRange(item.Files.ToArray());
            var data = new DataObject();
            data.SetFileDropList(coll);
            return data;
        }
        if (item.HasText && item.Text != null)
        {
            return new DataObject(DataFormats.UnicodeText, item.Text);
        }
        return null;
    }
}
