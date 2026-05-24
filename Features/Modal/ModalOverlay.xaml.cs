using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Listhing.ViewModels;

namespace Listhing.Views.Controls;

/// <summary>
/// ModalOverlay.xaml の相互作用ロジック
/// </summary>
public partial class ModalOverlay : UserControl
{
    private Storyboard? _fadeInStoryboard;
    private Storyboard? _fadeOutStoryboard;
    private Storyboard? _contentScaleInStoryboard;

    /// <summary>
    /// モーダルコンテンツ依存関係プロパティ
    /// </summary>
    public static readonly DependencyProperty ModalContentProperty =
        DependencyProperty.Register(
            nameof(ModalContent),
            typeof(object),
            typeof(ModalOverlay),
            new PropertyMetadata(null, OnModalContentChanged));

    /// <summary>
    /// モーダルコンテンツ
    /// </summary>
    public object? ModalContent
    {
        get => GetValue(ModalContentProperty);
        set => SetValue(ModalContentProperty, value);
    }

    /// <summary>
    /// 閉じるコマンド依存関係プロパティ
    /// </summary>
    public static readonly DependencyProperty CloseCommandProperty =
        DependencyProperty.Register(
            nameof(CloseCommand),
            typeof(ICommand),
            typeof(ModalOverlay),
            new PropertyMetadata(null));

    /// <summary>
    /// 閉じるコマンド（XAMLバインディング用）
    /// </summary>
    public ICommand? CloseCommand
    {
        get => (ICommand?)GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public ModalOverlay()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        KeyDown += OnKeyDown;
    }

    /// <summary>
    /// ロードされたとき
    /// </summary>
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _fadeInStoryboard = (Storyboard?)FindResource("FadeInStoryboard");
        _fadeOutStoryboard = (Storyboard?)FindResource("FadeOutStoryboard");
        _contentScaleInStoryboard = (Storyboard?)FindResource("ContentScaleInStoryboard");
    }

    /// <summary>
    /// モーダルコンテンツが変更されたとき
    /// </summary>
    private static void OnModalContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ModalOverlay overlay)
        {
            if (e.NewValue is UserControl view)
            {
                // ContentControl.Content に View を設定（View は自分の DataContext を維持）
                var contentControl = overlay.FindName("ModalContentControl") as ContentControl;
                if (contentControl != null)
                {
                    contentControl.Content = view;
                }
                overlay.PlayFadeInAnimation();
            }
            else
            {
                overlay.PlayFadeOutAnimation();
            }
        }
    }

    /// <summary>
    /// ContentControlを取得する（フォーカス設定用）
    /// </summary>
    public ContentControl? ContentControl => FindName("ModalContentControl") as ContentControl;

    /// <summary>
    /// フェードインアニメーションを再生
    /// </summary>
    private void PlayFadeInAnimation()
    {
        _fadeInStoryboard?.Begin();
        _contentScaleInStoryboard?.Begin();
    }

    /// <summary>
    /// フェードアウトアニメーションを再生
    /// </summary>
    private void PlayFadeOutAnimation()
    {
        _fadeOutStoryboard?.Begin();
    }

    /// <summary>
    /// オーバーレイがクリックされたとき（キャンセル）
    /// </summary>
    private void Overlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // オーバーレイ自体のクリックのみ処理（コンテンツエリアは除く）
        // senderがBorderでNameが設定されていない場合（半透明カバー）のみ閉じる
        if (sender is Border border && string.IsNullOrEmpty(border.Name))
        {
            CloseModal();
        }
    }

    /// <summary>
    /// 閉じるボタンがクリックされたとき
    /// </summary>
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        CloseModal();
    }
    
    /// <summary>
    /// モーダルを閉じる
    /// </summary>
    private void CloseModal()
    {
        CloseCommand?.Execute(null);
    }

    /// <summary>
    /// キーが押されたとき
    /// </summary>
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        // ESCキーでモーダルを閉じる
        if (e.Key == Key.Escape)
        {
            CloseModal();
            e.Handled = true;
        }
    }
}
