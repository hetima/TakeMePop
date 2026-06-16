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
    /// 物理ピクセル座標を中心にウィンドウを配置するDIP座標（左上）を求め、
    /// その座標が属するモニターの作業領域からはみ出さないようクランプして返す。
    /// </summary>
    /// <param name="centerPoint">ウィンドウ中心を合わせたい物理スクリーン座標</param>
    /// <param name="width">ウィンドウ幅（DIP）</param>
    /// <param name="height">ウィンドウ高さ（DIP）</param>
    public static Point ClampedTopLeft(System.Drawing.Point centerPoint, double width, double height)
    {
        var dipCenter = PhysicalToDip(centerPoint);
        double left = dipCenter.X - width / 2;
        double top = dipCenter.Y - height / 2;

        var area = GetWorkAreaDip(centerPoint);
        // 作業領域がウィンドウより小さい場合は左上端に寄せる（Clampの引数逆転を防ぐ）
        if (area.Width >= width)
            left = Math.Clamp(left, area.Left, area.Right - width);
        else
            left = area.Left;

        if (area.Height >= height)
            top = Math.Clamp(top, area.Top, area.Bottom - height);
        else
            top = area.Top;

        return new Point(left, top);
    }

    /// <summary>
    /// 指定座標が属するモニターの作業領域（タスクバー等を除く）をDIP座標で取得する。
    /// </summary>
    private static Rect GetWorkAreaDip(System.Drawing.Point point)
    {
        var monitor = MonitorFromPoint(new NativePoint(point.X, point.Y), MonitorDefaultToNearest);
        var info = new MonitorInfo { cbSize = Marshal.SizeOf<MonitorInfo>() };
        if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref info))
        {
            // 取得失敗時はプライマリの作業領域へフォールバック
            return SystemParameters.WorkArea;
        }

        var topLeft = PhysicalToDip(new System.Drawing.Point(info.rcWork.left, info.rcWork.top));
        var bottomRight = PhysicalToDip(new System.Drawing.Point(info.rcWork.right, info.rcWork.bottom));
        return new Rect(topLeft, bottomRight);
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

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hmonitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int cbSize;
        public NativeRect rcMonitor;
        public NativeRect rcWork;
        public uint dwFlags;
    }

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
