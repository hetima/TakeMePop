using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Listhing.Features.QuickHistoryWindow;

/// <summary>
/// クリップボード履歴を一覧表示するウィンドウ。
/// シングルトンとして管理し、Hide/Show で表示状態を切り替える。
/// </summary>
public partial class QuickHistoryWindow : Window
{
    private readonly QuickHistoryWindowViewModel _viewModel;

    public QuickHistoryWindow()
    {
        InitializeComponent();
        _viewModel = new QuickHistoryWindowViewModel();
        DataContext = _viewModel;
        Closing += (_, _) => _viewModel.Dispose();
        Deactivated += (_, _) =>
        {
            if (IsVisible) Close();
        };
    }

    public new void Show()
    {
        _viewModel.RefreshItems();
        base.Show();
        if (_viewModel.Items.Count > 0)
            HistoryListBox.SelectedIndex = 0;
        HistoryListBox.Focus();
    }

    public void ForceClose() => Close();

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
        bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
        bool noMod = Keyboard.Modifiers == ModifierKeys.None;

        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Up || e.Key == Key.Down)
        {
            int count = _viewModel.Items.Count;
            if (count == 0) return;
            int current = HistoryListBox.SelectedIndex;
            HistoryListBox.SelectedIndex = e.Key == Key.Up
                ? Math.Max(0, current - 1)
                : Math.Min(count - 1, current + 1);
            HistoryListBox.ScrollIntoView(HistoryListBox.SelectedItem);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter && !ctrl && !shift)
        {
            if (HistoryListBox.SelectedItem is HistoryDisplayItem selected)
                CopyItem(selected);
            e.Handled = true;
            return;
        }

        if ((e.Key == Key.Delete || e.Key == Key.Back) && !ctrl && !shift)
        {
            if (HistoryListBox.SelectedItem is HistoryDisplayItem toDelete)
            {
                int nextIndex = _viewModel.DeleteItem(toDelete);
                SelectItemAfterRefresh(nextIndex);
            }
            e.Handled = true;
            return;
        }

        if (ctrl || shift) return;

        int index = KeyToIndex(e.Key);
        if (index < 0) return;

        e.Handled = true;

        var items = _viewModel.Items;
        if (index >= items.Count) return;
        var displayItem = items[index];

        if (noMod)
        {
            CopyItem(displayItem);
        }
    }

    /// <summary>
    /// 履歴更新通知による再描画後に指定インデックスを選択する。
    /// </summary>
    private void SelectItemAfterRefresh(int index)
    {
        if (index < 0) return;

        Dispatcher.BeginInvoke(() =>
        {
            if (index >= HistoryListBox.Items.Count) return;

            HistoryListBox.SelectedIndex = index;
            HistoryListBox.ScrollIntoView(HistoryListBox.SelectedItem);
            HistoryListBox.Focus();
        }, DispatcherPriority.Background);
    }

    /// <summary>
    /// キーコードを 0 始まりのインデックスに変換する。
    /// 1→0, 2→1, ..., 9→8, 0→9。対応しないキーは -1。
    /// </summary>
    private static int KeyToIndex(Key key)
    {
        return key switch
        {
            Key.D1 or Key.NumPad1 => 0,
            Key.D2 or Key.NumPad2 => 1,
            Key.D3 or Key.NumPad3 => 2,
            Key.D4 or Key.NumPad4 => 3,
            Key.D5 or Key.NumPad5 => 4,
            Key.D6 or Key.NumPad6 => 5,
            Key.D7 or Key.NumPad7 => 6,
            Key.D8 or Key.NumPad8 => 7,
            Key.D9 or Key.NumPad9 => 8,
            Key.D0 or Key.NumPad0 => 9,
            _ => -1,
        };
    }

    private void HistoryListBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (HistoryListBox.SelectedItem is HistoryDisplayItem displayItem)
        {
            HistoryListBox.SelectedItem = null;
            CopyItem(displayItem);
        }
    }

    /// <summary>
    /// アイテムのテキストをクリップボードにコピーして履歴から削除し、ウィンドウを隠す
    /// </summary>
    private void CopyItem(HistoryDisplayItem displayItem)
    {
        var item = displayItem.Item;
        if (!item.HasText || item.Text == null) return;

        _viewModel.DeleteItem(displayItem);
        Clipboard.SetText(item.Text);
        Close();
    }
}
