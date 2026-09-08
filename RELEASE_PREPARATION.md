# Runtime 公開準備メモ（2026-09-09）

## 今回の更新
- 共通メニューに「全アイテムをリセット」「ECSを無効化」「アイテム毎の設定」を追加。
- 個別メニューを有効化基準から無効化基準へ変更。個別無効化は初期値OFF、保存OFF。
- 共有パラメーターは EasyCarrySystem/Item/ResetAll と EasyCarrySystem/Item/AllDisabled。
- 全リセット要求を各アイテムのParameter Driverで解除する処理を削除。
- GrabBlocked時の手の状態判定をAny Stateへ集約し、持ち替え遷移の条件を追加。
- GestureCheck.controllerの変更はパラメーターのController参照の再保存。
- 先行コミットのパッケージパス固定、Advanced互換性チェック、共有メニューの子階層検索・スロット操作修正も今回のpushに含む。

## 確認結果
- ユーザーによるメニュー更新後の動作確認完了。
- Runtime Editorのdotnet build成功（警告27件、エラー0件）。
- 両Controllerの遷移条件は定義済みパラメーターを参照。
- パッケージ内に旧名 IsResetAll / DisabledAll / Item/IsEnabled は残存しない。
- Unity YAMLの空欄に由来する末尾空白がgit diff --checkで報告されるが、Unity保存形式として維持。
- GitHub Actionsは今回実行していない。公開物を使ったクリーン環境での導入試験は未実施。

## 公開前の残件
1. 次回バージョンを決め、BasicとAdvancedのバージョンを揃える。新機能を含むため0.6.0を候補とする。現状のpackage.jsonは0.5.0のまま。0.5.0タグは既に存在するので、そのままBuild Releaseを実行しない。
2. 現在の互換性チェックはBasicとAdvancedの完全一致を要求する。Basicだけバージョンを変更すると警告対象となる。
3. 新しいプレファブにはMAのMinimumVersion: 1.18.0という記録がある。package.jsonのMA下限は1.17.1なので、下限バージョンでの導入検証または依存下限の見直しを行う。ユーザー側の現在のMAは1.18.7。
4. バージョン調整後、クリーンなアバタープロジェクトでVPM導入・更新と複数アイテムのメニューを確認する。個別無効化と全体無効化の組み合わせ、全リセット、既存プレファブのoverrideも確認する。
5. GitHub ActionsのBuild Releaseをmasterで手動実行する。配布対象はPackages/com.serre.easycarry-system.runtime。Build Repo Listing完了後にVCCで新バージョンを確認する。

## 開発環境について
SDK再導入で生じたPackages管理設定・ProjectSettings・resolver・UserSettingsの差分は今回の機能コミットに含めない。作業ツリーに保持する。
Serre_AvatarはローカルUPMでRuntimeリポジトリを参照している。公開パッケージへローカル参照設定は含めない。
