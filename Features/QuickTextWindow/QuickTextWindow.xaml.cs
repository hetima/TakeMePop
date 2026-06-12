using System.Windows;
using System.Windows.Input;

namespace Listhing.Features.QuickTextWindow;

public partial class QuickTextWindow : Window
{
    private bool _forceClose = false;

    public QuickTextWindow()
    {
        InitializeComponent();
        Closing += (_, e) =>
        {
            if (!_forceClose)
            {
                e.Cancel = true;
                Hide();
            }
        };
    }

    /// <summary>クリップボードのテキストをセットして表示する</summary>
    public new void Show()
    {
        var text = Clipboard.ContainsText() ? Clipboard.GetText() : "";
        TextEditor.Text = text;
        TextEditor.CaretIndex = TextEditor.Text.Length;
        base.Show();
        Activate();
        TextEditor.Focus();
    }

    /// <summary>アプリ終了時に実際に閉じる</summary>
    public void ForceClose()
    {
        _forceClose = true;
        Close();
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Hide();

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.OemComma && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
        {
            App.ShowSettingsWindow();
            e.Handled = true;
        }
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(TextEditor.Text))
            Clipboard.SetText(TextEditor.Text);
        App.ShowToast(Listhing.Strings.CopiedMessage, durationMs: 600, position: Features.ToastWindow.ToastPosition.NearMouse);
    }

    /// <summary>カスタムリサイズグリップのドラッグイベントハンドラ</summary>
    private void ResizeGrip_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
    {
        Width  = Math.Max(Width  + e.HorizontalChange, MinWidth);
        Height = Math.Max(Height + e.VerticalChange,   MinHeight);
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        App.UpdateQuickTextWindowSize(Width, Height);
    }
}
