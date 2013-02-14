#TakeMePop

Finder で選択されている項目をフローティングパネルに表示するアプリケーションです。Mac OS X 10.7 、10.8 に対応しています。

- Finder のツールバーに登録して使うことを想定しています。
- 起動された時点の Finder 選択項目を取得します。表示中に選択項目が変化してもパネルは更新されません。
- パネルは何枚でも作成できます。Finder のツールバーをクリックするたび新規に作成されます。
- パネルはオープンセーブダイアログより手前に表示されます
- このパネルからのドラッグは Finder からドラッグを行うのとほぼ同義です。
- パネルはデスクトップスペース間を勝手に移動するのでフルスクリーンアプリケーションにもドロップできます
- パネルにファイルをドロップするとリストに追加されます。
- Dock には現れず、すべてのパネルが閉じられたら終了します。

#Download
ビルド済みのバイナリは <http://hetima.com/takemepop/> で公開しています。

#Option

パネル左下のギアアイコンをクリックするとメニューが出てきます。以下の項目を設定できます

- Close After Dropped : 項目がどこかにドロップされたら自動的にパネルを閉じます
- Includes Window Target : 親フォルダもリストに表示されるようになります
- When Finder Has No Selection : Finder でなにも選択してなかった場合、最前面ウインドウのみを追加するか、開いているウインドウ全部を追加するか選べます

#License
MIT License
