using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Listhing.ViewModels;

/// <summary>
/// INotifyPropertyChangedを実装した基底クラス
/// </summary>
public class ObservableObject : INotifyPropertyChanged
{
    /// <summary>
    /// プロパティ変更イベント
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// プロパティ変更を通知
    /// </summary>
    /// <param name="propertyName">プロパティ名</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// プロパティ値を設定し、変更があった場合に通知
    /// </summary>
    /// <typeparam name="T">プロパティの型</typeparam>
    /// <param name="field">フィールドの参照</param>
    /// <param name="value">新しい値</param>
    /// <param name="propertyName">プロパティ名</param>
    /// <returns>変更があったかどうか</returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
