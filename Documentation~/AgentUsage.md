# AIエージェント向け利用上の注意

この文書は、Symphony Frameworkを導入したプロジェクトの`Assets/`配下へ利用コードを書くAIエージェント向けです。APIの説明とコード例は[README.md](../README.md)を正本とし、ここでは実装時に誤りやすい判断だけを扱います。

必要なモジュールの節だけを読んでください。パッケージ本体を開発する場合はこの文書ではなく、[SymphonyWorkspaceのCONTRIBUTING.md](https://github.com/HIBIKI5201/SymphonyWorkspace/blob/dev/Documentation/CONTRIBUTING.md)に従います。

## 共通の前提

- `Packages/manifest.json`またはパッケージの`package.json`で導入バージョンを確認し、移行情報が必要なら[CHANGELOG.md](../CHANGELOG.md)を読む。
- asmdefを使う利用側コードは`SymphonyFrameWork`を参照する。自動生成enumを直接使う場合だけ`SymphonyFrameWork.Enum`も参照する。
- `ServiceLocator`、`SceneLoader`、`SaveDataRegistry`、`AudioManager`、`PauseManager`はstatic Facadeである。`new`や`.Instance`は使わない。
- `SymphonyOrchestrator`が最初のシーンより前に自動初期化する。Bootstrap用GameObjectや専用シーンを作らない。
- `SceneManagerConfig`、`AudioManagerConfig`、`SaveSystemConfig`は`internal`である。型として参照せず、InspectorまたはProject Settingsから設定する。
- `SymphonyFrameWork.Editor`はEditor専用である。Runtime asmdefやPlayerビルド対象コードから参照しない。
- パッケージ直下の`Cache/Log.txt`はEditor用の生成キャッシュである。編集・コミットせず、不要なら削除してよい。

## APIの参照先

| 機能 | namespace | 主な入口 | README |
| --- | --- | --- | --- |
| Service Locator | `SymphonyFrameWork.System.ServiceLocate` | `ServiceLocator`, `ServiceInjector` | [Service Locator](../README.md#service-locator) |
| Scene Loader | `SymphonyFrameWork.System.SceneLoad` | `SceneLoader` | [Scene Loader](../README.md#scene-loader) |
| Save Data System | `SymphonyFrameWork.System.SaveSystem` | `SaveDataRegistry`, `SaveDataContent`, `SaveDataLoader` | [Save Data System](../README.md#save-data-system) |
| Audio Manager | `SymphonyFrameWork.System` | `AudioManager` | [Audio Manager](../README.md#audio-manager) |
| Pause Manager | `SymphonyFrameWork.System` | `PauseManager` | [Pause Manager](../README.md#pause-manager) |

## Service Locator

- 登録と解除を同じライフサイクルの対として書く。基本形は`OnEnable`で`RegisterInstance`、`OnDisable`で`UnregisterInstance`。
- 通常の`RegisterInstance`がfalseの場合も候補の所有権は呼び出し側に残る。型重複時に候補を自動解放してよい場合だけ`RegisterInstanceWithAutoDispose`へ所有権を移す。
- `LocateType.Locator`は参照だけを登録する。`LocateType.Singleton`はComponentを管理オブジェクト配下へ移動するため、シーンローカルなオブジェクトには使わない。
- 任意依存は`TryGetInstance<T>`、nullを許容する既存コードは`GetInstance<T>`、必須依存は`GetRequiredInstance<T>`を使う。
- 登録中の型と登録方式を一覧で調べる場合は`GetRegistrationInfos()`、既知の型だけを調べる場合は`TryGetRegistrationInfo(Type, out ...)`を使う。返る`ServiceRegistrationInfo`は取得時点のスナップショットとして扱う。
- `GetInstanceAsync<T>`の期限超過は`TimeoutException`、呼び出し側キャンセルは`OperationCanceledException`。`TryGetInstanceAsync<T>`がfalseへ変換するのは期限超過だけ。
- `SceneLoader`が自動注入するのはロードしたシーンのルートにある`IInjectable<T...>`。実行時`Instantiate`したオブジェクトには`ServiceInjector.Inject(...)`を手動で呼ぶ。

## Scene Loader

- 対象シーンがBuild SettingsのScene Listにあることを、シーン名をコードへ書く前に確認する。
- シーン遷移は原則`SceneLoader`を使う。`SceneManager.LoadScene`を直接使うと、優先度管理、依存注入、`IInitializeAsync`が働かない。
- `LoadSceneMode.Single`は対象をAdditiveでロードした後、他の追跡シーンをアンロードする。現在のシーンを残す場合は`Additive`を使う。
- 依存注入または`IInitializeAsync`の失敗は`SceneInitializationException`。Build Settings未登録など通常のロード失敗はfalseで返る。
- 追跡中のScene名が分からない状態で一覧を調べる場合は`SceneLoader.GetSceneInfos()`を使う。既知のScene名だけを調べる場合は`TryGetSceneInfo`を使い、戻る`SceneLoadInfo`を取得時点のスナップショットとして扱う。

## Save Data System

- 保存型は`SaveDataContent`を継承した、デフォルトコンストラクタを持つ具象classにする。
- `SaveDataRegistry.Get<T>()`が返す型単位のキャッシュを編集する。別インスタンスとの二重管理を作らない。
- 非同期I/Oを行う独自ローダーでは、先に`LoadAsync<T>()`をawaitしてから`Get<T>()`する。
- 保存先の失敗は`SaveDataOperationException`の`Operation`、`DataType`、`LoaderType`、`InnerException`で診断する。キャンセルは`OperationCanceledException`のまま伝播する。
- 独自`SaveDataLoader`はProject Settingsの選択肢へ自動登録される。設定ScriptableObjectを手動生成しない。

## Audio Manager

- `GetAudioSource`で使うグループ名が`AudioManagerConfig.asset`へ登録済みか確認する。
- `VolumeSliderChanged`へ渡す値はdBではなく0〜1の比率。

## Pause Manager

- ポーズ中も止める待機には`PausableWaitForSecondAsync`、`PausableWaitForSecond`、`PausableNextFrameAsync`を使う。`Task.Delay`や通常の`WaitForSeconds`では代用しない。
- `PauseManager.IPausable`は有効化時に登録し、無効化時に解除する。

## 非推奨APIと移行

非推奨APIの期限と移行先は[CHANGELOG.md](../CHANGELOG.md)を正本とします。古いコードを修正するときは、現在のバージョンのソースとCHANGELOGを確認し、過去バージョンのnamespaceやAPI一覧をこの文書へ固定しないでください。
