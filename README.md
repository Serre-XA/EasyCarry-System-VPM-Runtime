# EasyCarry System Basic

EasyCarry Systemで作成されたアイテムを利用し、基本的な調整を行うための無料パッケージです。

本READMEでは、Unityの基本的な操作や用語の説明は省略しています。

> [!NOTE]
> 現在は公開準備中です。導入方法や仕様は正式リリースまでに変更される場合があります。

## 概要

EasyCarry Systemは、VRChatアバター向けの持ち運び・装備ギミックを導入するためのシステムです。

| エディション | 用途 |
| --- | --- |
| EasyCarry System Basic | 対応アイテムの利用と基本的な調整 |
| EasyCarry System Advanced | 対応アイテムの制作と詳細な設定 |

## 主な機能

- アイテムスロットの変更
- アイテムの当たり判定調整
- 手持ち位置と装備位置の調整
- EasyCarry System対応アイテムのセットアップ

## 動作環境

- Unity 2022.3
- VRChat SDK - Avatars 3.10.x
- Modular Avatar 1.17.1以上、2.0.0未満

## インストール

### VCCから導入する場合

[![Add to VCC](https://img.shields.io/badge/Add%20to%20VCC-EasyCarry%20System%20Basic-2BACEA?style=for-the-badge)](https://serre-xa.github.io/EasyCarry-System-VPM-Runtime/)

上のボタンで配布ページを開き、ページ内の`Add to VCC`を押してください。VCCが起動し、EasyCarry SystemのVPMリポジトリを追加する確認画面が表示されます。

ボタンが反応しない場合は、VCCの`Settings`、`Packages`、`Add Repository`の順に開き、次のURLを追加してください。

`https://serre-xa.github.io/EasyCarry-System-VPM-Runtime/index.json`

1. 対象プロジェクトの`Manage Project`を開きます。
2. `EasyCarry System Basic`を追加します。

### UnityPackageから導入する場合

<!-- TODO: ReleaseページとUnityPackageの導入手順を記載する。 -->

## 基本的な使い方

1. セットアップしたいPrefabまたはGameObjectをアバター内へ配置します。
2. 対象PrefabまたはGameObjectを右クリックし、`EasyCarry System`から`Setup`を実行します。
3. Inspectorからスロット、位置、当たり判定を調整します。

> [!NOTE]
> Basic版では、装備位置00のボーン参照としてSpineのみ指定できます。

## 配布された対応アイテムを導入する

1. 導入したいアイテムのPrefabをアバター内へ配置します。
2. Prefabの`Easy Carry System Item Reference`コンポーネントにある`EasyCarry System Setup`ボタンを押します。
    <br>対象アイテムの右クリックメニューから`EasyCarry System`、`Setup`の順に選択することもできます。
3. 必要に応じて、アイテムスロットやアバターに合わせた位置を調整します。
4. 一度プレイモードへ移行し、EasyCarry Systemに関するエラーが発生しないことを確認します。
5. VRChatへアップロードし、実機で動作を確認します。

## 注意事項

<!-- TODO: MA Bone Proxy、既存コンポーネント、アップロード前処理などの注意点を記載する。 -->

- 導入前にUnityプロジェクトのバックアップを作成してください。
- 対応するVRChat SDKとModular Avatarが導入されていることを確認してください。

## 更新とアンインストール

<!-- TODO: VCC経由の更新方法と、安全なアンインストール手順を記載する。 -->

VCC経由で導入した場合は、`Manage Project`に表示される`-`ボタンから削除してください。

## トラブルシューティング

<!-- TODO: GestureChecker、Setup、ビルドエラーなどの代表的な対処方法を記載する。 -->

## ライセンス

本パッケージは[MIT License](LICENSE.md)の下で公開されています。

依存パッケージおよび第三者制作物には、それぞれのライセンスが適用されます。

## サポート

不具合や要望は[GitHub Issues](https://github.com/Serre-XA/EasyCarry-System-VPM-Runtime/issues)へお寄せください。

## クレジット

- 制作者: Serre

<!-- TODO: ロゴ制作者、使用ライブラリ、協力者、参考資料などを記載する。 -->
