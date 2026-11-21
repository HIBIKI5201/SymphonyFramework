# 6.x から 7.0.0 への移行

7.0.0 は、名前空間・アセンブリ・主要な型の名前をそろえ直したメジャー更新です（#227）。この文書は、**6.x（main で配布した最後の版は 6.15.2）から 7.0.0 へ直接上げる**手順と、名前の対応、挙動が変わった点をまとめます。6.16.0〜6.20.0 は開発途中の版で、単独では配布していません。

## 先に結論

1. 更新の前に、プロジェクトをコミットするかバックアップを取ります。
2. パッケージを 7.0.0 へ上げて Unity を開きます。**多くの利用側コードは、そのまま警告（CS0618）付きでコンパイルできます。** 6.x の名前の Facade（`ServiceLocator`、`SceneLoader`、`SaveStore`、`AudioManager`、`PauseManager` など）は、新しい Facade へ転送する `[Obsolete]` 型として残しています。
3. 名前空間だけ移った型（属性、`SymphonyTween`、`SymphonyDebugLogger`、例外など）は、Unity の **API Updater** が書き換えます。確認のダイアログが出たら、バックアップを取ったうえで更新を許可してください。
4. 設定アセット、セーブデータ、Suite アセット、ポーズカテゴリーの生成物は、Editor の起動時にそのまま引き継がれるか、自動で移行されます（[自動で移行されるもの](#自動で移行されるもの)）。
5. 警告を消すには、[名前空間の対応](#名前空間の対応)と[型とメンバーの対応](#型とメンバーの対応)に沿って書き換えます。**転送用の旧名は 8.0.0 で削除します。**

## 名前空間の対応

`using` の行は、まず次の表のとおりに書き換えてください。表記は `SymphonyFrameWork`（W が大文字）から `SymphonyFramework` へ変わり、`System` の下にあった機能はそれぞれの名前空間へ移りました。

| 6.x | 7.0.0 | 主な型 |
| --- | --- | --- |
| `SymphonyFrameWork` | `SymphonyFramework` | `SymphonyComponentUtil`、`SymphonyStringUtil`、`IGameObject` |
| `SymphonyFrameWork.Attribute` | `SymphonyFramework.Attribute` | Inspector 属性 |
| `SymphonyFrameWork.Config` | `SymphonyFramework.Config` | 設定アセット |
| `SymphonyFrameWork.Core` | `SymphonyFramework.Core` | `SymphonyConstant`、`EditorSymphonyConstant` |
| `SymphonyFrameWork.Debugger` / `.HUD` / `.Logger` | `SymphonyFramework.Debugger` / `.HUD` / `.Logger` | `SymphonyDebugLogger`、`SymphonyDebugHUD`、`SymphonyStopWatch` |
| `SymphonyFrameWork.Editor` / `.Debugger` / `.SettingProvider` | `SymphonyFramework.Editor` / `.Debugger` / `.SettingProvider` | Editor 拡張 |
| `SymphonyFrameWork.Exceptions` | `SymphonyFramework.Exceptions` | `SymphonyNotInitializedException` |
| `SymphonyFrameWork.Utility` | `SymphonyFramework.Utility` | `SymphonyTween`、`SymphonyAwaitable`、`SymphonyVisualElement` |
| `SymphonyFrameWork.System.ServiceLocate` | `SymphonyFramework.Services` | `Partitura`（旧 `ServiceLocator` / `ServiceInjector`） |
| `SymphonyFrameWork.System.SceneLoad` | `SymphonyFramework.Scenes` | `Attacca`（旧 `SceneLoader`） |
| `SymphonyFrameWork.System.SceneBlock` | `SymphonyFramework.Scenes` | `Suite`（旧 `SceneBlockLoader`） |
| `SymphonyFrameWork.System.SaveSystem` | `SymphonyFramework.SaveData` | `Manuscript`（旧 `SaveStore`） |
| `SymphonyFrameWork.System`（Audio） | `SymphonyFramework.Audio` | `Sonority`（旧 `AudioManager`） |
| `SymphonyFrameWork.System`（Pause） | `SymphonyFramework.Pause` | `Fermata`（旧 `PauseManager`）、`IPausable`、`PauseInfo` |

アセンブリ名も `SymphonyFramework` / `SymphonyFramework.Core` / `SymphonyFramework.Editor` になりました。**自前の asmdef から名前で参照している場合は書き換えてください。** GUID で参照している場合はそのまま動きます。

## 型とメンバーの対応

### Partitura（旧 Service Locator）

| 6.x | 7.0.0 |
| --- | --- |
| `ServiceLocator.RegisterInstance` / `RegisterInstanceWithAutoDispose` | `Partitura.Register` / `RegisterWithAutoDispose` |
| `ServiceLocator.UnregisterInstance` / `DestroyInstance` / `IsExistInstance` | `Partitura.Unregister` / `Destroy` / `IsRegistered` |
| `ServiceLocator.GetRequiredInstance<T>()` | `Partitura.Get<T>()` |
| `ServiceLocator.GetInstance<T>()`（無ければ `null`） | `Partitura.TryGet<T>(out T)` |
| `ServiceLocator.TryGetInstance` / `GetInstanceAsync` / `TryGetInstanceAsync` | `Partitura.TryGet` / `GetAsync` / `TryGetAsync` |
| `ServiceLocator.RegisterAfterLocate<T>(Action<T>)` | `Partitura.WhenRegistered<T>(Action<T>)` |
| `ServiceLocator.RegisterAfterLocate<T>(Action)` | `Partitura.WhenRegistered<T>(_ => …)` |
| `ServiceInjector.Inject` / `CreateInstance` | `Partitura.Inject` / `CreateInstance` |
| `ServiceLocateComponent` | `ServiceComponent` |
| `SymphonyLocateObject<T>`（`GetInstance` / `GetInstanceAsync` / `TryGetInstance`） | `PartituraReference<T>`（`Get` / `GetAsync` / `TryGet`） |
| `LocateTypeEnum.Singleton` / `.Locator` | `ServiceLifetimeEnum.Persistent` / `.Scene` |
| `ServiceRegistrationInfo.LocateType` | `ServiceRegistrationInfo.Lifetime` |
| `SymphonyConfigLocator`（public） | 削除（内部の `SymphonyConfigProvider` になった） |
| `SymphonyEditorConfigLocator` | `SymphonyEditorConfigProvider` |

### Attacca と Suite（旧 Scene Loader と Scene Block）

| 6.x | 7.0.0 |
| --- | --- |
| `SceneLoader.LoadSceneAsync` / `LoadScenesAsync` | `Attacca.LoadAsync`（単体・配列の overload） |
| `SceneLoader.UnloadSceneAsync` / `UnloadScenesAsync` | `Attacca.UnloadAsync`（単体・配列の overload） |
| 進捗の `Action<float> loadingAction` | `IProgress<float> progress` |
| `SceneLoader.IsExist` / `GetExistScene` | `Attacca.IsLoaded` / `TryGetScene` |
| `SceneLoader.RegisterAfterSceneLoad` / `WaitForLoadSceneAsync` | `Attacca.WhenLoaded` / `WaitForLoadAsync` |
| `SceneBlockLoader.TryGetBlockInfo` / `GetBlockInfos` | `Suite.TryGetInfo` / `GetInfos` |
| `SceneBlockAsset` / `SceneBlockEntry` / `SceneBlockInfo` / `SceneBlockLoadStateEnum` / `SceneBlockPlanException` | `SuiteAsset` / `SuiteEntry` / `SuiteInfo` / `SuiteLoadStateEnum` / `SuitePlanException` |
| `SceneBlockAsset.BlockName` / `SceneBlockInfo.BlockName` | `SuiteAsset.SuiteName` / `SuiteInfo.SuiteName` |

### Manuscript と Sonority（旧 Save Store と Audio Manager）

| 6.x | 7.0.0 |
| --- | --- |
| `SaveStore`（`Exists` / `Get` / `IsLoaded` / `LoadAsync` / `SaveAsync` / `DeleteAsync` / `GetEntries`） | `Manuscript`（メソッド名は同じ） |
| `AudioManager.VolumeSliderChanged(name, value)` | `Sonority.SetVolume(name, value)` |
| `AudioManager.GetAudioSource(name)`（未登録は `null`） | `Sonority.TryGetSource(name, out source)` / `Sonority.GetSource(name)`（未登録は例外） |

### Fermata（旧 Pause Manager）

| 6.x | 7.0.0 |
| --- | --- |
| `PauseManager.IPausable`（入れ子） | `IPausable`（トップレベル） |
| `PauseManager.IPausable.RegisterPauseManager(x)` / `UnregisterPauseManager(x)` | `Fermata.Register(x)` / `Fermata.Unregister(x)` |
| `PauseManager.Pause`（プロパティ） | `Fermata.SetPauseAll` / `Fermata.IsPausedAny` |
| `PauseManager.OnPauseChanged`（イベント） | `Fermata.AddPauseChangedHandler(Action<bool>)` / `RemovePauseChangedHandler(Action<bool>)` |
| `PauseManager.PausableNextFrameAsync` | `Fermata.NextFrameAsync` |
| `PauseManager.PausableWaitForSecond` / `PausableWaitForSecondAsync` | `Fermata.WaitForSeconds` / `WaitForSecondsAsync` |
| `PauseManager.PausableWaitUntil` | `Fermata.WaitUntilAsync` |
| `PauseManager.PausableDestroy` / `PausableInvoke` | `Fermata.DestroyAsync` / `InvokeAsync` |

カテゴリーを指定しない待機・遅延・購読は、すべてのカテゴリーが対象です。どれか1つでもポーズ中なら止まります（6.12.0 以降と同じ動き）。

### 削除したAPI

6.x で `[Obsolete]` にしていた次のAPIは、7.0.0 に転送を残さず削除しました（[Deprecations.md](./Deprecations.md#削除済み)）。

| 6.x | 7.0.0 |
| --- | --- |
| `SymphonyDebugLogger.DirectLog` / `TextLog` | `LogDirect` / `LogText` |
| `SymphonyDebugLogger.CheckComponentNull` / `IsComponentNotNull` | `LogAndCheckComponentNull`（`IsComponentNotNull` とは戻り値の意味が逆） |
| `SymphonyTween.TweeningLerp` / `TweeningCurve` | `SymphonyTween.Tweening` |
| `AssetStoreToolsPackager.PackageModeEnum` と `Export(string[], PackageModeEnum, bool, bool)` | `Export(string[], AssetStoreToolsPackagePipeline)` |
| `AssetStoreToolsPackageContext` | `AssetStoreToolsPackageExportContext` |
| `EditorSymphonyConstant.ASSET_STORE_TOOLS_IGNORE_FILE` と `ignore.txt` | `PackagerConfig.json` |

## 挙動が変わったもの

転送用の旧名を通しても、次の点は 7.0.0 の動きになります。

| 対象 | 6.x | 7.0.0 |
| --- | --- | --- |
| `Partitura.Destroy(instance)` / `IsRegistered(instance)` | 渡したインスタンスを見ず、型だけで処理 | 現在の登録と同じ参照のときだけ動く。**旧 `ServiceLocator.DestroyInstance(instance)` / `IsExistInstance(instance)` は 6.x と同じく型だけで処理する** |
| `PartituraReference<T>.Get()` | （旧 `GetInstance`）登録が無ければ `null` | `ServiceNotRegisteredException`。旧 `SymphonyLocateObject<T>.GetInstance` は `null` のまま |
| `Attacca.UnloadAsync` | Suite が保持しているシーンも落とせた | 警告を出し、何もせず `false` を返す。Suite のシーンは `Suite.UnloadAsync` で片付ける。**旧 `SceneLoader` も同じ** |
| `Manuscript.GetEntries()` | （旧 `SaveStore.GetEntries`）未初期化で空の一覧 | 未初期化で `SymphonyNotInitializedException`。**旧 `SaveStore.GetEntries` も同じ** |
| `Sonority.GetSource(name)` | （旧 `GetAudioSource`）Config に無い名前で `null` | `ArgumentException`。旧 `AudioManager.GetAudioSource` は `null` のまま |
| ポーズカテゴリーの生成物 | 専用の `SymphonyFrameWork.PauseCategory` アセンブリ | `SymphonyFramework.Enum` アセンブリに入る。Editor の起動時に移行する |
| `SceneLoadConfig` | — | 「初期化時にロードするSuite」を設定できる |

## 自動で移行されるもの

| 対象 | 仕組み |
| --- | --- |
| `Assets/Resources/`、`Assets/Scripts/`、`Assets/Editor/`、`UserSettings/` 直下の `SymphonyFrameWork` フォルダ | Editor の起動時に `SymphonyFramework` へ移す。GUID を保つので、設定アセットとその値、生成 asmdef への参照は引き継がれる。両方の表記のフォルダがある場合はエラーで止まる |
| 設定アセット、`ServiceComponent`、`SuiteAsset` などの MonoBehaviour / ScriptableObject | スクリプトの `.meta`（GUID）を保っている。`ServiceComponent` の登録方式は値の順序を変えていない。`SuiteAsset` は旧 `Block Name` の値を読み込む |
| `SaveDataConfig` に保存したローダー、パイプラインアセットに保存した手順 | 6.x の名前を指す `[MovedFrom]` で解決する。2.x 以前の旧型名で保存されたものは対象外 |
| セーブデータ | 保存キーは利用側のデータ型の名前なので、そのまま読める |
| ポーズカテゴリーの生成物 | Editor の起動時に、生成物の `using` と継承を新しい名前へ書き換え、専用 asmdef を削除し、`SymphonyFramework.Enum` から `SymphonyFramework` への参照を付ける |
| 名前空間だけ移った型、`SceneBlock*` から `Suite*` へ改名した型、`ServiceLocateComponent` | 6.x の名前を指す `[MovedFrom]` で、API Updater が利用側スクリプトを書き換える |

## 自動では移らないもの

次の書き方は、転送用の旧名でも API Updater でも直りません。コンパイルエラーになった箇所を、上の表に沿って書き換えてください。

- **自前の asmdef から、フレームワークや生成物を名前で参照している。** `SymphonyFrameWork*` は `SymphonyFramework*` に、`SymphonyFrameWork.PauseCategory` は `SymphonyFramework.Enum` に書き換えます。
- **`IInjectable<T>` の `Inject` を明示的に実装している**（`void IInjectable<Foo>.Inject(Foo foo)`）。旧 `IInjectable<T>` は新しい型を継承しているだけなので、宣言元が合いません。`using SymphonyFramework.Services;` へ書き換えてください。暗黙の実装（`public void Inject(Foo foo)`）はそのまま動きます。
- **旧 `SaveDataLoaderStrategy` / `PlayerPrefsSaveDataLoaderStrategy` を継承したローダーへ、新しい `SaveDataContent` だけを継承したデータを渡す。** 旧ローダーは旧 `SaveDataContent` を受け取る形のまま動くよう橋渡ししているため、新しい型だけのデータでは `InvalidOperationException` になります。ローダーとデータの `using` を同時に書き換えてください。
- **旧 `SaveStore.Get(Type)` の戻り値を、旧 `SaveDataContent` 型の変数で受けている。** 戻り値は新しい `SaveDataContent` です。
- **旧 `SceneBlockLoader` の戻り値を `SceneBlockInfo` と明示的に書いている。** 戻り値は `SuiteInfo` です（API Updater が型名を書き換えた後は通ります）。
- 改名したメンバー（`ServiceRegistrationInfo.LocateType`、`SuiteAsset.BlockName` など）、`SymphonyDocumentPageEnum` と `SymphonyMcpTools` の改名、削除したAPI、`SymphonyConfigLocator`。
- サンプル。旧サンプルを消して、Package Manager から新しい名前のサンプルを入れ直してください。サンプルで保存した値は、データ型の名前が変わったため読み込まれません。

## 転送用の旧名（8.0.0 で削除）

| 旧名（`SymphonyFrameWork.*`） | 転送先 |
| --- | --- |
| `System.ServiceLocate.ServiceLocator` / `ServiceInjector` | `Partitura` |
| `System.ServiceLocate.SymphonyLocateObject<T>` | `PartituraReference<T>` |
| `System.ServiceLocate.IInjectable` / `IInjectable<T0…T3>` | 同名の新しい型を継承 |
| `System.ServiceLocate.LocateTypeEnum` | `ServiceLifetimeEnum` へ変換 |
| `System.SceneLoad.SceneLoader` | `Attacca` |
| `System.SceneBlock.SceneBlockLoader` | `Suite` |
| `System.SaveSystem.SaveStore` | `Manuscript` |
| `System.SaveSystem.SaveDataContent` / `SaveDataLoaderStrategy` / `PlayerPrefsSaveDataLoaderStrategy` | 同名の新しい型を継承 |
| `System.AudioManager` | `Sonority` |
| `System.PauseManager`（入れ子の `IPausable` を含む） | `Fermata`、`IPausable` |

旧 `PauseManager.IPausable` をカテゴリーに指定した場合と、旧 `PauseManager.IPausable` だけを実装した対象は、6.x と同じく既定カテゴリー（新しい `IPausable`）として扱います。

一覧と削除予定の正本は [Deprecations.md](./Deprecations.md) です。

## 確かめ方

1. Console に `CS0618`（`[Obsolete]`）以外のエラーが無いこと。
2. Play を開始し、サービスの取得、シーンのロード、セーブの読み込み、ポーズが 6.x と同じように動くこと。
3. Console の `CS0618` の警告が、書き換えるたびに減っていくこと。0 になれば、8.0.0 へ上げても影響を受けません。
