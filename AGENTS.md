# プロジェクトルール

- ファイル名変更やプロジェクト内でのファイル移動は `git mv` を使用する
- ビルド確認: `dotnet build TakeMePop.csproj`（x64 / net10.0-windows / WPF）

## アーキテクチャ

- .NET 10 / WPF / x64 のWindows常駐ユーティリティ（タスクトレイ常駐、`ShutdownMode.OnExplicitShutdown`）
- グローバルフックは **SharpHook**（libuiohook）を使用。トレイアイコンは Hardcodet.NotifyIcon.Wpf
- アプリ概要・機能仕様は CLAUDE.md を参照

### 名前空間について（重要）

- アプリ名は TakeMePop だが、**全ソースの namespace は `Listhing`** で統一されている（`Listhing.Services` など）
- 新規ファイルも既存に合わせて `Listhing.*` を使うこと。勝手に `TakeMePop.*` へリネームしない
- i18n リソースクラスも `Listhing.Strings`

### フォルダ構成（実際の構成）

```
TakeMePop/
├── Features/              # 機能単位のまとまり（View + ViewModel + 付随サービス）
│   ├── MainWindow/        # メインウィンドウ
│   ├── PopWindow/         # ドロップ受け皿ウィンドウ（中核機能）
│   ├── TransparentWindow/ # ドラッグ開始直後に出す透明な受け皿
│   ├── QuickHistoryWindow/# クリップボード履歴一覧（Hide/Showで使い回すシングルトン）
│   ├── QuickTextWindow/   # クリップボードテキスト編集ウィンドウ
│   ├── ToastWindow/       # トースト通知
│   ├── SettingWindow/     # 設定画面
│   ├── EditShortcutKey/   # ショートカットキー編集モーダル
│   └── Modal/             # ウィンドウ内モーダル基盤（ModalService）
├── Models/                # AppSettings 等のデータモデル
├── Services/              # フック・クリップボード・設定などのサービス
├── Converters/            # 値コンバータ
├── Helpers/               # ユーティリティ（ShortcutKey, AppConstants, ShellIconHelper 等）
└── Properties/i18n/       # Strings.resx / Strings.ja.resx
```

- 機能ごとに Features/ にまとめる。複数機能で使うものは Services/ Helpers/ などの種類別フォルダへ
- Features 内の namespace はフォルダパスに従う（例: `Listhing.Features.PopWindow`）。
  ViewModel 基盤クラス（ObservableObject, RelayCommand）は `Listhing.ViewModels` namespace だが物理的には Services/ にある

### シングルトンサービス

App.xaml.cs で生成し、`App.XXX` 静的プロパティ経由で参照する:

| サービス | 生成タイミング | 役割 |
|---|---|---|
| `App.SettingsService` | App コンストラクタ | 設定の読み書き（%LocalAppData%\TakeMePop\settings.json、DEBUG時は debug_settings.json） |
| `App.GlobalHookService` | App コンストラクタ | SharpHook の唯一のフックインスタンス。各サービスへイベント中継 |
| `App.MouseHookService` | App コンストラクタ | ドラッグ検出と早期キャプチャ（TransparentWindow 表示）のトリガー |
| `App.KeyboardHookService` | Application_Startup | グローバルホットキー登録、Ctrl+C / Ctrl+X ダブルタップ検出 |
| `App.ClipboardService` | Application_Startup | WM_CLIPBOARDUPDATE 監視と履歴管理（HwndSource 使用のため WPF 初期化後に生成） |

ウィンドウの生成・管理（PopWindow リスト、QuickHistory/QuickText のシングルトン保持）も App.xaml.cs の静的メソッドが担う。

### スレッドモデル（重要）

- SharpHook のイベント（GlobalHookService 経由）は**フック専用スレッド**で発火する
- UI 操作・WPF オブジェクトへのアクセスは必ず `Dispatcher.BeginInvoke` 経由で行う（MouseHookService / KeyboardHookService が踏襲しているパターンに従う）
- KeyboardHookService のホットキーリストはスナップショット配列（volatile）でフックスレッドから読む方式

## 命名規則

### C#
| 種類 | 規則 | 例 |
|------|------|-----|
| クラス | PascalCase | `PopWindow`, `ClipboardItem` |
| メソッド | PascalCase | `ApplySettings`, `TryCapture` |
| プロパティ | PascalCase | `IsDragging`, `HasFiles` |
| プライベートフィールド | _camelCase | `_viewModel`, `_isDragging` |
| 定数 | PascalCase | `AppName` |

### XAML
| 種類 | 規則 | 例 |
|------|------|-----|
| コントロール名 | PascalCase + サフィックス | `HistoryListBox`, `CloseButton` |
| リソースキー | PascalCase | `KeyboardFocusBorderColorBrush` |

## バインディング

- ViewModel は `ObservableObject`（自前実装、Services/ObservableObject.cs）を継承し `OnPropertyChanged()` を呼ぶ
- コレクションは `ObservableCollection<T>` を使用
- 小さなウィンドウはコードビハインド中心の実装も許容されている（PopWindow, QuickHistoryWindow 等が前例）

## 多言語対応

- 言語リソースは `Properties/i18n/Strings.resx`（英語・デフォルト）と `Strings.ja.resx`（日本語）
- UI テキストのハードコード禁止。`Listhing.Strings.XXX` または XAML から参照
- リソースキーは PascalCase（例: `CopiedMessage`）
- 言語切り替えは再起動時に反映
- 設定フォルダは `Environment.SpecialFolder.LocalApplicationData` 配下

## コメント・ドキュメント

- XML ドキュメントコメントをパブリックメンバーに記述
- コメントは日本語で書く

---

# 既知の問題・改善提案

コードレビューで見つかった潜在的な問題。修正時はこのリストも更新すること。

## バグの可能性が高いもの

1. **ホットキー変更時に旧キーが残る** — [SettingsView.xaml.cs](Features/SettingWindow/SettingsView.xaml.cs) は設定値を新キーで上書きしてから `App.ApplyHotkeySettings()` を呼ぶが、`ApplyHotkeySettings` は「現在の（=新しい）設定値」で `UnregisterHotkey` するため、変更前のキーの登録が解除されず残り続ける。変更前のキーを控えて解除するか、`UnregisterAll` 方式にすべき。

2. **DPI スケーリング非対応の座標計算** — `GetCursorPos` や SharpHook が返す物理ピクセル座標を、WPF の `Left`/`Top`（DIP 単位）へそのまま代入している。スケール 100% 以外の環境ではウィンドウがカーソルからずれる。該当箇所: App.xaml.cs（`ShowQuickTextWindow`, `OnDefaultOpenHotkey`, `CreatePopWindow`）、TransparentWindow.ShowNearPoint、ToastWindow.PlaceWindow。`PresentationSource.CompositionTarget.TransformFromDevice` 等での変換が必要。

3. **仮想ファイルのみのアイテムがコピーできない** — [PopWindow.xaml.cs](Features/PopWindow/PopWindow.xaml.cs) の `CopyButton_Click` は `item.Files` を見るが、ブラウザからの仮想ファイルは `TempFiles` にしか入らないため（`Files == null`）何もコピーされない。ドラッグ出力と同様に `AllFiles` を使うべき。`OnRevealHotkey`（App.xaml.cs）の `GetFiles()` も同様に `Files` のみ参照。

4. **一時ファイルのパス衝突** — ClipboardItem.ExtractVirtualFiles は `%TEMP%\TakeMePop\<元ファイル名>` 固定なので、同名ファイルを再ドロップすると前のアイテムの実体が上書きされ、さらに片方の `Dispose()` でもう片方のファイルも消える。アイテムごとに GUID サブフォルダを切るのが安全。

5. **TransparentWindow がドロップを処理しない** — `AllowDrop="True"` だが DragOver/Drop ハンドラが未実装のため、ドロップしても受け取れない（カーソルが禁止マークになる）。CLAUDE.md の想定機能（ドロップを受けて PopWindow へ渡す）が未完。

## 改善した方がよいもの

6. **PopWindow のリサイズで毎回設定ファイル保存** — `SizeChanged += App.SavePopWindowSize`（即ファイル I/O）はウィンドウ生成時の `Width` 代入やリサイズドラッグ中も連続発火する。デバウンスするか Closed 時にまとめて保存する方がよい。

7. **ClipboardItem の所有権が曖昧** — PopWindow は履歴中の ClipboardItem をそのまま保持し、Item 差し替え時・Close 時に `Dispose()` する。現状は履歴アイテムが TempFiles を持たないため実害がないが、将来 TempFiles 持ちのアイテムが履歴に入ると、履歴に残ったまま一時ファイルが削除される。また `ClipboardService.TrimHistoryIfNeeded` も削除アイテムを Dispose していない。

8. **QuickHistoryWindow.CopyItem の挙動** — 履歴から削除してから `Clipboard.SetText` するため、WM_CLIPBOARDUPDATE で同じ内容が履歴の最新として再追加される（「先頭に移動」相当）。意図的ならコメントで明示すべき。

9. **`App.ApplyFontSize` のコメントと実装の不一致** — コメント・XMLドキュメントは「8-48」だが判定は `> 40`。

10. **`new` によるメソッド隠蔽** — QuickHistoryWindow / QuickTextWindow の `public new void Show()` は `Window` 型の参照経由だと呼ばれない。動作はしているが壊れやすいので、別名メソッド（`ShowAndActivate` 等）が安全。

## 前身プロジェクト（Listhing）の残骸

動作には影響しないが紛らわしいもの。削除は明示的に依頼された場合のみ:

- 未使用: `App.WorkspaceNames` / `UpdateJumpList` / `SaveOpenedWorkspaces`、`AppSettings.OpenedWorkspaceNames` / `WindowSettings`、`AppConstants.Database` / `Ui` / `GetVSCodeExePath`、`SettingsService.MatchesShortcut`、`ShortcutMatchResult`
- `MouseHookService.DragStarted` イベントはどこからも購読されていない
