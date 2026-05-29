using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Documents; // Run クラスに必要
using System.Windows.Media;
using System.Drawing;     // FontFamily クラスに必要

namespace Listhing.Features.ToastWindow;

/// <summary>
/// トースト通知の表示位置種別
/// </summary>
public enum ToastPosition
{
    /// <summary>スクリーン下部に表示（複数トーストは上にずらして重ならないようにする）</summary>
    ScreenBottom,
    /// <summary>マウスポインタの少し上に表示</summary>
    NearMouse,
}

public partial class ToastWindow : Window
{
    private const double BottomMargin = 150.0;
    private const double ToastSpacing = 8.0;

    // スクリーン下部に積まれているトーストを管理する静的リスト
    private static readonly List<ToastWindow> _stackedToasts = [];

    private readonly int _durationMs;
    private readonly ToastPosition _position;

    private ToastWindow(string text, double fontSize, int durationMs, ToastPosition position, string? icon=null)
    {
        InitializeComponent();
        _durationMs = durationMs;
        _position = position;

        if (icon == null)
        {
            MessageText.Text = text;
            MessageText.FontSize = fontSize;
        }
        else
        {
            MessageText.Inlines.Add(new Run(icon)
            {
                FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons"),
                FontSize = fontSize,
            });
            MessageText.Inlines.Add(new Run(": " + text)
            {
                FontSize = fontSize,
            });
        }

        // 表示内容と同じ構造で計測してウィンドウサイズを確定する
        var (textWidth, textHeight) = MeasureContent(text, fontSize, icon);
        Width  = textWidth  + 8 * 3 + 8 * 2; // Padding左右 + Grid Margin左右
        Height = textHeight + 6 * 2 + 6 + 12; // Padding上下 + Grid Margin上下

        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    /// <summary>
    /// トーストを表示する。
    /// </summary>
    /// <param name="text">表示テキスト</param>
    /// <param name="fontSize">フォントサイズ</param>
    /// <param name="durationMs">表示時間（ミリ秒）</param>
    /// <param name="position">表示位置</param>
    public static void Show(string text, double fontSize = 13, int durationMs = 3000, ToastPosition position = ToastPosition.ScreenBottom, string? iconText = null)
    {
        var toast = new ToastWindow(text, fontSize, durationMs, position, iconText);
        toast.Show();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // SizeToContent が確定した後に位置を決める
        PlaceWindow();

        if (_position == ToastPosition.ScreenBottom)
            _stackedToasts.Add(this);

        PlayFadeIn();
        StartAutoClose();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (_position == ToastPosition.ScreenBottom)
        {
            _stackedToasts.Remove(this);
            // 残ったトーストを下から詰め直す
            RepositionStackedToasts();
        }
    }

    /// <summary>
    /// ウィンドウをスクリーン座標に配置する。
    /// </summary>
    private void PlaceWindow()
    {
        var screen = System.Windows.SystemParameters.WorkArea;

        if (_position == ToastPosition.NearMouse)
        {
            GetCursorPos(out var pt);
            Left = pt.X - ActualWidth / 2;
            Top = pt.Y - ActualHeight - 20;
            // スクリーン外補正
            Left = Math.Clamp(Left, screen.Left, screen.Right - ActualWidth);
            Top = Math.Clamp(Top, screen.Top, screen.Bottom - ActualHeight);
        }
        else
        {
            // 既存のスタック済みトーストの最上面より上に配置
            double usedHeight = _stackedToasts
                .Where(t => t != this)
                .Sum(t => t.ActualHeight + ToastSpacing);

            Left = screen.Left + (screen.Width - ActualWidth) / 2;
            Top = screen.Bottom - BottomMargin - ActualHeight - usedHeight;
        }
    }

    /// <summary>
    /// スクリーン下部のトーストを下から詰め直す。
    /// </summary>
    private static void RepositionStackedToasts()
    {
        var screen = System.Windows.SystemParameters.WorkArea;
        double offset = 0;
        // リスト末尾が最新（一番下）
        for (int i = _stackedToasts.Count - 1; i >= 0; i--)
        {
            var t = _stackedToasts[i];
            t.Top = screen.Bottom - BottomMargin - t.ActualHeight - offset;
            offset += t.ActualHeight + ToastSpacing;
        }
    }

    private void PlayFadeIn()
    {
        Opacity = 0;
        var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        BeginAnimation(OpacityProperty, anim);
    }

    private void StartAutoClose()
    {
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(_durationMs)
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            PlayFadeOutAndClose();
        };
        timer.Start();
    }

    private void PlayFadeOutAndClose()
    {
        var anim = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        anim.Completed += (_, _) => Close();
        BeginAnimation(OpacityProperty, anim);
    }

    /// <summary>
    /// 表示内容と同じ TextBlock 構造で描画サイズを計測する。
    /// </summary>
    private (double width, double height) MeasureContent(string text, double fontSize, string? icon)
    {
        var tb = new TextBlock
        {
            FontFamily = MessageText.FontFamily ?? System.Windows.SystemFonts.MessageFontFamily,
            FontStyle = MessageText.FontStyle,
            FontWeight = MessageText.FontWeight,
            FontStretch = MessageText.FontStretch,
            TextWrapping = TextWrapping.NoWrap,
        };
        if (icon == null)
        {
            tb.Text = text;
            tb.FontSize = fontSize;
        }
        else
        {
            tb.Inlines.Add(new Run(icon)
            {
                FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons"),
                FontSize = fontSize,
            });
            tb.Inlines.Add(new Run(": " + text) { FontSize = fontSize });
        }
        tb.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
        return (tb.DesiredSize.Width, tb.DesiredSize.Height);
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool GetCursorPos(out System.Drawing.Point lpPoint);
}
