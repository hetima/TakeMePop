using System.Windows;
using System.Windows.Input;

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

        // 閉じるではなく非表示にする（シングルトン再利用）
        Closing += (_, e) =>
        {
            e.Cancel = true;
            Hide();
        };
    }

    /// <summary>
    /// ウィンドウを表示する直前に履歴リストを最新化する
    /// </summary>
    public new void Show()
    {
        _viewModel.RefreshItems();
        base.Show();
        Activate();
    }

    /// <summary>
    /// シングルトン破棄時に呼ぶ
    /// </summary>
    public void ForceClose()
    {
        Closing -= null;
        _viewModel.Dispose();
        Close();
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
        bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
        bool noMod = Keyboard.Modifiers == ModifierKeys.None;

        if (e.Key == Key.Escape)
        {
            Hide();
            e.Handled = true;
            return;
        }

        int index = KeyToIndex(e.Key);
        if (index < 0) return;

        e.Handled = true;

        var items = _viewModel.Items;
        if (index >= items.Count) return;
        var displayItem = items[index];

        if (ctrl)
        {
            DeleteItem(displayItem);
        }
        else if (shift)
        {
            PasteItem(displayItem);
            DeleteItem(displayItem);
        }
        else if (noMod)
        {
            PasteItem(displayItem);
        }
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

    /// <summary>
    /// アイテムのテキストをクリップボードにセットしてウィンドウを隠し、前面ウィンドウへ Ctrl+V を送る
    /// </summary>
    private void PasteItem(HistoryDisplayItem displayItem)
    {
        var item = displayItem.Item;
        if (!item.HasText || item.Text == null) return;

        Clipboard.SetText(item.Text);
        Hide();

        // フォーカスが前のウィンドウに戻るのを待ってからペースト
        System.Windows.Threading.DispatcherTimer timer = new()
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            App.GlobalHookService.PostCtrlV();
        };
        timer.Start();
    }

    /// <summary>
    /// 対応するアイテムを ClipboardService.History から削除する
    /// </summary>
    private static void DeleteItem(HistoryDisplayItem displayItem)
    {
        App.ClipboardService.History.Remove(displayItem.Item);
        App.ClipboardService.NotifyHistoryChanged();
    }
}
