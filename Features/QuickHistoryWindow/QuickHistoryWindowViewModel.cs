using System.Collections.ObjectModel;
using System.Windows;
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

    /// <summary>一覧に表示するラベル</summary>
    public string Label { get; }

    /// <summary>アイコン文字（Segoe Fluent Icons）</summary>
    public string Icon { get; }

    /// <summary>キャンセル項目かどうか</summary>
    public bool IsCancel { get; }

    /// <summary>元の ClipboardItem</summary>
    public ClipboardItem? Item { get; }

    private HistoryDisplayItem(int number, string label, string icon, ClipboardItem? item, bool isCancel)
    {
        Number = number;
        Label = label;
        Icon = icon;
        Item = item;
        IsCancel = isCancel;
    }

    /// <summary>キャンセル用の表示アイテムを作成する</summary>
    public static HistoryDisplayItem CreateCancel() =>
        new(0, Listhing.Strings.CancelButton, "", null, true);

    public HistoryDisplayItem(int number, ClipboardItem item)
    {
        Number = number;
        Item = item;
        IsCancel = false;
        Label = item.GetHeadline(60);
        // ファイルはフォルダアイコン、テキストはアイコンなし
        Icon = item.HasFiles ? "" : "";
    }
}

/// <summary>
/// QuickHistoryWindow の ViewModel
/// </summary>
public class QuickHistoryWindowViewModel : ObservableObject
{
    /// <summary>表示する履歴アイテムリスト（キャンセル + 最大5件、最新が先頭）</summary>
    public ObservableCollection<HistoryDisplayItem> Items { get; } = [];

    private GridLength _iconColumnWidth = new(20);
    /// <summary>アイコン列の幅。フォントサイズ変更時に更新する。</summary>
    public GridLength IconColumnWidth
    {
        get => _iconColumnWidth;
        set => SetProperty(ref _iconColumnWidth, value);
    }

    /// <summary>フォントサイズに合わせてアイコン列幅を更新する</summary>
    public void UpdateIconColumnWidth(double fontSize) =>
        IconColumnWidth = new GridLength(Math.Ceiling(fontSize * 1.5));

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
