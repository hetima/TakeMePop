using System.Windows;
using Listhing;

namespace Listhing.Features.PopWindow;

public partial class PopWindow : Window
{
    public PopWindow()
    {
        InitializeComponent();
        DataContext = new PopWindowViewModel();
        Closed += (_, _) => App.RemovePopWindow(this);
        SizeChanged += (_, _) => App.SavePopWindowSize(Width, Height);
    }

    private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
