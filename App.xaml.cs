using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Shell;
using Listhing.Helpers;
using Listhing.Models;
using System.Windows.Controls;
using Listhing.Features.TransparentWindow;
using Listhing.Services;
using Listhing.ViewModels;
using Hardcodet.Wpf.TaskbarNotification;

namespace Listhing;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    #region COM API Declarations

    /// <summary>
    /// COMライブラリを初期化します
    /// </summary>
    [DllImport("ole32.dll")]
    private static extern int CoInitializeEx(IntPtr pvReserved, uint dwCoInit);

    /// <summary>
    /// COMライブラリを解放します
    /// </summary>
    [DllImport("ole32.dll")]
    private static extern void CoUninitialize();

    /// <summary>
    /// COINITフラグ
    /// </summary>
    private enum COINIT : uint
    {
        /// <summary>マルチスレッドアパートメント</summary>
        COINIT_MULTITHREADED = 0x0,
        /// <summary>シングルスレッドアパートメント</summary>
        COINIT_APARTMENTTHREADED = 0x2,
        /// <summary>OLE 1.0の動作を無効化</summary>
        COINIT_DISABLE_OLE1DDE = 0x4,
        /// <summary>高速割り込み無効化</summary>
        COINIT_SPEED_OVER_MEMORY = 0x8
    }

    #endregion

    /// <summary>
    /// 多重起動防止
    /// </summary>
    private static readonly string MutexName = "hetima-Listhing-Mutex-Name";
    private static Mutex? _mutex;


    /// <summary>
    /// ワークスペース名のリスト
    /// </summary>
    public static List<string> WorkspaceNames { get; set; } = new List<string>();

    /// <summary>
    /// Settings service instance (singleton-like)
    /// </summary>
    public static SettingsService SettingsService { get; private set; } = null!;

    public static MouseHookService MouseHookService { get; private set; } = null!;

    private static TransparentWindow? _transparentWindow;
    private static DispatcherTimer? _activationDelayTimer;

    public static TaskbarIcon? TrayIcon { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public App()
    {
        // COMライブラリを初期化（SHOpenFolderAndSelectItemsなどで使用）
        CoInitializeEx(IntPtr.Zero, (uint)(COINIT.COINIT_APARTMENTTHREADED | COINIT.COINIT_DISABLE_OLE1DDE));


        // Initialize services
        SettingsService = new SettingsService();
        MouseHookService = new MouseHookService(Dispatcher);

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

    }

    /// <summary>
    /// Application startup handler
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            _mutex = new Mutex(true, MutexName, out bool createdNew);
            if (!createdNew)
            {
                // すでに起動している場合
                _mutex.Close();
                _mutex = null;
                Shutdown();
                return;
            }
        }
        catch (AbandonedMutexException)
        {
            // Mutexが放棄されている場合は無視して続行
            _mutex = new Mutex(true, MutexName, out bool createdNew);
            if (!createdNew)
            {
                // すでに起動している場合
                _mutex.Close();
                _mutex = null;
                Shutdown();
                return;
            }
        }


        // Load settings
        SettingsService.Load();

        // Apply language culture
        ApplyLanguage(SettingsService.Settings.Language);

        base.OnStartup(e);
    }

    /// <summary>
    /// JumpListにワークスペースを追加します
    /// </summary>
    private void UpdateJumpList()
    {
        try
        {
            // アプリケーションの実行ファイルパスを取得
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath))
            {
                return;
            }

            // JumpListを作成
            var jumpList = new JumpList();
            jumpList.ShowRecentCategory = false;
            jumpList.ShowFrequentCategory = false;

            // ワークスペースごとにJumpList項目を作成
            foreach (var workspaceName in WorkspaceNames)
            {
                var jumpTask = new JumpTask
                {
                    Title = workspaceName,
                    Description = $"Open {workspaceName} workspace",
                    ApplicationPath = exePath,
                    Arguments = $"--workspace \"{workspaceName}\"",
                    IconResourcePath = exePath,
                    IconResourceIndex = 0
                };
                jumpList.JumpItems.Add(jumpTask);
            }

            // JumpListを適用
            JumpList.SetJumpList(Application.Current, jumpList);
        }
        catch (Exception ex)
        {
            // JumpListの更新に失敗してもアプリケーションは続行
            System.Diagnostics.Debug.WriteLine($"Failed to update JumpList: {ex.Message}");
        }
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // TRAY ICON
        Version? _version = Assembly.GetExecutingAssembly().GetName().Version;
        string _vstring = _version?.ToString(3) ?? "0.0.0";
        TrayIcon = FindResource("TrayIcon") as TaskbarIcon;
        if (TrayIcon != null)
        {
            TrayIcon.ToolTipText = string.Format("{0} v{1}", AppConstants.AppName, _vstring);
        }
        bool anyWindowOpen = false;

        if (!anyWindowOpen)
        {
            // リストがnullか空の場合やワークスペースが存在しなかった場合はデフォルトワークスペースを開く
            CreateMainWindow(AppConstants.AppName);
        }

        _transparentWindow = new TransparentWindow();
        _transparentWindow.DataDropped += OnTransparentWindowDataDropped;

        var tg = SettingsService.Settings.TransparentGuard;
        _transparentWindow.ApplySettings(tg.Size, tg.ShowPattern);
        MouseHookService.ApplySettings(tg.ActivationPixels, tg.DismissDelayMs);

        SettingsService.TransparentGuardChanged += OnTransparentGuardChanged;

        _activationDelayTimer = new System.Windows.Threading.DispatcherTimer();
        _activationDelayTimer.Tick += OnActivationDelayTick;

        MouseHookService.EarlyCaptureRequested += OnEarlyCaptureRequested;
        MouseHookService.DragEnded += OnDragEnded;

        MouseHookService.Start();
    }

    private static void OnTransparentGuardChanged(object? sender, EventArgs e)
    {
        var tg = SettingsService.Settings.TransparentGuard;
        _transparentWindow?.ApplySettings(tg.Size, tg.ShowPattern);
        MouseHookService.ApplySettings(tg.ActivationPixels, tg.DismissDelayMs);
    }

    private static void OnActivationDelayTick(object? sender, EventArgs e)
    {
        _activationDelayTimer?.Stop();
        if (_transparentWindow != null)
            _transparentWindow.AllowDrop = true;
    }

    private static void OnEarlyCaptureRequested(object? sender, System.Drawing.Point point)
    {
        var tg = SettingsService.Settings.TransparentGuard;
        if (!tg.IsEnabled) return;

        if (_transparentWindow == null) return;

        int delayMs = tg.ActivationDelayMs;
        if (delayMs > 0)
        {
            _transparentWindow.AllowDrop = false;
            _activationDelayTimer!.Interval = TimeSpan.FromMilliseconds(delayMs);
            _activationDelayTimer.Stop();
            _activationDelayTimer.Start();
        }
        else
        {
            _transparentWindow.AllowDrop = true;
        }

        _transparentWindow.ShowNearPoint(point);
    }

    private static void OnDragEnded(object? sender, EventArgs e)
    {
        _activationDelayTimer?.Stop();
        _transparentWindow?.Hide();
    }

    private static void OnTransparentWindowDataDropped(object? sender, IDataObject data)
    {
        // TODO: ドロップ受け皿ウィンドウを表示してデータを渡す
    }


    private void Setting_Click(object sender, EventArgs e)
    {
        ShowSettingsWindow();
    }

    private void GitHub_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://github.com/hetima/TakeMePop") { UseShellExecute = true });
    }
    private void Quit_Click(object sender, EventArgs e)
    {
        Shutdown();
    }
    
    public static bool CreateMainWindow(string title)
    {

        MainWindow? mainWindow = null;
        if (mainWindow != null)
        {
            mainWindow.Activate();
            return true;
        }

        mainWindow = new MainWindow(title);
        mainWindow.FontSize = SettingsService.Settings.FontSize;
        mainWindow.Show();
        // Show()した後に呼ぶ
        ApplyTheme(SettingsService.Settings.Theme, mainWindow);
        return true;
    }

    /// <summary>
    /// SettingsWindowを表示する
    /// </summary>
    public static void ShowSettingsWindow()
    {
        foreach (var win in Application.Current.Windows.OfType<SettingsWindow>())
        {
            win.Activate();
            return;
        }

        SettingsWindow settingsWindow = new SettingsWindow();
        settingsWindow.FontSize = SettingsService.Settings.FontSize;
        settingsWindow.Show();
        ApplyTheme(SettingsService.Settings.Theme, settingsWindow);
    }


    /// <summary>
    /// Apply theme based on AppTheme setting
    /// </summary>
    /// <param name="theme">Theme setting</param>
    /// <param name="specificWindow">このウィンドウにだけ適用</param>
    public static void ApplyTheme(ThemeMode theme, Window? specificWindow = null)
    {
        ThemeMode actualTheme = theme;
        SolidColorBrush focusBrush = new(Colors.DarkGray);

        if (specificWindow != null)
        {
            specificWindow.ThemeMode = actualTheme;
            specificWindow.Resources["KeyboardFocusBorderColorBrush"] = focusBrush;
        }
        else
        {
            Application.Current.ThemeMode = actualTheme;
            Application.Current.Resources["KeyboardFocusBorderColorBrush"] = focusBrush;
            foreach (var window in Application.Current.Windows.OfType<Window>())
            {
                window.ThemeMode = actualTheme;
                window.Resources["KeyboardFocusBorderColorBrush"] = focusBrush;
            }
        }
    }

    /// <summary>
    /// Apply font size to main window
    /// </summary>
    /// <param name="fontSize">Font size (8-48)</param>
    public static void ApplyFontSize(double fontSize)
    {
        // 8より小さい、または40より大きい場合は14にする
        if (fontSize < 8 || fontSize > 40)
        {
            fontSize = 14;
        }

        foreach (var mainWindow in Application.Current.Windows.OfType<MainWindow>())
        {
            mainWindow.FontSize = fontSize;
        }
    }

    /// <summary>
    /// Apply language culture
    /// </summary>
    /// <param name="language">Language setting (null for auto-detection)</param>
    private static void ApplyLanguage(AppLanguage? language)
    {
        var cultureName = language switch
        {
            AppLanguage.Ja => "ja",
            AppLanguage.En => "en",
            null => null, // Use Windows setting
            _ => "en"
        };

        if (cultureName != null)
        {
            var culture = new System.Globalization.CultureInfo(cultureName);
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
        }
        // If null, the default culture (Windows setting) is used
    }

    /// <summary>
    /// アプリケーションのすべてのデータを保存する
    /// </summary>
    public static void SaveAll()
    {
        foreach (var mainWindow in Application.Current.Windows.OfType<MainWindow>())
        {
            var dc = mainWindow.DataContext as MainWindowViewModel;
            dc?.SaveWindowSettings(mainWindow);
        }

        // 設定を保存
        SettingsService.Save();
    }

    /// <summary>
    /// SaveOpenedWorkspacesを実行してよいかどうか
    /// </summary>
    public static bool SaveOpenedWorkspacesLock { get; set; } = false;

    /// <summary>
    /// 開いているウィンドウを保存する
    /// </summary>
    public static void SaveOpenedWorkspaces()
    {


    }


    /// <summary>
    /// Application exit handler
    /// </summary>
    protected override void OnExit(ExitEventArgs e)
    {
        SaveOpenedWorkspaces();
        MouseHookService?.Dispose();
        _transparentWindow?.Close();
        TrayIcon?.Dispose();
        // COMライブラリを解放
        CoUninitialize();

        // Mutexを解放
        if (_mutex != null)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // Mutexを所有していない場合は無視
            }
            finally
            {
                _mutex.Close();
            }
        }

        base.OnExit(e);
    }
}

