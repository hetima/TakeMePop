using System.Windows.Input;

namespace Listhing.ViewModels;

/// <summary>
/// シンプルなICommand実装
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    /// <summary>
    /// CanExecuteChangedイベント
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="execute">実行アクション</param>
    /// <param name="canExecute">実行可否判定関数</param>
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// 実行可否判定
    /// </summary>
    public bool CanExecute(object? parameter)
    {
        return _canExecute == null || _canExecute();
    }

    /// <summary>
    /// 実行
    /// </summary>
    public void Execute(object? parameter)
    {
        _execute();
    }

    /// <summary>
    /// CanExecuteChangedイベントを発生
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// ジェネリック版RelayCommand
/// </summary>
public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    /// <summary>
    /// CanExecuteChangedイベント
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="execute">実行アクション</param>
    /// <param name="canExecute">実行可否判定関数</param>
    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// 実行可否判定
    /// </summary>
    public bool CanExecute(object? parameter)
    {
        return _canExecute == null || _canExecute((T?)parameter);
    }

    /// <summary>
    /// 実行
    /// </summary>
    public void Execute(object? parameter)
    {
        _execute((T?)parameter);
    }

    /// <summary>
    /// CanExecuteChangedイベントを発生
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
