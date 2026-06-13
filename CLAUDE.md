# TakeMePop

## アプリ概要

マウスの動きをグローバルに監視し、ドラッグ＆ドロップ操作中にポインタ近くへ小さなウィンドウをポップアップするユーティリティ。

**主なシナリオ:**
2. ドラッグ中に激しく振る（シェイク）ジェスチャーを検出してウィンドウを表示
3. マウスポインタがスクリーン端に到達したらウィンドウを表示
4. グローバルホットキーでウィンドウを手動表示

**救済ユースケース:** クリックのつもりがドラッグになった、意図しない場所にドロップしてしまった、などの場面での受け皿となる。

## 技術スタック

- .NET 10.0 / WPF / C#
- x64 Windows
- グローバルマウスフック: **SharpHook** を使用予定
  - `MouseDragged` イベントで D&D 中を検出
  - `MousePressed` / `MouseReleased` でドラッグ開始/終了を判定
  - 管理者権限不要（SetWindowsHookEx は標準ユーザーで動作）

## ライブラリ選定

### SharpHook（採用）
- NuGet: `SharpHook` / `SharpHook.Reactive` https://github.com/TolikPylypchuk/SharpHook
- クロスプラットフォーム対応（libuiohook ラッパー）
- `MouseDragged` イベントを直接サポート（D&D 検出が容易）
- Rx.NET 対応でイベントの Throttle・フィルタが簡潔
- まず SharpHook だけ追加して通常のイベントハンドラで実装するのが最もシンプル。後から「マウス移動イベントを間引きたい」となったときに .Reactive を足すという順序でも問題なし

## アーキテクチャ方針

- `MouseHookService` はシングルトンとして `App.xaml.cs` で初期化・破棄
- ポップアップウィンドウは WPF の通常ウィンドウ（`Topmost=true`、`ShowInTaskbar=false`）
- ドラッグ中の判定は「MousePressed かつ MouseDragged イベントを受信中」で行う
- Rx.NET の `Observable.Throttle` でマウス移動イベントを間引く

## 注意事項

- SharpHook のフックは専用スレッドで動作するため、UI 操作は Dispatcher 経由で行う
- `MouseDragged` イベントは「マウスボタンを押しながら移動」を意味し、OS レベルの D&D 状態とは別物
  - 真の D&D 判定は `DragEnter` / `DragLeave` の WPF イベントと組み合わせて補完する
