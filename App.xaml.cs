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
using Listhing.Features.PopWindow;
using Listhing.Features.ToastWindow;
using Listhing.Features.QuickHistoryWindow;
using Listhing.Features.QuickTextWindow;
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
    private static readonly string MutexName = "hetima-TakeMePop-Mutex-Name";
    private static Mutex? _mutex;


    /// <summary>
    /// ワークスペース名のリスト
    /// </summary>
    public static List<string> WorkspaceNames { get; set; } = new List<string>();

    /// <summary>
    /// Settings service instance (singleton-like)
    /// </summary>
    public static SettingsService SettingsService { get; private set; } = null!;

    public static GlobalHookService GlobalHookService { get; private set; } = null!;
    public static MouseHookService MouseHookService { get; private set; } = null!;
    public static KeyboardHookService KeyboardHookService { get; private set; } = null!;
    public static ClipboardService ClipboardService { get; private set; } = null!;

    private static TransparentWindow? _transparentWindow;
    private static readonly List<PopWindow> _popWindows = new();
    private static QuickHistoryWindow? _quickHistoryWindow;
    private static QuickTextWindow? _quickTextWindow;

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
        GlobalHookService = new GlobalHookService();
        MouseHookService = new MouseHookService(GlobalHookService, Dispatcher);

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

        // 前回終了時に残った仮想ファイル用の一時ファイルを削除
        ClipboardItem.CleanupTempFiles();

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

    /// <summary>
    /// アプリケーション起動時の初期化処理
    /// </summary>
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

        // HwndSource を使うため WPF 初期化後に生成する
        ClipboardService = new ClipboardService();
        KeyboardHookService = new KeyboardHookService(GlobalHookService, Dispatcher, ClipboardService);

        _transparentWindow = new TransparentWindow();

        var tg = SettingsService.Settings.TransparentGuard;
        _transparentWindow.ApplySettings(tg.Size);
        MouseHookService.ApplySettings(tg.ActivationPixels, tg.DismissDelayMs);

        SettingsService.TransparentGuardChanged += OnTransparentGuardChanged;

        MouseHookService.EarlyCaptureRequested += OnEarlyCaptureRequested;
        MouseHookService.DragEnded += OnDragEnded;

        GlobalHookService.Start();

        KeyboardHookService.CtrlCDoubleTapped += OnCtrlCDoubleTapped;
        KeyboardHookService.CtrlXDoubleTapped += OnCtrlXDoubleTapped;

        ApplyHotkeySettings();
    }

    /// <summary>
    /// ShortcutSettings のグローバルホットキーを KeyboardHookService に登録する。
    /// 設定変更時にも呼び出して再適用する。
    /// </summary>
    public static void ApplyHotkeySettings()
    {
        var shortcuts = SettingsService.Settings.ShortcutSettings;

        // 変更前の古いキーが残らないよう、一度すべて解除してから登録し直す
        KeyboardHookService.ClearHotkeys();

        if (!shortcuts.DefaultOpenKey.IsEmpty)
            KeyboardHookService.RegisterHotkey(shortcuts.DefaultOpenKey, OnDefaultOpenHotkey);

        if (!shortcuts.QuickHistoryKey.IsEmpty)
        {
            KeyboardHookService.RegisterQuickHistoryHotkey(shortcuts.QuickHistoryKey, ToggleQuickHistoryWindow);
            KeyboardHookService.QuickHistorySelectNext = ShowOrSelectNextQuickHistory;
            KeyboardHookService.QuickHistoryCommit = CommitQuickHistorySelection;
            KeyboardHookService.QuickHistoryCancel = CancelQuickHistory;
        }
    }

    /// <summary>
    /// Quick History ウィンドウの表示・非表示を切り替える。
    /// </summary>
    /// <summary>
    /// QuickText ウィンドウをマウスカーソル付近に表示する。
    /// </summary>
    public static void ShowQuickTextWindow()
    {
        if (_quickTextWindow == null)
        {
            var qt = SettingsService.Settings.QuickTextWindow;
            _quickTextWindow = new QuickTextWindow
            {
                FontSize = SettingsService.Settings.FontSize,
                Width = qt.Width,
                Height = qt.Height
            };
            ApplyTheme(SettingsService.Settings.Theme, _quickTextWindow);
            _quickTextWindow.ApplyPopupFontFamily(SettingsService.Settings.PopupFontFamily);
        }

        // すでに表示中なら位置を動かさない（新規表示時のみマウス付近に配置）
        if (!_quickTextWindow.IsVisible)
        {
            GetCursorPos(out var pos);
            var dipPos = ScreenCoordinateHelper.PhysicalToDip(pos);
            _quickTextWindow.Left = dipPos.X - _quickTextWindow.Width / 2;
            _quickTextWindow.Top  = dipPos.Y - _quickTextWindow.Height / 2;
        }
        _quickTextWindow.Show();
    }

    public static void ToggleQuickHistoryWindow()
    {
        if (_quickHistoryWindow == null)
        {
            _quickHistoryWindow = new QuickHistoryWindow
            {
                FontSize = SettingsService.Settings.FontSize
            };
            ApplyTheme(SettingsService.Settings.Theme, _quickHistoryWindow);
            _quickHistoryWindow.ApplyPopupFontFamily(SettingsService.Settings.PopupFontFamily);
        }

        if (_quickHistoryWindow.IsVisible)
            _quickHistoryWindow.Hide();
        else
            _quickHistoryWindow.Show();
    }

    /// <summary>
    /// QuickHistory ウィンドウを表示中なら選択を次のアイテムへ進める。
    /// </summary>
    public static void ShowOrSelectNextQuickHistory()
    {
        if (_quickHistoryWindow == null)
        {
            _quickHistoryWindow = new QuickHistoryWindow
            {
                FontSize = SettingsService.Settings.FontSize
            };
            ApplyTheme(SettingsService.Settings.Theme, _quickHistoryWindow);
        }

        if (!_quickHistoryWindow.IsVisible)
        {
            _quickHistoryWindow.Show();
            return;
        }

        _quickHistoryWindow.SelectNext();
    }

    /// <summary>
    /// QuickHistory ウィンドウの選択中アイテムをコピー＆ペーストして閉じる。
    /// </summary>
    public static void CommitQuickHistorySelection()
    {
        _quickHistoryWindow?.CommitSelection();
    }

    /// <summary>
    /// 表示件数設定の変更などを QuickHistory ウィンドウのリストへ反映する。
    /// </summary>
    public static void RefreshQuickHistoryItems()
    {
        _quickHistoryWindow?.RefreshItems();
    }

    /// <summary>
    /// モデファイキーセレクトを対象キー以外でキャンセルし、パネルを閉じる。
    /// </summary>
    public static void CancelQuickHistory()
    {
        if (_quickHistoryWindow != null && _quickHistoryWindow.IsVisible)
            _quickHistoryWindow.Hide();
    }

    /// <summary>
    /// DefaultOpenKey ホットキー押下時の処理。
    /// 既存の PopWindow があればマウスカーソル位置へ移動し、なければ新規作成する。
    /// </summary>
    private static void OnDefaultOpenHotkey()
    {
        GetCursorPos(out var pos);
        var dipPos = ScreenCoordinateHelper.PhysicalToDip(pos);
        var existing = _popWindows.FirstOrDefault();
        if (existing != null)
        {
            var pw = SettingsService.Settings.PopWindow;
            existing.Left = dipPos.X - pw.Width / 2;
            existing.Top  = dipPos.Y - pw.Height / 2;
            existing.Activate();
        }
        else
        {
            CreatePopWindow(pos);
        }
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out System.Drawing.Point lpPoint);


    private static void OnCtrlXDoubleTapped(object? sender, EventArgs e)
    {
        ShowQuickTextWindow();
    }

    /// <summary>
    /// Ctrl+C ダブルタップ時にクリップボード内容を PopWindow へ表示する。
    /// </summary>
    private static void OnCtrlCDoubleTapped(object? sender, EventArgs e)
    {
        var item = ClipboardService.History.Count > 0 ? ClipboardService.History[^1] : null;
        if (item != null && item.HasText && !item.HasFiles)
        {
            ShowQuickTextWindow();
            return;
        }
        GetCursorPos(out var pos);
        CreatePopWindow(pos, item);
    }

    /// <summary>
    /// TransparentGuard 設定変更時に各サービスへ再適用する。
    /// </summary>
    private static void OnTransparentGuardChanged(object? sender, EventArgs e)
    {
        var tg = SettingsService.Settings.TransparentGuard;
        _transparentWindow?.ApplySettings(tg.Size);
        MouseHookService.ApplySettings(tg.ActivationPixels, tg.DismissDelayMs);
    }

    /// <summary>
    /// ドラッグ開始後の早期キャプチャ要求時に TransparentWindow をポインタ近くへ表示する。
    /// </summary>
    private static void OnEarlyCaptureRequested(object? sender, System.Drawing.Point point)
    {
        var tg = SettingsService.Settings.TransparentGuard;
        if (!tg.IsEnabled) return;

        _transparentWindow?.ShowNearPoint(point);
    }

    /// <summary>
    /// ドラッグ終了時に TransparentWindow を非表示にする。
    /// </summary>
    private static void OnDragEnded(object? sender, EventArgs e)
    {
        _transparentWindow?.Hide();
    }

    public static void CreatePopWindow(System.Drawing.Point nearPoint, ClipboardItem? item = null)
    {
        // Pinned=false のウィンドウを再利用（最大1個）
        var existing = _popWindows.FirstOrDefault(w => !w.Pinned);
        if (existing != null)
        {
            if (item != null) existing.SetItem(item);
            existing.Activate();
            return;
        }

        var pw = SettingsService.Settings.PopWindow;
        var win = new PopWindow();
        var dipPoint = ScreenCoordinateHelper.PhysicalToDip(nearPoint);
        win.Width = pw.Width;
        win.Height = pw.Height;
        win.Left = dipPoint.X - pw.Width / 2;
        win.Top = dipPoint.Y - pw.Height / 2;
        _popWindows.Add(win);
        win.FontSize = SettingsService.Settings.FontSize;
        win.Show();
        if (item != null) win.SetItem(item);
        ApplyTheme(SettingsService.Settings.Theme, win);
    }

    /// <summary>
    /// トースト通知を表示する。
    /// </summary>
    /// <param name="text">表示テキスト</param>
    /// <param name="fontSize">フォントサイズ（省略時 13）</param>
    /// <param name="durationMs">表示時間ミリ秒（省略時 3000）</param>
    /// <param name="position">表示位置（省略時 ScreenBottom）</param>
    public static void ShowToast(string text, double fontSize = 13, int durationMs = 3000, ToastPosition position = ToastPosition.ScreenBottom, string? iconText = null)
    {
        ToastWindow.Show(text, fontSize, durationMs, position, iconText);
    }

    /// <summary>
    /// 管理リストから PopWindow を削除する（ウィンドウのクローズ時に呼ばれる）。
    /// </summary>
    public static void RemovePopWindow(PopWindow w)
    {
        _popWindows.Remove(w);
    }

    /// <summary>
    /// PopWindow のサイズを設定に保存する。
    /// </summary>
    public static void SavePopWindowSize(double width, double height)
    {
        SettingsService.Settings.PopWindow.Width = width;
        SettingsService.Settings.PopWindow.Height = height;
        SettingsService.Save();
    }

    /// <summary>
    /// QuickTextWindow のサイズを設定に保存する。
    /// </summary>
    /// <summary>
    /// QuickTextWindow のサイズをメモリ上の設定に反映する（ファイル保存はアプリ終了時）。
    /// </summary>
    public static void UpdateQuickTextWindowSize(double width, double height)
    {
        SettingsService.Settings.QuickTextWindow.Width = width;
        SettingsService.Settings.QuickTextWindow.Height = height;
    }


    /// <summary>
    /// トレイアイコンの「設定」メニュークリック時に設定ウィンドウを開く。
    /// </summary>
    private void Setting_Click(object sender, EventArgs e)
    {
        ShowSettingsWindow();
    }

    /// <summary>
    /// トレイアイコンの「GitHub」メニュークリック時にブラウザでリポジトリを開く。
    /// </summary>
    private void GitHub_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://github.com/hetima/TakeMePop") { UseShellExecute = true });
    }
    /// <summary>
    /// トレイアイコンの「終了」メニュークリック時にアプリを終了する。
    /// </summary>
    private void Quit_Click(object sender, EventArgs e)
    {
        Shutdown();
    }
    
    /// <summary>
    /// MainWindowを表示する。既に開いていれば前面に出す。
    /// </summary>
    public static void ShowMainWindow()
    {
        foreach (var win in Application.Current.Windows.OfType<MainWindow>())
        {
            win.Activate();
            return;
        }

        var mainWindow = new MainWindow();
        mainWindow.FontSize = SettingsService.Settings.FontSize;
        mainWindow.Show();
        // Show()した後に呼ぶ
        ApplyTheme(SettingsService.Settings.Theme, mainWindow);
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

        foreach (var window in _popWindows)
            window.FontSize = fontSize;

        if (_quickTextWindow != null)
            _quickTextWindow.FontSize = fontSize;

        if (_quickHistoryWindow != null)
        {
            _quickHistoryWindow.FontSize = fontSize;
            _quickHistoryWindow.UpdateIconColumnWidth(fontSize);
        }
    }

    /// <summary>
    /// QuickTextWindow・QuickHistoryWindow にフォントを適用する。
    /// 空文字列の場合はフォントをリセット（システムデフォルトへ）する。
    /// </summary>
    public static void ApplyPopupFontFamily(string fontFamily)
    {
        _quickTextWindow?.ApplyPopupFontFamily(fontFamily);
        _quickHistoryWindow?.ApplyPopupFontFamily(fontFamily);
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
    /// Application exit handler
    /// </summary>
    protected override void OnExit(ExitEventArgs e)
    {
        SettingsService.Save();
        MouseHookService?.Dispose();
        GlobalHookService?.Dispose();
        ClipboardService?.Dispose();
        _transparentWindow?.Close();
        _quickHistoryWindow?.ForceClose();
        _quickTextWindow?.ForceClose();
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

