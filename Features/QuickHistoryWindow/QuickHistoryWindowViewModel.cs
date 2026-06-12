using System.Collections.ObjectModel;
using Listhing.Services;
using Listhing.ViewModels;

namespace Listhing.Features.QuickHistoryWindow;

/// <summary>
/// 履歴リストの1行を表す表示アイテム
/// </summary>
public class HistoryDisplayItem
{
    /// <summary>番号</summary>
    public int Number { get; }

    /// <summary>一覧に表示するラベル（"1 テキスト..."形式）</summary>
    public string Label { get; }

    /// <summary>キャンセル項目かどうか</summary>
    public bool IsCancel { get; }

    /// <summary>元の ClipboardItem</summary>
    public ClipboardItem? Item { get; }

    private HistoryDisplayItem(int number, string label, ClipboardItem? item, bool isCancel)
    {
        Number = number;
        Label = label;
        Item = item;
        IsCancel = isCancel;
    }

    /// <summary>キャンセル用の表示アイテムを作成する</summary>
    public static HistoryDisplayItem CreateCancel() =>
        new(0, Listhing.Strings.CancelButton, null, true);

    public HistoryDisplayItem(int number, ClipboardItem item)
    {
        Number = number;
        Item = item;
        IsCancel = false;
        Label = $"{number} {item.GetHeadline(60)}";
    }
}

/// <summary>
/// QuickHistoryWindow の ViewModel
/// </summary>
public class QuickHistoryWindowViewModel : ObservableObject
{
    /// <summary>表示する履歴アイテムリスト（キャンセル + 最大5件、最新が先頭）</summary>
    public ObservableCollection<HistoryDisplayItem> Items { get; } = [];

    public QuickHistoryWindowViewModel()
    {
        App.ClipboardService.HistoryChanged += OnHistoryChanged;
        RefreshItems();
    }

    private void OnHistoryChanged()
    {
        // ClipboardService は別スレッドから呼ばれる可能性があるため Dispatcher 経由で更新
        System.Windows.Application.Current.Dispatcher.BeginInvoke(RefreshItems);
    }

    /// <summary>
    /// History から最新5件を取得して Items を再構築する
    /// </summary>
    public void RefreshItems()
    {
        Items.Clear();
        Items.Add(HistoryDisplayItem.CreateCancel());

        var history = App.ClipboardService.History;
        int count = Math.Min(history.Count, 5);
        for (int i = 0; i < count; i++)
        {
            // 最新が先頭（history の末尾から逆順）
            var item = history[history.Count - 1 - i];
            Items.Add(new HistoryDisplayItem(i + 1, item));
        }
    }

    /// <summary>指定アイテムを削除し、削除後に選択すべきインデックスを返す</summary>
    public int DeleteItem(HistoryDisplayItem displayItem)
    {
        if (displayItem.IsCancel || displayItem.Item == null) return -1;

        int index = Items.IndexOf(displayItem);
        int nextCount = Math.Min(Math.Max(App.ClipboardService.History.Count - 1, 0), 5) + 1;
        App.ClipboardService.RemoveFromHistory(displayItem.Item);
        return Math.Min(index, nextCount - 1);
    }

    public void Dispose()
    {
        App.ClipboardService.HistoryChanged -= OnHistoryChanged;
    }
}
