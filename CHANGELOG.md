# Changelog

## [3.1.0] - 2026-08-04
Asset Store Tools Packager の設定を `PackagerConfig.json` へ集約し、依存関係だけでは拾えないアセットの取りこぼしを修正しました。**Runtime の公開APIとセーブデータ形式は変更していません。** 変更は Editor 専用ツールに閉じています。

### Fix

- **「Used Dependencies」出力で `.asmdef` / `.asmref` とネイティブプラグインが落ちる問題を修正しました。** 強制的に含めるファイルが `.cs` のハードコード1種類しかなく、`AssetDatabase.GetDependencies` の依存関係グラフに載らないファイルが両方の条件から外れていました。出力したパッケージを導入した先で、asmdef 参照が解決できずコンパイルエラーになります。

  拡張子のホワイトリストを設定項目にし、既定値へ `.asmdef` / `.asmref` とプラグインバイナリを含めました。既存の `.cs` 強制追加もこのホワイトリストへ統合しています。

- **アセットの収集を `Directory.GetFiles` から `AssetDatabase.FindAssets` へ変更しました。** `.bundle` / `.framework` のようなフォルダ形式のネイティブプラグインは Unity が単一アセットとして扱うため、ファイル列挙では中身のファイルしか拾えず、`ExportPackage` へ渡しても出力されませんでした。あわせて、Unity がインポートしないファイルは収集対象から外れます（元々出力できなかったものです）。

- **除外設定ファイルが `Asset Store Tools Path` の変更に追従しない問題を修正しました。** 除外設定のパスだけが既定パスの定数から組まれており、Project Settings でパスを変更すると除外設定が読まれなくなっていました。

- 除外フォルダ名の比較で大文字小文字を区別しないようにしました。Windows のファイルシステムと判定が食い違うためです。

### Change

- **パッケージ化の設定を対象フォルダ直下の `PackagerConfig.json` へ集約しました。** 除外フォルダ名（旧 `ignore.txt`）と、新しく追加した強制包含拡張子の両方を1つのJSONで管理します。`Assets/` 配下にあるため、利用側のリポジトリで版管理し、手で編集できます。

  設定の分担は「どこにあるか」（`Asset Store Tools Path`、`Exported Packages Path`）が Project Settings、「何を詰めるか」（除外フォルダ、強制包含拡張子）が `PackagerConfig.json` です。

  **移行方法**: 作業は不要です。`PackagerConfig.json` が無く `ignore.txt` がある場合、初回の読み込みで内容を自動的に引き継ぎ、ログで通知します。`ignore.txt` は自動削除しないため、内容を確認してから削除してください。

- **設定ファイルが壊れている場合、既定値へフォールバックせずパッケージ化を中止します。** 除外設定が無視されて意図しないフォルダが出力されるより、止めて気づける方が安全なためです。エラーログにファイルパスと解析失敗の位置が出ます。

- Project Settings の Asset Store Tools Packager から、除外フォルダと強制包含拡張子を編集できるようにしました。編集内容は `Save` ボタンでファイルへ書き込みます。

### Deprecated

- **`EditorSymphonyConstant.ASSET_STORE_TOOLS_IGNORE_FILE` を非推奨にしました。** `ignore.txt` は読み込まれなくなったためです。

  **移行方法**: 設定ファイルの名前が必要な場合は `EditorSymphonyConstant.ASSET_STORE_TOOLS_CONFIG_FILE_NAME` を使ってください。パスは `Asset Store Tools Path` の設定値と連結して組み立てます。次のメジャー更新で、旧定数と `ignore.txt` からの移行処理をあわせて削除します。

## [3.0.0] - 2026-08-04
**3.0.0 の確定版です。** `3.0.0-preview.1`〜`preview.3` で積み上げた Awaitable 移行（Phase 5）を Scene Load、Service Locate、Debug HUD、Component、Editor UI、Loader Strategy まで広げ、移行期間を終えた旧シムの削除と改名（Phase 6）を行いました。preview 版から更新する場合は、本項の Breaking をすべて確認してください。

`SaveDataContent` のフィールド構成、保存されるJSON、PlayerPrefsのキーは変更していないため、**既存のセーブデータはそのまま読み込めます。**

### Breaking

- **`SceneLoader` の非同期API4件を改名し、戻り値を `Awaitable` にしました。** `LoadScene` → `LoadSceneAsync`、`LoadScenes` → `LoadScenesAsync`、`UnloadScene` → `UnloadSceneAsync`、`UnloadScenes` → `UnloadScenesAsync`。`WaitForLoadSceneAsync` は名前を維持し、戻り値だけ `Awaitable` になります。

  **移行方法**: 呼び出し名に `Async` を付ける。`await` して使っている場合、それ以外の変更は不要です。

  ```csharp
  // 2.x
  bool ok = await SceneLoader.LoadScene("Game", priority: 10);
  // 3.0.0
  bool ok = await SceneLoader.LoadSceneAsync("Game", priority: 10);
  ```

  戻り値を `Task` / `ValueTask` として受けている場合は `SymphonyAwaitable.AsTask` で変換します。**`Awaitable` は1回しか `await` できず、フィールドへ保存も共有もできません。**

- **`ServiceLocator.GetInstanceAsync<T>()` と `TryGetInstanceAsync<T>()` が `Awaitable` を返すようになりました。** 名前と引数は変わりません。あわせて未初期化時の `SymphonyNotInitializedException` が、await 時ではなく**呼び出し時に同期的に送出**されます。

- **`SymphonyLocateObject<T>.GetInstanceAsync()`、`SymphonyDebugHUD.AddText()`、`SymphonyTween.Tweening()` / `PausableTweening()` が `Awaitable` を返すようになりました。**

- **`SaveStore.Get<T>()` の暗黙の同期読み込みを廃止しました。** 読み込み済みの値だけを返し、未読み込みの型を渡すと `InvalidOperationException` を送出します。

  2.xは未読み込み時に `LoadAsync(...).GetAwaiter().GetResult()` で同期ブロックしていました。**非同期I/Oを行うローダーではこの同期ブロックが完了不能になります。** PlayerLoopで進む待機を含む処理を、そのPlayerLoopを止めたまま待つためです。

  **移行方法**: 初回取得を `await SaveStore.LoadAsync<T>()` に変え、戻り値でインスタンスを受け取ります。読み込み済みか判定する `SaveStore.IsLoaded<T>()` を追加しました。

  ```csharp
  // 2.x（初回アクセスで暗黙にロードしていた）
  PlayerData data = SaveStore.Get<PlayerData>();

  // 3.0.0
  PlayerData data = await SaveStore.LoadAsync<PlayerData>();

  // 読み込み済みなら同期取得できる
  if (SaveStore.IsLoaded<PlayerData>())
  {
      PlayerData cached = SaveStore.Get<PlayerData>();
  }
  ```

  同期I/Oが必要なローダー向けの明示的な同期契約は、3.0.0の既定APIには追加していません。

- **`SaveDataLoaderStrategy` の派生実装が `Awaitable` を返すようになりました。** 対象は `LoadJsonAsync` / `SaveJsonAsync` / `DeleteCoreAsync` の3つです。

  **移行方法**: 戻り値型を置換し、同期的に完了する実装は `default` ではなく `SymphonyAwaitable.Completed()`（値を返す場合は `SymphonyAwaitable.FromResult(json)`）を返します。**`Awaitable` は参照型のため、`default` は `null` になり await で例外になります。** `ExistsCore` / `SerializeToJson` / `OverwriteFromJson` は変更ありません。

  ```csharp
  // 2.x
  protected override ValueTask SaveJsonAsync(Type dataType, string json, CancellationToken token)
  {
      File.WriteAllText(GetPath(dataType), json);
      return default;
  }
  // 3.0.0
  protected override Awaitable SaveJsonAsync(Type dataType, string json, CancellationToken token)
  {
      File.WriteAllText(GetPath(dataType), json);
      return SymphonyAwaitable.Completed();
  }
  ```

- **`SymphonyVisualElement.Initialize_S()` が `Awaitable` を返すようになりました。** 継承しているEditor拡張は戻り値型と `return default;` を上記と同じ要領で書き換えます。

- **公開enum7型を `Enum` サフィックスへ改名しました。** enumは他のenum型へ暗黙変換できず演算子も定義できないため、互換シムを作れません。

  | 2.x | 3.0.0 |
  | --- | --- |
  | `LocateType` | `LocateTypeEnum` |
  | `SceneLoadState` | `SceneLoadStateEnum` |
  | `SaveDataOperation` | `SaveDataOperationEnum` |
  | `SymphonyDebugLogger.LogKind` | `SymphonyDebugLogger.LogKindEnum` |
  | `SymphonyVisualElement.InitializeType` | `SymphonyVisualElement.InitializeTypeEnum` |
  | `SymphonyVisualElement.LoadType` | `SymphonyVisualElement.LoadTypeEnum` |
  | `AssetStoreToolsPackager.PackageMode` | `AssetStoreToolsPackager.PackageModeEnum` |

  **改名したのは型名だけです。** `ServiceRegistrationInfo.LocateType` のようなプロパティ名は変更していません。自動生成型の `SceneListEnum`、`TagsEnum`、`LayersEnum`、`AudioGroupTypeEnum` は既に規則へ適合しているため変更ありません。

- **`SaveDataRegistryEntryInfo` を `SaveDataEntryInfo` へ改名しました。** `SaveStore.GetEntries()` の戻り値要素型です。メンバー（`DataType`、`Data`、`SaveDate`、`IsLoaded`）は変更ありません。

- **Config3件を改名しました。設定アセットのファイル名を手動でリネームする必要があります。**

  | 2.x のアセット | 3.0.0 のアセット |
  | --- | --- |
  | `SceneManagerConfig.asset` | `SceneLoadConfig.asset` |
  | `AudioManagerConfig.asset` | `AudioConfig.asset` |
  | `SaveSystemConfig.asset` | `SaveDataConfig.asset` |

  Configは `Resources.Load<T>(typeof(T).Name)` で解決するため、**アセット名を変えないと設定が解決されず、シーン初期化・AudioMixer設定・ローダー選択がすべて既定値へ戻ります。** 自動移行は2.xで試して失敗しているため、本バージョンでも行いません。

  **手順**: Unityを開く前に `Assets/Resources/SymphonyFrameWork/` の3つのアセットをリネームします。先にUnityを開いて空のアセットが自動生成された場合は、**自動生成分を削除してから旧アセットをリネームしてください。** 逆順にすると設定値が失われます。

  Config型自体は `internal` のため、型名を直接参照しているコードはありません。`AudioConfig` の入れ子型 `AudioGroupSettings` は `AudioGroupConfig` へ改名しましたが、これも `internal` です。

- **`SymphonyLocate` を `ServiceLocateComponent` へ改名しました。** Inspectorから登録する公開Componentです。**スクリプトのGUIDは維持しているため、シーンやPrefabに配置済みの参照は切れません。** コード上で型名を参照している場合のみ置換してください。

- **移行期間を終えた次の型を削除しました。**

  | 削除した型 | 代替 |
  | --- | --- |
  | `SaveDataRegistry` | `SaveStore` |
  | `SaveDataLoader` | `SaveDataLoaderStrategy` |
  | `PlayerPrefsSaveDataLoader` | `PlayerPrefsSaveDataLoaderStrategy` |
  | `SymphonyTask` | `SymphonyAwaitable` |
  | `ISaveDataLoader<T>`、`JsonUtilityDataLoader<T>`、`NugetDataLoader<T>`（`Runtime/Obsolete/`） | `SaveDataLoaderStrategy` |

- **Editorの管理パネル3件を改名しました。** `SaveDataRegistryWindow` → `SaveDataWindow`、`ServiceLocatorWindow` → `ServiceLocateWindow`、`SceneLoaderWindow` → `SceneLoadWindow`。UXMLも同名へ改名しています。サブシステム名へ揃える規則です。

- 内部型 `SaveSystem` を `SaveDataInitializer` へ、`SymphonyOrchestratorObject` を `SymphonyLifetimeComponent` へ改名しました。いずれも `internal` のため利用側への影響はありません。名前空間 `SymphonyFrameWork.System.SaveSystem` は変更していません。

### Change

- `SceneLoader` と `ServiceLocator` の内部レイヤーは `Task` のままです。公開面だけ `Awaitable` へ変換します。`SceneLoadService` の複数シーン待機と `ServiceLocator` の登録待機は、いずれも1つの待機を複数箇所で共有するため、`Awaitable` では表現できません。
- `SceneLoadService.LoadScenes` / `UnloadScenes` が、各シーンの結果を `Task.Result` ではなく `await` で取り出すようにしました。`Result` は例外を `AggregateException` へ包むため、2.x の `ValueTask` と例外の伝播が変わってしまいます。**利用側から見える例外の型は2.xと同じです。**
- `IInitializeAsync` は `Task` を維持します。`InitializeTask` は完了状態を保持して `IsDone` から参照するため、保存も共有もできない `Awaitable` へ移行できません。

### Add

- `SaveStore.IsLoaded<T>()` / `IsLoaded(Type)` を追加しました。`Get<T>()` が使用できる状態か、例外を発生させずに判定できます。
- `Get<T>()` の新しい契約に対するPlayModeテストを2件追加しました。未読み込み時に `InvalidOperationException` になること、読み込み後は `LoadAsync<T>()` の戻り値と同一インスタンスを返すことを検証します。

### Fix

- `SaveDataContent` の保存日時が `SaveDataLoaderStrategy` の管理下から外れていた経路を塞ぎました（`UpdateSaveDate()` / `ClearSaveDate()` は `internal`）。
- Symphony Administrator の Save Data パネルが、未読み込みの型を選択しただけで保存先へI/Oを発生させないようにしました。未読み込みの場合はLoadの実行を促す表示に変わります。保存時は正本を確定させるため先にロードします。

## [3.0.0-preview.3] - 2026-08-04
### Breaking
- **`SaveStore` の非同期API6件が `ValueTask` ではなく `Awaitable` を返すようになりました。** 対象は `LoadAsync<T>` / `LoadAsync(Type)` / `SaveAsync<T>` / `SaveAsync(Type)` / `DeleteAsync<T>` / `DeleteAsync(Type)` です。

  **移行方法**: `await` して使っている場合、**ソースの変更は不要です。**

  ```csharp
  // 2.x でも 3.0.0 でもそのまま動く
  await SaveStore.SaveAsync<PlayerData>();
  PlayerData data = await SaveStore.LoadAsync<PlayerData>();
  ```

  `Task` や `ValueTask` として受けている場合は `SymphonyAwaitable.AsTask` で変換します。**`Awaitable` は1回しか `await` できず、保存も共有もできません。**

  非推奨の `SaveDataRegistry` も同じ戻り値型になります。

- **`SaveStore` の同期取得 `Get<T>()` の挙動は変わりません。** 内部の読み込み経路は `Task` のままです。

### Change
- **Symphony Administrator の Save Data パネルが Load / Save / Delete を非同期で実行するようになりました。** 従来は完了まで Editor のメインスレッドを止めていました。`Awaitable` を同期待機すると完了の伝播にメインスレッドが必要なためデッドロックするため、この変更は `Awaitable` 化と不可分です。

### Add
- `SaveStore` の非同期APIのPlayModeテストを6件追加した。保存と読み込みの往復、削除で既定値へ戻ること、キャンセル時の例外型、不正な型の同期例外を検証する。
  - **同じ型へ続けて `LoadAsync` を呼んでも両方が完了すること**を回帰テストにした。内部は重複排除で1つの `Task` を共有するが、`Awaitable` は共有できないため呼び出しごとに別の `Awaitable` を作っている。この両立が壊れると、2回目の読み込みが返ってこなくなる。

## [3.0.0-preview.2] - 2026-08-04
### Breaking
- **`PauseManager.PausableDestroy` と `PausableInvoke` が `void` ではなく `Awaitable` を返すようになりました。**

  **移行方法**: 文として呼んでいる場合、**ソースの変更は不要です。**

  ```csharp
  // 2.x でも 3.0.0 でもそのまま動く
  PauseManager.PausableDestroy(gameObject, 1.0f);

  // 3.0.0 では完了を待機できる
  await PauseManager.PausableDestroy(gameObject, 1.0f);
  ```

  待機せずに呼んでも、破棄と処理の実行は従来どおり行われます。

### Fix
- **`PausableDestroy` と `PausableInvoke` の引数エラーが、呼び出し元へ同期的に伝わるようになりました。** 従来は `async void` だったため、`ArgumentNullException` や `ArgumentOutOfRangeException` が非同期メソッドの中で投げられ、呼び出し側の `try/catch` では捕まえられずUnityの未処理例外ハンドラへ流れていました。検証を同期部分へ移しています。

### Add
- `PausableInvoke` と `PausableDestroy` のPlayModeテストを5件追加した。待機した場合の実行、**待機せずに呼んでも実行されること**、引数エラーが同期的に投げられること、GameObjectの破棄を検証する。

### 既知の代償
- `Awaitable` は完了を観測した時点でプールへ返却されます。**待機せずに呼んだ場合はプールへ戻らず、通常のGC対象になります。** 無制限に溜まるものではなく、プールの効果が失われるだけです。fire-and-forgetとして使うAPIであるため、この代償を受け入れています。

## [3.0.0-preview.1] - 2026-08-04

**3.0.0 系の最初のプレビューです。** 破壊的変更を段階的に積み上げ、Phase 5（Awaitable移行）とPhase 6（改名とシム削除）を終えた時点で `3.0.0` として確定します。プレビュー版は「壊れることを承知で先行して試す」ためのものです。

### Breaking
- **`PauseManager` の非同期APIが `Task` ではなく `Awaitable` を返すようになりました。** 対象は `PausableNextFrameAsync`、`PausableWaitForSecondAsync`、`PausableWaitUntil` の3つです。

  **移行方法**: `await` して使っている場合、**ソースの変更は不要です。**

  ```csharp
  // 2.x でも 3.0.0 でもそのまま動く
  await PauseManager.PausableWaitForSecondAsync(1.0f, destroyCancellationToken);
  ```

  変更が必要なのは、戻り値を `Task` として受けている場合だけです。

  ```csharp
  // 2.x
  Task task = PauseManager.PausableWaitForSecondAsync(1.0f);
  await Task.WhenAll(task, other);

  // 3.0.0
  await SymphonyAwaitable.WhenAll(
      PauseManager.PausableWaitForSecondAsync(1.0f),
      other);
  ```

  **`Awaitable` は1回しか `await` できず、保存や共有ができません。** フィールドへ持たず、その場で待機してください。`Task` と混ぜる必要がある場合は `SymphonyAwaitable.AsTask` で変換します。

  引数、例外の種類と条件、キャンセルの扱いは変更していません。キャンセル時は従来どおり `OperationCanceledException` です。

  `PausableWaitForSecond`（Coroutine用の `IEnumerator` 版）、`PausableDestroy`、`PausableInvoke` は変更していません。`async void` の2つは、戻り値を持たせると await されなかった例外が観測できなくなるため据え置きます。

### Add
- Pauseの非同期APIのPlayModeテストを5件追加した。フレーム進行、指定秒の経過、**ポーズ中に待機が進まないこと**、キャンセル時の例外型、条件待機を検証する。
  - キャンセルの検証は `Awaitable` を直接 `await` して行う。`AsTask` で包むと `TaskCanceledException` に化け、利用側が実際に受け取る型を測れないため。

## [2.20.0] - 2026-08-04
### Add
- セーブデータの保存先を差し替える拡張点を `SaveDataLoaderStrategy` と `PlayerPrefsSaveDataLoaderStrategy` へ改名した。DesignPhilosophy の「拡張点には役割を表すサフィックスを付ける」に従う。中身（`protected abstract` メンバー）は変更していない。

### Deprecated
- `SaveDataLoader` と `PlayerPrefsSaveDataLoader` を非推奨にした。**移行方法**: 継承元を `SaveDataLoaderStrategy` / `PlayerPrefsSaveDataLoaderStrategy` へ置換する。それ以外の変更は不要。**3.0.0 で削除する。**
  - 旧型は新しい基底を継承した空の抽象クラスとして残る。**旧型を継承した既存のローダーはそのまま動作し、`SaveSystemConfig` のローダー選択にも引き続き現れる。**

### Change
- 内部ローダー `JsonUtilitySaveDataLoader` / `NewtonsoftSaveDataLoader` を `...Strategy` へ改名した。`internal` のため利用側のコードには影響しない。
  - **`SaveSystemConfig.asset` は `[SerializeReference]` で型名を保存しているため、`[MovedFrom]` を付けて旧型名から解決できるようにしている。** 既存の設定アセットはそのまま読み込まれ、ローダーの選択は失われない。

## [2.19.0] - 2026-08-04
### Add
- `SaveStore` を追加した。`SaveDataRegistry` と同じ公開APIを持ち、シグネチャ・例外の種類と条件・シリアライズ形式のいずれも変わらない。
  - Round I1／I2 で内部を分割した結果、**この型はレジストリではなくストアになっていた**。実際のレジストリは `SaveDataEntryRegistry`（内部）であり、名前が役割と食い違っていたため改名した。

### Deprecated
- `SaveDataRegistry` を非推奨にした。**移行方法**: `SaveDataRegistry` を `SaveStore` へ置換する。それ以外の変更は不要。**3.0.0 で削除する。**
  - 旧型は `SaveStore` へ転送するだけのFacadeとして残るため、既存コードはコンパイルが通り、`CS0618` の警告で移行先が案内される。
  - `SaveDataRegistryEntryInfo` と Editor の `SaveDataRegistryWindow` は本バージョンでは改名しない。`GetEntries()` の戻り値要素型を変えると `IReadOnlyList<T>` が変換できず破壊的になるため、3.0.0 でまとめて扱う。

## [2.18.1] - 2026-08-04
### Change
- Audioの内部構造をレイヤーごとに分割した。Scene Load、Service Locate、Save Data、Pauseと同じ形へ揃えている。**公開APIのシグネチャ、例外の種類と条件はいずれも変更していない。**
  - `AudioGroupEntity`（Domain）— グループ名を同一性とし、音量割合からデシベル値への変換規則を持つ
  - `AudioGroupRegistry`（Application）— グループの保持と、構築処理を実行済みかどうかの記録
  - `AudioService`（Application）— Configの解釈、AudioSourceの遅延構築、AudioMixerへの反映
  - `IAudioSourceHost` / `AudioSourceHost`（Infrastructure）— AudioSourceを載せるGameObjectの所有
  - `AudioManager` は引数検証と転送だけを行い、状態を持たなくなった
- `AudioManager.cs` を `Runtime/System/Audio/` へ移した。名前空間は変更していないため、利用側の `using` に影響しない。
- 音量変換に使う最小音量 `-80` dB を `AudioGroupEntity.MINIMUM_VOLUME_DECIBEL` として名前付き定数にした。変換式は従来と恒等。

### Fix
- **AudioMixerが未割り当ての場合に、空の `AudioManager` GameObject が `DontDestroyOnLoad` へ生成されていたのを修正した。** GameObjectの生成を最初のAudioSource生成まで遅らせたため、1つも作らない場合は生成されない。
- **公開パラメーターが見つからないオーディオグループで、音量変更時の警告が出ていなかったのを修正した。** 初期音量へ0を代入していたため判定が常に成立せず、存在しないパラメーター名で `SetFloat` が呼ばれていた（Unity側で黙って失敗する）。従来も音量は変わっておらず、観測される差は警告が出るようになる点のみ。

### Add
- `AudioGroupEntity` と `AudioGroupRegistry` のEditModeテストを14件追加した。音量変換の境界値と線形補間、初期音量が取得できない場合の扱い、構築済み記録が全消去で戻ることを検証する。
  - **全消去で構築済み記録が戻ること**を回帰テストにした。ここが戻らないとPlay Modeの2回目でAudioSourceが作られなくなる。

## [2.18.0] - 2026-08-03
### Add
- ポーズ機構の管理状態を取得時点の不変値として返す公開 `PauseInfo` を追加した。`PauseManager.GetPauseInfo()` でポーズ状態と `IPausable` の購読件数を取得できる。購読件数は解除し忘れが積み上がっていないかの確認に使う。
- Pauseの内部構造をレイヤーごとに分割した。Scene Load、Service Locate、Save Dataと同じ形へ揃えている。
  - `PauseStateEntity`（Domain）— ポーズ状態と、状態が実際に変化したかどうかの判定を保持する
  - `PausableRegistry`（Application）— `IPausable` と通知処理の対応表を所有する
  - `PauseService`（Application）— 状態変更と通知を担当する
  - `PauseQuery` / `PauseDto`（Adaptor）、`PauseViewModel`（View）
- 管理パネルのPauseに `IPausable` の購読件数を表示するようにした。
- Pauseの EditModeテストを34件追加した。同値の再通知抑止、重複登録の排除、購読者例外の扱い、Resetの後始末を検証する。

### Fix
- **`PauseManager.Pause` に現在と同じ値を代入したとき、`OnPauseChanged` を発行しないようにした。** 従来は `Pause = true` を2回続けると `IPausable.Pause()` が2回呼ばれていた。**利用側への影響**: 同じ値での再通知に依存した実装は動作が変わる。状態変更の通知としては値が変わったときだけ発行するのが正しいため、修正として扱う。
- 管理パネルのPauseが、Play Mode外でも操作ボタンを受け付けて `SymphonyNotInitializedException` を出していたのを修正した。未初期化時はボタンを無効化する。

### Change
- **管理パネルからEditor更新ごとのpollingを完全に除去した。** Pauseが最後の1件で、`EditorApplication.update` の購読自体が無くなっている。各パネルは状態変更eventを購読する。
- 管理パネルのPauseがリフレクション（`typeof(PauseManager).GetField("_pause", ...)`）で内部状態を読むのをやめた。パッケージ内でinternal状態をリフレクションで読む箇所は無くなった。
- `PauseManager.cs` を `Runtime/System/Pause/` へ移した。名前空間は変更していないため、利用側の `using` に影響しない。
- internalアクセサ `PauseManager.IsPaused` と `PausableSubscriberCount` を削除した。MCP診断は公開Infoを使う。

## [2.17.1] - 2026-08-03
### Change
- ソースファイルの文字コードを規約へ揃えた。BOM が無かった17件の `.cs` へ UTF-8 BOM を付与している。**コードの挙動、公開API、シリアライズ形式はいずれも変更していない。** 各ファイルの差分は先頭行のみ。
  - BOM が無くてもコンパイルは通るため、日本語コメントを含むファイルが環境によってはシステム既定のコードページで解釈され、文字化けしうる状態だった。

## [2.17.0] - 2026-08-03
### Add
- `SaveDataRegistryEntryInfo.IsLoaded` を追加した。**`Data != null` は「読み込み済み」を意味しない。** キャッシュは初回アクセス時に既定値で作られるため、キャッシュの有無と永続化データを読み込んだかどうかは別の状態である。Editorの管理パネルは従来この2つを取り違えて表示していた。
- Save Dataの読み取り経路をAdaptorへ集約した。Scene LoadおよびService Locateと同じ形へ揃えている。
  - `SaveDataQuery`（Adaptor）— Entityを公開Infoと表示用Dtoへ変換する。永続化データの存在確認（`Exists`）は、状態が変わるたびに全型分のI/Oが走るため含めない
  - `SaveDataDto`（Adaptor）— 表示に必要な値だけを持つ不変値
  - `SaveDataViewModel`（View）— Serviceの状態変更を`ReactiveProperty`へ変換する
- `SaveDataQuery` と `SaveDataViewModel` のEditModeテストを14件追加した。並び順、キャッシュの有無と読み込み済み状態の区別、通知の発生条件、購読解除を検証する。
  - **保存では版番号が変わらない**ことを回帰テストにした。保存で変わるのは保存日時だけでレジストリの構造は変わらないため、版番号ベースの変更検出では取りこぼす。

### Change
- `SaveDataRegistry.GetEntries()` の並び順を型の完全名のordinal昇順として定めた。従来は`Dictionary`の列挙順であり、順序は未定義だった。
- Editorの管理パネルがEditor更新ごとの走査をやめ、状態変更の通知を購読するようになった。表示内容は従来と同じで、`SymphonyAdministrator`がSave Dataをpollingしなくなる。
- `SaveDataEntryRegistry` のスナップショットキャッシュを廃止した。読み込み済み状態の変化で無効化されず、保存日時の変化も検出できなかったため。

### Fix
- 管理パネルの State 表示が、キャッシュがあるだけの型を `Loaded` と表示していたのを修正した。

## [2.16.0] - 2026-08-03
### Change
- Save Dataの内部構造をレイヤーごとに分割した。`SaveDataRegistry`（420行）が公開Facade・キャッシュ保持・処理順・例外変換・ローダー解決・スナップショット生成をすべて担っていたため、Scene LoadおよびService Locateと同じ形へ揃えた。**公開APIのシグネチャ、例外の種類と条件、シリアライズ形式はいずれも変更していない。**
  - `SaveDataEntryEntity`（Domain）— セーブデータ型を同一性とし、キャッシュ内容と読み込み済み状態を保持する。状態変更は状態遷移メソッドからのみ行う
  - `SaveDataEntryRegistry`（Application）— 型をキーにEntityを所有・検索する。排他制御をこの型へ集約し、Service側はロックを意識しない
  - `SaveDataService`（Application）— 処理順、重複ロードの排除、`SaveDataOperationException` への変換、ローダー解決を担当する
  - `SaveDataRegistry` は引数検証と転送だけを行い、状態を持たなくなった
- `JsonUtilitySaveDataLoader` と `NewtonsoftSaveDataLoader` を `Internal/Infrastructure/` へ移した。GUIDは維持しているため、既存の参照は切れない。

### Add
- `SaveDataEntryEntity` と `SaveDataEntryRegistry` のEditModeテストを17件追加した。同一性、読み込み済み状態の遷移、型ごとの独立性、スナップショットのキャッシュ、全消去を検証する。
  - **同期的に完了するローダーで進行中タスクを登録しないこと**を回帰テストにした。従来はコード上のコメントでのみ説明されていたが、ここが壊れると「Loadしても保存済みデータが読み込まれない」という発見しにくい不具合になるため、テストで固定した。

## [2.15.0] - 2026-08-03
### Add
- 登録キー、payload、登録方式を取得時点の不変値として返す公開`ServiceRegistrationInfo`を追加した。`ServiceLocator.GetRegistrationInfos()`で登録一覧を型名順に取得でき、`TryGetRegistrationInfo`で既知の型を点検索できる。
- `ServiceLocateQuery`の点検索、一覧順、表示名、スナップショット性と、`ServiceLocateViewModel`の初期値、状態変更通知、通知抑制、購読解除を検証するEditModeテストを追加した。

### Change
- Service Locatorの読み取り経路を内部`ServiceLocateQuery`へ集約し、公開API向け`ServiceRegistrationInfo`とView向け`ServiceLocateDto`を分離した。`ServiceLocator`はRegistry／Entityを直接読まず、状態照会をQueryへ転送する。
- `ServiceLocatorWindow`を`ServiceLocateViewModel`の読み取り専用ReactiveProperty購読へ変更した。Editor更新ごとの登録一覧pollingを廃止し、状態変更時だけ型名、payload名、登録方式を再描画する。Play Mode終了時に購読を解除するため、Domain Reload無効でも前回の表示状態を残さない。
- `SymphonyMcpTools.GetServiceLocatorJson()`を公開`ServiceRegistrationInfo`から生成するようにした。既存JSONフィールドを維持し、Componentでは記録された登録方式、Component以外では従来どおりLocatorを実効値として返す。

### Fix
- Play Mode終了時にOrchestratorがService Locatorを解放した後で`SymphonyLocate.OnDisable()`が登録状態を照会し、`SymphonyNotInitializedException`を記録する問題を修正した。未初期化時は解除処理をスキップし、Domain Reload無効で2回往復して終了時エラーと登録残留がないことを確認した。

## [2.14.0] - 2026-08-02
### Add
- 重複登録で失敗した候補の所有権を呼び出し側へ残す通常の`RegisterInstance`に加え、候補を明示的に自動解放する`RegisterInstanceWithAutoDispose`のgeneric／`Type` overloadを追加した。
- `ServiceRegistrationEntity`の状態遷移、`ServiceLocateRegistry`の登録・検索・スナップショット・待機callback、`ServiceLocateService`の所有権と処理順を検証するEditModeテストを20件追加した。

### Change
- Service LocatorのRuntime内部をDomainの`ServiceRegistrationEntity`、Applicationの`ServiceLocateRegistry`／`ServiceLocateService`、Infrastructureの`ServiceHostComponent`へ分割した。登録状態、Command、Unity Componentの所有・解放をそれぞれ独立した責務にした。
- 通常の`RegisterInstance`が型重複で失敗したとき、渡された候補を暗黙に`Dispose`／破棄しないよう変更した。既存登録は維持され、失敗した候補の所有権は呼び出し側に残る。
- AdministratorのService Locator表示からprivate fieldへのリフレクションを除去し、変更不能な登録スナップショットを参照するようにした。Service Locator SampleはSingleton重複候補を自動破棄する新APIの利用例へ更新した。

## [2.13.0] - 2026-08-02
### Add
- 追跡中Sceneの名前、状態、優先度、進捗、Active判定を取得時点の不変値として返す公開`SceneLoadInfo`を追加した。`SceneLoader.GetSceneInfos()`で既知のScene名なしに一覧を取得でき、`TryGetSceneInfo`で1件を安全に照会できる。既存の`IsExist`と`TryGetState`は互換性を維持する。
- `SceneLoadQuery`の点検索、一覧順、Active判定、スナップショット性と、`SceneLoadViewModel`の初期値、内容同値時の通知抑制、購読解除を検証するEditModeテストを追加した。

### Change
- Scene Loadの読み取り経路を内部`SceneLoadQuery`へ集約し、公開API向け`SceneLoadInfo`とView向け`SceneLoadDto`を分離した。`SceneLoader`はRegistry／Entityを直接読まず、状態照会をQueryへ転送する。
- `SceneLoaderWindow`を`SceneLoadViewModel`の読み取り専用ReactiveProperty購読へ変更した。private fieldのリフレクションとEditor更新ごとのDictionary再取得を廃止し、状態変更時だけScene名、状態、優先度、進捗、Active判定を再描画する。Play Mode終了時には購読を解除するため、Domain Reload無効でも前回の表示状態を残さない。
- `SymphonyMcpTools.GetSceneLoaderJson()`を公開`SceneLoadInfo`から生成するようにし、既存JSONフィールドを維持したまま`progress`と`isActive`を追加した。Scene Loader Sampleと利用者向け文書も新しい状態照会APIへ更新した。

## [2.12.0] - 2026-08-02
### Add
- シーン名とActive Scene選択用の優先度を一体で扱う公開Value Object `SceneLoadRequest`を追加した。単一／複数ロード用の新しいoverloadへ渡せるため、複数シーンでもシーン名と優先度の対応を崩さず指定できる。
- `SceneLoadRequest` overloadの進捗通知へC#標準の`IProgress<float>`を採用した。既存の`Action<float>` overloadは同期adapterとして維持し、ソース互換性と通知タイミングを保つ。
- `SceneLoadRequest`、シーン単位の状態遷移、Registry、Serviceの優先度判断と進捗伝播を検証するEditModeテストを追加した。

### Change
- Scene LoadのRuntime内部を`SceneLoadEntity`、`SceneLoadRegistry`、`SceneLoadService`、`ISceneLoader`、`UnitySceneLoader`へ分割した。状態と優先度の判断をUnityの`SceneManager`／`AsyncOperation`境界から切り離し、`SceneLoadManager`、`SceneLoadData`、`SceneResetter`を置き換えた。
- 起動時のScene同期とリセット処理を`SceneLoadService`へ統合した。公開`SceneLoader`の既存シグネチャ、`SceneLoadState`、`SceneManagerConfig`、シリアライズ済みアセットは変更していない。
- Scene Loader SampleのScene A経路を`SceneLoadRequest`と`IProgress<float>`の利用例へ更新した。Editor WindowとMCP診断は新しいEntity一覧へ追従し、表示するJSONの意味を維持する。

## [2.11.0] - 2026-07-31
### Add
- `ReactiveProperty<T>` の単体テストを21件追加した。等値比較（既定と custom comparer、配列が参照比較になること）、`notifyCurrent` の有無、購読解除と多重 Dispose、通知中に購読者が購読・解除した場合、購読者が例外を投げた場合の分離、破棄後の拒否、メインスレッド以外からの呼び出しの拒否を検証する。2.10.0 で追加した時点ではテストを持てなかったため、レビューだけが品質保証になっていた。

### Change
- **テストをパッケージ内へ戻した。** `Tests/Editor/` と `Tests/Runtime/` に配置し、パッケージへ同梱する。両 asmdef の `defineConstraints` に `UNITY_INCLUDE_TESTS` を指定しているため、**利用側のビルドには含まれない。**
- `Runtime/AssemblyInfo.cs` と `Core/AssemblyInfo.cs` がテストアセンブリへ `InternalsVisibleTo` を与えるようにした。これにより `internal` な内部実装も単体テストの対象にできる。従来は公開APIの範囲でしかテストできず、`ReactiveProperty` のような内部基盤を検証できなかった。

### Fix
- `ReactiveProperty` のメインスレッド判定が `Awaitable` を毎回生成し、await せずに破棄していた問題を修正した。Unity の `Awaitable` はプールされた型で、await されないとプールへ返却されない。値の更新ごとに呼ばれる経路であり、後続の ViewModel が高頻度に更新するとプールを圧迫する。副作用のない Unity API 読み取りでスレッドを判定する形へ変更し、メインスレッド上ではアロケーションが発生しないようにした。

## [2.10.0] - 2026-07-31
### Add
- `ReactiveProperty<T>` と `IReadOnlyReactiveProperty<T>` を `Core` へ追加した。値が変化したときだけ購読者へ通知する最小の基盤で、後続バージョンで導入する ViewModel が表示状態を View と Editor Window へ伝えるために使う。現在 Editor Window は毎フレーム内部状態をポーリングしているが、これを変更時のみの反映へ移行するための土台になる。外部ライブラリ（R3 / UniRx）は導入していない。
  - どちらも `internal` のため、**利用側の公開APIは増えていない。**
  - 通知は購読者リストのスナップショットに対して行うため、通知中に購読者が購読・解除しても壊れない。
  - 1人の購読者が例外を投げても残りへの通知は続き、例外はまとめて1回記録する。
  - 値の更新・購読・破棄はメインスレッドに限定し、それ以外からの呼び出しは例外にする。
  - 配列や `List` は既定では参照比較になるため、内容比較用の comparer とスナップショットの用意は利用側の責務としている。

## [2.9.0] - 2026-07-31
### Fix
- **Play Mode 終了時に解放されていなかった状態を解放するようにした。** `PauseManager` の `IPausable` 購読辞書と `OnPauseChanged` の購読、`AudioManager` が生成した GameObject と AudioSource、`SymphonyDebugHUD` の描画コンポーネントは、これまで終了処理を持っておらず残り続けていた。Enter Play Mode Options で Domain Reload を無効にしている環境では、これがゴースト参照や二重購読の原因になる。**利用側への影響**: Play Mode を繰り返したときに、前回の購読が残って通知が多重に飛ぶ問題が解消される。

### Change
- Runtime の終了処理を `SymphonyOrchestrator` の1件へ集約した。従来は `SaveDataRegistry`・`SceneLoader`・`ServiceLocator` がそれぞれ `destroyCancellationToken` へ登録しており、**解放順が保証されていなかった**。今後は Orchestrator が1回だけ登録し、初期化の逆順（`SymphonyDebugHUD` → `AudioManager` → `SceneLoader` → `ServiceLocator` → `PauseManager` → `SaveSystem`）で解放する。1つの解放が失敗しても残りを解放し、例外は記録してから握り潰さずまとめて報告する。
- サブシステムから Composition Root への逆参照を除去した。従来 `ServiceLocateData`・`AudioManager`・`SymphonyDebugHUD` が `SymphonyOrchestrator.PreserveObject()` / `CreateSystemObject()` を直接呼んでいたが、`ISystemObjectFactory` 契約を Composition が実装して注入する形へ変えた。`Runtime/` から Composition 内部への参照は0件になった。
  - **`AudioManager` と `SymphonyDebugHUD` の遅延生成は維持している。** GameObject は従来どおり初回利用時に生成され、使わなければ生成されない。
  - `ServiceLocateData` はコンストラクタでの GameObject 生成をやめ、生成済みのものを受け取る形にした。コンストラクタから副作用を出さないため。
- `ServiceLocateData` が持っていた `[RuntimeInitializeOnLoadMethod]` を除去し、終了検知の初期化と解除を Orchestrator の初期化・終了フェーズへ移した。二重購読が残らなくなる。

## [2.8.0] - 2026-07-31
### Change
- Editorの初期化を `SymphonyEditorOrchestrator` へ集約した。従来は `PackageInitializer`・`AutoEnumGenerator`・`SymphonyDebugLogFileWriter`・`TagsAndLayersPostProcessor` の4型がそれぞれ `[InitializeOnLoad]` を持ち、互いの順序を知らないまま並行して走っていた。今後は自動初期化属性を持つのはOrchestratorだけで、各モジュールは明示的に `Initialize()` / `Shutdown()` を呼ばれる。初期化順が確定し、assembly reload とEditor終了時に構築順の逆順で解放されるようになった。
  - 初期化状態（`Uninitialized` / `Initializing` / `Ready` / `ShuttingDown`）を持ち、`Ready` 以外では再初期化しない。
  - `Shutdown` は多重呼び出しを無害にし、同期的かつ非ブロッキングで動く。1モジュールの終了処理が失敗しても残りを解放する。
  - `AssetPostprocessor` が `Ready` 前に受け取った変更は、その場で処理せず `Ready` 後に1回だけ処理する。
- 起動時の `AssetDatabase.Refresh()` を、最大5回からOrchestratorでの1回へ集約した。実際にアセット変更があった場合だけ実行する。メニューや設定変更を起点とするRefresh（`AssetStoreToolsPackager`・`FolderGenerator`・`SaveSystemSettingProvider`・enum生成のメニュー経路）は従来どおり残している。
- `PackageInitializer` を `public static` から `internal static` へ変更した。`[InitializeOnLoad]` により自動実行される型であり、外部から呼ぶ想定のAPIではないため。**利用側への影響**: Editorアセンブリの型のため、Runtimeコードとビルド済みプレイヤーには影響しない。

## [2.7.0] - 2026-07-31
### Add
- Editor専用の `SymphonyMcpTools` を追加。`GetServiceLocatorJson()` / `GetSceneLoaderJson()` / `GetSaveDataJson()` / `GetPauseJson()` で、各サブシステムの現在状態をJSONで取得できる。uLoopMCP の `execute-dynamic-code` など自動化されたデバッグから状態を**列挙**するための入口。従来は点検索（`IsExistInstance<T>()`、`TryGetState(name, out)`）しか無く、「何が登録されているか」を知るにはリフレクションを書くしかなかった。
  - どのメソッドも**例外を投げず、必ず有効なJSONを返す**。未初期化時は `"initialized": false`、読み取り失敗時は `"error"` を含む。
  - Save Dataの出力は型名・保存日時・ロード済み状態だけで、**`SaveDataContent` の内容は含まない**（セーブデータに機微な値が入りうるため）。
  - Service Locatorの `effectiveLocateType` は登録時に渡された値ではなく、現在の状態から導いた実効値。Service Locatorは `LocateType` を保持しないため。
  - Editorアセンブリ専用であり、Runtimeコードからは参照できない。Playerビルドにも含まれない。
  - **Architecture Revision Phase 3 までの暫定手段**であり、将来は Adaptor の Query と Info による状態照会へ置き換える。

### Change
- 上記の実装のため、`ServiceLocator` / `SceneLoader` / `PauseManager` / `SaveDataRegistry` へ `internal` の読み取り専用アクセサを追加した。公開APIは増えていない。setterも副作用も持たない。従来Editor側がリフレクションでprivateフィールドを覗いていた経路を、型安全な参照へ置き換えるための土台になる。

## [2.6.0] - 2026-07-31
### Add
- `SymphonyAwaitable` を追加。Unity 6 の `Awaitable` に対して、完了済み値（`Completed` / `FromResult`）、複数処理の待機（`WhenAll`）、条件待機（`WaitUntil` / `WaitWhile`）、協調的タイムアウト（`WithTimeout`）、`Task` との相互変換（`FromTask` / `AsTask`）を提供する。3.0.0 で予定している Awaitable 全面移行の基盤になる。
  - `WhenAny` は提供しない。未完了側の Awaitable と例外を安全に消費する一般契約を定められないため。具体的な利用要件が生じた時点で追加を判断する。
  - `WhenAll` へ渡した Awaitable は、このメソッドが唯一の消費者になる。呼び出し側で別途awaitしてはならない。
  - `FromTask` は元 Task がどのスレッドで完了しても、await 側が必ずメインスレッドで再開する。待機をキャンセルした後も元 Task の完了と例外を観測し続けるため、未観測例外を作らない。
  - `WithTimeout` は開始済みの Awaitable ではなく、linked token を受け取る factory を取る。token を無視する処理は強制停止できない協調的なタイムアウトである。

### Change
- `SymphonyTask.BackGroundThreadAction` / `BackGroundThreadActionAsync` が、処理後に `Awaitable.MainThreadAsync()` でメインスレッドへ戻るようにした。従来は復帰せず、後続処理と完了ログがメインスレッド外で実行されていた。あわせて引数 null の検証を追加した。**利用側への影響**: 従来バックグラウンドスレッドで継続していたコードが、メインスレッドで動くようになる。
- `SceneLoader`、`PauseManager`、`SymphonyPackageLoader` の内部待機を `SymphonyAwaitable` へ置き換えた。公開APIの挙動は変わらない。
- テストをパッケージへ同梱しなくなった。EditMode / PlayMode のテストは開発用ワークスペースリポジトリ側で管理する。

### Deprecated
- `SymphonyTask` を非推奨にした。`SymphonyAwaitable` が同等以上の機能を提供するため。3.0.0 で削除する。**移行方法**:
  - `SymphonyTask.WaitUntil(cond)` → `SymphonyAwaitable.WaitUntil(cond)`。条件の反転は不要（同名同義のAPIを用意した）
  - `SymphonyTask.BackGroundThreadAction` / `BackGroundThreadActionAsync` → `SymphonyAwaitable` の同名メソッド
  - `SymphonyTask.OnComplete(this Awaitable, ...)` → `SymphonyAwaitable.OnComplete`
  - `SymphonyTask.OnComplete(this Task, ...)` は以前から非推奨。`Awaitable` 版へ移行する

## [2.5.0] - 2026-07-31
### Add
- Framework配下のアセット移動に対する保護の強さを、`Project Settings > SymphonyFrameWork` から「有効化／警告／無効化」の3段階で選べるようにした。従来は一律で差し戻すか警告するかの2択で、切り替えもメニューに隠れていたため、テストフォルダの移設のように意図した移動を行いたい場面で扱いにくかった。「警告」では移動を続行するか元に戻すかをその場で選べる。1回の移動操作でダイアログは1回だけ表示する。

### Change
- Editor設定の保存先を `EditorPrefs` から `UserSettings/SymphonyFrameWork/` 配下の設定アセットへ移した。`EditorPrefs` はレジストリに入るため値の確認・リセット・削除ができず、設定の所在が不透明だった。**利用側への影響**: 既存の `EditorPrefs` の値は引き継がれず、アセット保護モードとService Locatorのログ設定はいずれも既定値から始まる。`UserSettings/` は `.gitignore` 対象のため、従来どおり開発者ごとの設定として扱われる。
- `Tools/SymphonyFrameWork/Settings/Symphony Asset Lock` メニューを廃止し、`Project Settings > SymphonyFrameWork` へ統合した。
- アセット移動の判定を `OnPostprocessAllAssets` から `OnWillMoveAsset` へ変更した。従来は一度移動させてから `AssetDatabase.MoveAsset` で戻していたが、戻り値で移動そのものを止められるため、余計な移動と `AssetDatabase.Refresh()` が不要になった。あわせて判定を前方一致にし、パスに `SymphonyFrameWork` を含むだけの無関係なアセットを誤検知しないようにした。
- `SymphonyDebugHUD.Show()` / `Hide()` の `[MenuItem]` をEditor側の型へ移した。メニューのパスと両メソッドのシグネチャは変更していない。

### Fix
- Runtimeコードから `UnityEditor` への参照を除去した。`ServiceLocator` と `ServiceLocateManager` が `EditorPrefs` を、`SymphonyDebugHUD` が `MenuItem` を、`SymphonyVisualElement` が `AssetDatabase` を参照しており、`#if UNITY_EDITOR` で囲まれていてもRuntimeアセンブリがEditor APIに依存していた。設定値とローダーはEditor側のCompositionから注入する形へ変更した。`LoadType.AssetDataBase` を含む公開APIは変更していない。

## [2.4.3] - 2026-07-31
### Change
- 本体開発向けのドキュメント（`Documentation~/CONTRIBUTING.md`、`CodeGuidelines.md`、`DesignPhilosophy.md`）を、開発用ワークスペースリポジトリ [SymphonyWorkspace](https://github.com/HIBIKI5201/SymphonyWorkspace) の `Documentation/` へ移設。本パッケージはそのワークスペースへ submodule として組み込まれて開発されるようになり、Unityプロジェクト・検証環境・開発手順はワークスペース側が持つ責務になったため。このリポジトリには**パッケージ利用者向けの内容だけ**を残し、`Documentation~/` は削除した。本体開発ドキュメントの所在は README の「ドキュメント」節が案内する。
- `AGENTS.md` から「本体を変更する立場か、利用する立場か」を読者に判定させる前置きと、本体開発者向けの注意書きを削除。本ファイルは利用者向けの内容のみになった。記載しているAPI・作法・検証手順に変更はない。
- `README.md` の「ドキュメント」節から設計思想・コーディングガイドライン・本体開発ガイドへのリンクを削除し、ワークスペースリポジトリへの案内に置き換え。あわせて `package.json` と乖離していたバージョン表記を修正した。

## [2.4.2] - 2026-07-31
### Change
- `SymphonyOrchestrator` の管理オブジェクトを、実行時に生成する専用の `SymphonySystem` シーンからUnity標準の `DontDestroyOnLoad` に変更。専用シーンの生成・待機とScene Loader側の除外処理が不要になり、`LoadSceneMode.Single` 相当の遷移でもフレームワークのランタイム状態をUnity標準の永続化機構で保持する。公開API、設定アセット、セーブデータ形式の変更はない。

## [2.4.1] - 2026-07-30
Save Systemの公開範囲を、DesignPhilosophy.mdの[公開範囲](https://github.com/HIBIKI5201/SymphonyWorkspace/blob/main/Documentation/DesignPhilosophy.md#公開範囲)へ明記した「サブシステムの機能はすべてFacade経由で呼び出す」原則に合わせました。Facade（`SaveDataRegistry`）以外の公開型から機能を起動できた経路を塞ぎ、Facade以外の`public`をenum、基底クラス、Value Object、例外、Inspector属性だけに揃えます。

`SaveDataRegistry` の `Exists`／`Get`／`LoadAsync`／`SaveAsync`／`DeleteAsync`／`GetEntries`、`SaveDataContent` の継承、`SaveDataLoader`・`PlayerPrefsSaveDataLoader` の継承、`SaveDataRegistryEntryInfo` の読み取りは、いずれもシグネチャを変更していません。README.mdとSampleに載っている使い方だけでセーブデータを扱っていたコードは、修正なしでそのまま動きます。影響を受けるのは、Facadeを介さずローダーや保存日時を直接操作していたコードと、`SaveSystem<TData, TLoader>` を使っていたコードに限られ、いずれもドキュメントで案内していた利用方法ではありません。この影響範囲からメジャーではなくパッチ更新として扱います。シリアライズ形式（`SaveDataContent` のフィールド構成、保存されるJSON、PlayerPrefsのキー）は変更していないため、既存のセーブデータはそのまま読み込めます。

### Change
- `SaveSystem<TData, TLoader>` を削除。`SaveDataRegistry` と同じ機能へ別経路で到達できる二重Facadeであり、独自のキャッシュを持つためConfigで選択したローダーを経由せず、Registry Windowにも表示されなかった。**移行方法:** `SaveSystem<TData, TLoader>.Get()`／`Save()`／`Load()`／`GetDate()`／`Dispose()` を、`SaveDataRegistry.Get<T>()`／`SaveAsync<T>()`／`LoadAsync<T>()`／`Get<T>().SaveDate`／`RefreshLoader` 相当へ置き換える。ローダーは型引数で指定せず、`Project Settings > SymphonyFrameWork > Save System` で選択する。
- `SaveDataLoader` の `Exists`、`LoadAsync`、`SaveAsync`、`DeleteAsync` を `internal` へ変更。ローダーは利用側が継承して保存先を差し替える契約であり、呼び出すのは `SaveDataRegistry` の役割のため。**移行方法:** 独自ローダーを直接呼んでいた箇所を `SaveDataRegistry` の同名メソッドへ置き換える。継承側が実装する `protected abstract` メンバー（`ExistsCore`、`LoadJsonAsync`、`SaveJsonAsync`、`DeleteCoreAsync`、`SerializeToJson`、`OverwriteFromJson`）は変更していないため、既存の独自ローダーはそのままコンパイルできる。
- `SaveDataContent.UpdateSaveDate()` と `ClearSaveDate()` を `internal` へ変更。保存日時は `SaveDataLoader` がライフサイクルとして更新するため、利用側から任意の値を注入できないようにした。`SaveDate` は従来どおり `public` で読み取れる。
- `SaveDataRegistry.GetCurrentLoader()` を `internal` へ変更。Adaptorが選択した実装を公開APIへ出さないため。現在のローダーはProject SettingsとRegistry Windowで確認する。
- `SaveDataRegistry.RefreshLoader()` を `internal` へ変更。Configを編集するEditorのProject Settings画面のためだけに存在するメンバーのため。
- `SaveDataRegistryEntryInfo` のコンストラクタを `internal` へ変更。生成はRegistryの責務であり、利用側は `SaveDataRegistry.GetEntries()` で取得する。構造体自体と `DataType`、`Data`、`SaveDate` は `public` のまま。
- DesignPhilosophy.mdの「公開範囲」へ、サブシステムの機能はすべてFacade経由で呼び出し、Facade以外の`public`はenum、抽象基底クラス・interface、Value Object、例外、Inspector属性に限るという原則を明記。あわせて、契約型では利用側が実装するメンバーを`protected abstract`、フレームワークが駆動するメンバーを`internal`にすること、フレームワークが生成して返すValue Objectはコンストラクタを`internal`にすること、Editor・Composition専用のメンバーはFacade上でも`public`にしないこと、1つのサブシステムに複数の公開入口を作らないことを規約として追加した。
- `Template/` 配下の扱いを整理。DesignPhilosophy.mdで「テンプレート実装はinternal」と「`PlayerPrefsSaveDataLoader` は拡張点としてpublic」が矛盾していたため、`Template/` は利用側が継承する抽象基底クラスを置く場所、そのまま使う具象実装は `Internal/` に置いて`internal`、と定義し直した。`PlayerPrefsSaveDataLoader` は`public`のまま変更なし。

## [2.4.0] - 2026-07-30
### Add
- `ServiceLocator.GetRequiredInstance<T>()` と `ServiceNotRegisteredException` を追加。存在が必須の依存関係をnullのまま注入せず、未登録だったサービス型を呼び出し側で判定できるようにした。既存の `GetInstance<T>()`、`TryGetInstance<T>()` の契約は変更していない。
- セーブデータの存在確認、読み込み、保存、削除に失敗した際、操作種別、データ型、ローダー型、原因例外を保持する `SaveDataOperationException` と `SaveDataOperation` を追加。
- シーンロード後の依存注入または `IInitializeAsync` に失敗した際、シーン名、ルートGameObject名、初期化型、原因例外を保持する `SceneInitializationException` を追加。

### Change
- `ServiceInjector.Inject(...)` とScene Loaderの自動注入は必須サービス取得を使用する。依存が未登録の場合はnullを渡さず、`ServiceNotRegisteredException` を原因として通知する。
- `GetInstanceAsync<T>()` の待機期限超過を `TimeoutException`、呼び出し側キャンセルを `OperationCanceledException` として区別した。`TryGetInstanceAsync<T>()` は期限超過だけをfalseへ変換し、キャンセルを握りつぶさない。
- null、空文字、範囲外の引数には `ArgumentNullException`、`ArgumentException`、`ArgumentOutOfRangeException` を使用するよう、Scene Loader、Audio Manager、Pause Manager、Debug HUDの公開入口を明確化。
- Save Data Registryから伝播していたローダー固有例外を `SaveDataOperationException` でラップし、シーン初期化から伝播していた実装固有例外を `SceneInitializationException` でラップする。**移行方法:** 従来の具体的な例外を直接catchしていたコードは各専用例外をcatchし、`InnerException` を確認する。キャンセル処理のcatchは `OperationCanceledException` のままでよい。

### Fix
- 初期化前の `ServiceLocator`、`SceneLoader`、`AudioManager`、`PauseManager` 呼び出しが実装詳細の `NullReferenceException` になる問題を修正し、`SymphonyNotInitializedException` へ統一。
- Service Locatorの非同期待機がタイムアウトまたはキャンセルされた後も登録待ちコールバックを保持していた問題を修正。
- Domain Reload無効時にPause Managerの登録辞書だけが残り、再生し直した際に通知を再購読できない問題を修正。

## [2.3.0] - 2026-07-30
2.2.0で導入した `SymphonyFrameWork.System.API` 名前空間を撤回し、公開Facadeクラスをそれぞれのサブシステムの名前空間へ戻しました。Facadeとその引数・戻り値のValue Object（`LocateType`、`SceneLoadState`）が別々の名前空間に分かれ、1つのサブシステムを使うだけで `using` が2つ必要になっていた状態を解消するためです。公開範囲の区別は名前空間ではなくフォルダ（`Internal/`）で表す方針へ変更しました。

Facadeの名前空間が2.1.0以前へ戻るだけで、クラス名・メンバー・シグネチャ・シリアライズ形式は一切変わりません。2.1.0以前の書き方（`SymphonyFrameWork.System.ServiceLocate` などを直接使うコード）はそのまま動き、`using` の修正が必要なのは2.2.0〜2.2.1の2バージョンだけに存在した `SymphonyFrameWork.System.API` を使ったコードに限られます。この影響範囲の狭さから、メジャーではなくマイナー更新として扱います。

### Change
- `SymphonyFrameWork.System.API` 名前空間を削除し、Facadeクラスを2.1.0以前と同じ名前空間へ戻した。**移行方法:** `using SymphonyFrameWork.System.API;` を、使用しているFacadeに応じて次へ置き換える（クラス名とメンバーの変更はないため、`using` 以外の修正は不要）。
  - `ServiceLocator`、`ServiceInjector` → `using SymphonyFrameWork.System.ServiceLocate;`（`LocateType` と同じ名前空間になったため、両方使う場合の `using` は1つで足りる）
  - `SceneLoader` → `using SymphonyFrameWork.System.SceneLoad;`（`SceneLoadState` と同じ）
  - `SaveDataRegistry`、`SaveSystem<TData, TLoader>` → `using SymphonyFrameWork.System.SaveSystem;`（`SaveDataContent`、`SaveDataLoader` と同じ）
  - `AudioManager`、`PauseManager` → `using SymphonyFrameWork.System;`
- 2.2.0で追加した旧namespace互換の `[Obsolete]` シム（`Runtime/Obsolete/System/`、`Runtime/Obsolete/SceneLoad/`、`Runtime/Obsolete/ServiceLocate/`、および `Runtime/Obsolete/SaveSystem/` の `SaveDataRegistry`・`SaveSystem<TData, TLoader>`）を削除。復帰した本体が同じ名前空間・同じ型名を占めるため、シムとしては成立しない。**2.1.0以前のコードは、このシムを介さず本体を直接参照する形に戻るため、非推奨警告（CS0618）が出なくなる。** `Runtime/Obsolete/SaveSystem/` に残る `ISaveDataLoader<T>`・`JsonUtilityDataLoader<T>`・`NugetDataLoader<T>` は名前空間移動とは無関係の非推奨APIであり、そのまま残る。
- 公開Facadeの置き場所を `Runtime/System/API/` から各サブシステムのフォルダ直下（`Runtime/System/SaveSystem/`、`Runtime/System/SceneLoader/`、`Runtime/System/ServiceLocator/`、`AudioManager`・`PauseManager` は `Runtime/System/`）へ戻した。`.meta` ごと移動しているためGUIDは維持している。
- Runtime内の `internal` な型を、所属するフォルダ直下の `Internal/` へ移動（`Runtime/System/Internal/`、`Runtime/System/SaveSystem/Internal/`、`Runtime/System/SceneLoader/Internal/`、`Runtime/System/ServiceLocator/Internal/`、`Runtime/Configs/Internal/`、`Runtime/Debug/DebugHUD/Internal/`）。`Internal` は可視性を表すだけで責務ではないため名前空間には含めず、`Core/Internal/` と同じ扱い。フォルダを見ればそのフォルダの公開範囲が分かり、`Internal/` の外にあるのは利用側から使える型だけになる。名前空間・型名・シグネチャは変更していないため、利用側への影響はない。空になった `Runtime/Configs/ConfigData/` は削除した。
- 上記に合わせて DesignPhilosophy.md（Facadeと内部Managerの節、公開範囲の節）、CodeGuidelines.md（ディレクトリ表、`## 名前空間`）、README.md、AGENTS.md の記述とコード例を更新。

## [2.2.1] - 2026-07-29
### Add
- `Samples/Runtime/DebuggerSample`: `SymphonyDebugLogger`（`LogDirect`の重要度別出力、`AddText`／`NewText`／`LogText`による複数行ログの蓄積と一括出力、`LogAndCheckComponentNull`）、`SymphonyDebugHUD`（`Show`／`Hide`、`AddText(Func<string>)`と`RemoveText`の対、時間指定の一時表示）、`SymphonyStopWatch`（`Start`／`Stop`による計測、未開始IDの警告）をPlayモードで確認できるサンプルシーンとスクリプト。`package.json`の`samples`へ`Debugger Sample`として追加。

### Fix
- `SymphonyDebugHUD.Initialize()` が、破棄済みのHUDに対して `MissingReferenceException` を投げていた問題を修正。Domain Reloadを無効にした状態で再生を繰り返すと、前回の再生で生成した `Lazy` がstaticに残り、`IsValueCreated` はtrueのままHUDのGameObjectだけが破棄済みになるため、`Destroy` へ渡す `gameObject` の取得で例外になっていた。

### Change
- 2.2.0で残した旧namespaceの `[Obsolete]` シムを `Runtime/Obsolete/` へ集約。移行元の名前空間ごとにサブフォルダ（`System`／`SaveSystem`／`SceneLoad`／`ServiceLocate`）を分けている。名前空間・型名・シグネチャはいずれも変更していないため、利用側のコードとシリアライズ済みデータへの影響はない（.metaごと移動しているのでGUIDも維持）。綴りが誤っていた `Runtime/System/SaveSystem/Obsolute/` はこの集約で解消した。移行が完了したメジャー更新でフォルダごと削除する。
- 現役のinternal実装と非推奨シムが同居していた `Runtime/System/SaveSystem/SaveSystem.cs` を分割。`internal static class SaveSystem`（CoreSystemがライフタイムを紐付ける実装）は元の位置に残し、`[Obsolete]` な `SaveSystem<TData, TLoader>` のみ `Runtime/Obsolete/SaveSystem/SaveSystem.cs` へ移した。
- UnityEngine.Objectの遅延生成を担う内部ユーティリティ `SymphonyLazyObject<T>`（internal、`Core/Internal/`、名前空間は `SymphonyFrameWork.Core`）を追加し、`SymphonyDebugHUD` の遅延生成を `System.Lazy<T>` から移行。`System.Lazy<T>` はUnity側での破棄を検知できないため、破棄済みなら生成し直す判定と破棄処理をこのクラスへ集約する。フレームワーク内でUnityEngine.Objectを遅延生成する箇所は今後これを使用する。
- `Core/AssemblyInfo.cs` を追加し、`SymphonyFrameWork.Core` の `internal` 型をRuntime（`SymphonyFrameWork`）とEditor（`SymphonyFrameWork.Editor`）へ `InternalsVisibleTo` で公開。どのサブシステムにも属さない内部ヘルパーの置き場を `Core/Internal/` に定め、利用側アセンブリからは引き続き参照できない状態を保つ。

公開APIの追加・変更はなく、追加したのは既存機能の利用例と上記の修正のみ。

## [2.2.0] - 2026-07-29
サブシステムごとに散らばっていた公開Facadeクラスを、消費者から見て分かりやすい単一の場所へ集約しました。破壊的変更はありません。

### Add
- `SymphonyFrameWork.System.API` 名前空間（`Runtime/System/API/`）を新設し、全サブシステムのFacadeクラス（`ServiceLocator`、`ServiceInjector`、`SaveDataRegistry`、`SaveSystem<TData, TLoader>`、`SceneLoader`、`AudioManager`、`PauseManager`）をここへ集約。内部実装（Manager、Data等）は従来どおり各サブシステムのフォルダ・名前空間に残る。

### Deprecated
- 旧namespace（`SymphonyFrameWork.System.ServiceLocate`、`SymphonyFrameWork.System.SceneLoad`、`SymphonyFrameWork.System.SaveSystem`、無印の`SymphonyFrameWork.System`）にあった上記Facadeクラスは、`[Obsolete(error: false)]` を付けたシムとして当面残る。呼び出しは内部で新しい `SymphonyFrameWork.System.API` の同名クラスへ転送されるだけなので、既存コードはそのままコンパイル・動作するが、コンパイル時に非推奨警告（CS0618）が出る。**移行方法:** 対象クラスを使用している箇所の `using` を `SymphonyFrameWork.System.API` に張り替える。`LocateType`・`SceneLoadState` はFacade本体ではなく引数・戻り値のValue Objectのため移動しておらず、引き続き旧namespace（`SymphonyFrameWork.System.ServiceLocate` / `SymphonyFrameWork.System.SceneLoad`）から参照する。

## [2.1.0] - 2026-07-29
### Add
- `SymphonyDebugLogger.LogDirect` 経由のログをEditor限定でファイルへキャッシュ出力する機能。5秒間隔または50件到達時にバッファをまとめて `Cache/Log.txt`（パッケージ直下）へ書き込み、`Application.quitting`／アセンブリリロード前にも強制フラッシュする（実体は新規Editor拡張 `SymphonyDebugLogFileWriter` が `SymphonyDebugLogger` の内部イベントを購読して担当し、Runtime層はファイルI/Oを持たない）
- `SymphonyConstant.GetFrameworkAbsolutePath()`（Editor専用）: Framework自身の実配置パス（Assets直置き、またはUPM経由のPackages／Library/PackageCache）を絶対パスで解決するユーティリティ

## [2.0.0] - 2026-07-22
設計思想（[`DesignPhilosophy.md`](https://github.com/HIBIKI5201/SymphonyWorkspace/blob/main/Documentation/DesignPhilosophy.md)）の改訂に合わせて、主要サブシステムの公開範囲とアーキテクチャを再構成しました。破壊的変更を含みます。

### Add
- `IInjectable<T...>` を実装したシーンのルートオブジェクトへ、`SceneLoader` がロード完了時に自動注入する機能

### Breaking
- `SaveSystem<TData, TLoader>` のローダー制約を `where TLoader : ISaveDataLoader<TData>, new()` から `where TLoader : SaveDataLoader, new()` へ変更。旧 `ISaveDataLoader<T>` 実装（Obsoleteの `NugetDataLoader<T>`、`JsonUtilityDataLoader<T>` など）は `TLoader` に指定できなくなる。**移行方法:** `SaveSystem<TData, TLoader>` の利用箇所を `SaveDataRegistry.Get<T>()`／`SaveAsync<T>()`／`LoadAsync<T>()` へ置き換える（ローダー選択は `SaveSystemConfig` のProject Settingsに委ねる）。
- 次の型を `public` から `internal` へ変更。型名を直接参照しているコードはコンパイルできなくなる: `ServiceLocateManager`、`ServiceLocateData`、`SceneLoadManager`、`SceneLoadData`（`SceneInfo`含む）、`SceneResetter`、`SymphonyCoreSystem`（`MoveObjectToSymphonySystem`含む）、`JsonUtilitySaveDataLoader`、`NewtonsoftSaveDataLoader`、`AudioManagerConfig`、`SaveSystemConfig`、`SceneManagerConfig`、`SymphonyHUDDrawer`。
- `SymphonyLocate` を含む、継承を前提としない具象クラスを `sealed` 化。既存のサブクラスはコンパイルできなくなる。

### Change
- Configの解決を `SymphonyCoreSystem` とEditorのCompositionからの注入へ変更
- `SaveDataRegistry.RefreshLoader()` が注入済みResolverからローダーを再解決する構造へ変更
- Framework内部のログ呼び出しを `LogDirect`／`LogText` へ移行
- Editorアセンブリへ `InternalsVisibleTo` を追加（Runtimeのinternal型をEditor拡張から参照するため。利用側プロジェクトのアセンブリには適用されない）

### Compatibility
- `ServiceLocator`、`SceneLoader`、`SaveDataRegistry`、`AudioManager`、`PauseManager` のFacade APIは維持
- `SaveDataLoader` と `PlayerPrefsSaveDataLoader` は利用側の拡張点としてpublicを維持
- `ServiceInjector.Inject(...)` は手動呼び出し用のAPIとして維持（シーンロードを経由しない生成向け）
- 上記Breakingに該当しないコードは、Facade API経由の利用であれば影響を受けない

## [1.27.20] - 2026-07-12
### Update
- SceneLoader

## [1.27.19] - 2026-07-03
### Add
- Samples

## [1.27.18] - 2026-06-18
### Update
- AssetStoreToolsPackager

## [1.27.17] - 2026-05-01
### Update
- SceneLoader

## [1.27.16] - 2026-03-29
### Update
- AssetStoreToolsPackager

## [1.27.15] - 2026-03-25
### Add
- SceneNameSelectorAttribute

## [1.27.14] - 2026-03-19
### Fix
- SaveSystem

## [1.27.13] - 2026-03-19
### Fix
- AssemblyGenerator

## [1.27.12] - 2026-03-19
### Fix
- SaveSystem

## [1.27.11] - 2026-03-19
### Fix
- SaveSystem

## [1.27.10] - 2026-03-19
### Update
- SaveSystem

## [1.27.9] - 2026-03-19
### Update
- SaveSystem

## [1.27.8] - 2026-03-18
### Update
- AssemblyGenerator

## [1.27.7] - 2026-03-14
### Update
- SymphonyDebugHUD

## [1.27.6] - 2026-03-14
### Update
- FolderGenerator

## [1.27.5] - 2026-03-01
### Update
- ServiceLocator

## [1.27.4] - 2026-03-01
### Update
- ServiceLocator

## [1.27.3] - 2026-03-01
### Update
- SymphonyVisualElement

## [1.27.2] - 2026-02-26
### Update
- SceneLoader

## [1.27.0] - 2025-12-05
### Add
- SubClassSelector

## [1.26.1] - 2025-12-03
### Update
- AssetStoreToolsPackager

## [1.26.0] - 2025-10-30
### Add
- SymphonyStringUtil

## [1.25.4] - 2025-10-23
### Fix
- SymphonyDebugLogger

## [1.25.3] - 2025-10-23
### Update
- FolderGenerator

## [1.25.2] - 2025-10-21
### Fix
- SymphonyDebugHUD

## [1.25.1] - 2025-10-19
### Add
- SymphonyLocateObject

## [1.24.2] - 2025-10-19
### Update
- SymphonyDebugLogger

## [1.24.1] - 2025-10-19
### Add
- TagSelectorAttribute

## [1.23.21] - 2025-10-08
### Update
- SymphonyDebugHUD

## [1.23.20] - 2025-10-08
### Update
- ServiceLocator

## [1.23.19] - 2025-10-08
### Fix
- ServiceLocator

## [1.23.18] - 2025-08-06
### Add
- SymphonyDebugLogger

## [1.23.17] - 2025-07-19
### Fix
- ServiceLocator

## [1.23.16] - 2025-07-17
### Fix
- SymphonyLocate

## [1.23.15] - 2025-07-17
### Update
- ServiceLocator

## [1.23.14] - 2025-07-17
### Update
- ServiceLocator

## [1.23.13] - 2025-07-16
### Fix
- ServiceLocator

## [1.23.12] - 2025-07-16
### Fix
- SceneLoader

## [1.23.11] - 2025-07-16
### Fix
- FolderGenerator

## [1.23.10] - 2025-07-16
### Fix
- SymphonyLocate

## [1.23.9] - 2025-07-16
### Fix
- SymphonyAdministrator

## [1.23.8] - 2025-07-11
### Fix
- SymphonyLocate

## [1.23.7] - 2025-07-11
### Update
- ServiceLocator

## [1.23.6] - 2025-07-09
### Update
- ServiceLocator

## [1.23.5] - 2025-07-06
### Fix
- SymphonyLocate

## [1.23.4] - 2025-07-04
### Fix
- SceneLoader

## [1.23.3] - 2025-07-03
### Add
- SymphonyDebugHUD

## [1.23.2] - 2025-07-03
### Fix
- ServiceLocator

## [1.23.1] - 2025-06-23
### Update
- SceneLoader

## [1.23.0] - 2025-06-23
### Add
- IInitializeAsync

## [1.22.2] - 2025-06-23
### Update
- SaveDataSystem

## [1.22.1] - 2025-06-17
### Update
- AssemblyGenerator

## [1.22.0] - 2025-06-03
### Add
- AssetStoreToolsPackager

## [1.21.13] - 2025-06-03
### Update
- ServiceLocator

## [1.21.12] - 2025-05-31
### Fix
- SceneLoader

## [1.21.11] - 2025-05-31
### Update
- SymphonyLocate

## [1.21.10] - 2025-05-31
### Fix
- ServiceLocator

## [1.21.9] - 2025-05-28
### Update
- AudioManager

## [1.21.8] - 2025-05-28
### Fix
- SceneManagerConfig

## [1.21.7] - 2025-05-19
### Fix
- ServiceLocator

## [1.21.6] - 2025-05-19
### Fix
- SceneLoader

## [1.21.5] - 2025-05-18
### Fix
- SceneLoader

## [1.21.4] - 2025-05-18
### Update
- ServiceLocator

## [1.21.3] - 2025-05-15
### Update
- SceneLoader

## [1.21.2] - 2025-05-14
### Update
- ServiceLocator

## [1.21.1] - 2025-05-14
### Update
- SymphonyLocate

## [1.21.0] - 2025-05-14
### Fix
- EditorSymphonyConstant

## [1.20.18] - 2025-05-14
### Update
- SceneLoader

## [1.20.17] - 2025-05-06
### Fix
- AssemblyGenerator
- EnumGenerator

## [1.20.16] - 2025-05-05
### Update
- PauseManager

## [1.20.15] - 2025-05-2
### Fix
- PackageInitializer

## [1.20.14] - 2025-04-29
### Update
- PackageInitializer
- SymphonyConfigManager

## [1.20.13] - 2025-04-05
### Fix
- AutoEnumGeneratorConfig
- SymphonyAdministrator

## [1.20.12] - 2025-04-05
### Update
- SceneLoader

## [1.20.11] - 2025-04-05
### Fix
- PackageInitializer

## [1.20.10] - 2025-04-05
### Update
- AudioManager

## [1.20.9] - 2025-04-05
### Fix
- AutoEnumGenerator

## [1.20.8] - 2025-04-05
### Fix
- PackageInitializer

## [1.20.7] - 2025-03-12
### Update
- SymphonyAdministrator

## [1.20.6] - 2025-03-12
### Fix
- EnumGenerator

## [1.20.5] - 2025-03-12
### Add
- PackageInitializer

## [1.20.4] - 2025-03-10
### Fix
- SymphonyConfigLocator
- SymphonyEditorConfigLocator

## [1.20.3] - 2025-03-10
### Add
- AssemblyGenerator
### Fix
- EnumGenerator

## [1.20.2] - 2025-03-10
### Update
- SymphonyPackageLoader

## [1.20.1] - 2025-03-08
### Update
- FoldierGenerator

## [1.20.0] - 2025-03-05
### Update
- AutoEnumGenerator
- AutoEnumGeneratorConfig
### Fix
- EnumGenerator

## [1.19.21] - 2025-03-05
### Add
- TagsAndLayersPostProcessor

## [1.19.20] - 2025-03-05
### Update
- ServiceLocater

## [1.19.19] - 2025-03-05
### Add
- AudioManager
- AudioManagerConfig

## [1.19.18] - 2025-03-04
### Update
- SymphonyTask

## [1.19.17] - 2025-03-04
### Update
- SymphonyTween

## [1.19.16] - 2025-03-02
### Update
- SymphonyDebugLog

## [1.19.15] - 2025-03-02
### Update
- SaveDataManager

## [1.19.14] - 2025-03-02
### Add
- FoldierGenerator

## [1.19.13] - 2025-03-01
### Add
- SymphonyEditorConfigLocator

## [1.19.12] - 2025-03-01
### Fix
- AutoEnumGenerator

## [1.19.11] - 2025-03-01
### Update
- SceneLoader
- SceneManagerConfig

## [1.19.10] - 2025-02-28
### Add
- SymphonyConfigLocator

## [1.19.9] - 2025-02-28
### Add
- EnumGeneratorConfig

## [1.19.8] - 2025-02-28
### Update
- EnumGenerator

## [1.19.7] - 2025-02-27
### Update
- SceneManagerConfig

## [1.19.6] - 2025-02-27
### Fix
- EnumGenerator

## [1.19.5] - 2025-02-26
### Update
- SceneManagerConfig

## [1.19.4] - 2025-02-26
### Update
- DisplayTextAttribute

## [1.19.3] - 2025-02-26
### Update
- SymphonyConstant

## [1.19.2] - 2025-02-25
### Update
- EnumGenerator

## [1.19.1] - 2025-02-25
### Update
- SymphonyAssetPostProcessor

## [1.19.0] - 2025-02-25
### Add
- DisplayTextAttribute
- ReadOnryAttribute

## [1.18.11] - Y2025-02-24
### Update
- EnumGenerator

## [1.18.10] - 2025-02-24
### Add
- SymphonyConstant

## [1.18.9] - 2025-02-24
### Update
- EnumGenerator

## [1.18.8] - 2025-02-24
### Add
- EnumGenerator

## [1.18.7] - 2025-02-23
### Add
- SymphonyFrameWork-Editor assembly

## [1.18.6] - 2025-02-23
### Add
- SceneManagerConfig

## [1.18.5] - 2025-02-22
### Add
- SymphonyConfigManager

## [1.18.4] - 2025-02-22
### Update
- SymphonyTween

## [1.18.3] - 2025-02-22
### Add
- SymphonyTween

## [1.18.2] - 2025-02-21
### Update
- SymphonyStopWatch

## [1.18.1] - 2025-02-21
### Update
- SymphonyAdministrator

## [1.18.0] - 2025-02-21
### Add
- SymphonyPackageLoader

## [1.17.7] - 2025-02-20
### Add
- SymphonyLocate

## [1.17.6] - 2025-02-20
### Update
- SymphonyAdministrator

## [1.17.5] - 2025-02-20
### Update
- SymphonyVisualElement

## [1.17.4] - 2025-02-20
### Update
- PauseManager

## [1.17.3] - 2025-02-20
### Update
- SymphonyUtility

## [1.17.2] - 2025-02-20
### Fix
- SymphonySystem
- ServiceLocator
- SceneLoader
- SaveDataSystem
- PauseManager

## [1.17.1] - 2025-02-20
### Update
- SymphonyTask

## [1.17.0] - 2025-02-19
### Add
- SymphoWindow
- SymphonyAssetPostProcessor

## [1.16.8] - 2025-02-19
### Add
- SymphonySingleton
### Update
- ServiceLocator
### Fix
- ServiceLocator

## [1.16.7] - 2025-02-18
### Update
- PauseManager

## [1.16.6] - 2025-02-18
### Update
- SymphonyVisualElement

## [1.16.5] - 2025-02-18
### Update
- PauseManager

## [1.16.4] - 2025-02-18
### Update
- ServiceLocator

## [1.16.3] - 2025-02-17
### Update
- ServiceLocator
### Fix
- SymphonyVisualElement

## [1.16.2] - 2025-02-17
### Update
- SymphonyVisualElement

## [1.16.1] - 2025-02-17
### Update
- ServiceLocator
### Fix
- ServiceLocator
- SceneLoader
- SaveDataSystem
- PauseManager

## [1.16.0] - 2025-02-16
### Add
- SymphonyVisualElement

## [1.15.0] - 2025-02-16
### Update
- SceneLoader

## [1.14.0] - 2025-02-15
### Fix
- SymphonySystem
- SceneLoader

## [1.13.0] - 2025-02-15
### Add
- SymphonySystem

## [1.12.0] - 2025-02-15
### Add
- SymphonyUtility
### Update
- SingletonDirector -> ServiceLocator

## [1.11.0] - 2025-02-14
### Add
- Assembly Definition

## [1.03.0] - 2025-02-14
### Update
- SymphonyDebugLog

## [1.02.0] - 2025-02-14
### Add
- PauseManager

## [1.01.0] - 2025-02-13
### Update
- SymphonyStopWatch
### Fix
- SingletonDirector
- SymphonyTask

## [1.00.0] - 2025-02-13
### Add
- SaveDataSystem
- SceneLoader
- SingletonDirector
- SymphonyDebugLog
- SymphonyStopWatch
- SymphonyTask
