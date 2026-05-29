using System.Collections.ObjectModel;
using Listhing.Services;
using Listhing.ViewModels;

namespace Listhing.Features.QuickHistoryWindow;

/// <summary>
/// 履歴リストの1行を表す表示アイテム
/// </summary>
public class HistoryDisplayItem
{
    /// <summary>番号（1〜9, 0）</summary>
    public int Number { get; }

    /// <summary>一覧に表示するラベル（"1 テキスト..."形式）</summary>
    public string Label { get; }

    /// <summary>元の ClipboardItem</summary>
    public ClipboardItem Item { get; }

    public HistoryDisplayItem(int number, ClipboardItem item)
    {
        Number = number;
        Item = item;
        Label = $"{number} {item.GetHeadline(60)}";
    }
}

/// <summary>
/// QuickHistoryWindow の ViewModel
/// </summary>
public class QuickHistoryWindowViewModel : ObservableObject
{
    /// <summary>表示する履歴アイテムリスト（最大10件、最新が先頭）</summary>
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
    /// History から最新10件を取得して Items を再構築する
    /// </summary>
    public void RefreshItems()
    {
        Items.Clear();
        var history = App.ClipboardService.History;
        int count = Math.Min(history.Count, 10);
        for (int i = 0; i < count; i++)
        {
            // 最新が先頭（history の末尾から逆順）
            var item = history[history.Count - 1 - i];
            int number = (i + 1) % 10; // 1〜9, 0
            Items.Add(new HistoryDisplayItem(number, item));
        }
    }

    public void Dispose()
    {
        App.ClipboardService.HistoryChanged -= OnHistoryChanged;
    }
}
