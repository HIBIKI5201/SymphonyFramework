# Symphony Framework

Unity プロジェクトで繰り返し必要になる、シーン遷移、サービス管理、セーブデータ、オーディオ、ポーズ、デバッグ機能をまとめたゲーム開発向けフレームワークです。

- 対応 Unity: **Unity 6（6000.0）以降**
- 現在のバージョン: **1.27.20**
- ライセンス: **MIT**

## 主な機能

| 機能 | 概要 |
| --- | --- |
| Scene Loader | 非同期ロード／アンロード、進捗通知、複数シーン、優先度によるActive Scene管理 |
| Service Locator | Component、interface、通常のclassの登録・取得・待機・破棄 |
| Save Data System | 型単位のキャッシュ、ロード、保存、削除、ローダーの差し替え |
| Audio Manager | AudioMixerGroupごとのAudioSource生成と音量制御 |
| Pause Manager | ポーズ状態、通知イベント、ポーズ対応の待機・Tween |
| Debug Tools | 管理ウィンドウ、ランタイムHUD、ロガー、ストップウォッチ |
| Editor Tools | Scene／Tag／Layer／Audio enum生成、フォルダ・asmdef生成、Inspector属性 |

## 必要なパッケージ

依存パッケージは `package.json` に定義されています。UPMで導入した場合はUnityが自動的に解決します。

- Addressables `1.21.19`
- Newtonsoft Json `3.2.1`

## インストール

### Unity Package Managerから導入する

1. Unityで `Window > Package Manager` を開きます。
2. `+` から `Install package from git URL...` を選びます。
3. 次のURLを入力します。

```text
https://github.com/HIBIKI5201/SymphonyFramework.git
```

`Packages/manifest.json` に直接追加する場合は、`dependencies` に次の項目を追加します。

```json
{
  "dependencies": {
    "symphonyframework": "https://github.com/HIBIKI5201/SymphonyFramework.git"
  }
}
```

特定バージョンに固定する場合は、リポジトリに存在するタグまたはコミットをURL末尾の `#` 以降へ指定してください。

### Assetsとして導入する

ソースを直接配置する場合は、フレームワークのルートが次のパスになるようにします。

```text
Assets/SymphonyFrameWork
```

Editor拡張のパス判定とアセット保護がこの配置を前提としているため、フォルダ名は変更しないでください。

## 初期設定

導入後にスクリプトのコンパイルが完了すると、必要な設定アセットと自動生成コードが作成されます。

```text
Assets/
├─ Resources/SymphonyFrameWork/
│  ├─ SceneManagerConfig.asset
│  ├─ AudioManagerConfig.asset
│  └─ SaveSystemConfig.asset
└─ Scripts/SymphonyFrameWork/
   ├─ SceneListEnum.cs
   ├─ TagsEnum.cs
   ├─ LayersEnum.cs
   ├─ AudioGroupTypeEnum.cs
   └─ SymphonyFrameWork.Enum.asmdef
```

ランタイムでは `SymphonyCoreSystem` が最初のシーンより前に自動初期化され、専用の `SymphonySystem` シーンを作成します。Bootstrap用のGameObjectを手動で置く必要はありません。

主な設定場所は次のとおりです。

- `Window > SymphonyFrameWork > Symphony Administrator`: Scene、Service Locator、Save Data、Pause、enum生成の状態確認と操作
- `Project Settings > SymphonyFrameWork > Save System`: セーブデータローダーの選択
- `Assets/Resources/SymphonyFrameWork/SceneManagerConfig.asset`: 再生開始時のシーン初期化
- `Assets/Resources/SymphonyFrameWork/AudioManagerConfig.asset`: AudioMixerとグループ設定

asmdefを使用しているゲーム側コードから本フレームワークを利用する場合は、`SymphonyFrameWork` を参照に追加してください。自動生成enumを直接使う場合は `SymphonyFrameWork.Enum` も追加します。

## クイックスタート

### Service Locator

インスタンスはコードから登録できるほか、GameObjectへ `SymphonyLocate` を追加してInspectorから登録できます。

```csharp
using SymphonyFrameWork.System.ServiceLocate;
using UnityEngine;

public sealed class GameSession : MonoBehaviour
{
    private void OnEnable()
    {
        ServiceLocator.RegisterInstance(this, LocateType.Locator);
    }

    private void OnDisable()
    {
        ServiceLocator.UnregisterInstance(this);
    }
}

public sealed class PlayerController : MonoBehaviour
{
    private async void Start()
    {
        // 未登録なら、登録されるまで最大10秒待機します。
        GameSession session = await ServiceLocator.GetInstanceAsync<GameSession>(
            grace: 10,
            token: destroyCancellationToken);

        // 登録済みと分かっている場合は同期的に取得できます。
        GameSession sameSession = ServiceLocator.GetInstance<GameSession>();
    }
}
```

`LocateType` の違い:

- `Locator`: 参照のみを登録し、GameObjectの親子関係を変更しません。
- `Singleton`: Componentの場合はService Locatorの管理オブジェクト配下へ移動し、通常のシーン遷移から分離します。

interface型で登録する場合は型引数を明示します。

```csharp
ServiceLocator.RegisterInstance<IGameSession>(session);
IGameSession current = ServiceLocator.GetInstance<IGameSession>();
```

`IInjectable<T...>` と `ServiceInjector.Inject(...)` を使った、最大4依存までの明示的な注入にも対応しています。

### Scene Loader

対象シーンをBuild SettingsのScene Listへ追加してから使用してください。

```csharp
using SymphonyFrameWork.System.SceneLoad;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneTransition : MonoBehaviour
{
    public async void OpenGameScene()
    {
        bool succeeded = await SceneLoader.LoadScene(
            sceneName: "Game",
            loadingAction: progress => Debug.Log($"Loading: {progress:P0}"),
            mode: LoadSceneMode.Additive,
            priority: 10,
            token: destroyCancellationToken);

        if (succeeded)
        {
            SceneLoader.SetActiveScene("Game");
        }
    }

    public async void CloseGameScene()
    {
        await SceneLoader.UnloadScene("Game", token: destroyCancellationToken);
    }
}
```

優先度が現在のActive Scene以上のシーンをロードすると、そのシーンがActiveになります。Active Sceneをアンロードした場合は、ロード済みの中から最も優先度が高いシーンへ切り替わります。

`SceneManagerConfig` では、再生開始時に既存シーンをリセットするか、最初にロードするシーン、リセット対象外のシーンを設定できます。

ロードしたシーンのルートGameObjectが `IInitializeAsync` を実装している場合、`SceneLoader` はその初期化処理がすべて完了してからロード成功を返します。

### Save Data System

セーブ対象は `SaveDataContent` を継承した、デフォルトコンストラクタを持つ具象classとして定義します。

```csharp
using System;
using SymphonyFrameWork.System.SaveSystem;

[Serializable]
public sealed class PlayerData : SaveDataContent
{
    public int Level = 1;
    public int Gold;
}
```

Registryが保持するインスタンスを編集し、その型を指定して保存します。

```csharp
PlayerData data = SaveDataRegistry.Get<PlayerData>();
data.Gold += 100;

await SaveDataRegistry.SaveAsync<PlayerData>();
await SaveDataRegistry.LoadAsync<PlayerData>();
await SaveDataRegistry.DeleteAsync<PlayerData>();
```

- `Get<T>()`: 同じ型について単一のキャッシュを返します。初回は永続化データを同期的にロードします。
- `LoadAsync<T>()`: 永続化データを現在のキャッシュへ読み込みます。
- `SaveAsync<T>()`: 現在のキャッシュを保存し、`SaveDate` を更新します。
- `DeleteAsync<T>()`: 永続化データを削除し、キャッシュを初期値へ戻します。

既定では `JsonUtilitySaveDataLoader` がJSONをPlayerPrefsへ保存します。`Project Settings > SymphonyFrameWork > Save System` から `NewtonsoftSaveDataLoader` へ変更できます。ファイルやクラウドなど別の保存先を使う場合は `SaveDataLoader` を継承して各抽象メソッドを実装してください。

非同期I/Oを行う独自ローダーでは、メインスレッドをブロックしないよう、最初に `await SaveDataRegistry.LoadAsync<T>()` を呼んでから `Get<T>()` することを推奨します。

### Audio Manager

1. `AudioManagerConfig.asset` にAudioMixerを割り当てます。
2. AudioMixerGroup名、公開したVolume Parameter名、Loopの有無を登録します。
3. グループ名でAudioSourceを取得して再生します。

```csharp
using SymphonyFrameWork.System;
using UnityEngine;

AudioSource bgm = AudioManager.GetAudioSource("BGM");
bgm.clip = bgmClip;
bgm.Play();

// 0.0～1.0の割合で音量を変更します。
AudioManager.VolumeSliderChanged("BGM", 0.5f);
```

AudioSourceはグループごとに遅延生成され、`SymphonySystem` シーンで管理されます。

### Pause Manager

```csharp
using SymphonyFrameWork.System;

PauseManager.OnPauseChanged += paused =>
{
    // UI更新など
};

PauseManager.Pause = true;
await PauseManager.PausableWaitForSecondAsync(1.0f, destroyCancellationToken);
PauseManager.Pause = false;
```

ポーズ中に経過時間を止めるCoroutine／Task、遅延Destroy、遅延Invoke、`PausableTweening` を利用できます。オブジェクト単位の通知には `PauseManager.IPausable` を実装し、`IPausable.RegisterPauseManager(this)`／`IPausable.UnregisterPauseManager(this)` で購読を管理します。

## Editor・デバッグ支援

- `Symphony Administrator`: Service Locator、Scene Loader、Save Data Registry、Pause状態を再生中に確認
- `SymphonyDebugHUD`: FPS、メモリ使用量と任意テキストをGame Viewへ表示
- `SymphonyDebugLogger`: 複数行ログの組み立てと種別付き出力
- `SymphonyStopWatch`: ID単位の簡易処理時間計測
- `AutoEnumGenerator`: Scene List、Tag、Layer、Audio Groupのenumを生成
- `FolderGenerator`: Markdownで定義したプロジェクトフォルダを生成
- `AssemblyGenerator`: asmdefの作成と参照追加
- Inspector属性: `[ReadOnly]`、`[DisplayText]`、`[TagSelector]`、`[SceneNameSelector]`、`[SubclassSelector]`
- Utility: `SymphonyTask`、`SymphonyTween`、`SymphonyStringUtil`、`SymphonyComponentUtil`

## サンプル

リポジトリの [`Samples/Runtime`](./Samples/Runtime) に次のサンプルがあります。

- `ServiceLocatorSample`: 登録、同期取得、非同期取得、Singletonのシーン跨ぎ
- `SaveDataSystemSample`: 複数データ型の編集、保存、再ロード、削除、Registry状態表示

## ディレクトリ構成

```text
Core/       Runtime／Editor共通の定数と基盤asmdef
Runtime/    ビルドに含まれるシステム、Component、Utility、属性
Editor/     設定画面、管理ウィンドウ、Drawer、Generator
Samples/    利用例
```

## ドキュメント

- [Symphony Framework Document](https://lying-foxglove-81a.notion.site/Symphony-Framework-Document-19b7c2c6cc02806b9b97cb8a97c9f11a?pvs=74)
- [変更履歴](./CHANGELOG.md)
- [コーディングガイドライン](./CodeGuidelines.md)

## コントリビューター

- [5unad0ke1](https://github.com/5unad0ke1)

## ライセンス

Copyright (c) 2026 HIBIKI_5201

このプロジェクトはMIT Licenseで公開されています。詳細は [`LICENSE.txt`](./LICENSE.txt) を参照してください。
