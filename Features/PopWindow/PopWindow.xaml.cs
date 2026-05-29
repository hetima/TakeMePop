using System.IO;
using System.Windows;
using System.Windows.Input;
using Listhing.Helpers;
using Listhing.Services;

namespace Listhing.Features.PopWindow;

public partial class PopWindow : Window
{
    private PopWindowViewModel _viewModel;
    private bool _isDraggingOut;
    private bool _isDragPending;
    private bool _droppedOnSelf;
    private Point _dragStartPoint;

    public PopWindow()
    {
        InitializeComponent();
        _viewModel = new PopWindowViewModel();
        DataContext = _viewModel;
        Closed += (_, _) => App.RemovePopWindow(this);
        SizeChanged += (_, _) => App.SavePopWindowSize(Width, Height);
    }

    public void SetItem(ClipboardItem item) => _viewModel.Item = item;
    public bool Pinned => _viewModel.Pinned;

    /// <summary>保持しているファイルパスのリストを返す（ファイルがなければ null）</summary>
    public IReadOnlyList<string>? GetFiles() => _viewModel.Item?.Files;

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape) Close();
        base.OnKeyDown(e);
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        var item = _viewModel.Item;
        if (item == null) return;

        if (item.HasFiles && item.Files != null)
        {
            var coll = new System.Collections.Specialized.StringCollection();
            coll.AddRange(item.Files.ToArray());
            Clipboard.SetFileDropList(coll);
        }
        else if (item.HasText && item.Text != null)
        {
            Clipboard.SetText(item.Text);
        }

        App.ShowToast(Listhing.Strings.CopiedMessage, durationMs: 600, position: Features.ToastWindow.ToastPosition.NearMouse);
    }

    private void CloseOnSuccessButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.Pinned = !_viewModel.Pinned;
    }

    private void ContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.ContextMenu contextMenu) return;
        var historyItem = contextMenu.Items
            .OfType<System.Windows.Controls.MenuItem>()
            .FirstOrDefault(m => m.Name == "ClipboardHistoryMenuItem");
        if (historyItem == null) return;

        historyItem.Items.Clear();

        var history = App.ClipboardService.History
            .AsEnumerable()
            .Reverse()
            .Take(10);

        foreach (var item in history)
        {
            var menuItem = MenuHelper.CreateMenuItem(item.GetHeadline(30));
            menuItem.Tag = item;

            if (item.HasFiles)
            {
                menuItem.SetIconText(AppConstants.IconTexts.File);
            }

            menuItem.Click += ClipboardHistoryItem_Click;
            historyItem.Items.Add(menuItem);
        }

        historyItem.IsEnabled = historyItem.Items.Count > 0;
    }

    private void ClipboardHistoryItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.MenuItem { Tag: ClipboardItem item })
            SetItem(item);
    }

    private void OpenMainWindow_Click(object sender, RoutedEventArgs e)
    {
        App.ShowMainWindow();
    }

    private void Setting_Click(object sender, RoutedEventArgs e)
    {
        App.ShowSettingsWindow();
    }

    private void Quit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void ContentArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.Item == null) return;
        _dragStartPoint = e.GetPosition(ContentArea);
        _isDragPending = true;
        ContentArea.CaptureMouse();
    }

    private void ContentArea_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragPending || e.LeftButton != MouseButtonState.Pressed) return;

        var pos = e.GetPosition(ContentArea);
        if (Math.Abs(pos.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(pos.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        _isDragPending = false;
        ContentArea.ReleaseMouseCapture();

        var data = BuildDataObject(_viewModel.Item!);
        if (data == null) return;

        _isDraggingOut = true;
        _droppedOnSelf = false;
        var effect = DragDrop.DoDragDrop(ContentArea, data, DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link);
        _isDraggingOut = false;

        if (effect != DragDropEffects.None && !_droppedOnSelf && !_viewModel.Pinned)
            Close();
    }

    private void ContentArea_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragPending = false;
        ContentArea.ReleaseMouseCapture();
    }

    private void ContentArea_DragEnter(object sender, DragEventArgs e)
    {
        if (_isDraggingOut) return;
        _viewModel.IsDropTarget = true;
    }

    private void ContentArea_DragLeave(object sender, DragEventArgs e)
    {
        _viewModel.IsDropTarget = false;
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
        _viewModel.IsDropTarget = false;
        if (_isDraggingOut)
            _droppedOnSelf = true;
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

    /// <summary>
    /// カスタムリサイズグリップのドラッグイベントハンドラ
    /// </summary>
    public void ResizeGrip_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
    {
        // ウィンドウのサイズを変更
        double newWidth = Width + e.HorizontalChange;
        double newHeight = Height + e.VerticalChange;

        // 最小サイズを制限
        newWidth = Math.Max(newWidth, MinWidth);
        newHeight = Math.Max(newHeight, MinHeight);

        Width = newWidth;
        Height = newHeight;
    }
}
