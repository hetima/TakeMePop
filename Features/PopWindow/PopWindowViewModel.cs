using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Listhing.Helpers;
using Listhing.Services;
using Listhing.ViewModels;

namespace Listhing.Features.PopWindow;

public class PopWindowViewModel : ObservableObject
{
    private ClipboardItem? _item;

    public ClipboardItem? Item
    {
        get => _item;
        set
        {
            _item = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayText));
            OnPropertyChanged(nameof(IconText));
            OnPropertyChanged(nameof(FileIcon));
            OnPropertyChanged(nameof(FileLabel));
            OnPropertyChanged(nameof(IsText));
            OnPropertyChanged(nameof(IsFile));
            OnPropertyChanged(nameof(HasContent));
        }
    }

    /// <summary>テキスト表示用（文字列アイテムの場合）</summary>
    public string? DisplayText => _item?.Text;

    /// <summary>ファイルアイコン絵文字（複数ファイルはStack、単一は空）</summary>
    public string IconText
    {
        get
        {
            if (_item?.Files is { Count: > 1 })
                return AppConstants.IconTexts.Stack;
            return string.Empty;
        }
    }

    /// <summary>単一ファイルのシェルアイコン（単一ファイル時のみ）</summary>
    public ImageSource? FileIcon
    {
        get
        {
            if (_item?.Files is not { Count: 1 }) return null;
            return ShellIconHelper.GetIcon(_item.Files[0]);
        }
    }

    /// <summary>ファイル数表示テキスト（単一ファイルはファイル名、複数は件数）</summary>
    public string FileLabel
    {
        get
        {
            if (_item?.Files is null) return string.Empty;
            if (_item.Files.Count == 1)
                return Path.GetFileName(_item.Files[0]) ?? _item.Files[0];
            return $"{_item.Files.Count} files";
        }
    }

    public bool IsText => _item?.HasText == true && !(_item?.HasFiles == true);
    public bool IsFile => _item?.HasFiles == true;
    public bool HasContent => _item != null;

    private bool _isDropTarget;
    public bool IsDropTarget
    {
        get => _isDropTarget;
        set { _isDropTarget = value; OnPropertyChanged(); }
    }
}
