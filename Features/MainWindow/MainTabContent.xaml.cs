using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Listhing.Views;

/// <summary>
/// MainTabContent.xaml の相互作用ロジック
/// </summary>
public partial class MainTabContent : UserControl
{
    public MainTabContent()
    {
        InitializeComponent();
    }

    /// <summary>
    /// カスタムリサイズグリップのドラッグイベントハンドラ
    /// MainWindowのResizeGrip_DragDeltaを呼び出す
    /// </summary>
    private void ResizeGrip_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
    {
        // MainWindowのインスタンスを取得
        var mainWindow = Window.GetWindow(this) as MainWindow;
        if (mainWindow != null)
        {
            // MainWindowのResizeGrip_DragDeltaを呼び出す
            mainWindow.ResizeGrip_DragDelta(sender, e);
        }
    }

    /// <summary>
    /// 視覚ツリーから指定された型の子要素を探す
    /// </summary>
    public static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T result)
            {
                return result;
            }

            var resultOfChild = FindVisualChild<T>(child);
            if (resultOfChild != null)
            {
                return resultOfChild;
            }
        }
        return null;
    }
}
