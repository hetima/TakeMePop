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

/// <summary>フォント選択リストの1項目（表示名と実値のペア）</summary>
public record FontFamilyItem(string DisplayName, string Value);

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
    private readonly List<FontFamilyItem> _fontFamilyItems;

    /// <summary>
    /// PropertyChangedEvent
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Constructor
    /// </summary>
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

        // フォントリスト: 先頭がデフォルト、以降インストール済みフォント昇順
        var systemFonts = Fonts.SystemFontFamilies
            .Select(f => f.Source)
            .OrderBy(f => f)
            .Select(f => new FontFamilyItem(f, f))
            .ToList();
        systemFonts.Insert(0, new FontFamilyItem(Listhing.Strings.DefaultLabel, ""));
        _fontFamilyItems = systemFonts;
    }

    /// <summary>Selected theme</summary>
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

    /// <summary>Selected language</summary>
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

    /// <summary>フォント選択リスト（先頭がデフォルト）</summary>
    public List<FontFamilyItem> FontFamilyItems => _fontFamilyItems;

    /// <summary>
    /// QuickTextWindow・QuickHistoryWindow のフォント名。空文字列はシステムデフォルト。
    /// </summary>
    public string PopupFontFamily
    {
        get => _settingsService.Settings.PopupFontFamily;
        set
        {
            if (_settingsService.Settings.PopupFontFamily != value)
            {
                _settingsService.Settings.PopupFontFamily = value;
                _settingsService.Save();
                App.ApplyPopupFontFamily(value);
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Font size (8-48)</summary>
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

    /// <summary>Whether restart is required (theme or language changed)</summary>
    public bool IsRestartRequired => _selectedTheme != _originalTheme || _selectedLanguage != _originalLanguage;

    /// <summary>クリップボード履歴の上限件数</summary>
    public int ClipboardHistoryLimit
    {
        get => _settingsService.Settings.ClipboardHistoryLimit;
        set
        {
            if (_settingsService.Settings.ClipboardHistoryLimit != value)
            {
                _settingsService.Settings.ClipboardHistoryLimit = value;
                _settingsService.Save();
                OnPropertyChanged();
            }
        }
    }

    /// <summary>デフォルトのアイテムを開くショートカットキー</summary>
    public ShortcutKey DefaultOpenKey => _settingsService.Settings.ShortcutSettings.DefaultOpenKey;

    /// <summary>Quick History ウィンドウを表示するショートカットキー</summary>
    public ShortcutKey QuickHistoryKey => _settingsService.Settings.ShortcutSettings.QuickHistoryKey;

    private PopWindowSettings Pw => _settingsService.Settings.PopWindow;

    public Models.LinkDropFormat PwLinkDropFormat
    {
        get => Pw.LinkDropFormat;
        set { if (Pw.LinkDropFormat != value) { Pw.LinkDropFormat = value; _settingsService.Save(); OnPropertyChanged(); } }
    }

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

    private QuickHistorySettings Qh => _settingsService.Settings.QuickHistory;

    /// <summary>モデファイキーセレクト（修飾キーを押している間に選択移動する動作）</summary>
    public bool QhModifierSelect
    {
        get => Qh.ModifierSelect;
        set { if (Qh.ModifierSelect != value) { Qh.ModifierSelect = value; _settingsService.Save(); OnPropertyChanged(); } }
    }

    /// <summary>モデファイキーセレクト中、対象キー以外でキャンセルする</summary>
    public bool QhCancelOnOtherKey
    {
        get => Qh.CancelOnOtherKey;
        set { if (Qh.CancelOnOtherKey != value) { Qh.CancelOnOtherKey = value; _settingsService.Save(); OnPropertyChanged(); } }
    }

    /// <summary>履歴リストの表示件数（4〜10）</summary>
    public int QhDisplayCount
    {
        get => Qh.DisplayCount;
        set { if (Qh.DisplayCount != value) { Qh.DisplayCount = value; _settingsService.Save(); App.RefreshQuickHistoryItems(); OnPropertyChanged(); } }
    }

    private QuickTextWindowSettings Qt => _settingsService.Settings.QuickTextWindow;

    /// <summary>閉じる時にテキストをクリップボードへコピーする</summary>
    public bool QtAutoCopyOnClose
    {
        get => Qt.AutoCopyOnClose;
        set { if (Qt.AutoCopyOnClose != value) { Qt.AutoCopyOnClose = value; _settingsService.Save(); OnPropertyChanged(); } }
    }

    /// <summary>ESC キーでウィンドウを閉じる</summary>
    public bool QtCloseOnEsc
    {
        get => Qt.CloseOnEsc;
        set { if (Qt.CloseOnEsc != value) { Qt.CloseOnEsc = value; _settingsService.Save(); OnPropertyChanged(); } }
    }

    /// <summary>ショートカットキーの表示を更新する</summary>
    public void RefreshShortcutKeys()
    {
        OnPropertyChanged(nameof(DefaultOpenKey));
        OnPropertyChanged(nameof(QuickHistoryKey));
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
