# Symphony Frameworkの構成

この文書は、利用側のコードから見たパッケージの構成です。どのアセンブリを参照し、どの型を入口にし、何が自動で動くかをまとめています。モジュールごとの使い方は[Modules/](./Modules/)を参照してください。

## アセンブリ

| アセンブリ | 内容 | 利用側からの参照 |
| --- | --- | --- |
| `SymphonyFrameWork` | Runtime API、Component、設定型、Utility | 参照する |
| `SymphonyFrameWork.Enum` | プロジェクトのScene、Tag、Layer、Audio Groupから生成するenum | 生成enumを使う場合だけ参照する |
| `SymphonyFrameWork.PauseCategory` | Project Settingsから生成したポーズカテゴリー | 生成したカテゴリーを使う場合だけ参照する |
| `SymphonyFrameWork.Core` | RuntimeとEditorが共有する定数と基盤 | 通常は不要 |
| `SymphonyFrameWork.Editor` | 設定画面、管理ウィンドウ、Drawer、Generator。Playerビルドには含まれない | Editor用asmdefから必要なときだけ |

Runtimeのコードから`SymphonyFrameWork.Editor`を参照しないでください。

## パッケージのディレクトリ

```text
SymphonyFrameWork/
├─ Core/                    Runtime／Editor共通の定数と基盤
├─ Runtime/                 Playerビルドへ含まれる機能
│  ├─ Attribute/           Inspector属性
│  ├─ Component/           GameObjectへ追加するComponent
│  ├─ Debug/               HUD、Logger、StopWatch
│  ├─ Interface/           注入・初期化などの公開契約
│  ├─ Service/             Scene、Service Locator、Save、Audio、Pause
│  └─ Utility/             Awaitable、Tween、文字列等の補助
├─ Editor/                  Editor専用UI、Drawer、Generator
├─ Samples~/Runtime/        Package Managerから導入する利用例
├─ Tests/                   EditMode／PlayModeテスト
└─ Documentation~/         利用者向けの文書（Modules/にモジュール別文書）
```

**各フォルダの`Internal/`は公開APIではありません。** 利用側のコードは`Internal/`の型に依存しないでください。

## 公開の入口と拡張点

主要な機能はstaticなFacadeから使います。`new`や`.Instance`は使いません。

| モジュール | 入口（Facade） | 利用側が実装・継承する型 |
| --- | --- | --- |
| [Service Locator](./Modules/ServiceLocator.md) | `ServiceLocator`、`ServiceInjector` | `IInjectable<T...>` |
| [Scene Loader](./Modules/SceneLoader.md) | `SceneLoader` | `IInitializeAsync` |
| [Scene Block](./Modules/SceneBlock.md) | `SceneBlockLoader` | `SceneBlockAsset`（アセットとして作る） |
| [Save Data System](./Modules/SaveDataSystem.md) | `SaveStore` | `SaveDataContent`、`SaveDataLoaderStrategy` |
| [Audio Manager](./Modules/AudioManager.md) | `AudioManager` | なし |
| [Pause Manager](./Modules/PauseManager.md) | `PauseManager` | `PauseManager.IPausable`とその派生カテゴリー |

照会APIが返す`ServiceRegistrationInfo`、`SceneLoadInfo`、`SceneBlockInfo`、`SaveDataEntryInfo`、`PauseInfo`は、取得した時点のスナップショットです。後から状態が変わっても値は変わりません。

`SceneLoadConfig`、`AudioConfig`、`SaveDataConfig`は`internal`です。InspectorかProject Settingsから設定し、型として参照しないでください。

## 起動と終了

`SymphonyOrchestrator`が自動で初期化します。利用側でBootstrap用のGameObjectやシーンを作る必要はありません。

1. 最初のシーンのロード前に、前回の状態を破棄し、常駐用のオブジェクトを作る。
2. Save → Pause → Service Locator → Scene → Audio → Debug HUD の順に初期化する。
3. 最初のシーンのロード後に、`SceneLoadConfig`の設定でシーンの管理を始める。
4. Play Modeを終えると、初期化と逆の順に解放する。

Domain Reloadを無効にしていても、状態は次のPlay Modeへ持ち越されません。初期化前や解放後に呼んだときの動きは、各モジュール文書の「うまくいかないとき」にあります。

## シーンロード時の連携

`SceneLoader`（と`SceneBlockLoader`）でロードしたシーンは、次の順で準備されます。

1. シーンを非同期でロードする。
2. ルートGameObjectのうち`IInjectable<T...>`を実装したものへ、`ServiceLocator`から依存を注入する。
3. `IInitializeAsync`を実装したものの`InitializeAsync`を待つ。
4. 優先度を評価してActive Sceneを決め、ロードを完了する。

2か3で失敗すると`SceneInitializationException`になります。この連携が必要なシーンでは`SceneManager.LoadScene`を直接使わず、`SceneLoader`を入口にしてください。
