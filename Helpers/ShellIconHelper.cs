using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Listhing.Helpers;

/// <summary>
/// Shell APIを使ってファイルのアイコンを取得するヘルパー
/// </summary>
public static class ShellIconHelper
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private const uint SHGFI_ICON = 0x000000100;
    private const uint SHGFI_LARGEICON = 0x000000000;
    private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
    private const uint FILE_ATTRIBUTE_NORMAL = 0x80;

    /// <summary>
    /// ファイルパスからシェルアイコンを取得する。取得失敗時は null を返す
    /// </summary>
    public static ImageSource? GetIcon(string filePath)
    {
        var shfi = new SHFILEINFO();
        var result = SHGetFileInfo(filePath, FILE_ATTRIBUTE_NORMAL, ref shfi,
            (uint)Marshal.SizeOf(shfi), SHGFI_ICON | SHGFI_LARGEICON);

        if (result == IntPtr.Zero || shfi.hIcon == IntPtr.Zero)
            return null;

        try
        {
            var icon = Icon.FromHandle(shfi.hIcon);
            var bitmap = Imaging.CreateBitmapSourceFromHIcon(
                shfi.hIcon,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
        finally
        {
            DestroyIcon(shfi.hIcon);
        }
    }
}
