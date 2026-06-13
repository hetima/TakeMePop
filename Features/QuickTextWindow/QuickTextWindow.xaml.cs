using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

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
                HideWindow();
            }
        };
    }

    /// <summary>
    /// 設定に応じて自動コピーしてからウィンドウを隠す。閉じる経路はすべてここを通す。
    /// </summary>
    private void HideWindow()
    {
        if (App.SettingsService.Settings.QuickTextWindow.AutoCopyOnClose
            && !string.IsNullOrEmpty(TextEditor.Text))
        {
            Clipboard.SetText(TextEditor.Text);
        }
        Hide();
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

    private void CloseButton_Click(object sender, RoutedEventArgs e) => HideWindow();

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.OemComma && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
        {
            App.ShowSettingsWindow();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && App.SettingsService.Settings.QuickTextWindow.CloseOnEsc)
        {
            HideWindow();
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

    /// <summary>テキストエディタのフォントを適用する。空文字列はシステムデフォルトへリセット。</summary>
    public void ApplyPopupFontFamily(string fontFamily)
    {
        TextEditor.FontFamily = string.IsNullOrEmpty(fontFamily)
            ? SystemFonts.MessageFontFamily
            : new FontFamily(fontFamily);
    }
}
