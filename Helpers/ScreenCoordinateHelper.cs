using System.Runtime.InteropServices;
using System.Windows;

namespace Listhing.Helpers;

/// <summary>
/// Win32の物理スクリーン座標とWPFのDIP座標を変換するヘルパー。
/// </summary>
public static class ScreenCoordinateHelper
{
    private const int MonitorDefaultToNearest = 2;
    private const double DefaultDpi = 96.0;

    /// <summary>
    /// 物理ピクセルのスクリーン座標をWPFのDIP座標へ変換する。
    /// </summary>
    public static Point PhysicalToDip(System.Drawing.Point point)
    {
        var dpi = GetDpiForPoint(point);
        return new Point(point.X * DefaultDpi / dpi, point.Y * DefaultDpi / dpi);
    }

    /// <summary>
    /// 指定座標が属するモニターのDPIを取得する。
    /// </summary>
    private static double GetDpiForPoint(System.Drawing.Point point)
    {
        var monitor = MonitorFromPoint(new NativePoint(point.X, point.Y), MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero) return DefaultDpi;

        return GetDpiForMonitor(monitor, MonitorDpiType.EffectiveDpi, out var dpiX, out _) == 0
            ? dpiX
            : DefaultDpi;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(NativePoint pt, int flags);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hmonitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativePoint(int x, int y)
    {
        public readonly int X = x;
        public readonly int Y = y;
    }

    private enum MonitorDpiType
    {
        EffectiveDpi = 0,
    }
}
