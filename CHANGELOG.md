# Changelog

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
