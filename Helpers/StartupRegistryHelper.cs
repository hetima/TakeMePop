using System.Diagnostics;
using Microsoft.Win32;

namespace Listhing.Helpers;

/// <summary>
/// Windows のスタートアップ項目（HKCU\...\Run）への登録・解除を管理するヘルパー。
/// 値名は AppName 固定で、データには実行ファイルのフルパスを格納する。
/// </summary>
public static class StartupRegistryHelper
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>レジストリの値名（AppName を流用）</summary>
    private const string ValueName = AppConstants.AppName;

    /// <summary>
    /// 現在の実行ファイルのフルパスを取得する。取得できない場合は null。
    /// </summary>
    public static string? GetCurrentExePath() => Environment.ProcessPath;

    /// <summary>
    /// スタートアップに登録されている実行パスを取得する。未登録なら null。
    /// </summary>
    public static string? GetRegisteredPath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(ValueName) as string;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to read startup registry: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 既存の TakeMePop 項目を取り除いてから、現在の実行ファイルパスで登録し直す。
    /// </summary>
    public static void Register()
    {
        var exePath = GetCurrentExePath();
        if (string.IsNullOrEmpty(exePath))
        {
            Debug.WriteLine("Failed to register startup: exe path is unknown.");
            return;
        }

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);
            // 既存を取り除いてから（=上書き）登録。パスは引用符で囲む
            key.SetValue(ValueName, $"\"{exePath}\"");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to register startup: {ex.Message}");
        }
    }

    /// <summary>
    /// スタートアップから TakeMePop 項目を取り除く。未登録の場合は何もしない。
    /// </summary>
    public static void Unregister()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            if (key?.GetValue(ValueName) != null)
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to unregister startup: {ex.Message}");
        }
    }
}
