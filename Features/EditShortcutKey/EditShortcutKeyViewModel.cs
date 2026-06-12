using System.Windows.Input;
using Listhing.Helpers;
using Listhing.Models;
using Listhing.Services;

namespace Listhing.ViewModels;

/// <summary>
/// ショートカットキー編集モーダルのViewModel
/// </summary>
public class EditShortcutKeyViewModel : ModalContentViewModel
{
    private readonly SettingsService _settingsService;
    private readonly Action<ShortcutKey?> _onShortcutChanged;
    private readonly string _actionName;
    private readonly Action? _onShortcutRemoved;
    private ShortcutKey _currentShortcut;
    private string _displayString;
    private string _warningMessage;
    private bool _shortcutConflicted;

    /// <summary>
    /// 禁止されたショートカットキーのリスト
    /// </summary>
    private static readonly string[] ForbiddenShortcuts = new[]
    {
        "F2",
        "Ctrl+S", "Ctrl+W", "Ctrl+Q", "Ctrl+T", "Ctrl+A", "Ctrl+F", "Ctrl+Z", "Ctrl+X", "Ctrl+C", "Ctrl+V", "Ctrl+OemComma", "Alt+F4", "Alt+Up", "Alt+Left", "Alt+Right", "Alt+Down", "Alt+LeftAlt", "Alt+RightAlt", "Alt+LeftCtrl", "Alt+RightCtrl", "Alt+LeftShift", "Alt+RightShift"
    };

    /// <summary>
    /// アクション名
    /// </summary>
    public string ActionName => _actionName;

    /// <summary>
    /// 表示文字列
    /// </summary>
    public string DisplayString
    {
        get => _displayString;
        private set
        {
            if (_displayString != value)
            {
                _displayString = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 警告メッセージ
    /// </summary>
    public string WarningMessage
    {
        get => _warningMessage;
        private set
        {
            if (_warningMessage != value)
            {
                _warningMessage = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// ショートカットが競合しているかどうか
    /// </summary>
    public bool ShortcutConflicted
    {
        get => _shortcutConflicted;
        private set
        {
            if (_shortcutConflicted != value)
            {
                _shortcutConflicted = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// ショートカットを削除コマンド
    /// </summary>
    public ICommand ClearCommand { get; }

    /// <summary>
    /// 保存コマンド
    /// </summary>
    public ICommand SaveCommand { get; }

    /// <summary>
    /// 閉じるコマンド
    /// </summary>
    public ICommand CloseCommand { get; }

    /// <summary>
    /// 競合するショートカットを削除コマンド
    /// </summary>
    public ICommand RemoveConflictingShortcutCommand { get; }

    /// <summary>
    /// コンストラクタ（アプリケーションショートカットキー編集用）
    /// </summary>
    /// <param name="settingsService">設定サービス</param>
    /// <param name="actionName">アクション名</param>
    /// <param name="currentShortcut">現在のショートカットキー</param>
    /// <param name="onShortcutChanged">ショートカット変更時のコールバック</param>
    /// <param name="onShortcutRemoved">ショートカット削除時のコールバック</param>
    public EditShortcutKeyViewModel(
        SettingsService settingsService,
        string actionName,
        ShortcutKey currentShortcut,
        Action<ShortcutKey?> onShortcutChanged,
        Action? onShortcutRemoved)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _onShortcutChanged = onShortcutChanged ?? throw new ArgumentNullException(nameof(onShortcutChanged));
        _onShortcutRemoved = onShortcutRemoved;
        _actionName = actionName ?? throw new ArgumentNullException(nameof(actionName));
        _currentShortcut = currentShortcut ?? throw new ArgumentNullException(nameof(currentShortcut));
        _displayString = _currentShortcut.IsEmpty ? Strings.NonePlaceholder : _currentShortcut.ToDisplayString();
        _warningMessage = string.Empty;
        _shortcutConflicted = false;

        ClearCommand = new RelayCommand(OnClear, CanClear);
        SaveCommand = new RelayCommand(OnSave, CanSave);
        CloseCommand = new RelayCommand(OnClose);
        RemoveConflictingShortcutCommand = new RelayCommand(OnRemoveConflictingShortcut, CanRemoveConflictingShortcut);
    }

    /// <summary>
    /// キーダウンイベントを処理する
    /// </summary>
    /// <param name="e">キーイベント引数</param>
    public void OnKeyDown(KeyEventArgs e)
    {
        // Escキーはキャンセル
        if (e.Key == Key.Escape)
        {
            OnClose();
            e.Handled = true;
            return;
        }

        // DeleteキーとBackspaceキーはクリア
        if (e.Key == Key.Delete || e.Key == Key.Back)
        {
            OnClear();
            e.Handled = true;
            return;
        }

        // 修飾キーのみの場合は無視
        if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl ||
            e.Key == Key.LeftAlt || e.Key == Key.RightAlt ||
            e.Key == Key.LeftShift || e.Key == Key.RightShift ||
            e.Key == Key.LWin || e.Key == Key.RWin)
        {
            return;
        }

        // ショートカットキーを作成
        var newShortcut = new ShortcutKey(e);
        _currentShortcut = newShortcut;
        UpdateDisplay();
        CheckForConflicts();
        ((RelayCommand)ClearCommand).RaiseCanExecuteChanged();
        ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
        ((RelayCommand)RemoveConflictingShortcutCommand).RaiseCanExecuteChanged();
        e.Handled = true;
    }

    /// <summary>
    /// 表示を更新する
    /// </summary>
    private void UpdateDisplay()
    {
        DisplayString = _currentShortcut.IsEmpty ? Strings.NonePlaceholder : _currentShortcut.ToDisplayString();
    }

    /// <summary>
    /// 競合をチェックする
    /// </summary>
    private void CheckForConflicts()
    {
        WarningMessage = string.Empty;
        ShortcutConflicted = false;

        // 禁止されたショートカットキーかチェック
        var shortcutString = _currentShortcut.ToString();
        if (ForbiddenShortcuts.Contains(shortcutString))
        {
            WarningMessage = Strings.ForbiddenShortcutMessage;
            return;
        }

        // 他のショートカットキーと競合していないかチェック
        var shortcutSettings = _settingsService.Settings.ShortcutSettings;
        if (_currentShortcut.Equals(shortcutSettings.DefaultOpenKey) ||
            _currentShortcut.Equals(shortcutSettings.RevealKey))
        {
            WarningMessage = Strings.DuplicateShortcutMessage;
            ShortcutConflicted = true;
            return;
        }
    }

    /// <summary>
    /// クリアコマンドを実行できるかどうか
    /// </summary>
    private bool CanClear()
    {
        return !_currentShortcut.IsEmpty;
    }

    /// <summary>
    /// クリアコマンドを実行する
    /// </summary>
    private void OnClear()
    {
        _currentShortcut = new ShortcutKey(Key.None);
        UpdateDisplay();
        WarningMessage = string.Empty;
        ((RelayCommand)ClearCommand).RaiseCanExecuteChanged();
        ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
        ((RelayCommand)RemoveConflictingShortcutCommand).RaiseCanExecuteChanged();
    }

    /// <summary>
    /// 保存コマンドを実行できるかどうか
    /// </summary>
    private bool CanSave()
    {
        return _currentShortcut.IsEmpty || string.IsNullOrEmpty(WarningMessage);
    }

    /// <summary>
    /// 保存コマンドを実行する
    /// </summary>
    private void OnSave()
    {
        _onShortcutChanged(_currentShortcut.IsEmpty ? new ShortcutKey(Key.None) : _currentShortcut);
        RequestClose = ModalCloseResult.OK;
    }

    /// <summary>
    /// モーダルを閉じる
    /// </summary>
    private void OnClose()
    {
        RequestClose = ModalCloseResult.Cancel;
    }

    /// <summary>
    /// モーダルが閉じられたときの処理
    /// </summary>
    /// <param name="result">閉じる理由</param>
    public override void ModalClosed(ModalCloseResult result)
    {
        if (result == ModalCloseResult.Cancel)
        {
            _onShortcutChanged(null);
        }
    }

    /// <summary>
    /// 競合するショートカットを削除コマンドを実行できるかどうか
    /// </summary>
    private bool CanRemoveConflictingShortcut()
    {
        // ショートカットが競合している場合のみ実行可能
        return ShortcutConflicted;
    }

    /// <summary>
    /// 競合するショートカットを削除コマンドを実行する
    /// </summary>
    private void OnRemoveConflictingShortcut()
    {
        RemoveConflictingShortcuts();
    }

    /// <summary>
    /// 競合するショートカットを削除する
    /// </summary>
    private void RemoveConflictingShortcuts()
    {
        var shortcutSettings = _settingsService.Settings.ShortcutSettings;
        var shortcutString = _currentShortcut.ToString();

        // 基本アクションのショートカットキーと競合している場合
        if (_currentShortcut.Equals(shortcutSettings.DefaultOpenKey))
        {
            shortcutSettings.DefaultOpenKey = new ShortcutKey(Key.None);
        }
        else if (_currentShortcut.Equals(shortcutSettings.RevealKey))
        {
            shortcutSettings.RevealKey = new ShortcutKey(Key.None);
        }

        // 設定を保存
        _settingsService.Save();

        // 競合チェックを再実行して表示を更新
        CheckForConflicts();

        // コマンドの状態を更新
        ((RelayCommand)RemoveConflictingShortcutCommand).RaiseCanExecuteChanged();
        ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();

        // ショートカット削除コールバックを呼び出す
        _onShortcutRemoved?.Invoke();
    }
}
