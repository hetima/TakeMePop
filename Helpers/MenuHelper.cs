using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Listhing.Helpers;

/// <summary>
/// メニューに関するヘルパークラス
/// </summary>
public static class MenuHelper
{
    public static readonly FontFamily IconFont = new("Segoe Fluent Icons, Segoe MDL2 Assets");

    /// <summary>
    /// MenuItemを作る
    /// HorizontalContentAlignment と VerticalContentAlignment を設定しないと
    /// Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
    /// みたいなBindingエラーが出ることがある
    /// </summary>
    public static MenuItem CreateMenuItem(
        string header,
        string? shortcut = null,
        ICommand? cmd = null,
        object? commandParameter = null,
        string? iconText = null,
        bool isEnabled = true)
    {
        MenuItem result = new MenuItem
        {
            Header = header,
            InputGestureText = shortcut,
            Command = cmd,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            CommandParameter = commandParameter,
            IsEnabled = isEnabled
        };

        if (!string.IsNullOrWhiteSpace(iconText))
        {
            TextBlock tb = new TextBlock
            {
                Text = iconText,
                FontFamily = IconFont
            };
            result.Icon = tb;
        }

        return result;
        
    }

    /// <summary>
    /// MenuItemにIconを付ける
    /// </summary>
    /// <param name="itm">MenuItem</param>
    /// <param name="imgSrc">表示するImageSource</param>
    public static void SetImageSource(this MenuItem itm, ImageSource imgSrc)
    {
        if (imgSrc != null)
        {
            Image img = new Image
            {
                Source = imgSrc,
                Width = 16,
                Height = 16,
                Margin = new Thickness(0)
            };
            itm.Icon = img;
        }
    }
}
