# プロジェクトルール

- ファイル名変更やプロジェクト内でのファイル移動は `git mv` を使用する

## アーキテクチャ

- .NET 10 を用いたWindowsデスクトップアプリケーション

### MVVMパターン
- **Model**: ビジネスロジックとデータ
- **View**: XAMLのみ、コードビハインドは最小限
- **ViewModel**: INotifyPropertyChanged実装、Viewとのバインディング担当

### フォルダ構成
```
Listhing/
├── Features/*        # 機能別にまとめられたフォルダ
├── Models/           # データモデル
├── ViewModels/       # ビューモデル
├── Views/            # View (XAML + コードビハインド)
├── Services/         # ビジネスサービス
├── Converters/       # 値コンバータ
├── Controls/         # カスタムコントロール
├── Helpers/          # ユーティリティクラス
└── Properties/        # 画像、スタイル、リソースディクショナリ
```
- 機能ごとにFeaturesにまとめる。複数機能で使われるファイルはそれぞれの種類フォルダに入れる
- Features内のnamespaceはフォルダパスではなくファイル種類に従う（Listhing.Features.XXX ではなく Listhing.Viewsなど）

## 命名規則

### C#
| 種類 | 規則 | 例 |
|------|------|-----|
| クラス | PascalCase | `MainWindow`, `TaskItem` |
| メソッド | PascalCase | `OnInitialized`, `LoadData` |
| プロパティ | PascalCase | `IsEnabled`, `TaskList` |
| プライベートフィールド | _camelCase | `_taskService`, `_isLoading` |
| 定数 | PascalCase | `MaxRetryCount` |

### XAML
| 種類 | 規則 | 例 |
|------|------|-----|
| コントロール名 | PascalCase + サフィックス | `TaskListBox`, `SaveButton` |
| リソースキー | PascalCase | `PrimaryBrush`, `CardStyle` |

## バインディング
- ViewModel は `Observable` プロパティを使用
- コレクションは `ObservableCollection<T>` を使用

## サービス・データベース管理

### Singleton管理
以下のクラスはApp.xaml.csでSingletonとして管理し、アプリケーション全体で共有する：

- **SettingsService** (`App.SettingsService`)
  - アプリケーション設定の管理
  - 全てのViewModelで `App.SettingsService` を参照

## 多言語対応 (ローカライゼーション)

### リソース管理
- 言語リソースは `Properties/i18n/` フォルダに配置
- リソースファイル形式: `Strings.{言語コード}.resx`
  - `Strings.resx` (デフォルト: 英語)
  - `Strings.ja.resx` (日本語)

### 命名規則
| 種類 | 規則 | 例 |
|------|------|-----|
| リソースキー | PascalCase | `DialogTitle`, `SaveButton` |
| 言語コード | ISO 639-1 | `ja`, `en`, `zh` |


### 実装ガイドライン
- ハードコードされた文字列を禁止
- 全てのUIテキストをリソースから取得
- 言語切り替えは再起動時に反映
- 日付・数値フォーマットもカルチャー対応
- 設定フォルダは Environment.SpecialFolder.LocalApplicationData に作成する

### サポート言語
- 英語 (en) - デフォルト
- 日本語 (ja)

## コメント・ドキュメント
- XML ドキュメントコメントをパブリックメンバーに記述
- 複雑なロジックには日本語でコメント
