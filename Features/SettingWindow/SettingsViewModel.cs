using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Linq;
using System.Windows.Media;
using Listhing.Helpers;
using Listhing.Models;
using Listhing.Services;
using System.Windows;

namespace Listhing.ViewModels;

/// <summary>
/// SettingsTab ViewModel
/// </summary>
public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly SettingsService _settingsService;
    private readonly ModalService _modalService;
    private readonly ThemeMode _originalTheme;
    private readonly AppLanguage? _originalLanguage;
    private readonly double _originalFontSize;
    private ThemeMode _selectedTheme;
    private AppLanguage _selectedLanguage;
    private double _fontSize;
    private ObservableCollection<string>? _allFontNames;

    /// <summary>
    /// PropertyChangedEvent
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="settingsService">Settings service</param>
    /// <param name="modalService">Modal service</param>
    public SettingsViewModel(SettingsService settingsService, ModalService modalService)
    {
        _settingsService = settingsService;
        _modalService = modalService ?? throw new ArgumentNullException(nameof(modalService));
        _originalTheme = settingsService.Settings.Theme;
        _originalLanguage = settingsService.Settings.Language;
        _originalFontSize = settingsService.Settings.FontSize;
        _selectedTheme = settingsService.Settings.Theme;
        _selectedLanguage = settingsService.Settings.Language ?? AppLanguage.En;
        _fontSize = settingsService.Settings.FontSize;
        
        // ターミナルフォント名リストを初期化
        var fontNames = new List<string>
        {
            "Consolas",
            "Courier New",
            "BIZ UDGothic",
            "HackGen35",
            "HackGen Console",
            "IBM Plex Mono",
            "UDEV Gothic",
            "UDEV Gothic 35JPDOC",
        };
        
        // システムにインストールされているフォントのみを残す
        var installedFonts = Fonts.SystemFontFamilies.Select(f => f.Source).ToHashSet(StringComparer.OrdinalIgnoreCase);
        
        var availableFonts = fontNames.Where(f => installedFonts.Contains(f)).OrderBy(f => f).ToList();

        // 全フォントリストを初期化（インストールされているフォントのみ）
        _allFontNames = new ObservableCollection<string>(
            Fonts.SystemFontFamilies
                .Select(f => f.Source)
                .OrderBy(f => f)
                .ToList());
    }

    /// <summary>
    /// Selected theme
    /// </summary>
    public ThemeMode SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (_selectedTheme != value)
            {
                _selectedTheme = value;
                _settingsService.Settings.Theme = value;
                _settingsService.Save();
                App.ApplyTheme(value);
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Selected language
    /// </summary>
    public AppLanguage SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (_selectedLanguage != value)
            {
                _selectedLanguage = value;
                _settingsService.Settings.Language = value;
                _settingsService.Save();
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsRestartRequired));
            }
        }
    }

    /// <summary>
    /// Font size (8-48)
    /// </summary>
    public double FontSize
    {
        get => _fontSize;
        set
        {
            if (_fontSize != value)
            {
                _fontSize = value;
                _settingsService.Settings.FontSize = value;
                App.ApplyFontSize(value);
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Whether restart is required (theme or language changed)
    /// </summary>
    public bool IsRestartRequired => _selectedTheme != _originalTheme || _selectedLanguage != _originalLanguage;

    /// <summary>
    /// デフォルトのアイテムを開くショートカットキー
    /// </summary>
    public ShortcutKey DefaultOpenKey => _settingsService.Settings.ShortcutSettings.DefaultOpenKey;

    /// <summary>
    /// アイテムの場所を表示するショートカットキー
    /// </summary>
    public ShortcutKey RevealKey => _settingsService.Settings.ShortcutSettings.RevealKey;


    private TransparentGuardSettings Tg => _settingsService.Settings.TransparentGuard;

    public bool TgIsEnabled
    {
        get => Tg.IsEnabled;
        set { if (Tg.IsEnabled != value) { Tg.IsEnabled = value; _settingsService.Save(); _settingsService.NotifyTransparentGuardChanged(); OnPropertyChanged(); } }
    }

    public double TgSize
    {
        get => Tg.Size;
        set { if (Tg.Size != value) { Tg.Size = value; _settingsService.Save(); _settingsService.NotifyTransparentGuardChanged(); OnPropertyChanged(); } }
    }

    public int TgActivationPixels
    {
        get => Tg.ActivationPixels;
        set { if (Tg.ActivationPixels != value) { Tg.ActivationPixels = value; _settingsService.Save(); _settingsService.NotifyTransparentGuardChanged(); OnPropertyChanged(); } }
    }

    public int TgDismissDelayMs
    {
        get => Tg.DismissDelayMs;
        set { if (Tg.DismissDelayMs != value) { Tg.DismissDelayMs = value; _settingsService.Save(); _settingsService.NotifyTransparentGuardChanged(); OnPropertyChanged(); } }
    }

    /// <summary>
    /// ショートカットキーの表示を更新する
    /// </summary>
    public void RefreshShortcutKeys()
    {
        OnPropertyChanged(nameof(DefaultOpenKey));
        OnPropertyChanged(nameof(RevealKey));
    }

    /// <summary>
    /// OnPropertyChanged
    /// </summary>
    /// <param name="propertyName">Property name</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}
