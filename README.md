# Symphony Framework

Symphony Frameworkは、Unityゲームで何度も作ることになる「シーン遷移」「サービス共有」「セーブ」「オーディオ」「ポーズ」を、同じ使い方で扱えるようにまとめたフレームワークです。

最初のシーンより前に自動で初期化されるため、専用のBootstrapシーンやManagerプレハブを用意せず、必要な機能から使い始められます。

- 対応Unity: **Unity 6（6000.0）以降**
- 現在のバージョン: **3.9.2**
- ライセンス: **MIT**

## Symphony Frameworkでできること

| やりたいこと | 使う機能 |
| --- | --- |
| シーンを非同期で読み込み、進捗やActive Sceneを管理したい | [Scene Loader](./Documentation~/Modules/SceneLoader.md) |
| シーンを跨いでサービスやComponentを共有したい | [Service Locator](./Documentation~/Modules/ServiceLocator.md) |
| データ型ごとに保存・読み込み・削除したい | [Save Data System](./Documentation~/Modules/SaveDataSystem.md) |
| AudioMixerGroupごとの再生元と音量をまとめたい | [Audio Manager](./Documentation~/Modules/AudioManager.md) |
| ゲーム全体を止め、待機やTweenもポーズへ追従させたい | [Pause Manager](./Documentation~/Modules/PauseManager.md) |
| 実行中の状態、FPS、ログ、処理時間を確認したい | [Debug](./Documentation~/Modules/Debug.md) |
| Scene、Tag、Layer等をenum化し、Inspector入力を安全にしたい | [Editor機能](./Documentation~/EditorTools.md) |

すべてを導入時に設定する必要はありません。使いたい機能のモジュール文書とサンプルから始められます。

## まず試す

1. Package Managerからパッケージを導入します。
2. Unityのコンパイル完了を待ちます。必要な設定アセットとenumが自動生成されます。
3. Package Managerの`Samples`から、試したい機能のサンプルをインポートします。
4. サンプルシーンを開いてPlayします。

Scene Loader Sampleは対象シーンをBuild Settingsへ追加、Audio Manager SampleはAudioMixerの設定が必要です。各サンプルの説明は[サンプル](#サンプル)にあります。

## インストール

### Unity Package Managerから導入する

Unityで`Window > Package Manager`を開き、`+ > Install package from git URL...`へ次のURLを入力します。

```text
https://github.com/HIBIKI5201/SymphonyFramework.git
```

`Packages/manifest.json`へ直接追加する場合:

```json
{
  "dependencies": {
    "symphonyframework": "https://github.com/HIBIKI5201/SymphonyFramework.git"
  }
}
```

特定バージョンに固定する場合は、リポジトリに存在するタグまたはコミットをURL末尾の`#`以降へ指定してください。

### Assetsとして導入する

ソースを直接配置する場合は、フレームワークのルートを次のパスにします。

```text
Assets/SymphonyFrameWork
```

Editor拡張のパス判定とアセット保護がこの配置を前提とするため、フォルダ名は変更しないでください。

### 依存パッケージ

依存関係は`package.json`に定義され、UPM導入時にUnityが解決します。

- Addressables `1.21.19`
- Newtonsoft Json `3.2.1`

## 初期設定

導入後のコンパイルで、次の設定アセットとコードが利用プロジェクト側へ生成されます。

```text
Assets/
├─ Resources/SymphonyFrameWork/
│  ├─ SceneLoadConfig.asset
│  ├─ AudioConfig.asset
│  └─ SaveDataConfig.asset
└─ Scripts/SymphonyFrameWork/
   ├─ SceneListEnum.cs
   ├─ TagsEnum.cs
   ├─ LayersEnum.cs
   ├─ AudioGroupTypeEnum.cs
   └─ SymphonyFrameWork.Enum.asmdef
```

主な設定場所:

- `Window > SymphonyFrameWork > Symphony Administrator`: 各機能の状態確認とenum生成
- `Project Settings > SymphonyFrameWork > Save System`: セーブデータローダーの選択
- `SceneLoadConfig.asset`: 再生開始時のシーン初期化
- `AudioConfig.asset`: AudioMixerとグループ設定

asmdefを使うゲーム側コードは`SymphonyFrameWork`を参照してください。自動生成enumを直接使う場合だけ`SymphonyFrameWork.Enum`も追加します。

## 機能ごとの使い方

| モジュール | 内容 |
| --- | --- |
| [Service Locator](./Documentation~/Modules/ServiceLocator.md) | 登録、取得、注入、Service Locate パネル、ログ設定 |
| [Scene Loader](./Documentation~/Modules/SceneLoader.md) | 非同期ロード、進捗、初期化、Scene Load パネル |
| [Save Data System](./Documentation~/Modules/SaveDataSystem.md) | 保存、ローダー、Save System設定、Save Data パネル |
| [Audio Manager](./Documentation~/Modules/AudioManager.md) | AudioSource取得、音量制御、AudioConfig |
| [Pause Manager](./Documentation~/Modules/PauseManager.md) | ポーズ、通知、ポーズ対応待機、Pause パネル |
| [Debug](./Documentation~/Modules/Debug.md) | `SymphonyDebugHUD`、`SymphonyDebugLogger`、`SymphonyStopWatch`、ログのファイル出力 |
| [Utility](./Documentation~/Modules/Utility.md) | `SymphonyAwaitable`、`SymphonyTween`、`SymphonyStringUtil`、`SymphonyComponentUtil`、`SymphonyVisualElement` |
| [Inspector属性](./Documentation~/Modules/InspectorAttributes.md) | `[ReadOnly]`、`[DisplayText]`、`[TagSelector]`、`[SceneNameSelector]`、`[SubclassSelector]` |

各モジュールの文書に、クイックスタート、実装時の注意、対応するEditor機能、内部構造がまとまっています。

## Editor・デバッグ支援

索引、設定ファイルの置き場、アセット保護、Editorの初期化は[Editor機能](./Documentation~/EditorTools.md)にあります。

| 機能 | 内容 |
| --- | --- |
| [Symphony Administrator](./Documentation~/EditorTools.md#symphony-administrator) | Service Locator、Scene Loader、Save Data、Pauseの状態確認 |
| [AutoEnumGenerator](./Documentation~/Modules/AutoEnumGenerator.md) | Scene、Tag、Layer、Audio Groupのenum生成 |
| [Asset Store Tools Packager](./Documentation~/Modules/AssetStoreToolsPackager.md) | Asset Store Toolsの出力、差分インポート、出力パイプライン |
| [Project Structure Tools](./Documentation~/Modules/ProjectStructureTools.md) | FolderGenerator、AssemblyGenerator、SymphonyPackageLoader |

Runtimeモジュールに紐づくEditor機能（Save System設定、Service Locatorのログ設定、`SymphonyDebugHUD`、`SymphonyMcpTools`、Inspector属性）は、上の[機能ごとの使い方](#機能ごとの使い方)から各モジュール文書を参照してください。

## サンプル

Package Managerから[`Samples/Runtime`](./Samples/Runtime)の各サンプルを利用プロジェクトへインポートできます。

- `ServiceLocatorSample`: 登録、同期取得、非同期取得、Singleton
- `SaveDataSystemSample`: 複数データ型の編集、保存、再ロード、削除
- `SceneLoaderSample`: 追加ロード、Active Scene切り替え、アンロード
- `PauseManagerSample`: Pause切り替え、`IPausable`、ポーズ対応待機
- `AudioManagerSample`: AudioSource取得と音量制御
- `DebuggerSample`: Logger、HUD、StopWatch

## ドキュメント

- [変更履歴](./CHANGELOG.md)
- [パッケージ構成・クラス図](./Documentation~/Architecture.md)
- [Editor機能](./Documentation~/EditorTools.md)
- [非推奨APIと削除予定](./Documentation~/Deprecations.md)
- Modules/
  - [Service Locator](./Documentation~/Modules/ServiceLocator.md)
  - [Scene Loader](./Documentation~/Modules/SceneLoader.md)
  - [Save Data System](./Documentation~/Modules/SaveDataSystem.md)
  - [Audio Manager](./Documentation~/Modules/AudioManager.md)
  - [Pause Manager](./Documentation~/Modules/PauseManager.md)
  - [Debug](./Documentation~/Modules/Debug.md)
  - [Utility](./Documentation~/Modules/Utility.md)
  - [Inspector属性](./Documentation~/Modules/InspectorAttributes.md)
  - [AutoEnumGenerator](./Documentation~/Modules/AutoEnumGenerator.md)
  - [Asset Store Tools Packager](./Documentation~/Modules/AssetStoreToolsPackager.md)
  - [Project Structure Tools](./Documentation~/Modules/ProjectStructureTools.md)
- [AGENTS.md（AIエージェント向けの導線と常時ルール）](./AGENTS.md)
  - [利用コードを書くときの注意事項](./Documentation~/AgentUsage.md)
  - [利用コードの検証手順](./Documentation~/AgentVerification.md)
- [Symphony Framework Document](https://lying-foxglove-81a.notion.site/Symphony-Framework-Document-19b7c2c6cc02806b9b97cb8a97c9f11a?pvs=74)

同じ内容をブラウザで読めるHTMLが`Documentation~/Html/`にあります。**Unityから`Window > SymphonyFrameWork > Documentation`を選ぶと索引がブラウザで開きます。** Project Settingsの各画面にある`ドキュメントを開く`からは、その画面に対応するモジュール文書が開きます。

**正本はMarkdownで、HTMLは生成物です。** 内容の修正はMarkdown側へ行ってください。

本体開発向けの作業手順、コーディング規約、設計思想は、開発用リポジトリ[SymphonyWorkspace](https://github.com/HIBIKI5201/SymphonyWorkspace)の`Documentation/`にあります。

## コントリビューター

- [5unad0ke1](https://github.com/5unad0ke1)

## 参考・謝辞

- [Unity SerializeReferenceExtensions（mackysoft）](https://github.com/mackysoft/Unity-SerializeReferenceExtensions)
- [UniRx（neuecc）](https://github.com/neuecc/UniRx)（今後の機能実装で参考予定）
- [DOTween（Demigiant）](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676)

## ライセンス

Copyright (c) 2026 HIBIKI_5201

このプロジェクトはMIT Licenseで公開されています。詳細は[LICENSE.txt](./LICENSE.txt)を参照してください。
