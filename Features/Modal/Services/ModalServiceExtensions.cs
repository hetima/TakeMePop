using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Listhing.ViewModels;
using Listhing.Models;

namespace Listhing.Services;

/// <summary>
/// ModalService の拡張メソッド
/// </summary>
public static class ModalServiceExtensions
{
    /// <summary>
    /// MainWindowViewModelを取得するヘルパーメソッド
    /// </summary>
    /// <param name="sender">イベント発生源</param>
    /// <returns>MainWindowViewModel（取得失敗時はnull）</returns>
    private static ModalService? GetMainWindowModalService(object sender)
    {
        var window = Window.GetWindow(sender as DependencyObject);
        if (window != null && window.DataContext is IModalOwner modalOwner)
        {
            return modalOwner.ModalService;
        }

        return null;
    }

    /// <summary>
    /// ViewModel を指定してモーダルを表示する（View は自動的に作成される）
    /// </summary>
    /// <typeparam name="TView">View の型</typeparam>
    /// <typeparam name="TViewModel">ViewModel の型</typeparam>
    /// <param name="modalService">モーダルサービス</param>
    /// <param name="viewModel">表示する ViewModel</param>
    public static void ShowWithViewModel<TView, TViewModel>(
        this ModalService modalService,
        TViewModel viewModel)
        where TView : UserControl, new()
        where TViewModel : class
    {
        var view = new TView { DataContext = viewModel };
        modalService.Show(view);
    }

    /// <summary>
    /// ViewModel を指定してモーダルを表示する（senderからModalServiceを取得）
    /// </summary>
    /// <typeparam name="TView">View の型</typeparam>
    /// <typeparam name="TViewModel">ViewModel の型</typeparam>
    /// <param name="sender">イベント発生源（Window.GetWindowでMainWindowを取得するために使用）</param>
    /// <param name="viewModel">表示する ViewModel</param>
    public static void ShowWithViewModel<TView, TViewModel>(
        object sender,
        TViewModel viewModel)
        where TView : UserControl, new()
        where TViewModel : class
    {
        var modalService = GetMainWindowModalService(sender);

        // ModalServiceを取得してモーダルを表示
        modalService?.ShowWithViewModel<TView, TViewModel>(viewModel);
    }

    /// <summary>
    /// OK/キャンセルモーダルを表示する
    /// </summary>
    /// <param name="modalService">モーダルサービス</param>
    /// <param name="message">表示するメッセージ</param>
    /// <param name="okLabel">OKボタンのラベル（nullの場合はデフォルト値）</param>
    /// <param name="cancelLabel">キャンセルボタンのラベル（nullの場合はデフォルト値）</param>
    /// <param name="okCallback">OKボタンが押されたときのコールバック</param>
    /// <param name="cancelCallback">キャンセルボタンが押されたときのコールバック</param>
    public static void ShowOkCancelModal(
        this ModalService modalService,
        string message,
        string? okLabel = null,
        string? cancelLabel = null,
        Action? okCallback = null,
        Action? cancelCallback = null)
    {
        var viewModel = new OkCancelModalViewModel(
            message,
            okLabel,
            cancelLabel,
            okCallback,
            cancelCallback,
            false);

        var view = new Views.OkCancelModalView { DataContext = viewModel };
        modalService.Show(view);
    }

    /// <summary>
    /// OK/キャンセルモーダルを表示する（senderからModalServiceを取得）
    /// </summary>
    /// <param name="sender">イベント発生源（Window.GetWindowでMainWindowを取得するために使用）</param>
    /// <param name="message">表示するメッセージ</param>
    /// <param name="okLabel">OKボタンのラベル（nullの場合はデフォルト値）</param>
    /// <param name="cancelLabel">キャンセルボタンのラベル（nullの場合はデフォルト値）</param>
    /// <param name="okCallback">OKボタンが押されたときのコールバック</param>
    /// <param name="cancelCallback">キャンセルボタンが押されたときのコールバック</param>
    public static void ShowOkCancelModal(
        object sender,
        string message,
        string? okLabel = null,
        string? cancelLabel = null,
        Action? okCallback = null,
        Action? cancelCallback = null)
    {
        var modalService = GetMainWindowModalService(sender);

        // ModalServiceを取得してモーダルを表示
        modalService?.ShowOkCancelModal(
            message,
            okLabel,
            cancelLabel,
            okCallback,
            cancelCallback);
    }

    /// <summary>
    /// OKモーダルを表示する
    /// </summary>
    /// <param name="modalService">モーダルサービス</param>
    /// <param name="message">表示するメッセージ</param>
    /// <param name="okLabel">OKボタンのラベル（nullの場合はデフォルト値）</param>
    /// <param name="okCallback">OKボタンが押されたときのコールバック</param>
    public static void ShowOkModal(
        this ModalService modalService,
        string message,
        string? okLabel = null,
        Action? okCallback = null)
    {
        var viewModel = new OkCancelModalViewModel(
            message,
            okLabel,
            null,
            okCallback,
            null,
            true);

        var view = new Views.OkCancelModalView { DataContext = viewModel };
        modalService.Show(view);
    }

    /// <summary>
    /// OKモーダルを表示する（senderからModalServiceを取得）
    /// </summary>
    /// <param name="sender">イベント発生源（Window.GetWindowでMainWindowを取得するために使用）</param>
    /// <param name="message">表示するメッセージ</param>
    /// <param name="okLabel">OKボタンのラベル（nullの場合はデフォルト値）</param>
    /// <param name="okCallback">OKボタンが押されたときのコールバック</param>
    public static void ShowOkModal(
        object sender,
        string message,
        string? okLabel = null,
        Action? okCallback = null)
    {
        var modalService = GetMainWindowModalService(sender);

        // ModalServiceを取得してモーダルを表示
        modalService?.ShowOkModal(
            message,
            okLabel,
            okCallback);
    }

    /// <summary>
    /// テキスト入力モーダルを表示する
    /// </summary>
    /// <param name="modalService">モーダルサービス</param>
    /// <param name="message">表示するメッセージ</param>
    /// <param name="defaultText">デフォルトのテキスト</param>
    /// <param name="okLabel">OKボタンのラベル（nullの場合はデフォルト値）</param>
    /// <param name="cancelLabel">キャンセルボタンのラベル（nullの場合はデフォルト値）</param>
    /// <param name="okCallback">OKボタンが押されたときのコールバック（入力された文字列を引数に受け取る）</param>
    /// <param name="cancelCallback">キャンセルボタンが押されたときのコールバック</param>
    public static void ShowInputTextModal(
        this ModalService modalService,
        string message,
        string defaultText = "",
        string? okLabel = null,
        string? cancelLabel = null,
        Action<string>? okCallback = null,
        Action? cancelCallback = null)
    {
        var viewModel = new InputTextModalViewModel(
            message,
            defaultText,
            okLabel,
            cancelLabel,
            okCallback,
            cancelCallback,
            false);

        var view = new Views.InputTextModalView { DataContext = viewModel };
        modalService.Show(view);
    }

    /// <summary>
    /// テキスト入力モーダルを表示する（senderからModalServiceを取得）
    /// </summary>
    /// <param name="sender">イベント発生源（Window.GetWindowでMainWindowを取得するために使用）</param>
    /// <param name="message">表示するメッセージ</param>
    /// <param name="defaultText">デフォルトのテキスト</param>
    /// <param name="okLabel">OKボタンのラベル（nullの場合はデフォルト値）</param>
    /// <param name="cancelLabel">キャンセルボタンのラベル（nullの場合はデフォルト値）</param>
    /// <param name="okCallback">OKボタンが押されたときのコールバック（入力された文字列を引数に受け取る）</param>
    /// <param name="cancelCallback">キャンセルボタンが押されたときのコールバック</param>
    public static void ShowInputTextModal(
        object sender,
        string message,
        string defaultText = "",
        string? okLabel = null,
        string? cancelLabel = null,
        Action<string>? okCallback = null,
        Action? cancelCallback = null)
    {
        var modalService = GetMainWindowModalService(sender);
        modalService?.ShowInputTextModal(
            message,
            defaultText,
            okLabel,
            cancelLabel,
            okCallback,
            cancelCallback);
    }
}
