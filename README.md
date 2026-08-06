# Symphony Framework

Symphony Frameworkは、Unityゲームで何度も作ることになる「シーン遷移」「サービス共有」「セーブ」「オーディオ」「ポーズ」を、同じ使い方で扱えるようにまとめたフレームワークです。

最初のシーンより前に自動で初期化されるため、専用のBootstrapシーンやManagerプレハブを用意せず、必要な機能から使い始められます。

- 対応Unity: **Unity 6（6000.0）以降**
- 現在のバージョン: **3.8.2**
- ライセンス: **MIT**

## Symphony Frameworkでできること

| やりたいこと | 使う機能 |
| --- | --- |
| シーンを非同期で読み込み、進捗やActive Sceneを管理したい | Scene Loader |
| シーンを跨いでサービスやComponentを共有したい | Service Locator |
| データ型ごとに保存・読み込み・削除したい | Save Data System |
| AudioMixerGroupごとの再生元と音量をまとめたい | Audio Manager |
| ゲーム全体を止め、待機やTweenもポーズへ追従させたい | Pause Manager |
| 実行中の状態、FPS、ログ、処理時間を確認したい | Debug Tools |
| Scene、Tag、Layer等をenum化し、Inspector入力を安全にしたい | Editor Tools |

すべてを導入時に設定する必要はありません。使いたい機能のクイックスタートとサンプルから始められます。

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

### Service Locator

共有したいインスタンスを登録し、必要な場所から型で取得します。

```csharp
using SymphonyFrameWork.System.ServiceLocate;
using UnityEngine;

public sealed class GameSession : MonoBehaviour
{
    private void OnEnable()
    {
        ServiceLocator.RegisterInstance(this, LocateTypeEnum.Locator);
    }

    private void OnDisable()
    {
        ServiceLocator.UnregisterInstance(this);
    }
}
```

```csharp
GameSession session = ServiceLocator.GetRequiredInstance<GameSession>();
GameSession awaited = await ServiceLocator.GetInstanceAsync<GameSession>(
    grace: 10,
    token: destroyCancellationToken);
```

`Locator`は参照だけを登録し、`Singleton`はComponentを永続管理オブジェクト配下へ移動します。GameObjectへ`ServiceLocateComponent`を追加してInspectorから登録することもできます。

通常の`RegisterInstance`が型重複で`false`を返した場合、渡した候補の所有権は呼び出し側に残ります。重複した新規候補を以後使わず、自動的に`Dispose`またはGameObject破棄してよい場合だけ、所有権移譲を明示する`RegisterInstanceWithAutoDispose`を使います。

```csharp
ServiceLocator.RegisterInstanceWithAutoDispose(
    newSession,
    LocateTypeEnum.Singleton);
```

登録中の型、payload、登録方式を一覧で診断する場合は、取得時点の不変な`ServiceRegistrationInfo`を使います。既知の型だけを調べる場合は`TryGetRegistrationInfo`を使えます。

```csharp
foreach (ServiceRegistrationInfo registration
         in ServiceLocator.GetRegistrationInfos())
{
    Debug.Log(
        $"{registration.ServiceType.Name}: {registration.LocateType}");
}
```

シーンロード時の自動注入には`IInjectable<T...>`、実行時に生成したオブジェクトへの手動注入には`ServiceInjector.Inject(...)`を使います。

### Scene Loader

対象シーンをBuild Settingsへ追加してから、名前を指定してロードします。

```csharp
using System;
using SymphonyFrameWork.System.SceneLoad;
using UnityEngine;
using UnityEngine.SceneManagement;

public async void OpenGameScene()
{
    var request = new SceneLoadRequest("Game", priority: 10);
    IProgress<float> progress = new Progress<float>(
        value => Debug.Log($"Loading: {value:P0}"));

    bool succeeded = await SceneLoader.LoadSceneAsync(
        request,
        progress,
        mode: LoadSceneMode.Additive,
        token: destroyCancellationToken);

    if (succeeded)
    {
        SceneLoader.SetActiveScene("Game");
    }
}
```

`SceneLoadRequest`はシーン名とActive Scene選択用の優先度を1つの値として扱います。複数シーンでは`SceneLoadRequest[]`を`LoadScenesAsync`へ渡せるため、シーン名と優先度の対応を崩しません。進捗の推奨契約は`IProgress<float>`です。既存のシーン名と`Action<float>`を使うoverloadも互換性のため利用できます。

追跡中の全シーンは、変更不能な`SceneLoadInfo`のスナップショットとして取得できます。取得後にロード状態が変わっても、既に取得した値は変化しません。

```csharp
foreach (SceneLoadInfo sceneInfo in SceneLoader.GetSceneInfos())
{
    Debug.Log(
        $"{sceneInfo.SceneName}: {sceneInfo.State}, "
        + $"Priority={sceneInfo.Priority}, Progress={sceneInfo.Progress:P0}, "
        + $"Active={sceneInfo.IsActive}");
}

if (SceneLoader.TryGetSceneInfo("Game", out SceneLoadInfo gameScene))
{
    Debug.Log($"Game scene progress: {gameScene.Progress:P0}");
}
```

Scene Loaderは非同期ロード、進捗、複数シーン、優先度によるActive Scene切り替えをまとめて扱います。ルートGameObjectが`IInjectable<T...>`や`IInitializeAsync`を実装している場合は、注入と初期化の完了も待機します。

### Save Data System

保存したいデータを`SaveDataContent`の派生classとして定義します。

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

初回は`LoadAsync<T>()`で読み込み、戻り値のインスタンスを編集して保存します。

```csharp
PlayerData data = await SaveStore.LoadAsync<PlayerData>();
data.Gold += 100;

await SaveStore.SaveAsync<PlayerData>();
await SaveStore.DeleteAsync<PlayerData>();
```

読み込み済みであれば、同じインスタンスを`Get<T>()`で同期的に取り出せます。**`Get<T>()`は暗黙の読み込みを行いません。** 未読み込みの型を渡すと`InvalidOperationException`になります。非同期I/Oを行うローダーで暗黙の同期読み込みを許すと、待機を同期ブロックして完了不能になるためです。

```csharp
if (SaveStore.IsLoaded<PlayerData>())
{
    PlayerData cached = SaveStore.Get<PlayerData>();
}
```

既定ではJSONをPlayerPrefsへ保存します。別のシリアライザ、ファイル、クラウド等を使う場合は`SaveDataLoaderStrategy`を継承し、Project Settingsから選択します。

### Audio Manager

`AudioConfig.asset`へAudioMixerとグループを登録すると、グループ名からAudioSourceを取得できます。

```csharp
using SymphonyFrameWork.System;
using UnityEngine;

AudioSource bgm = AudioManager.GetAudioSource("BGM");
bgm.clip = bgmClip;
bgm.Play();

AudioManager.VolumeSliderChanged("BGM", 0.5f);
```

AudioSourceはグループごとに必要になった時点で生成され、シーン遷移後も保持されます。音量は0〜1の比率で指定します。

### Pause Manager

ゲーム全体のポーズ状態を切り替え、通知やポーズ対応の待機処理を利用できます。

```csharp
using SymphonyFrameWork.System;

private void OnEnable()
{
    PauseManager.OnPauseChanged += HandlePauseChanged;
}

private void OnDisable()
{
    PauseManager.OnPauseChanged -= HandlePauseChanged;
}

private void HandlePauseChanged(bool paused)
{
    // UI更新など
}

public void SetPaused(bool paused)
{
    PauseManager.Pause = paused;
}

private async void RunAfterOneActiveSecond()
{
    await PauseManager.PausableWaitForSecondAsync(1.0f, destroyCancellationToken);
    // ポーズ時間を除く1秒後の処理
}
```

Coroutine、非同期処理、遅延Destroy、遅延Invoke、Tweenをポーズへ追従させられます。オブジェクト単位の通知には`PauseManager.IPausable`を実装します。

実装時の例外、キャンセル、登録解除などの注意点は[AIエージェント向け利用上の注意](./Documentation~/AgentUsage.md)にまとめています。

## Editor・デバッグ支援

各機能の入口、設定の保存先、注意点は[Editor機能](./Documentation~/EditorTools.md)にあります。

- `Symphony Administrator`: Service Locator、Scene Loader、Save Data、Pauseの状態確認
- `SymphonyDebugHUD`: FPS、メモリ使用量、任意テキストのGame View表示
- `SymphonyDebugLogger`: 複数行ログとEditorでの`Cache/Log.txt`出力
- `SymphonyStopWatch`: ID単位の簡易処理時間計測
- `AutoEnumGenerator`: Scene、Tag、Layer、Audio Groupのenum生成
- `FolderGenerator`: Markdownからプロジェクトフォルダを生成
- `AssemblyGenerator`: asmdefの作成と参照追加
- Inspector属性: `[ReadOnly]`、`[DisplayText]`、`[TagSelector]`、`[SceneNameSelector]`、`[SubclassSelector]`
- Utility: `SymphonyAwaitable`、`SymphonyTween`、`SymphonyStringUtil`、`SymphonyComponentUtil`

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
- [AGENTS.md（AIエージェント向けの導線と常時ルール）](./AGENTS.md)
  - [利用コードを書くときの注意事項](./Documentation~/AgentUsage.md)
  - [利用コードの検証手順](./Documentation~/AgentVerification.md)
- [Symphony Framework Document](https://lying-foxglove-81a.notion.site/Symphony-Framework-Document-19b7c2c6cc02806b9b97cb8a97c9f11a?pvs=74)

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
