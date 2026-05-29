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
            _item?.Dispose();
            _item = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayText));
            OnPropertyChanged(nameof(IconText));
            OnPropertyChanged(nameof(FileIcon));
            OnPropertyChanged(nameof(FileLabel));
            OnPropertyChanged(nameof(IsText));
            OnPropertyChanged(nameof(IsFile));
            OnPropertyChanged(nameof(HasContent));
            OnPropertyChanged(nameof(IsImageFile));
            OnPropertyChanged(nameof(Thumbnail));
        }
    }

    /// <summary>テキスト表示用（文字列アイテムの場合）</summary>
    public string? DisplayText => _item?.Text;

    /// <summary>ファイルアイコン絵文字（複数ファイルはStack、単一は空）</summary>
    public string IconText
    {
        get
        {
            if (_item?.AllFiles is { Count: > 1 })
                return AppConstants.IconTexts.Stack;
            return string.Empty;
        }
    }

    /// <summary>単一ファイルのシェルアイコン（単一ファイル時のみ）</summary>
    public ImageSource? FileIcon
    {
        get
        {
            if (_item?.AllFiles is not { Count: 1 }) return null;
            return ShellIconHelper.GetIcon(_item.AllFiles[0]);
        }
    }

    /// <summary>ファイル数表示テキスト（単一ファイルはファイル名、複数は件数）</summary>
    public string FileLabel
    {
        get
        {
            var files = _item?.AllFiles;
            if (files is null or { Count: 0 }) return string.Empty;
            if (files.Count == 1)
                return Path.GetFileName(files[0]) ?? files[0];
            return $"{files.Count} files";
        }
    }

    public bool IsText => _item?.HasText == true && !(_item?.HasFiles == true);
    public bool IsFile => _item?.HasFiles == true;
    public bool HasContent => _item != null;

    private static readonly HashSet<string> ImageExtensions =
        [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif", ".ico"];

    /// <summary>単一画像ファイルかどうか</summary>
    public bool IsImageFile
    {
        get
        {
            if (_item?.AllFiles is not { Count: 1 }) return false;
            var ext = Path.GetExtension(_item.AllFiles[0]).ToLowerInvariant();
            return ImageExtensions.Contains(ext);
        }
    }

    /// <summary>画像ファイルのサムネイル（画像以外はnull）</summary>
    public ImageSource? Thumbnail
    {
        get
        {
            if (!IsImageFile || _item?.AllFiles is null) return null;
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(_item.AllFiles[0]);
                bmp.DecodePixelWidth = 256;
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch
            {
                return null;
            }
        }
    }

    private bool _isDropTarget;
    public bool IsDropTarget
    {
        get => _isDropTarget;
        set { _isDropTarget = value; OnPropertyChanged(); }
    }


    private bool _pinned;
    public bool Pinned
    {
        get => _pinned;
        set
        {
            _pinned = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PinnedIconText));
        }
    }

    /// <summary>ピン留め状態を示すアイコン文字</summary>
    public string PinnedIconText => _pinned ? "" : "";
}
