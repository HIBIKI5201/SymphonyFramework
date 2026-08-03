# Symphony Frameworkの構成

この文書は、パッケージ内を移動するときや、公開型どうしの関係を確認するときの構成図です。APIの使用例は[README.md](../README.md)、AIエージェント向けの判断ルールは[AgentUsage.md](./AgentUsage.md)を参照してください。

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
│  ├─ System/              Scene、Service、Save、Audio、Pause
│  └─ Utility/             Awaitable、Tween、文字列等の補助
├─ Editor/                  Editor専用UI、Drawer、Generator
├─ Samples/Runtime/         Package Managerから導入する利用例
├─ Tests/                   EditMode／PlayModeテスト
└─ Documentation~/         Asset Importされない利用者向け詳細文書
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
    Orchestrator->>Systems: SceneManagerConfigを渡してシーン管理開始
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
        LocateType
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
    class SaveDataRegistry {
        <<static>>
        Get()
        LoadAsync()
        SaveAsync()
        DeleteAsync()
    }
    class SaveDataContent {
        <<abstract>>
    }
    class SaveDataLoader {
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
    SaveDataRegistry --> SaveDataContent : 型単位でキャッシュ
    SaveDataRegistry --> SaveDataLoader : 永続化を委譲
    PauseManager --> IPausable : 状態を通知
```

Facadeは利用側の入口、interfaceと抽象classは利用側が実装する拡張点です。ConfigとManagerの内部実装はFacadeの背後に隠し、利用側へ公開しません。

Scene Loadの内部では、Commandと読み取りを分離しています。

```mermaid
flowchart LR
    Loader["SceneLoader"] -->|Command| Service["SceneLoadService"]
    Service --> Registry["SceneLoadRegistry"]
    Registry --> Entity["SceneLoadEntity"]
    Loader -->|Query| Query["SceneLoadQuery"]
    Query --> Registry
    Query --> Info["SceneLoadInfo"]
    Service -->|状態変更event| ViewModel["SceneLoadViewModel"]
    ViewModel -->|ReactiveProperty| Window["SceneLoaderWindow"]
```

`SceneLoadQuery`だけがRegistry／Entityを読み取り、利用側には`SceneLoadInfo`、Viewには内部Dtoを返します。`SceneLoaderWindow`はViewModelを購読し、RuntimeのRegistryやEntityを直接参照しません。

Service LocateのRuntime内部では、登録Command、読み取り変換、表示状態、Unity所有処理を分離しています。

```mermaid
flowchart LR
    Locator["ServiceLocator"] -->|Command| Service["ServiceLocateService"]
    Service --> Registry["ServiceLocateRegistry"]
    Registry --> Entity["ServiceRegistrationEntity"]
    Locator -->|Query| Query["ServiceLocateQuery"]
    Query --> Registry
    Query --> Info["ServiceRegistrationInfo"]
    Query --> Dto["ServiceLocateDto"]
    Service -->|状態変更event| ViewModel["ServiceLocateViewModel"]
    ViewModel -->|ReactiveProperty| Window["ServiceLocatorWindow"]
    Service --> HostContract["IServiceHost"]
    Host["ServiceHostComponent"] --> HostContract
    Host --> Unity["Component / Transform / Destroy"]
    Orchestrator["SymphonyOrchestrator"] -->|生成・注入| Host
```

`ServiceLocateQuery`だけがRegistry／Entityを読み取り、利用側には`ServiceRegistrationInfo`、Viewには内部Dtoを返します。`ServiceLocatorWindow`はViewModelを購読し、登録状態が変わったときだけ再描画します。

通常登録が型重複で失敗しても候補payloadを解放しません。`RegisterInstanceWithAutoDispose`を選んだ場合だけ、失敗した候補を`ServiceHostComponent`が`IDisposable`またはComponentとして解放します。RegistryとEntityはUnity APIを参照しません。

Save DataのRuntime内部も同じ形で、処理順、読み取り変換、表示状態を分離しています。

```mermaid
flowchart LR
    Facade["SaveDataRegistry"] -->|Command| Service["SaveDataService"]
    Service --> Registry["SaveDataEntryRegistry"]
    Registry --> Entity["SaveDataEntryEntity"]
    Facade -->|Query| Query["SaveDataQuery"]
    Query --> Registry
    Query --> Info["SaveDataRegistryEntryInfo"]
    Query --> Dto["SaveDataDto"]
    Service --> Loader["SaveDataLoader"]
    Service -->|状態変更event| ViewModel["SaveDataViewModel"]
    ViewModel -->|ReactiveProperty| Window["SaveDataRegistryWindow"]
```

`SaveDataQuery`だけがRegistry／Entityを読み取り、利用側には`SaveDataRegistryEntryInfo`、Viewには内部Dtoを返します。`SaveDataRegistryWindow`はViewModelを購読し、状態が変わったときだけ再描画します。

永続化データが存在するかどうか（`Exists`）はQueryに含めません。ローダーへのI/Oであり、状態が変わるたびに全型分の問い合わせが走るためです。

Pauseも同じ形で、状態の保持、購読の管理、表示状態を分離しています。

```mermaid
flowchart LR
    Facade["PauseManager"] -->|Command| Service["PauseService"]
    Service --> State["PauseStateEntity"]
    Service --> Registry["PausableRegistry"]
    Facade -->|Query| Query["PauseQuery"]
    Query --> State
    Query --> Registry
    Query --> Info["PauseInfo"]
    Query --> Dto["PauseDto"]
    Service -->|状態変更event| ViewModel["PauseViewModel"]
    ViewModel -->|ReactiveProperty| Window["PauseWindow"]
```

`PauseStateEntity`が「状態が実際に変化したか」を判定するため、同じ値の再設定では`OnPauseChanged`を発行しません。

Audioは読み取り経路の利用者（Editor WindowとMCP診断）が無いため、QueryとViewModelを持ちません。

```mermaid
flowchart LR
    Facade["AudioManager"] -->|Command| Service["AudioService"]
    Service --> Registry["AudioGroupRegistry"]
    Registry --> Entity["AudioGroupEntity"]
    Service --> HostContract["IAudioSourceHost"]
    Host["AudioSourceHost"] --> HostContract
    Host --> Factory["ISystemObjectFactory"]
    Service --> Config["AudioManagerConfig / AudioMixer"]
```

`AudioSourceHost`は**最初にAudioSourceを求められた時点で初めてGameObjectを生成します。** AudioMixerが未割り当てなどでAudioSourceを1つも作らない場合、GameObjectも作られません。

`OnPauseChanged`（利用側が購読する公開event）の購読者例外は握りません。`OnStateChanged`（表示専用）だけは発行側で捕捉してログへ出し、ViewModelの失敗をゲーム側のポーズ処理へ波及させません。

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
