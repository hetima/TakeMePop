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
        Closed += (_, _) => { _viewModel.Item?.Dispose(); App.RemovePopWindow(this); };
        SizeChanged += (_, _) => App.SavePopWindowSize(Width, Height);
        ContentArea.SizeChanged += (_, _) => UpdateFileItemLayout();
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

    /// <summary>ファイル表示領域のサイズに応じてアイコン表示と並び方向を切り替える</summary>
    private void UpdateFileItemLayout()
    {
        var contentWidth = ContentArea.ActualWidth;
        var contentHeight = ContentArea.ActualHeight;
        var hideIcon = contentWidth < 96 && contentHeight < 96;
        var useHorizontalLayout = !hideIcon && contentWidth >= contentHeight * 2;

        FileIconHost.Visibility = hideIcon ? Visibility.Collapsed : Visibility.Visible;
        FileItemPanel.HorizontalAlignment = useHorizontalLayout
            ? HorizontalAlignment.Center
            : HorizontalAlignment.Stretch;
        FileIconColumn.Width = useHorizontalLayout
            ? GridLength.Auto
            : new GridLength(1, GridUnitType.Star);
        FileLabelColumn.Width = useHorizontalLayout
            ? new GridLength(1, GridUnitType.Star)
            : new GridLength(0);
        FileIconRow.Height = useHorizontalLayout
            ? new GridLength(1, GridUnitType.Star)
            : GridLength.Auto;
        FileLabelRow.Height = useHorizontalLayout
            ? new GridLength(0)
            : GridLength.Auto;
        System.Windows.Controls.Grid.SetRow(FileIconHost, 0);
        System.Windows.Controls.Grid.SetColumn(FileIconHost, 0);
        System.Windows.Controls.Grid.SetRow(FileLabelText, useHorizontalLayout ? 0 : 1);
        System.Windows.Controls.Grid.SetColumn(FileLabelText, useHorizontalLayout ? 1 : 0);
        FileIconHost.Margin = useHorizontalLayout
            ? new Thickness(0, 0, 4, 0)
            : new Thickness(0);
        FileLabelText.Margin = useHorizontalLayout
            ? new Thickness(4, 0, 4, 0)
            : new Thickness(4, 4, 4, 0);
        FileLabelText.TextAlignment = useHorizontalLayout
            ? TextAlignment.Left
            : TextAlignment.Center;
        FileLabelText.VerticalAlignment = useHorizontalLayout
            ? VerticalAlignment.Center
            : VerticalAlignment.Stretch;
        FileLabelText.MaxWidth = double.PositiveInfinity;
    }

    private void ContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        // 対象が実在するファイル・フォルダのときだけ「エクスプローラーで表示」を出す
        bool hasRealFile = GetRevealTargetPath() != null;
        ShowInExplorerMenuItem.Visibility = hasRealFile ? Visibility.Visible : Visibility.Collapsed;
        ShowInExplorerSeparator.Visibility = hasRealFile ? Visibility.Visible : Visibility.Collapsed;

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

    /// <summary>
    /// エクスプローラーで表示する対象パスを返す。実在するファイル・フォルダがなければ null。
    /// 一時ファイル（TempFiles）は対象外。
    /// </summary>
    private string? GetRevealTargetPath()
    {
        var files = _viewModel.Item?.Files;
        if (files == null) return null;
        foreach (var path in files)
        {
            if (File.Exists(path) || Directory.Exists(path)) return path;
        }
        return null;
    }

    private void ShowInExplorer_Click(object sender, RoutedEventArgs e)
    {
        var path = GetRevealTargetPath();
        if (path != null)
            FileSystemHelper.OpenFolderAndSelect(path);
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

        // Move された場合は一時ファイルが消えている可能性があるのでアイテムをクリア
        if (effect == DragDropEffects.Move && _viewModel.Item?.TempFiles != null)
        {
            _viewModel.Item.Dispose();
            _viewModel.Item = null;
        }

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
            e.Data.GetDataPresent("FileGroupDescriptorW") ||
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
        ClipboardItem.DebugDumpFormats(e.Data);
        var item = ClipboardItem.TryCreateFromDragData(e.Data);
        if (item != null)
            _viewModel.Item = item;
        e.Handled = true;
    }

    /// <summary>ClipboardItemからDragDrop用DataObjectを生成する</summary>
    private static DataObject? BuildDataObject(ClipboardItem item)
    {
        if (item.HasFiles)
        {
            var coll = new System.Collections.Specialized.StringCollection();
            coll.AddRange(item.AllFiles.ToArray());
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
