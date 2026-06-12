using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Listhing.Features.QuickHistoryWindow;

/// <summary>
/// クリップボード履歴を一覧表示するウィンドウ。
/// シングルトンとして管理し、Hide/Show で表示状態を切り替える。
/// </summary>
public partial class QuickHistoryWindow : Window
{
    private readonly QuickHistoryWindowViewModel _viewModel;
    private bool _forceClose;

    public QuickHistoryWindow()
    {
        InitializeComponent();
        _viewModel = new QuickHistoryWindowViewModel();
        DataContext = _viewModel;
        Closing += (_, e) =>
        {
            if (_forceClose) { _viewModel.Dispose(); return; }
            e.Cancel = true;
            Hide();
        };
    }

    public new void Show()
    {
        _viewModel.RefreshItems();
        base.Show();
        HistoryListBox.SelectedIndex = 0;
    }

    /// <summary>選択を次のアイテムへ進める（末尾なら先頭に戻る）</summary>
    public void SelectNext()
    {
        var items = _viewModel.Items;
        if (items.Count == 0) return;
        int next = (HistoryListBox.SelectedIndex + 1) % items.Count;
        HistoryListBox.SelectedIndex = next;
    }

    /// <summary>選択中アイテムをコピー＆ペーストしてウィンドウを閉じる</summary>
    public void CommitSelection()
    {
        if (HistoryListBox.SelectedItem is not HistoryDisplayItem displayItem) return;
        if (displayItem.IsCancel)
        {
            Hide();
            return;
        }

        CopyAndPaste(displayItem);
    }

    public void ForceClose()
    {
        _forceClose = true;
        Close();
    }

    /// <summary>リストのフォントを適用する。空文字列はシステムデフォルトへリセット。</summary>
    public void ApplyPopupFontFamily(string fontFamily)
    {
        HistoryListBox.FontFamily = string.IsNullOrEmpty(fontFamily)
            ? SystemFonts.MessageFontFamily
            : new FontFamily(fontFamily);
    }

    /// <summary>フォントサイズに合わせてアイコン列幅を更新する</summary>
    public void UpdateIconColumnWidth(double fontSize) =>
        _viewModel.UpdateIconColumnWidth(fontSize);

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
            {
                if (selected.IsCancel)
                {
                    Hide();
                    e.Handled = true;
                    return;
                }

                CopyItem(selected);
            }
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
    /// ペースト/コピー用のテキストを取得する。
    /// テキストアイテムはそのテキスト、ファイルアイテムはフルパス（複数なら改行区切り）を返す。
    /// </summary>
    private static string? GetPasteText(HistoryDisplayItem displayItem)
    {
        var item = displayItem.Item;
        if (item == null) return null;
        if (item.HasText && item.Text != null) return item.Text;
        if (item.HasFiles) return string.Join(Environment.NewLine, item.AllFiles);
        return null;
    }

    /// <summary>アイテムをコピーしてペーストし、ウィンドウを閉じる</summary>
    private void CopyAndPaste(HistoryDisplayItem displayItem)
    {
        var text = GetPasteText(displayItem);
        if (text == null) return;

        App.ClipboardService.IgnoreClipboardUpdatesFor(TimeSpan.FromSeconds(1));
        Clipboard.SetText(text);
        Hide();

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        timer.Tick += (_, _) => { timer.Stop(); App.GlobalHookService.PostCtrlV(); };
        timer.Start();
    }

    private void HistoryListBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (HistoryListBox.SelectedItem is HistoryDisplayItem displayItem)
        {
            HistoryListBox.SelectedItem = null;
            if (displayItem.IsCancel)
            {
                Hide();
                return;
            }

            CopyItem(displayItem);
        }
    }

    /// <summary>
    /// アイテムのテキストをクリップボードにコピーして履歴から削除し、ウィンドウを隠す
    /// </summary>
    private void CopyItem(HistoryDisplayItem displayItem)
    {
        var text = GetPasteText(displayItem);
        if (text == null) return;

        _viewModel.DeleteItem(displayItem);
        Clipboard.SetText(text);
        Close();
    }
}
