using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Listhing.Helpers;
using Listhing.Models;
using Listhing.Services;

namespace Listhing.Converters;

/// <summary>
/// ThemeMode struct to ComboBox index converter
/// ComboBox items: 0=System, 1=Light, 2=Dark
/// </summary>
public class ThemeToIndexConverter : IValueConverter
{
    private static readonly ThemeMode[] IndexToTheme = [ThemeMode.System, ThemeMode.Light, ThemeMode.Dark];

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ThemeMode tm)
        {
            if (tm == ThemeMode.None || tm == ThemeMode.System) return 0;
            if (tm == ThemeMode.Light) return 1;
            if (tm == ThemeMode.Dark) return 2;
        }
        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int index && index >= 0 && index < IndexToTheme.Length)
        {
            return IndexToTheme[index];
        }
        return ThemeMode.System;
    }
}

/// <summary>
/// JSON converter for System.Windows.ThemeMode struct
/// </summary>
public class ThemeModeJsonConverter : JsonConverter<ThemeMode>
{
    public override ThemeMode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (string.IsNullOrEmpty(value)) return ThemeMode.System;
            return new ThemeMode(value);
        }
        return ThemeMode.System;
    }

    public override void Write(Utf8JsonWriter writer, ThemeMode value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

/// <summary>
/// AppLanguage enum to ComboBox index converter
/// </summary>
public class LanguageToIndexConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is AppLanguage lang)
        {
            return (int)lang;
        }
        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int index)
        {
            return (AppLanguage)index;
        }
        return AppLanguage.En;
    }
}

/// <summary>
/// LinkDropFormat enum to ComboBox index converter
/// </summary>
public class LinkDropFormatToIndexConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is LinkDropFormat fmt ? (int)fmt : 0;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int index ? (LinkDropFormat)index : LinkDropFormat.TitleAndUrl;
}

/// <summary>
/// Boolean to Visibility converter
/// </summary>
public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isVisible && isVisible)
        {
            return Visibility.Visible;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Visible;
        }
        return false;
    }
}

/// <summary>
/// Boolean to Visibility converter (Inverse)
/// </summary>
public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isVisible && !isVisible)
        {
            return Visibility.Visible;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility != Visibility.Visible;
        }
        return true;
    }
}

/// <summary>
/// ShortcutKey 用の JSON コンバータ
/// </summary>
public class ShortcutKeyJsonConverter : JsonConverter<ShortcutKey>
{
    /// <summary>
    /// JSON から ShortcutKey を読み込む
    /// </summary>
    public override ShortcutKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (string.IsNullOrWhiteSpace(stringValue))
            {
                return new ShortcutKey(Key.None);
            }
            return new ShortcutKey(stringValue);
        }
        return new ShortcutKey(Key.None);
    }

    /// <summary>
    /// ShortcutKey を JSON に書き込む
    /// </summary>
    public override void Write(Utf8JsonWriter writer, ShortcutKey value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

/// <summary>
/// ShortcutKey を文字列に変換するコンバータ
/// </summary>
public class ShortcutKeyToStringConverter : IValueConverter
{
    /// <summary>
    /// ShortcutKey を文字列に変換
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ShortcutKey shortcutKey)
        {
            return shortcutKey.ToString();
        }
        return string.Empty;
    }

    /// <summary>
    /// 文字列から ShortcutKey に変換（未実装）
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 文字列を Visibility に変換するコンバータ
/// </summary>
public class StringToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// 文字列を Visibility に変換
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && !string.IsNullOrEmpty(str))
        {
            return Visibility.Visible;
        }
        return Visibility.Collapsed;
    }

    /// <summary>
    /// Visibility を文字列に変換（未実装）
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}


/// <summary>
/// null を Collapsed に変換するコンバータ（非null → Visible）
/// </summary>
public class NullToCollapsedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value != null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// パスからファイル名を取得するコンバータ
/// </summary>
public class PathToFileNameConverter : IValueConverter
{
    /// <summary>
    /// パスからファイル名を取得
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string path && !string.IsNullOrEmpty(path))
        {
            return System.IO.Path.GetFileNameWithoutExtension(path);
        }
        return string.Empty;
    }

    /// <summary>
    /// ファイル名からパスに変換（未実装）
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

