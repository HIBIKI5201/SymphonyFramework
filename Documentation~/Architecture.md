# Symphony Frameworkの構成

この文書は、パッケージ内を移動するときや、公開型どうしの関係を確認するときの構成図です。モジュールごとのAPI使用例、実装時の注意、Editor機能、内部構造は[Modules/](./Modules/)を参照してください。

## アセンブリ

```mermaid
flowchart LR
    Consumer["利用側アセンブリ"] --> Runtime["SymphonyFrameWork"]
    Consumer -. "自動生成enumを使う場合" .-> Enum["SymphonyFrameWork.Enum"]
    Editor["SymphonyFrameWork.Editor"] --> Runtime
    Editor --> Core["SymphonyFrameWork.Core"]
    Runtime --> Core
    Runtime --> Addressables["Unity.Addressables"]
    Runtime --> ResourceManager["Unity.ResourceManager"]
    Runtime --> Enum
```

- `SymphonyFrameWork`: Runtime API、Component、設定型、Utilityを含むメインアセンブリ。
- `SymphonyFrameWork.Core`: RuntimeとEditorが共有する定数・基盤。利用側が通常直接参照する必要はない。
- `SymphonyFrameWork.Editor`: Editor専用の設定画面、管理ウィンドウ、Drawer、Generator。Playerビルドには含まれない。
- `SymphonyFrameWork.Enum`: 利用プロジェクトのScene、Tag、Layer、Audio Groupから生成されるアセンブリ。利用する場合だけ消費者側asmdefから参照する。

参照方向は`Editor → Runtime → Core`です。RuntimeまたはCoreからEditorへ逆参照しません。

## パッケージのディレクトリ

```text
SymphonyFrameWork/
├─ Core/                    Runtime／Editor共通の定数と基盤
│  └─ Internal/            パッケージ内部だけで使う実装
├─ Runtime/                 Playerビルドへ含まれる機能
│  ├─ Attribute/           Inspector属性
│  ├─ Component/           GameObjectへ追加するComponent
│  ├─ Configs/             設定の検索とinternal設定型
│  ├─ Debug/               HUD、Logger、StopWatch
│  ├─ Interface/           注入・初期化などの公開契約
│  ├─ Orchestrator/        自動初期化を行うComposition Root
│  ├─ Service/             Scene、Service Locator、Save、Audio、Pause
│  └─ Utility/             Awaitable、Tween、文字列等の補助
├─ Editor/                  Editor専用UI、Drawer、Generator
├─ Samples~/Runtime/        Package Managerから導入する利用例（Unityのインポート対象外）
├─ Tests/                   EditMode／PlayModeテスト
└─ Documentation~/         Asset Importされない利用者向け詳細文書（Modules/にモジュール別文書）
```

各機能の`Internal/`は公開APIではありません。利用側コードとSampleは`Internal/`の型へ依存しません。

## 起動と終了

`SymphonyOrchestrator`がComposition Rootです。利用側でBootstrapを作る必要はありません。

```mermaid
sequenceDiagram
    participant Unity
    participant Orchestrator as SymphonyOrchestrator
    participant Systems as Subsystems
    participant Scene as Initial Scene

    Unity->>Orchestrator: BeforeSceneLoad
    Orchestrator->>Orchestrator: 前回の状態をShutdown
    Orchestrator->>Orchestrator: DontDestroyOnLoadオブジェクト生成
    Orchestrator->>Systems: Save → Pause → Service → Scene → Audio → HUDを初期化
    Unity->>Scene: 最初のシーンをロード
    Unity->>Orchestrator: AfterSceneLoad
    Orchestrator->>Systems: SceneLoadConfigを渡してシーン管理開始
    Note over Orchestrator,Systems: 終了時は初期化と逆順にResetRuntimeState
```

Play ModeのDomain Reloadが無効でも状態を持ち越さないよう、開始時に既存状態を破棄し、終了時は初期化済みサブシステムを逆順で解放します。

## 公開Facadeと拡張点

```mermaid
classDiagram
    class ServiceLocator {
        <<static>>
        RegisterInstance()
        RegisterInstanceWithAutoDispose()
        UnregisterInstance()
        GetRequiredInstance()
        GetRegistrationInfos()
        TryGetRegistrationInfo()
    }
    class ServiceRegistrationInfo {
        <<readonly struct>>
        ServiceType
        Instance
        LocateTypeEnum
    }
    class ServiceInjector {
        <<static>>
        Inject()
    }
    class IInjectable {
        <<interface>>
        Inject()
    }
    class SceneLoader {
        <<static>>
        LoadScene()
        UnloadScene()
        SetActiveScene()
        GetSceneInfos()
        TryGetSceneInfo()
    }
    class SceneLoadRequest {
        <<readonly struct>>
        SceneName
        Priority
    }
    class SceneLoadInfo {
        <<readonly struct>>
        SceneName
        State
        Priority
        Progress
        IsActive
    }
    class IInitializeAsync {
        <<interface>>
        InitializeAsync()
    }
    class SaveStore {
        <<static>>
        Get()
        LoadAsync()
        SaveAsync()
        DeleteAsync()
    }
    class SaveDataContent {
        <<abstract>>
    }
    class SaveDataLoaderStrategy {
        <<abstract>>
        LoadJsonAsync()
        SaveJsonAsync()
        DeleteCoreAsync()
        SerializeToJson()
        OverwriteFromJson()
    }
    class AudioManager {
        <<static>>
        GetAudioSource()
        VolumeSliderChanged()
    }
    class PauseManager {
        <<static>>
        Pause
        PausableWaitForSecondAsync()
    }
    class IPausable {
        <<interface>>
        OnPause()
        OnResume()
    }

    ServiceInjector ..> ServiceLocator : 登録済み依存を取得
    ServiceLocator --> ServiceRegistrationInfo : 登録状態のスナップショット
    ServiceInjector --> IInjectable : 注入
    SceneLoader --> SceneLoadRequest : ロード対象と優先度
    SceneLoader --> SceneLoadInfo : 追跡状態のスナップショット
    SceneLoader ..> ServiceInjector : ルートへ自動注入
    SceneLoader --> IInitializeAsync : 完了を待機
    SaveStore --> SaveDataContent : 型単位でキャッシュ
    SaveStore --> SaveDataLoaderStrategy : 永続化を委譲
    PauseManager --> IPausable : 状態を通知
```

Facadeは利用側の入口、interfaceと抽象classは利用側が実装する拡張点です。ConfigとManagerの内部実装はFacadeの背後に隠し、利用側へ公開しません。

サブシステム個別の内部構造は、各モジュール文書を参照してください。

- [Scene Load](./Modules/SceneLoader.md#内部構造)
- [Service Locate](./Modules/ServiceLocator.md#内部構造)
- [Save Data](./Modules/SaveDataSystem.md#内部構造)
- [Pause](./Modules/PauseManager.md#内部構造)
- [Audio](./Modules/AudioManager.md#内部構造)

## シーンロード時の連携

```mermaid
flowchart TD
    Request["SceneLoader.LoadScene"] --> Load["シーンを非同期ロード"]
    Load --> Roots["ルートGameObjectを走査"]
    Roots --> Inject{"IInjectableを実装?"}
    Inject -- Yes --> Resolve["ServiceLocatorから依存を解決して注入"]
    Inject -- No --> Initialize
    Resolve --> Initialize{"IInitializeAsyncを実装?"}
    Initialize -- Yes --> Await["InitializeAsyncの完了を待機"]
    Initialize -- No --> Complete["優先度を評価してロード完了"]
    Await --> Complete
    Resolve -. "失敗" .-> Exception["SceneInitializationException"]
    Await -. "失敗" .-> Exception
```

この連携が必要なシーンではUnity標準の`SceneManager.LoadScene`を直接使わず、`SceneLoader`を入口にします。
