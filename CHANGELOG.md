# Changelog

## [6.2.0] - 2026-08-18
メインツールバーから初期シーン処理を切り替えられるようにしました。

### Add

- **Unity 6000.3以降のメインツールバーへ `Scene Init` トグルを追加しました。** `SceneLoadConfig.asset` を選択せずに、次回のPlay Mode開始時に初期シーン処理を実行するか切り替えられます。Unityの公式Main Toolbar APIを登録口にしているため、外部のToolbar拡張パッケージやUnity内部型への反射には依存しません（[#189](https://github.com/HIBIKI5201/SymphonyFramework/issues/189)）。

## [6.1.1] - 2026-08-18
PackagerのImportタブで、取り込み元ディレクトリをエクスプローラーから直接指定できるようにしました。

### Fix

- **Importタブの取り込み元が `Exported Packages Path` 配下の出力履歴に限定される問題を修正しました。** `Select Folder` から任意の出力済みディレクトリを選択できるため、別の場所へコピーした出力物や他プロジェクトから受け取った出力物も差分インポートできます（[#184](https://github.com/HIBIKI5201/SymphonyFramework/issues/184)）。

## [6.1.0] - 2026-08-17
Symphony AdministratorにDebug HUDの状態確認とShow / Hide操作を追加しました。

### Add

- **Symphony AdministratorへDebug HUDパネルを追加しました。** Play Mode中の初期化、利用可否、表示状態、追加テキスト登録数を確認し、Show / Hideを操作できます。Edit Modeと通常ビルド条件では安全に操作を無効化します。

### Change

- **Debug HUDの状態変更をinternal ViewModelから通知するようにしました。** Shortcut、メニュー、公開APIのどの経路で表示や登録数が変わっても、Administratorを開いたまま最新状態へ追従します。Domain Reload無効でもPlay Mode終了時に購読と状態を解放します。

## [6.0.1] - 2026-08-17
Debug HUDのInput Actionを編集できず、Play Mode初期化時にBinding配列のAssertが出る不具合を修正しました。

### Fix

- **`Project Settings > SymphonyFrameWork > Debug HUD Shortcut` のInput Actionをクリックしても、展開やBindingの選択が反応しない不具合を修正しました。** SettingsProviderがIMGUIフレームごとに `SerializedObject` を作り直し、Input SystemのPropertyDrawerが保持するTreeView状態を失っていたことが原因です。同じ設定画面を表示している間は編集状態を再利用し、閉じたときに解放するようにしました。
- **設定画面を開いた後のPlay Mode初期化で `For singleton action, bindings array must match that of the action` が出る不具合を修正しました。** 変更が無いフレームでもBinding配列を適用し、Domain Reload無効環境でInput Systemの内部ActionMapだけが古い配列を参照していました。内容が変わった場合だけ保存し、保存後はAction IDとBindingを維持した新しいActionへ再構築します。

## [6.0.0] - 2026-08-17
Debug HUDを設定可能なInput Actionで表示し、EditorとDevelopment Buildだけで動作するようにしました。

### Breaking

- **通常のPlayerビルドでは `SymphonyDebugHUD` が動作しなくなりました。** `Show` / `Hide` / `AddText` / `RemoveText` は初期化後も何も行わず、HUD用GameObjectや入力監視を生成しません。製品版へデバッグ情報を混入させないための変更です。HUDを確認するときは、Unity EditorまたはDevelopment Buildを使用してください。
- **`AddText` はHUDを自動表示しなくなりました。** 表示内容だけを登録し、HUDが非表示でも登録を保持します。従来の自動表示に依存していたコードでは、必要なタイミングで `SymphonyDebugHUD.Show()` を呼ぶか、設定したShortcutで表示してください。

### Add

- **Debug HUDの表示を切り替えるInput Actionを追加しました。** 既定値は `Shift + D + P` です。`Project Settings > SymphonyFrameWork > Framework Settings > Debug HUD Shortcut` から、Keyboard・GamepadなどInput Systemが扱う任意のBindingへ変更できます（[#103](https://github.com/HIBIKI5201/SymphonyFramework/issues/103)）。
- **Debug HUDの参考ライブラリとして [SRDebugger](https://www.stompyrobot.uk/tools/srdebugger/) をREADMEへ追加しました**（[#108](https://github.com/HIBIKI5201/SymphonyFramework/issues/108)）。

### Change

- **`Hide` 後も `AddText` で登録した内容を保持するようにしました。** 再表示すると、非表示中に追加・削除した内容を含む最新の一覧が復元されます。
- **直接利用しているUnity Packageを `package.json` の `dependencies` へ明記しました。** Addressables、Input System、Audio、IMGUI、JSON Serialize、UIElements、Newtonsoft Json、Test Frameworkの検証済みバージョンを固定し、Unity Editorの標準同梱内容が変わっても依存解決できるようにしました。

## [5.1.0] - 2026-08-16
Save Dataパネルへ表示するセーブデータ型を、Project Settingsのチェックボックスで選べるようにしました。公開APIとシリアライズ形式は 5.0.1 から変更していません。

### Add

- **`Project Settings > SymphonyFrameWork > Save System` へ `Managed Save Data Types` を追加しました。** 5.0.0 系では、`SaveDataContent` を継承した型がプロジェクト内のどのアセンブリにあっても Save Data パネルへ並ぶため、**テスト用アセンブリの検証用セーブデータ型まで表示されていました**（[#170](https://github.com/HIBIKI5201/SymphonyFramework/issues/170)）。チェックを外した型はパネルへ表示されなくなります。
- **既定値は、テスト用アセンブリの型が無効、それ以外の型が有効です。** `nunit.framework` または `UnityEngine.TestRunner` を参照するアセンブリをテスト用と判定します。**既定値を決めるためだけの判定で、チェックはいつでも上書きできます。**型を追加したときは、設定を触らなくても既定で表示されます。
- **一覧は名前空間の階層で並び、分岐の無い名前空間は1行にまとまります。** 名前空間の行のチェックは配下をまとめて切り替え、混在している場合は混在表示になります。
- **設定は `ProjectSettings/Packages/symphonyframework/SaveDataVisibilityConfig.asset` へ保存されます。** 保存されるのは操作して切り替えた型だけで、画面を開くだけでは生成されません。**Editorの表示だけに効く設定で、`SaveStore` の実行時の挙動は変わりません。**
- **Save Data パネルは、設定の変更へ開いたまま追従します。** これまで型一覧はパネルの初期化時にしか作られませんでした。

## [5.0.1] - 2026-08-16
テストを38件追加し、公開型のテスト網羅を機械的に固定しました。公開APIとシリアライズ形式は 5.0.0 から変更していません。

### Add

- **`SymphonyStringUtil` / `SymphonyComponentUtil` / `SymphonyLazyObject<T>` へテストを追加しました**（38件）。いずれもこれまでテストがありませんでした。EditModeテストは377件から415件になりました。
- **公開型のテスト網羅を `PublicTypeTestCoverageTests` で固定しました。** 公開型 `X` に対して `Tests/Editor/XTests.cs` が無い場合、既知の残作業一覧（86件）に載っている型だけが許されます。**新しい公開型をテスト無しで追加すると落ちます。**一覧は減らすためのもので、テストを書いたのに消し忘れた場合と、削除済みの型が残っている場合も検出します。
- **テストを書く過程で見つかった `SymphonyComponentUtil.GetComponentInChildrenExcludeSelf` の不具合を、テストと `// TODO` で記録しました。** `includeInactive: false` でも直下の非アクティブな子を返します。`GetComponentInChildren` が検索の起点自身を `includeInactive` に関わらず対象へ含めるためです。**この版では挙動を変えていません。**修正は [#179](https://github.com/HIBIKI5201/SymphonyFramework/issues/179) で扱います。現在の挙動をテストで固定してあるため、修正時はそのテストを書き換えることになります。

## [5.0.0] - 2026-08-16
ComponentとInterfaceを所属モジュールへ移しました。4つの公開型の名前空間が変わる破壊的変更です。

### Breaking

- **Service Locatorに属する3つの型を `SymphonyFrameWork.System.ServiceLocate` へ移しました。** これまで `Runtime/Component/` と `Runtime/Interface/` に置かれ、モジュールと無関係な名前空間にありました。**どのモジュールの型なのかが、置き場からも名前空間からも分からない状態でした。**
- **`IInitializeAsync` を `SymphonyFrameWork.System.SceneLoad` へ移しました。** `SceneLoader` がシーンのルートを初期化するための契約であり、SceneLoader以外から使う場面がありません。
  - **移行方法**: `using` を書き換えてください。型名とメンバーは変わりません。

  | 型 | 旧namespace | 新namespace |
  | --- | --- | --- |
  | `ServiceLocateComponent` | `SymphonyFrameWork.Utility` | `SymphonyFrameWork.System.ServiceLocate` |
  | `SymphonyLocateObject<T>` | `SymphonyFrameWork.Utility` | `SymphonyFrameWork.System.ServiceLocate` |
  | `IInjectable` / `IInjectable<T...>` | `SymphonyFrameWork` | `SymphonyFrameWork.System.ServiceLocate` |
  | `IInitializeAsync` | `SymphonyFrameWork` | `SymphonyFrameWork.System.SceneLoad` |

  - **シーンとPrefabの参照は切れません。** `git mv` で `.meta` ごと移しており、`ServiceLocateComponent` を貼ったGameObjectはGUIDで解決されます。
  - **互換シムは置いていません。** `IInjectable` と `IInitializeAsync` はinterfaceで、旧名の別interfaceを残しても実装済みの型が新しい契約を満たさないため、移行期間を作れないためです。

### Change

- **`Runtime/Component/` を削除しました。** 中身の2型をどちらもService Locatorへ移し、空になったためです。
- `IGameObject` は `Runtime/Interface/` と `SymphonyFrameWork` 名前空間に残しています。特定のモジュールに属さない共通の契約のためです。

## [4.2.1] - 2026-08-16
サブシステムの置き場を Runtime/System から Runtime/Service へ変更しました。公開APIと名前空間、シリアライズ形式は 4.2.0 から変更していません。

### Change

- **`Runtime/System/` を `Runtime/Service/` へ改名しました。** `System` は責務を表すには大雑把で、C#の `System` 名前空間とも紛らわしいためです。`git mv` で `.meta` ごと移しており、**GUIDは維持されるため利用側の参照は切れません。**
- **名前空間は `SymphonyFrameWork.System.*` のままです。** `SymphonyFrameWork.System.SaveData` などは利用側のすべての `using` に現れるため、変更すると破壊的変更になります。フォルダ名と名前空間の乖離は意図的なもので、揃える場合はメジャー更新として別途扱います。**利用側のコードに必要な変更はありません。**

## [4.2.0] - 2026-08-16
エラーログへ Symphony Framework のバージョンを含めるようにしました。ログ1行だけで、不具合報告に必要な版が分かります。

### Add

- **`SymphonyConstant.VERSION` を追加しました。** `package.json` の `version` と同じ値を持つ定数です。**Playerビルドに `package.json` は含まれないため、実行時にバージョンを読む手段がありませんでした。** 更新は `release_round.py bump` が行い、`preflight` が `package.json` との一致を検査します。

### Change

- **エラーログの先頭へ `[SymphonyFrameWork v<版>]` を付けるようにしました。** 不具合の報告を受ける側が最初に必要とするのがバージョンで、これまでは利用者に別途確認してもらう必要がありました。**Consoleに出た1行、あるいは `Cache/Log.txt` の1行だけで版が分かります。**
  - 対象は**エラーだけ**です。通常ログと警告ログの見た目は変わりません。
  - **`LogException` はConsoleの表示を変えません。** バージョンを添えるには例外を包む必要があり、Consoleの先頭行が本来の例外型でなくなってスタックトレースの追跡が壊れるためです。ファイル出力（`Cache/Log.txt`）の行には付きます。
  - ログのテキストを完全一致で判定している利用側のコードがある場合、エラーログだけ一致しなくなります。

## [4.1.0] - 2026-08-16
フレームワーク内のログをすべて SymphonyDebugLogger 経由へ統一し、例外用の LogException を追加しました。Framework が出すログは Cache/Log.txt にも残ります。

### Add

- **`SymphonyDebugLogger.LogException(Exception, UnityEngine.Object)` を追加しました。** Unity Consoleへはスタックトレース付きの例外として出力し、ファイル出力の購読者へは型名と理由を1行に畳んだテキストをError種別で通知します。**Consoleの表示（スタックトレースとジャンプ）を犠牲にせず、ファイルにも例外の内容を残すための分担です。**`ArgumentNullException` のようにMessage自体が複数行になる例外があるため、改行は空白へ畳みます。nullを渡した場合は例外にせず、その旨を診断ログとして出します。

### Change

- **フレームワーク内のログを、すべて `SymphonyDebugLogger` 経由へ統一しました。** `Runtime/` `Core/` `Editor/` の35ファイル・104箇所が `UnityEngine.Debug` を直接呼んでおり、**Editorのファイル出力の購読者が拾えないため `Cache/Log.txt` に残っていませんでした。**Consoleに出ているのに後からログを追えない状態でした。Console上の見え方（重要度とメッセージ）は変わりません。
- **`SymphonyFrameWork.Core` アセンブリのログも同じ出力先へ載るようにしました。** CoreはRuntimeの下位にあり `SymphonyDebugLogger` を直接参照できないため、Coreへ中継口（`internal`）を置き、RuntimeとEditorのComposition Rootが出力先を注入します。**注入前に発生したCore層のログは、これまでどおりUnity標準の出力へ落ちます。**利用側から見える型は追加していません。

## [4.0.0] - 2026-08-15
サンプルをUnityのインポート対象外である Samples~ へ移しました。サンプルの13クラスが SymphonyFrameWork アセンブリから外れる破壊的変更です。

### Breaking

- **`Samples/` を `Samples~/` へ移しました。サンプルの13クラスが `SymphonyFrameWork` アセンブリから外れます。** これまで `Samples/` に asmdef が無かったため、サンプルスクリプトはパッケージ本体の `SymphonyFrameWork` アセンブリへ取り込まれ、**サンプルを使うかどうかに関わらず利用側の全ビルドへ出荷されていました。**さらに Package Manager の `samples` からインポートすると、同じ `SymphonyFrameWork.Samples.*` 名前空間・同じクラス名が `Assets/Samples/` にもコンパイルされ、**利用側プロジェクトが CS0101（同名の定義が複数存在する）でコンパイルできなくなっていました。**末尾チルダのフォルダはUnityのアセットパイプラインから不可視になるため、二重定義が起きなくなります。
  - **移行方法**: サンプルのクラス（`ServiceLocatorSample_1`、`SaveDataSystemSample_Controller` など13件）を利用側のコードから直接参照している場合、その参照は解決できなくなります。`Window > Package Manager > Symphony Framework > Samples` から必要なサンプルをインポートしてください。`Assets/Samples/Symphony Framework/<version>/<サンプル名>/` へコピーされ、利用側プロジェクトのコードとしてコンパイルされます。**サンプルは利用例であり、パッケージのAPIとして参照する対象ではありません。**
  - **すでにインポート済みで CS0101 が出ていた利用者は、この版へ更新するとエラーが解消します。**
- **`package.json` の `samples[].path` を `Samples~/Runtime/...` へ変更しました。** Unity標準のパッケージレイアウトに合わせています。Package Managerからのインポート操作と、コピー先の `Assets/Samples/` のパスは変わりません。

## [3.10.3] - 2026-08-15
Save Systemの設定画面を開いた副作用で設定アセットの生成が始まる問題を修正しました。公開APIのシグネチャとシリアライズ形式は3.10.2から変更していません。

### Fix

- **`Project Settings > SymphonyFrameWork > Save System` を開くだけで、パッケージ全体の設定アセット生成と `AssetDatabase.Refresh` が走っていた不具合を修正しました。** 設定画面の描画callbackが `SymphonyConfigManager.AllConfigCheck()` を直接呼んでいたためです。**Editor起動時のアセット変更を1回のRefreshへまとめる集約の外側で生成が起きており、生成対象は `SaveDataConfig` だけでなくSceneLoad・Audio・Editor設定を含む全アセットでした。**画面は未生成である旨と `設定アセットを生成` ボタンを表示するだけにし、生成は起動時と同じ `SymphonyEditorOrchestrator` の経路へ委譲します。ボタンを押さない限りアセットは変更されません。設定アセットが揃っている通常の状態では、画面の見た目も操作も変わりません。

## [3.10.2] - 2026-08-15
自動生成enumが、シーン名やタグ名にC#の予約語が含まれるとコンパイルできない.csを書き出す不具合を修正しました。公開APIのシグネチャとシリアライズ形式は3.10.1から変更していません。

### Fix

- **シーン名・タグ名・Audio Groupの名前にC#の予約語（`class`、`int`、`event`、`base` など）を使うと、生成された`SceneListEnum`などがコンパイルエラーになる不具合を修正しました。** `EnumGenerator` が「除外しました」と警告しながら、実際にはその候補を生成対象へ残していたためです。**利用者から見ると「シーンを追加したらプロジェクトが壊れた」という形で現れ、警告文が原因を指していませんでした。**予約語は除外せず、`@` を前置した識別子（`@class`）として生成します。除外するとそのシーンが `SceneListEnum` から消えて `SceneLoader` から参照できなくなり、利用者の意図を壊すためです。**利用側は `SceneListEnum.@class` と書けます。**警告文も実際の動作に合わせました。
- **予約語の判定表が6語しか持っておらず、`int` や `class` を含む大半の予約語を素通ししていた不具合を修正しました。** C#の予約語77語すべてを対象にします。`var`、`value`、`record` などの文脈キーワードは識別子として使えるため対象外で、これまでどおりそのまま生成します。
- **`value__` という名前の候補を除外するようにしました。** コンパイラがenumの値の格納に使う名前で、`@` を前置しても列挙子にできません。
- **識別子として使えない候補の判定で、nullや空文字が例外になっていたのを修正しました。** 他の無効な候補と同じく警告のうえ除外します。
- **重複の除去を、エスケープ後の名前で行うようにしました。** `class` と `@class` が両方あると同じ列挙子が2つ生成され、上のエスケープ自体が新たなコンパイルエラーになります。有効な候補だけの場合の生成結果と並び順（最初に現れた位置を保つ）は変わりません。

## [3.10.1] - 2026-08-15
警告を出すためのnull診断APIが、真のnull参照を渡すと例外で落ちる不具合を修正しました。公開APIのシグネチャとシリアライズ形式は3.10.0から変更していません。

### Fix

- **`SymphonyDebugLogger.CheckComponentNull` へ真のnull参照を渡すと、警告ではなく`NullReferenceException`になっていた不具合を修正しました。** nullと判定した分岐の中で`component.name`を読んでいたためです。`UnityEngine.Object`の`== null`は「破棄済みだがマネージド参照は生きている」場合と「真のnull参照」の場合の両方でtrueになり、後者では`name`の取得自体が失敗します。**名前を読まず、未代入（`unassigned`）と破棄済み（`destroyed`）のどちらであるかを警告文へ含める形へ変えました。**破棄済みの場合も`name`は読めないため、両方とも名前を出しません。
- **`SymphonyDebugLogger.LogAndCheckComponentNull` が、破棄済みのUnityオブジェクトを「nullではない」と報告していた不具合を修正しました。** 型引数に制約が無いため`== null`が`UnityEngine.Object`の比較演算子ではなく参照比較になっており、`Destroy`済みのComponentやGameObjectでは`false`を返していました。**戻り値を信じて参照した利用側が、警告を1つも見ないまま`MissingReferenceException`で落ちる状態でした。**対象が`UnityEngine.Object`のときだけUnityの比較演算子で判定します。Unityオブジェクト以外の参照に対する挙動は変わりません。

## [3.10.0] - 2026-08-14
Save Data パネルへ、Inspectorの接続状態を示す色ランプと、非接続でのセーブデータ編集を追加しました。Play Modeへの持ち越し設定が1つ増えます。既存の公開APIとシリアライズ形式は3.9.7から変更していません。

### Add

- **Save Data パネルが、Inspectorに映っているデータの所有者を色ランプで示すようになりました。** 緑はRegistryの現在インスタンスへ接続中、黄はWindow専用インスタンスへ保存値を読み込み済み、赤は新規のWindow専用インスタンス、消灯は未選択です。**接続中の編集はランタイムへ即座に効き、非接続の編集は誰にも効きません。**効き方が正反対なのに画面から見分けが付かず、どちらを触っているのか分からない状態でした。パネルの外枠も同じ色になります。
- **一覧で型を選ぶと、保存データがある場合は自動で読み込むようになりました。** 読み込みは非同期で、完了するまでランプは赤のまま、隣に `Loading…` を表示します。**完了時の上書きで編集内容が消えないよう、読み込み中のInspectorは編集できません。**選択を切り替えた後に届いた完了は破棄します。保存データが無い型では読み込みを開始しません。
- **非接続の状態でもセーブデータを編集・保存・削除できるようになりました。** これまで未読み込みの型はInspectorが空で、パネルから内容を作ることができませんでした。非接続時の Load / Save / Delete は **Registryを一切経由せず**、Window専用インスタンスと保存先だけを対象にします。Edit ModeでRegistryへ読み込んでも、Play Mode突入時にRegistryごと作り直されるため持ち越されないからです。
- **非接続で編集した内容を Play Mode へ持ち越せるようになりました。** パネルの `Play Mode へ持ち越す` を有効にすると、未保存の変更があるときに限り、Play Mode突入時へ保存先へ書き出します。ゲーム側の `SaveStore.LoadAsync` がその内容を読むため、Editorで用意したデータをそのままゲームで使えます。**既定は無効です。**有効にしない限り、Play Mode突入が保存先へ書き込むことはありません。設定は開発者ごとの `UserSettings/SymphonyFrameWork/SymphonyUserSettingConfig.asset` に保存され、Editorを再起動しても残ります。
- **`SymphonyUserSettingConfig` へ `IsSaveDataPlayModeCarryOverEnabled` を追加しました。** 既存アセットは既定値 `false` で読み込まれます。この版で追加した公開APIはこれ1件だけです。

### Change

- **Save Data パネルの操作と問い合わせが、View層の `SaveDataViewStore` を経由するようになりました。** これまでウィンドウは表示を ViewModel から購読しつつ、操作は `SaveStore` を直接呼んでいました。Registryを経由しない編集用I/Oが必要になり、これを利用側の公開APIへ広げたくないためです。`SaveDataViewStore` は `internal` で、問い合わせを Query へ、Command を Service へ委譲するだけの型です。**`SaveDataViewModel` は表示専用のまま変更していません。**利用側から見た `SaveStore` の振る舞いも変わりません。
- **一覧の見出しを `Registry Cache` から `Save Data Types` へ変えました。** Registryに載っている型だけでなく、対応する全型を表示するようになったためです。

## [3.9.7] - 2026-08-14
Save Data パネルがランタイムのロードへ追従しない不具合と、対応型が一覧に出ない不具合を修正しました。公開APIとシリアライズ形式は3.9.6から変更していません。

### Fix

- **Save Data パネルで型を選んだまま、ランタイムがその型をロードしても、表示が古いままだった不具合を修正しました。** `SaveDataWindow` は ViewModel の更新通知を受けても一覧を作り直すだけで、Inspector のバインド先を評価し直していませんでした。バインドの更新が型の選択と各ボタン操作の中でしか起きなかったためです。通知のたびにバインド元を評価し、解決した状態が変わったとき、または接続中の現在インスタンスが差し替わったときにInspectorを繋ぎ直します。**編集途中に無条件で作り直さないよう、変化が無い通知では再バインドしません。**
- **キャッシュ済みでも永続化済みでもないセーブデータ型が、パネルの一覧に1件も出なかった不具合を修正しました。** 一覧の生成が、Registryに載っている型と保存データが存在する型だけを行にしていました。まだ一度も使われていない型は選択する手段が無く、パネルから内容を確認できませんでした。対応する全型を行に含めます。一覧へ出すだけでは Registry のインスタンス生成も保存先へのI/Oも発生しません。

## [3.9.6] - 2026-08-12
Save Data Registryパネルにセーブデータが表示されない不具合を修正しました。公開APIとシリアライズ形式は3.9.5から変更していません。

### Fix

- **Symphony Administrator の Save Data Registry パネルに、セーブデータが1件も表示されない不具合を修正しました。** `SaveDataWindow` が一時編集用の `ScriptableObject` をコンストラクタ本体で生成していましたが、基底クラスのコンストラクタが `Initialize_S` を同期的に呼ぶため、その時点ではまだ生成されていませんでした。初期化が `NullReferenceException` で中断し、ViewModel の購読と一覧の更新に到達していませんでした。生成をフィールド初期化子へ移し、基底コンストラクタより先に完了するようにしています。
- **上記の初期化失敗が Console に出ていなかった問題を修正しました。** `SymphonyVisualElement.InitializeTask` は待機されないことがあり、`Initialize_S` の例外が観測されないまま初期化途中のUIだけが残っていました。例外をログしてから再送出するようにしています。
- **プロジェクトに `SaveDataContent` を継承した型が1つも無いとき、Save Data Registry パネルのステータスが「初期化中です…」のまま残る不具合を修正しました。** 型が見つからない理由を表示します。

## [3.9.5] - 2026-08-11
コード全体へコメントと可読性の規約を適用しました。公開APIとシリアライズ形式は3.9.4から変更していません。

### Change

- **`Tests/` と `Samples/` を除く全160ファイルへ、`Documentation/CodeGuidelines.md` のコメント規約を適用しました。** 規約は文書として存在していましたが、コード側は1ファイルも適合していませんでした。適合していないコードが多数派である限り、新しいコードの書き手は周囲を手本にするため、規約が定着しません。
- **型の中身を `#region 外部向けAPI` と `#region 内部処理` へ二分割しました。** 利用側から呼べるものと実装の都合が、型を開いた時点で分かれて見えます。メンバーを持たない型と型定義の無いファイルには入れていません。
- **メソッドの中へ、処理のまとまりごとのコメントを追加しました。** 特に、分岐が成立する状況、順序の理由、回避している問題を書いています。18,551行に対して278行しかコメントがない状態でした。
- **型とメソッドのサマリーを複数行形式に、プロパティとフィールドを1行形式に統一しました。** 流し読みしたときに型とメソッドの区切りが目に留まります。
- **ローカル変数の型を明示し、`var` を廃止しました**（匿名型を除く）。右辺が `new` の場合はターゲット型 `new()` で短縮しています。
- **制御構文の波括弧を補い、本文が1文なら1行形式、2文以上ならAllman形式へ揃えました。** 1行にすると120文字を超える場合はAllman形式のままにしています。

実行時の振る舞いと生成物は変わりません。ただし `EnumGenerator` が出力する列挙型のサマリーだけ、同じ規約に合わせて複数行形式になります。

## [3.9.4] - 2026-08-11
HTML版ドキュメントのmermaidを、図として表示するようにしました。公開APIとシリアライズ形式は3.9.3から変更していません。

### Add

- **`Documentation~/Html/` のmermaidブロックが、コードブロックではなく図として表示されるようになりました。** `Window > SymphonyFrameWork > Documentation` から開くHTMLで、モジュール文書の内部構造図とアーキテクチャ図がそのまま読めます。これまでは「オフラインで開く前提のため外部スクリプトを読み込まない」という理由から、mermaidのソースをそのまま表示していました。
- **図はSVGとして `Documentation~/Html/Diagrams/` へ同梱します。** 生成時にSVGへ変換して`<img>`から参照する方式のため、実行時のスクリプトを読み込みません。オフラインでもそのまま表示されます。ダークテーマでも読めるよう、図の下地は常に白で描画します。

## [3.9.3] - 2026-08-11
AIエージェント向け文書の役割分担を整理しました。公開APIとシリアライズ形式は3.9.2から変更していません。

### Change

- **`Documentation~/AgentUsage.md` の `## 共通の前提` を削除し、`AGENTS.md` の `## 1. 常に守ること` を常時ルールの唯一の正本にしました。** 7項目のうち6項目が`AGENTS.md`と同じ内容で、片方だけを更新すると2つの文書が食い違う状態でした。AgentUsage.mdは、namespaceと入口の型から読むべきモジュール文書を引くAPI索引に役割を絞ります。`## APIの参照先`と`## 非推奨APIと移行`は変更していません。
- **重複していなかった`Cache/Log.txt`の扱いを、`AGENTS.md`の`## 1. 常に守ること`へ9項目目として移しました。** 削除ではなく移動です。
- **`AGENTS.md`の`## 0. 作業内容ごとの参照先`へ、AgentUsage.mdへの行を追加しました。** これまでREADMEの索引からしか辿れず、エージェントが最初に読む文書からは到達できませんでした。

## [3.9.2] - 2026-08-11
Symphony AdministratorのUXMLが参照するUSSのパスを修正しました。公開APIとシリアライズ形式は3.9.1から変更していません。

### Fix

- **`PauseWindow.uxml` と `AutoEnumGeneratorWindow.uxml` の `<Style src>` が、実在しないフォルダのパスを指していた問題を修正しました。** それぞれ `Assets/Scripts/SymphonyFrameWork/SymphonyEditor/...` と `Assets/SymphonyFrameWork/SymphonyEditor/...` を指しており、どちらも過去のフォルダ構成の名残です。残る4つのUXMLと同じ `Assets/SymphonyFrameWork/Editor/Administrator/UITK/SymphonyWIndow.uss` へ揃えました。

  **表示は修正前も壊れていません。** `src` のクエリ文字列に含まれるGUIDでUnityがUSSを解決するためです。**壊れるのはGUIDが失われた場合と、パス側で解決する経路を通った場合**で、それまでは誤ったパスだけが残り、読む人に存在しないフォルダがあるかのような誤解を与えていました。表示、公開API、シリアライズ形式への影響はありません。

  **管理パネルの全UXMLについて、`<Style src>` のパスとGUIDが同じアセットを指すことを検証するEditModeテストを追加しました。** パスだけが古くなっても表示が壊れないため、テストが無いと同じ食い違いが再発しても気づけません。

## [3.9.1] - 2026-08-11
Symphony Administratorの各パネルからドキュメントを開けるようにしました。**公開APIとシリアライズ形式は3.9.0から変更していません。**

### Add

- **`Symphony Administrator` の5つのパネルへ、右上に `ドキュメント` ボタンを追加しました。** Service Locate / Scene Load / Save Data / Pause / Auto Enum Generator のそれぞれから、対応するモジュール文書がブラウザで開きます。状態を見ている画面から、その機能の説明へ直接移動できます。

  ボタンは `SymphonyWIndow.uss` の `document-button` で装飾し、パネルの表示領域を圧迫しない大きさにしています。**Play Modeの開始・終了を繰り返してもボタンは機能し続けます。** 購読先のButtonはパネルと寿命を共にするためです。

## [3.9.0] - 2026-08-11
Editorからドキュメントをブラウザで開けるようにしました。**既存の公開APIとシリアライズ形式は3.8.7から変更していません。**

### Add

- **`Window > SymphonyFrameWork > Documentation` を追加しました。** 同梱ドキュメントの索引がブラウザで開きます。ドキュメントの在り処を知らないと読めない状態を解消するためです。

- **`Project Settings > SymphonyFrameWork` の各画面へ `ドキュメントを開く` ボタンを追加しました。** 設定画面から、その設定を説明している文書へ直接移動できます。`SymphonyFrameWork` は[Editor機能](./Documentation~/EditorTools.md)、`Save System` は[Save Data System](./Documentation~/Modules/SaveDataSystem.md)、`Asset Store Tools Packager` は[Asset Store Tools Packager](./Documentation~/Modules/AssetStoreToolsPackager.md)を開きます。

- **Editor専用の公開API `SymphonyFrameWork.Editor.SymphonyDocumentation.Open(SymphonyDocumentPageEnum)` を追加しました。** 利用側のEditor拡張からも同じ経路でドキュメントを開けます。

  ```csharp
  SymphonyDocumentation.Open(SymphonyDocumentPageEnum.SceneLoader);
  ```

  **`Open` は例外を投げません。** Editorの補助機能であり、ドキュメントを開けないことで利用側の作業を止めないためです。同梱HTMLが見つからない場合は、Consoleへ警告を出したうえでGitHub上の正本Markdownを開きます。**フォールバック先は `main` ブランチです。** リポジトリにバージョンタグが無いため、導入バージョンでは固定できません。

## [3.8.7] - 2026-08-11
ドキュメントをブラウザで読めるHTMLをパッケージへ同梱しました。**公開APIとシリアライズ形式は3.8.6から変更していません。**

### Add

- **`Documentation~/Html/` に、README・CHANGELOG・`Documentation~` 配下の全Markdownと同じ内容のHTMLを同梱しました。** `Documentation~/Html/index.html` が入口です。Markdownのままではブラウザで読みにくく、レンダリングのために外部サービスへ頼る必要がありました。

  **正本はMarkdownで、HTMLは生成物です。** 内容の修正はMarkdown側へ行ってください。生成は開発用リポジトリの `scripts/build_module_docs.py` が行い、正本との乖離はリリース前の検証で検出されます。

  各HTMLは1ファイルで完結し、外部のCSS・フォント・スクリプトを読み込みません。`file://` で開けるようにするためで、ライトとダークの両方の配色に対応しています。**既知の制限として、mermaidの図はレンダリングされずコードブロックとして表示されます。**

## [3.8.6] - 2026-08-11
Editor機能のドキュメントをモジュールごとに分離しました。**公開APIとシリアライズ形式は3.8.5から変更していません。**

### Change

- **Editorモジュールごとの文書を `Documentation~/Modules/` へ追加しました。** `AutoEnumGenerator.md` / `AssetStoreToolsPackager.md` / `ProjectStructureTools.md` の3本です。`ProjectStructureTools.md` には `FolderGenerator` / `AssemblyGenerator` / `SymphonyPackageLoader` をまとめています。

- **`Documentation~/EditorTools.md` を、単一モジュールへ属さない横断的な内容だけに縮めました（458行 → 154行）。** 残るのは索引、設定ファイルの置き場、Symphony Administrator、Framework設定、アセット保護、設定アセットの自動生成、Editorの初期化です。**索引表の行は消さず、移送先のモジュール文書へのリンクへ張り替えています。** どこに何があるかはこの1ファイルで引き続き分かります。

- **3.8.5 でモジュール文書へ移した節（Save System設定、Service Locatorのログ設定、`SymphonyDebugHUD`、ログのファイル出力、`SymphonyMcpTools`、Inspector属性）を `EditorTools.md` から削除しました。** 3.8.5 の時点では両方へ載っており、片方だけが更新される状態でした。

- **README.md の「Editor・デバッグ支援」を索引表へ置き換えました。** Runtimeモジュールに紐づくEditor機能は「機能ごとの使い方」の各モジュール文書側にあることを明記しています。

## [3.8.5] - 2026-08-11
利用者向けドキュメントをRuntimeモジュールごとに分離しました。**公開APIとシリアライズ形式は3.8.4から変更していません。**

### Change

- **Runtimeモジュールごとの文書を `Documentation~/Modules/` へ追加し、1モジュールを使うために1ファイルだけ読めばよい形にしました。** これまでは1つのモジュールを理解するのに、README.mdのクイックスタート、`Documentation~/AgentUsage.md`の実装時の注意、`Documentation~/EditorTools.md`のEditor入口、`Documentation~/Architecture.md`の内部構造という4ファイルを開く必要がありました。AIが参照するときのコンテキスト効率と、読む人の負担がどちらも悪化していたためです。

  追加した文書は `ServiceLocator.md` / `SceneLoader.md` / `SaveDataSystem.md` / `AudioManager.md` / `PauseManager.md` / `Debug.md` / `Utility.md` / `InspectorAttributes.md` の8本です。各文書は「入口」「クイックスタート」「実装時の注意」「Editor機能」「内部構造」「関連」の構成で統一しています。

- **README.md の「機能ごとの使い方」を索引へ置き換えました。** コード例は各モジュール文書へ移しています。READMEのアンカー（`#service-locator` など）を参照していた場合は、対応するモジュール文書へのリンクへ変更してください。

- **`Documentation~/AgentUsage.md` からモジュール別の節を、`Documentation~/Architecture.md` からサブシステム個別の内部構成図5点を、それぞれ対応するモジュール文書へ移しました。** AgentUsage.mdには共通の前提とAPIの参照先だけが、Architecture.mdにはアセンブリ構成、ディレクトリ構成、起動と終了、公開Facade全体のclass図だけが残ります。

- **`SymphonyStopWatch` と `SymphonyDebugLogger` の使い方を `Debug.md` へ、Utilityの各型の用途を `Utility.md` へ書き起こしました。** どちらもこれまでREADMEの箇条書き1行しか説明がありませんでした。

- **`Project Settings > SymphonyFrameWork` のService Locatorログ設定に、`Destroy Instance` の項目があることを明記しました。** `Documentation~/EditorTools.md` には登録ログと取得ログの2つしか書かれていませんでした。

## [3.8.4] - 2026-08-08
Symphony AdministratorのUXML名前空間解決を修正しました。公開APIとシリアライズ形式は3.8.3から変更していません。

### Fix

- **`Symphony Administrator` を開くたび、5つの管理パネルに `UxmlElementAttribute` または登録済みfactoryが無いという警告が出る問題を修正しました。** 各パネル型には既に `[UxmlElement]` と生成済み `UxmlSerializedData` がありましたが、親の `SymphonyWindow.uxml` が `SymphonyFrameWork.Editor` 名前空間を宣言せず、完全修飾名を名前空間なしのタグとして記述していました。

  **UXMLのルートで名前空間prefixを宣言し、5つの要素をprefix付きの型名で参照するようにしました。** Pause / Service Locate / Scene Load / Save Data / Auto Enum Generator の各パネルが登録済みカスタム要素として生成されます。公開API、メニューパス、設定保存先、シリアライズ形式への変更はありません。

  **親UXMLを実際にインスタンス化し、5型すべてを取得できることを確認するEditModeテストを追加しました。** 名前空間宣言やタグの記述が再び崩れた場合はテストで検出されます。

## [3.8.3] - 2026-08-07
Play Mode終了時の解除でService Locatorが例外になる問題の修正です。**公開APIのシグネチャとシリアライズ形式は 3.8.2 から変更していません。**

### Fix

- **Play Mode終了時、利用側の `OnDestroy` / `OnDisable` からの `ServiceLocator.UnregisterInstance` が `SymphonyNotInitializedException` になる問題を修正しました。** `SymphonyOrchestrator` がService Locatorを解放した後にUnityがシーンオブジェクトを破棄するため、解除処理は未初期化のLocatorを叩きます。

  **`AGENTS.md` と `Documentation~/AgentUsage.md` が勧めている「`OnEnable` で `RegisterInstance`、`OnDisable` で `UnregisterInstance`」という基本形そのものが落ちていました。** 2.15.1 で `ServiceLocateComponent` に同じガードを入れていますが、判定に使う `IsInitialized` は `internal` のため、利用側には同じ手段がありませんでした。

  **未初期化を「何も登録されていない」として扱う操作を、戻り値で表現するように変えました。** `UnregisterInstance`（3種）、`DestroyInstance`（2種）、`IsExistInstance`（3種）、`GetInstance<T>`、`TryGetInstance<T>`、`GetRegistrationInfos`、`TryGetRegistrationInfo` が、未初期化時に例外を投げず `false` ／ `null` ／空一覧を返します。**破棄処理で初期化状態を確認する必要はありません。**

  **解除だけでなく照会も含めたのは、`ServiceLocateComponent` 自身が `IsExistInstance` → `UnregisterInstance` の順で呼んでいるためです。** 解除系だけ直すと、そのパターンを真似た利用側は1つ手前で同じ例外を踏みます。

  `RegisterInstance` 系、`GetRequiredInstance`、`GetInstanceAsync` / `TryGetInstanceAsync` / `RegisterAfterLocate` は**これまでどおり `SymphonyNotInitializedException` を投げます。** 登録は状態を足す操作なので黙って落とすと不具合を隠し、待機系は未初期化では永久に発火しないためです。

  **`null` 引数の検証は未初期化判定より先に行います。** `UnregisterInstance(null)`、`IsExistInstance(null)`、`TryGetRegistrationInfo(null, out _)` は状態によらず `ArgumentNullException` です。

  **未初期化時に例外を捕捉して分岐していた場合、その `catch` は動かなくなります。** 戻り値を見る形へ書き換えてください。

## [3.8.2] - 2026-08-07
起動時のシーン整理でロードとアンロードの順序を入れ替えた修正です。**公開APIとシリアライズ形式は 3.8.1 から変更していません。**

### Fix

- **`Is Reset And Load On Play` が有効なとき、整理対象がロード済みシーンの全件になる構成で起動時のシーン整理が失敗する問題を修正しました。** 初期シーン一覧にも `Reset Ignore Scene List` にも含まれないシーンを単独で開いて再生した場合や、ビルドで最初のシーンがどちらの一覧にも無い場合が該当します。

  これまでは「整理対象をアンロード → 初期シーンをロード」の順で処理していましたが、**Unityは最後の1シーンをアンロードできない**ため `SceneManager.UnloadSceneAsync` が `null` を返し、`Failed to start unloading scene:` のエラーで整理が止まっていました。エラーだけでは終わらず、消えなかったシーンが残ったまま初期シーンが加算ロードされるため、**破棄されたはずのシーンのオブジェクトが初期シーンの初期化より先に動きます。**

  2.4.3 で `SymphonyOrchestrator` の管理オブジェクトを専用シーンから `DontDestroyOnLoad` へ移した際、常に1シーンが残ることを前提にしていたこの順序依存が見落とされていました。

  **順序を「初期シーンをロード → 整理対象をアンロード」へ入れ替えました。** `LoadSceneMode.Single` 相当の遷移が以前から採っている順序と同じで、Scene Loader 全体で1つの順序になります。

  **ロード済みシーンの数で分岐しません。** これまで正常に動いていた構成でも順序が変わり、**初期シーンの `IInitializeAsync` が、整理対象のシーンの `OnDestroy` より先に走ります。** 整理されるシーンの破棄処理が初期シーンの登録した状態へ触れる作りになっている場合は、影響を受けます。分岐にしなかったのは、開いていたシーンの数によって初期化の順序が変わる状態を残さないためです。

  **初期シーンのロードに失敗した場合はアンロードを行いません。** 置き換え先が無いまま現在のシーンを捨てると、ロード済みシーンが0件になるか、最後の1件のアンロードが失敗して中途半端な構成が残るかのどちらかにしかならないためです。

## [3.8.1] - 2026-08-05
Asset Store Tools Packager のレビュー指摘を反映した修正です。**公開APIとシリアライズ形式は 3.8.0 から変更していません。**

### Fix

- **出力に失敗したパッケージが `PackageManifest.json` へ載る問題を修正しました。** 「使用中アセットが0件」で出力を飛ばした場合と、`ExportPackage` が例外を投げた場合のどちらでも `.unitypackage` は作られませんが、マニフェストには全ディレクトリが記録されていました。

  取り込み側はマニフェストを見て存在しないファイルを開こうとし、そのパッケージが `New` や `Updated` のまま残ります。**マニフェストへは出力できたものだけを書くようにしました。**

- **バージョンログを読み込めなかったとき、保留中の加算が失われる問題を修正しました。** 加算対象のディレクトリ名を読み込みより先に消費していたため、`PackageVersions.json` が一時的にロックされているような場合でも保留が捨てられていました。

  そのディレクトリのリビジョンは次に変更があるまで進まず、取り込み側が更新を見落とします。**ログを読み込めた後で保留を消費するようにしました。**

- **設定ファイルを生成できなくてもパッケージ出力が続行する問題を修正しました。** `PackagerConfig.json` の書き込みに失敗してもログを出すだけで、既定値の設定をそのまま返していました。除外フォルダが空のまま出力へ進みます。**生成に失敗した場合は設定を返さず、出力を中止します。** 読み込みに失敗した場合と同じ扱いです。

- **Import タブの `Refresh` が、選択中の出力済みフォルダを先頭へ戻す問題を修正しました。** 別プロジェクトの出力を確認している最中に選択が失われていました。

- **パッケージの取り込み直後に、候補一覧が古い状態のままになる問題を修正しました。** `AssetDatabase.ImportPackage` は `interactive: false` でも反映が非同期のため、呼び出し直後に読むと同梱の `ExportedVersion.json` が更新前のままでした。取り込んだはずのパッケージが `Updated` のまま残ります。**`AssetDatabase.importPackageCompleted` を購読し、完了後に読み直します。**

- **Project Settings の `Create Default Pipeline` が、書き込み権限の無いフォルダで Project Settings 画面の描画を壊す問題を修正しました。** `Directory.CreateDirectory` の例外捕捉が `IOException` だけで、`UnauthorizedAccessException` などが IMGUI の描画中へ伝播していました。

- README の「現在のバージョン」の表示が 3.2.0 のままだったのを修正しました。

## [3.8.0] - 2026-08-05
パイプラインの標準手順に基底型を設けました。**既存の手順の動作は変わりません。**

### Add

- **`AssetStoreToolsStandardStrategy` を追加しました。** フレームワークが標準で用意する4つの手順（Singles / Combine / Used Dependencies / Create ZIP）の基底型です。

  サブクラスセレクターは、中間クラスを持つ派生型を「基底型名/型名」の階層で表示します。標準の手順をこの型の下へまとめることで、**利用側が追加した手順と選択肢の上で区別できます。** 利用側が自作する手順は、これまでどおり `AssetStoreToolsPackageStepStrategy` を直接継承してください。

### Change

- 統合パッケージのログ用語を「統合パッケージ」へ統一しました。同じ処理に「合成パッケージ」「合計パッケージ」の3つの表記が混在しており、ログを読む側が別の処理と取り違えます。

## [3.7.0] - 2026-08-05

Packager ウィンドウの出力形式を、3.6.0 で追加したパイプラインから選ぶ形にしました。**固定の3オプション（`Export Mode` / `Create ZIP File` / `Used Dependencies`）は無くなります。**

### Add

- **Project Settings で出力パイプラインを配列としてアサインできるようにしました。** `Project Settings > SymphonyFrameWork > Asset Store Tools Packager` の `Export Pipelines` です。保存先は `ProjectSettings/Packages/symphonyframework/AssetStoreToolsPackagerData.asset` で、プロジェクト共有です。

  `Create Default Pipeline` を押すと、`Singles → Used Dependencies → Create ZIP` のテンプレートを `Assets/Editor/SymphonyFrameWork/Configs/` へ生成してアサインします。**同名のアセットがある場合は上書きせず別名で作ります。** 利用側が手を入れたパイプラインを、ボタンの誤操作で失わせないためです。

  **Framework の起動時に自動生成はしません。** 利用側のリポジトリへ意図しないアセットを増やさないためで、Runtime の設定アセットが自動生成されるのは Framework の動作に必須だからです。パイプラインは「使う人が構成するもの」です。

- **`AssetStoreToolsPackagerData.Pipelines` と `SetPipelines` を追加しました。**

### Change

- **Packager ウィンドウの `Export Mode` を、パイプラインを選ぶポップアップ `Export Pipeline` へ変えました。** 選択肢の表示名はアセット名です。`Create ZIP File` と `Used Dependencies` のトグルは、パイプラインの手順として表現されるため削除しました。

  **パイプラインが1つもアサインされていない場合は、案内を表示して Export ボタンを出しません。** `Open Project Settings` ボタンから設定画面へ移動できます。

  **移行方法**: `Project Settings > SymphonyFrameWork > Asset Store Tools Packager` で `Create Default Pipeline` を1度押してください。3.5.0 までの `Singles` + `Used Dependencies` + `Create ZIP File` と同じ内容です。ZIP や絞り込みが不要なら、生成されたアセットの `Steps` から該当の手順を外します。

  Export タブへ切り替えたとき、パイプラインの一覧を読み直します。ウィンドウを開いたまま Project Settings を変更することがあるためです。

### Deprecated

- **`AssetStoreToolsPackager.PackageModeEnum` を非推奨にしました。** `Combine` を除くと `Singles` と `Nothing` しか残らず、enum として意味を失うためです。**次のメジャー更新で enum ごと削除します。**

  **移行方法**: `AssetStoreToolsPackagePipeline` を使用してください。`Singles` は `AssetStoreToolsSinglePackageStrategy`、`Combine` は `AssetStoreToolsCombinePackageStrategy` に対応します。

- **`AssetStoreToolsPackager.Export(string[], PackageModeEnum, bool, bool)` を非推奨にしました。**

  **移行方法**: `Export(string[], AssetStoreToolsPackagePipeline)` を使用してください。フラグと手順の対応は次のとおりです。

  | 旧 | 新 |
  | --- | --- |
  | `mode` に `Singles` | `AssetStoreToolsSinglePackageStrategy` |
  | `mode` に `Combine` | `AssetStoreToolsCombinePackageStrategy` |
  | `usedDependencies: true` | `AssetStoreToolsUsedDependenciesStrategy` |
  | `createZip: true` | `AssetStoreToolsCreateZipStrategy` |

  **統合パッケージ出力は機能として残ります。** 非推奨にするのは enum の選択肢であって、統合パッケージそのものではありません。差分インポートの単位にならないため既定テンプレートには含めていませんが、必要な場合はパイプラインへ `AssetStoreToolsCombinePackageStrategy` を追加してください。

## [3.6.0] - 2026-08-05

Asset Store Tools Packager の出力手順を、順序付きの Strategy 列（パイプライン）として組み替えられるようにしました。**ウィンドウの操作と出力結果は 3.5.0 と同じで、公開APIは追加だけです。**

### Add

- **パッケージ出力の手順を表す拡張点 `AssetStoreToolsPackageStepStrategy` を追加しました。** 利用側でこれを継承すると、独自の手順をパッケージ出力へ差し込めます。

  これまでの出力内容は `Export Mode` / `Create ZIP File` / `Used Dependencies` の 3 つの固定オプションでしか表現できず、**ZIP の代わりに独自の圧縮をかける、出力後に社内ツールへ通知する、といった要求へフレームワークを書き換えずに応える手段がありませんでした。**

  手順は `Plan` と `Execute` の 2 段階を持ちます。パイプライン全体で「全手順の `Plan`」→「全手順の `Execute`」の順に走り、各段階の中では並び順どおりに実行されます。**2 段階にしているのは、出力前に確認ウィンドウへ内容を提示するためです。** 絞り込みが `Execute` の中で起きると、提示した一覧と実物が食い違います。

  | 既定の手順 | 段階 | 役割 |
  | --- | --- | --- |
  | `AssetStoreToolsUsedDependenciesStrategy` | `Plan` | 使用中アセットと強制包含拡張子だけへ絞る |
  | `AssetStoreToolsSinglePackageStrategy` | `Execute` | ディレクトリごとに出力し、`PackageManifest.json` を書く |
  | `AssetStoreToolsCombinePackageStrategy` | `Execute` | 全ディレクトリを 1 つへまとめる |
  | `AssetStoreToolsCreateZipStrategy` | `Execute` | 出力フォルダを ZIP 化する |

  **1 つの手順が例外を投げても、記録して次の手順へ進みます。** 利用側の自作手順の失敗で、フレームワーク側の出力まで巻き添えにしないためです。

- **手順の並びを保持する `AssetStoreToolsPackagePipeline`（`ScriptableObject`）を追加しました。** `Assets > Create > SymphonyFrameWork > Asset Store Tools Package Pipeline` から作成でき、`[SerializeReference]` と サブクラスセレクターで手順を選べます。

- **`AssetStoreToolsPackager.Export(string[], AssetStoreToolsPackagePipeline)` を追加しました。** パイプラインを指定して出力します。

- **`AssetStoreToolsPackagePlan` と `AssetStoreToolsPackagePlanEntry` を公開しました。** 自作の手順が出力対象を絞り込むために必要なためです。

  **`AssetStoreToolsPackagePlanEntry.FilterAssetPaths` は絞り込みしかできません。** 手順がアセットを追加できると、確認ウィンドウで提示した内容より多くのものが出力され得るためです。計画そのものを組み立てられるのはフレームワーク内部だけです。

- **`AssetStoreToolsPackageExportContext` を追加しました。** `Execute` 段階の手順が受け取る、出力先パスと確定済みの計画です。

### Change

- **確認ウィンドウの表示を `Export Mode` / `Create ZIP` / `Used Dependencies` の 3 行から、パイプライン名と手順の並びへ変えました。** 手順は並び順のまま表示します。**並べ替えは行いません。** 誤った順序（たとえば ZIP 化を出力より前へ置く）をそのまま見せることが、順序の誤りに気づく手段になるためです。

- **出力対象の収集を 1 本にまとめました。** 絞り込みの有無で収集方法が分かれていたものを、「収集」と「絞り込み」の合成へ変えています。合成結果は 3.5.0 と一致します。

  **絞り込みを行わない場合だけ、確認ウィンドウの一覧へ `.bundle` や `.framework` などフォルダ形式のアセットが増えます。** この経路の実際の出力はディレクトリ単位の再帰なので、もともと出力には含まれていました。提示内容が実物へ近づく変化で、**出力される `.unitypackage` の中身は変わりません。**

### Deprecated

- **`AssetStoreToolsPackageContext` を非推奨にしました。** `ref struct` はフィールドへ保持できず、パイプラインの拡張点へ渡せないためです。

  **移行方法**: `AssetStoreToolsPackageExportContext` を使用してください。プロパティ名と意味（`PackageName`、`ExportRoot`、`ExportLocalPath`、`ExportFullPath`、`DateTime`）は同じです。`ExportDirectories` は `Plan.Entries` の `DirectoryPath` から取得できます。

## [3.5.0] - 2026-08-05

Asset Store Tools Packager を2タブ構成にし、**更新されたパッケージだけを取り込む差分インポート**を追加しました。**Runtime の公開APIとセーブデータ形式は変更していません。**

### Add

- **Packager ウィンドウへ Import タブを追加しました。** 出力済みパッケージのうち、このプロジェクトで更新されたものだけを取り込めます。

  出力した `.unitypackage` を別プロジェクトへ取り込むとき、**どれが前回から変わったのかを判断する手段がありませんでした。** 変更の無いパッケージまで毎回インポートすると、ネイティブプラグインや大量のテクスチャを含むディレクトリでは1件あたり数十秒かかります。

  取り込み元の出力済みフォルダを選ぶと、各パッケージが次のいずれかに分類されます。

  | 状態 | 意味 | 既定の選択 |
  | --- | --- | --- |
  | `New` | このプロジェクトへまだ導入されていない | 選択する |
  | `Updated` | 導入済みだが、出力側の方が新しい | 選択する |
  | `UpToDate` | リビジョンが一致している | 選択しない |
  | `Newer` | ローカルの方が新しい | **選択しない** |

  判定は、出力先フォルダの `PackageManifest.json` と、ローカルの `<Asset Store Tools Path>/<ディレクトリ>/ExportedVersion.json` のリビジョンを突き合わせて行います。どちらも 3.3.0 で追加したものです。

  **`Newer` を既定で選択しないのは、ローカルの方が新しいものを取り込むと意図しない巻き戻しになるためです。** 必要な場合は手で選んでください。

### Change

- **Packager ウィンドウを Export / Import の2タブにしました。** Export タブの操作と挙動は変えていません。ウィンドウを増やさず1つに収めるための変更です。

  Import タブへ切り替えたとき、出力済みフォルダとリビジョンを読み直します。別プロジェクトで出力した直後に開くことがあるためです。

## [3.4.0] - 2026-08-05

Asset Store Tools Packager の統合パッケージ出力（`Combine`）を非推奨にしました。**削除はしていません。** 既存の指定はこれまでどおり動作します。

### Deprecated

- **`AssetStoreToolsPackager.PackageModeEnum.Combine` を非推奨にしました。** 統合パッケージは全ディレクトリを1つの `.unitypackage` へまとめるため、**ディレクトリ単位で取り出せず、差分インポートの単位になりません。**

  3.3.0 で追加したバージョンログは、パッケージ単位のリビジョンを比較して「更新されたものだけを取り込む」ためのものです。`Combine` で出力したパッケージはこの比較に載せられないため、3.3.0 の時点で `Combine` だけの出力では `PackageManifest.json` を作っていません。実態として差分インポートの対象外である状態を、型の上でも明示しました。

  **移行方法**: `Export Mode` を `Singles` にしてください。個別出力したパッケージは `PackageManifest.json` へ記録され、差分インポートの対象になります。コードから `AssetStoreToolsPackager.Export` を呼んでいる場合は、第2引数を `PackageModeEnum.Singles` にしてください。

  **次のメジャー更新で `PackageModeEnum` ごと削除します。** `Combine` を除くと `Singles` しか残らず、enum と `Export` の `mode` 引数が意味を失うためです。旧シグネチャは型として成立しなくなるため `[Obsolete]` のシムを残せません。削除時に一緒に変わる箇所は [Deprecations.md](./Documentation~/Deprecations.md) に記録しています。

### Change

- **Packager ウィンドウで `Combine` を選ぶと、廃止予定である旨を表示するようにしました。** 選択自体は禁止していません。既存の運用を突然壊さず、移行期間を設けるためです。

- `Combine` だけで出力したときのConsole警告に、廃止予定である旨と移行先を追記しました。

## [3.3.0] - 2026-08-05

Asset Store Tools Packager に、パッケージ単位のリビジョン記録を追加しました。**Runtime の公開APIとセーブデータ形式は変更していません。** 変更は Editor 専用ツールに閉じています。

### Add

- **パッケージ対象ディレクトリごとにリビジョン番号を記録するようにしました。** 出力した `.unitypackage` を別プロジェクトへ取り込むとき、どのパッケージが前回から変わったのかを判定する手段がありませんでした。出力日時は「出力した日時」であって「中身が変わった日時」ではないため、判断材料になりません。

  記録するファイルは3種類です。

  | ファイル | 置き場 | 意味 |
  | --- | --- | --- |
  | `PackageVersions.json` | `Asset Store Tools Path` 直下 | このプロジェクトでの各ディレクトリの現在リビジョン |
  | `ExportedVersion.json` | 各ディレクトリ直下 | そのパッケージを出力した時点のリビジョン |
  | `PackageManifest.json` | 出力先フォルダ | その回に出力したパッケージ名とリビジョンの一覧 |

  `ExportedVersion.json` はパッケージへ同梱されるため、**インポート先では「今そこに入っているパッケージのリビジョン」**を表します。これと `PackageManifest.json` を突き合わせることで、次のバージョンで差分インポートを実現します。

  `PackageVersions.json` と `ExportedVersion.json` は `Assets/` 配下にあります。**インポート先での比較に使うため、利用側のリポジトリで版管理してください。**

- **`EditorSymphonyConstant` へ3つの定数を追加しました。** `ASSET_STORE_TOOLS_VERSION_LOG_FILE_NAME`、`ASSET_STORE_TOOLS_EXPORTED_VERSION_FILE_NAME`、`ASSET_STORE_TOOLS_MANIFEST_FILE_NAME` です。既存の `ASSET_STORE_TOOLS_CONFIG_FILE_NAME` と同じく、利用側が `.gitignore` やビルドスクリプトから名前で参照できるようにするためです。

### Change

- **パッケージ対象フォルダ配下を変更すると `PackageVersions.json` が更新されます。** 変更の検知は `AssetPostprocessor` が行い、書き出しは `SymphonyEditorOrchestrator` がまとめて1回だけ実行します。

  リビジョンは「実際には変わっていないのに増える」ことがあります。Unityが再インポートしただけでも加算されるためです。これは**不要なインポートが1回余分に起きる方向**の誤りであり、「変わったのに増えない」という逆方向の誤りは起きません。

- **`AssetStoreToolsPackager.Export` が出力時に `ExportedVersion.json` と `PackageManifest.json` を書くようになりました。** シグネチャと戻り値、および出力パッケージの既存の中身は変わりません。パッケージへ `ExportedVersion.json` が1ファイル増えます。

- **統合パッケージ（`Export Mode = Combine`）だけで出力した場合、`PackageManifest.json` を作りません。** 統合パッケージはディレクトリ単位で取り出せず、差分インポートの単位にならないためです。その場合はConsoleへ警告を出します。

## [3.2.1] - 2026-08-05

Editor機能と非推奨APIのドキュメントを追加しました。**コードは変更していません。** 公開API、シリアライズ形式、挙動のいずれも 3.2.0 と同じです。

### Add

- **`Documentation~/EditorTools.md` を追加しました。** Editor機能の正本です。

  これまでEditor機能の説明は README の `Editor・デバッグ支援` にある10行の箇条書きだけで、**`AssetStoreToolsPackager`、`SymphonyPackageLoader`、`SymphonyAssetProtector`、`SymphonyMcpTools`、Project Settings の各設定項目は1行も書かれていませんでした。** Runtime の各サブシステムには README のクイックスタートがあるのに、Editor機能には対応する正本がありませんでした。

  `Editor/` 配下の全モジュールについて、何をするものか、どのメニューやProject Settingsから開くか、設定がどこへ保存されるか、版管理へ含めるべきかを記載しています。README の箇条書きは索引として残し、詳細はこちらへ委譲します。

- **`Documentation~/Deprecations.md` を追加しました。** 非推奨APIと削除予定の正本です。

  非推奨APIの正本はこれまで CHANGELOG でしたが、CHANGELOG は時系列の記録であり、**「今なお非推奨で、まだ消えていないもの」を一覧できません。** 非推奨化した版まで遡って読む必要があり、その後で削除されたのかどうかも分かりませんでした。

  現在7件の `[Obsolete]` メンバーがあり、そのうち**6件は非推奨化がCHANGELOGへ記載されておらず、削除予定も決まっていません**（`SymphonyDebugLogger` の4件と `SymphonyTween` の2件）。この文書では削除予定を「未定」と明記し、判断が必要な項目が残っていること自体が見えるようにしています。

### Change

- `Documentation~/AgentUsage.md` の「非推奨APIの期限と移行先は CHANGELOG.md を正本とします」を、`Deprecations.md` を正本とする記述へ変更しました。正本が2つになることを避けるためです。非推奨化の経緯と時期は引き続き CHANGELOG を参照します。

- `AGENTS.md` の参照先表で、Editor・デバッグ機能の参照先を `EditorTools.md` へ、非推奨APIと移行方法の参照先を `Deprecations.md` へ変更しました。

## [3.2.0] - 2026-08-04

Asset Store Tools Packager に、出力前の確認ウィンドウを追加しました。**Runtime の公開APIとセーブデータ形式は変更していません。**

### Add

- **エクスポート前に、パッケージへ含まれるアセットを階層表示で確認できるようにしました。** Packager ウィンドウの `Export Selected Directories` を押すと、出力せずに確認ウィンドウが開きます。`Export` で実行、`Cancel` で中止します。

  3.1.1 で修正した取りこぼしのように、意図したアセットが入っているかは出力後のパッケージを展開しないと分かりませんでした。出力前に気づけるようにするための追加です。

  ディレクトリごとにツリーを表示し、フォルダには配下のアセット数を出します。対象アセットが0件のディレクトリは「（対象アセットなし）」と表示され、出力時に警告になることが分かります。

### Change

- **出力処理を「計画の生成」と「計画の実行」に分けました。** 確認ウィンドウへ提示するには、出力前に内容が確定している必要があるためです。

  `AssetStoreToolsPackager.Export(string[], PackageModeEnum, bool, bool)` のシグネチャと挙動は変えていません。このAPIを直接呼ぶ経路は、従来どおり確認ウィンドウを通さずに出力します。

## [3.1.1] - 2026-08-04
3.1.0 で入れた `PackagerConfig.json` を土台に、依存関係だけでは拾えないアセットの取りこぼしを修正しました。**Runtime の公開APIとセーブデータ形式は変更していません。** 変更は Editor 専用ツールに閉じています。

### Fix

- **「Used Dependencies」出力で `.asmdef` / `.asmref` とネイティブプラグインが落ちる問題を修正しました。** 強制的に含めるファイルが `.cs` のハードコード1種類しかなく、`AssetDatabase.GetDependencies` の依存関係グラフに載らないファイルが両方の条件から外れていました。出力したパッケージを導入した先で、asmdef 参照が解決できずコンパイルエラーになります。

  拡張子のホワイトリストを設定項目にし、既定値へ `.asmdef` / `.asmref` とプラグインバイナリを含めました。既存の `.cs` 強制追加もこのホワイトリストへ統合しています。

- **アセットの収集を `Directory.GetFiles` から `AssetDatabase.FindAssets` へ変更しました。** `.bundle` / `.framework` のようなフォルダ形式のネイティブプラグインは Unity が単一アセットとして扱うため、ファイル列挙では中身のファイルしか拾えず、`ExportPackage` へ渡しても出力されませんでした。あわせて、Unity がインポートしないファイルは収集対象から外れます（元々出力できなかったものです）。

- **除外設定ファイルが `Asset Store Tools Path` の変更に追従しない問題を修正しました。** 除外設定のパスだけが既定パスの定数から組まれており、Project Settings でパスを変更すると除外設定が読まれなくなっていました。

- 除外フォルダ名の比較で大文字小文字を区別しないようにしました。Windows のファイルシステムと判定が食い違うためです。

## [3.1.0] - 2026-08-04
Asset Store Tools Packager の設定を `PackagerConfig.json` へ集約しました。**Runtime の公開APIとセーブデータ形式は変更していません。** 変更は Editor 専用ツールに閉じています。取りこぼしの修正は 3.1.1 で行っています。

### Change

- **パッケージ化の設定を対象フォルダ直下の `PackagerConfig.json` へ集約しました。** 除外フォルダ名（旧 `ignore.txt`）と、新しく追加した強制包含拡張子の両方を1つのJSONで管理します。`Assets/` 配下にあるため、利用側のリポジトリで版管理し、手で編集できます。

  設定の分担は「どこにあるか」（`Asset Store Tools Path`、`Exported Packages Path`）が Project Settings、「何を詰めるか」（除外フォルダ、強制包含拡張子）が `PackagerConfig.json` です。

  **移行方法**: 作業は不要です。`PackagerConfig.json` が無く `ignore.txt` がある場合、初回の読み込みで内容を自動的に引き継ぎ、ログで通知します。`ignore.txt` は自動削除しないため、内容を確認してから削除してください。

- **設定ファイルが壊れている場合、既定値へフォールバックせずパッケージ化を中止します。** 除外設定が無視されて意図しないフォルダが出力されるより、止めて気づける方が安全なためです。エラーログにファイルパスと解析失敗の位置が出ます。

- Project Settings の Asset Store Tools Packager から、除外フォルダと強制包含拡張子を編集できるようにしました。編集内容は `Save` ボタンでファイルへ書き込みます。

### Deprecated

- **`EditorSymphonyConstant.ASSET_STORE_TOOLS_IGNORE_FILE` を非推奨にしました。** `ignore.txt` は読み込まれなくなったためです。

  **移行方法**: 設定ファイルの名前が必要な場合は `EditorSymphonyConstant.ASSET_STORE_TOOLS_CONFIG_FILE_NAME` を使ってください。パスは `Asset Store Tools Path` の設定値と連結して組み立てます。次のメジャー更新で、旧定数と `ignore.txt` からの移行処理をあわせて削除します。

## [3.0.1] - 2026-08-04
3.0.0 の確定にあわせて見つかった、保存日時の管理漏れと Editor パネルの不要なI/Oを修正しました。**公開APIとセーブデータ形式は 3.0.0 から変更していません。**

### Fix

- `SaveDataContent` の保存日時が `SaveDataLoaderStrategy` の管理下から外れていた経路を塞ぎました（`UpdateSaveDate()` / `ClearSaveDate()` は `internal`）。
- Symphony Administrator の Save Data パネルが、未読み込みの型を選択しただけで保存先へI/Oを発生させないようにしました。未読み込みの場合はLoadの実行を促す表示に変わります。保存時は正本を確定させるため先にロードします。

## [3.0.0] - 2026-08-04
**3.0.0 の確定版です。** `3.0.0-preview.1`〜`preview.4` で積み上げた Awaitable 移行（Phase 5）を Scene Load、Service Locate、Debug HUD、Component、Editor UI、Loader Strategy まで広げ、移行期間を終えた旧シムの削除と改名（Phase 6）を行いました。preview 版から更新する場合は、本項の Breaking をすべて確認してください。

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

## [3.0.0-preview.4] - 2026-08-04

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

## [3.0.0-preview.3] - 2026-08-04
### Fix
- **`PausableDestroy` と `PausableInvoke` の引数エラーが、呼び出し元へ同期的に伝わるようになりました。** 従来は `async void` だったため、`ArgumentNullException` や `ArgumentOutOfRangeException` が非同期メソッドの中で投げられ、呼び出し側の `try/catch` では捕まえられずUnityの未処理例外ハンドラへ流れていました。検証を同期部分へ移しています。

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

## [2.18.3] - 2026-08-04
### Fix
- **AudioMixerが未割り当ての場合に、空の `AudioManager` GameObject が `DontDestroyOnLoad` へ生成されていたのを修正した。** GameObjectの生成を最初のAudioSource生成まで遅らせたため、1つも作らない場合は生成されない。
- **公開パラメーターが見つからないオーディオグループで、音量変更時の警告が出ていなかったのを修正した。** 初期音量へ0を代入していたため判定が常に成立せず、存在しないパラメーター名で `SetFloat` が呼ばれていた（Unity側で黙って失敗する）。従来も音量は変わっておらず、観測される差は警告が出るようになる点のみ。

## [2.18.2] - 2026-08-04
### Change
- Audioの内部構造をレイヤーごとに分割した。Scene Load、Service Locate、Save Data、Pauseと同じ形へ揃えている。**公開APIのシグネチャ、例外の種類と条件はいずれも変更していない。**
  - `AudioGroupEntity`（Domain）— グループ名を同一性とし、音量割合からデシベル値への変換規則を持つ
  - `AudioGroupRegistry`（Application）— グループの保持と、構築処理を実行済みかどうかの記録
  - `AudioService`（Application）— Configの解釈、AudioSourceの遅延構築、AudioMixerへの反映
  - `IAudioSourceHost` / `AudioSourceHost`（Infrastructure）— AudioSourceを載せるGameObjectの所有
  - `AudioManager` は引数検証と転送だけを行い、状態を持たなくなった
- `AudioManager.cs` を `Runtime/System/Audio/` へ移した。名前空間は変更していないため、利用側の `using` に影響しない。
- 音量変換に使う最小音量 `-80` dB を `AudioGroupEntity.MINIMUM_VOLUME_DECIBEL` として名前付き定数にした。変換式は従来と恒等。

### Add
- `AudioGroupEntity` と `AudioGroupRegistry` のEditModeテストを14件追加した。音量変換の境界値と線形補間、初期音量が取得できない場合の扱い、構築済み記録が全消去で戻ることを検証する。
  - **全消去で構築済み記録が戻ること**を回帰テストにした。ここが戻らないとPlay Modeの2回目でAudioSourceが作られなくなる。

## [2.18.1] - 2026-08-03
### Fix
- **`PauseManager.Pause` に現在と同じ値を代入したとき、`OnPauseChanged` を発行しないようにした。** 従来は `Pause = true` を2回続けると `IPausable.Pause()` が2回呼ばれていた。**利用側への影響**: 同じ値での再通知に依存した実装は動作が変わる。状態変更の通知としては値が変わったときだけ発行するのが正しいため、修正として扱う。
- 管理パネルのPauseが、Play Mode外でも操作ボタンを受け付けて `SymphonyNotInitializedException` を出していたのを修正した。未初期化時はボタンを無効化する。

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

### Change
- **管理パネルからEditor更新ごとのpollingを完全に除去した。** Pauseが最後の1件で、`EditorApplication.update` の購読自体が無くなっている。各パネルは状態変更eventを購読する。
- 管理パネルのPauseがリフレクション（`typeof(PauseManager).GetField("_pause", ...)`）で内部状態を読むのをやめた。パッケージ内でinternal状態をリフレクションで読む箇所は無くなった。
- `PauseManager.cs` を `Runtime/System/Pause/` へ移した。名前空間は変更していないため、利用側の `using` に影響しない。
- internalアクセサ `PauseManager.IsPaused` と `PausableSubscriberCount` を削除した。MCP診断は公開Infoを使う。

## [2.17.2] - 2026-08-03

### Change
- ソースファイルの文字コードを規約へ揃えた。BOM が無かった17件の `.cs` へ UTF-8 BOM を付与している。**コードの挙動、公開API、シリアライズ形式はいずれも変更していない。** 各ファイルの差分は先頭行のみ。
  - BOM が無くてもコンパイルは通るため、日本語コメントを含むファイルが環境によってはシステム既定のコードページで解釈され、文字化けしうる状態だった。

## [2.17.1] - 2026-08-03
### Fix
- 管理パネルの State 表示が、キャッシュがあるだけの型を `Loaded` と表示していたのを修正した。

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

## [2.15.1] - 2026-08-03
### Fix
- Play Mode終了時にOrchestratorがService Locatorを解放した後で`SymphonyLocate.OnDisable()`が登録状態を照会し、`SymphonyNotInitializedException`を記録する問題を修正した。未初期化時は解除処理をスキップし、Domain Reload無効で2回往復して終了時エラーと登録残留がないことを確認した。

## [2.15.0] - 2026-08-03
### Add
- 登録キー、payload、登録方式を取得時点の不変値として返す公開`ServiceRegistrationInfo`を追加した。`ServiceLocator.GetRegistrationInfos()`で登録一覧を型名順に取得でき、`TryGetRegistrationInfo`で既知の型を点検索できる。
- `ServiceLocateQuery`の点検索、一覧順、表示名、スナップショット性と、`ServiceLocateViewModel`の初期値、状態変更通知、通知抑制、購読解除を検証するEditModeテストを追加した。

### Change
- Service Locatorの読み取り経路を内部`ServiceLocateQuery`へ集約し、公開API向け`ServiceRegistrationInfo`とView向け`ServiceLocateDto`を分離した。`ServiceLocator`はRegistry／Entityを直接読まず、状態照会をQueryへ転送する。
- `ServiceLocatorWindow`を`ServiceLocateViewModel`の読み取り専用ReactiveProperty購読へ変更した。Editor更新ごとの登録一覧pollingを廃止し、状態変更時だけ型名、payload名、登録方式を再描画する。Play Mode終了時に購読を解除するため、Domain Reload無効でも前回の表示状態を残さない。
- `SymphonyMcpTools.GetServiceLocatorJson()`を公開`ServiceRegistrationInfo`から生成するようにした。既存JSONフィールドを維持し、Componentでは記録された登録方式、Component以外では従来どおりLocatorを実効値として返す。

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

## [2.11.1] - 2026-07-31
### Fix
- `ReactiveProperty` のメインスレッド判定が `Awaitable` を毎回生成し、await せずに破棄していた問題を修正した。Unity の `Awaitable` はプールされた型で、await されないとプールへ返却されない。値の更新ごとに呼ばれる経路であり、後続の ViewModel が高頻度に更新するとプールを圧迫する。副作用のない Unity API 読み取りでスレッドを判定する形へ変更し、メインスレッド上ではアロケーションが発生しないようにした。

## [2.11.0] - 2026-07-31
### Add
- `ReactiveProperty<T>` の単体テストを21件追加した。等値比較（既定と custom comparer、配列が参照比較になること）、`notifyCurrent` の有無、購読解除と多重 Dispose、通知中に購読者が購読・解除した場合、購読者が例外を投げた場合の分離、破棄後の拒否、メインスレッド以外からの呼び出しの拒否を検証する。2.10.0 で追加した時点ではテストを持てなかったため、レビューだけが品質保証になっていた。

### Change
- **テストをパッケージ内へ戻した。** `Tests/Editor/` と `Tests/Runtime/` に配置し、パッケージへ同梱する。両 asmdef の `defineConstraints` に `UNITY_INCLUDE_TESTS` を指定しているため、**利用側のビルドには含まれない。**
- `Runtime/AssemblyInfo.cs` と `Core/AssemblyInfo.cs` がテストアセンブリへ `InternalsVisibleTo` を与えるようにした。これにより `internal` な内部実装も単体テストの対象にできる。従来は公開APIの範囲でしかテストできず、`ReactiveProperty` のような内部基盤を検証できなかった。

## [2.10.0] - 2026-07-31

### Add
- `ReactiveProperty<T>` と `IReadOnlyReactiveProperty<T>` を `Core` へ追加した。値が変化したときだけ購読者へ通知する最小の基盤で、後続バージョンで導入する ViewModel が表示状態を View と Editor Window へ伝えるために使う。現在 Editor Window は毎フレーム内部状態をポーリングしているが、これを変更時のみの反映へ移行するための土台になる。外部ライブラリ（R3 / UniRx）は導入していない。
  - どちらも `internal` のため、**利用側の公開APIは増えていない。**
  - 通知は購読者リストのスナップショットに対して行うため、通知中に購読者が購読・解除しても壊れない。
  - 1人の購読者が例外を投げても残りへの通知は続き、例外はまとめて1回記録する。
  - 値の更新・購読・破棄はメインスレッドに限定し、それ以外からの呼び出しは例外にする。
  - 配列や `List` は既定では参照比較になるため、内容比較用の comparer とスナップショットの用意は利用側の責務としている。

## [2.9.1] - 2026-07-31
### Fix
- **Play Mode 終了時に解放されていなかった状態を解放するようにした。** `PauseManager` の `IPausable` 購読辞書と `OnPauseChanged` の購読、`AudioManager` が生成した GameObject と AudioSource、`SymphonyDebugHUD` の描画コンポーネントは、これまで終了処理を持っておらず残り続けていた。Enter Play Mode Options で Domain Reload を無効にしている環境では、これがゴースト参照や二重購読の原因になる。**利用側への影響**: Play Mode を繰り返したときに、前回の購読が残って通知が多重に飛ぶ問題が解消される。

## [2.9.0] - 2026-07-31
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

## [2.5.1] - 2026-07-31
### Fix
- Runtimeコードから `UnityEditor` への参照を除去した。`ServiceLocator` と `ServiceLocateManager` が `EditorPrefs` を、`SymphonyDebugHUD` が `MenuItem` を、`SymphonyVisualElement` が `AssetDatabase` を参照しており、`#if UNITY_EDITOR` で囲まれていてもRuntimeアセンブリがEditor APIに依存していた。設定値とローダーはEditor側のCompositionから注入する形へ変更した。`LoadType.AssetDataBase` を含む公開APIは変更していない。

## [2.5.0] - 2026-07-31
### Add
- Framework配下のアセット移動に対する保護の強さを、`Project Settings > SymphonyFrameWork` から「有効化／警告／無効化」の3段階で選べるようにした。従来は一律で差し戻すか警告するかの2択で、切り替えもメニューに隠れていたため、テストフォルダの移設のように意図した移動を行いたい場面で扱いにくかった。「警告」では移動を続行するか元に戻すかをその場で選べる。1回の移動操作でダイアログは1回だけ表示する。

### Change
- Editor設定の保存先を `EditorPrefs` から `UserSettings/SymphonyFrameWork/` 配下の設定アセットへ移した。`EditorPrefs` はレジストリに入るため値の確認・リセット・削除ができず、設定の所在が不透明だった。**利用側への影響**: 既存の `EditorPrefs` の値は引き継がれず、アセット保護モードとService Locatorのログ設定はいずれも既定値から始まる。`UserSettings/` は `.gitignore` 対象のため、従来どおり開発者ごとの設定として扱われる。
- `Tools/SymphonyFrameWork/Settings/Symphony Asset Lock` メニューを廃止し、`Project Settings > SymphonyFrameWork` へ統合した。
- アセット移動の判定を `OnPostprocessAllAssets` から `OnWillMoveAsset` へ変更した。従来は一度移動させてから `AssetDatabase.MoveAsset` で戻していたが、戻り値で移動そのものを止められるため、余計な移動と `AssetDatabase.Refresh()` が不要になった。あわせて判定を前方一致にし、パスに `SymphonyFrameWork` を含むだけの無関係なアセットを誤検知しないようにした。
- `SymphonyDebugHUD.Show()` / `Hide()` の `[MenuItem]` をEditor側の型へ移した。メニューのパスと両メソッドのシグネチャは変更していない。

## [2.4.4] - 2026-07-31

### Change
- 本体開発向けのドキュメント（`Documentation~/CONTRIBUTING.md`、`CodeGuidelines.md`、`DesignPhilosophy.md`）を、開発用ワークスペースリポジトリ [SymphonyWorkspace](https://github.com/HIBIKI5201/SymphonyWorkspace) の `Documentation/` へ移設。本パッケージはそのワークスペースへ submodule として組み込まれて開発されるようになり、Unityプロジェクト・検証環境・開発手順はワークスペース側が持つ責務になったため。このリポジトリには**パッケージ利用者向けの内容だけ**を残し、`Documentation~/` は削除した。本体開発ドキュメントの所在は README の「ドキュメント」節が案内する。
- `AGENTS.md` から「本体を変更する立場か、利用する立場か」を読者に判定させる前置きと、本体開発者向けの注意書きを削除。本ファイルは利用者向けの内容のみになった。記載しているAPI・作法・検証手順に変更はない。
- `README.md` の「ドキュメント」節から設計思想・コーディングガイドライン・本体開発ガイドへのリンクを削除し、ワークスペースリポジトリへの案内に置き換え。あわせて `package.json` と乖離していたバージョン表記を修正した。

## [2.4.3] - 2026-07-31

### Change
- `SymphonyOrchestrator` の管理オブジェクトを、実行時に生成する専用の `SymphonySystem` シーンからUnity標準の `DontDestroyOnLoad` に変更。専用シーンの生成・待機とScene Loader側の除外処理が不要になり、`LoadSceneMode.Single` 相当の遷移でもフレームワークのランタイム状態をUnity標準の永続化機構で保持する。公開API、設定アセット、セーブデータ形式の変更はない。

## [2.4.2] - 2026-07-30

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

## [2.4.1] - 2026-07-30
### Fix
- 初期化前の `ServiceLocator`、`SceneLoader`、`AudioManager`、`PauseManager` 呼び出しが実装詳細の `NullReferenceException` になる問題を修正し、`SymphonyNotInitializedException` へ統一。
- Service Locatorの非同期待機がタイムアウトまたはキャンセルされた後も登録待ちコールバックを保持していた問題を修正。
- Domain Reload無効時にPause Managerの登録辞書だけが残り、再生し直した際に通知を再購読できない問題を修正。

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

## [2.3.0] - 2026-07-30

2.2.0で導入した `SymphonyFrameWork.System.API` 名前空間を撤回し、公開Facadeクラスをそれぞれのサブシステムの名前空間へ戻しました。Facadeとその引数・戻り値のValue Object（`LocateType`、`SceneLoadState`）が別々の名前空間に分かれ、1つのサブシステムを使うだけで `using` が2つ必要になっていた状態を解消するためです。公開範囲の区別は名前空間ではなくフォルダ（`Internal/`）で表す方針へ変更しました。

Facadeの名前空間が2.1.0以前へ戻るだけで、クラス名・メンバー・シグネチャ・シリアライズ形式は一切変わりません。2.1.0以前の書き方（`SymphonyFrameWork.System.ServiceLocate` などを直接使うコード）はそのまま動き、`using` の修正が必要なのは2.2.0〜2.2.2の3バージョンだけに存在した `SymphonyFrameWork.System.API` を使ったコードに限られます。この影響範囲の狭さから、メジャーではなくマイナー更新として扱います。

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

## [2.2.2] - 2026-07-29
### Fix
- `SymphonyDebugHUD.Initialize()` が、破棄済みのHUDに対して `MissingReferenceException` を投げていた問題を修正。Domain Reloadを無効にした状態で再生を繰り返すと、前回の再生で生成した `Lazy` がstaticに残り、`IsValueCreated` はtrueのままHUDのGameObjectだけが破棄済みになるため、`Destroy` へ渡す `gameObject` の取得で例外になっていた。

## [2.2.1] - 2026-07-29
### Add
- `Samples/Runtime/DebuggerSample`: `SymphonyDebugLogger`（`LogDirect`の重要度別出力、`AddText`／`NewText`／`LogText`による複数行ログの蓄積と一括出力、`LogAndCheckComponentNull`）、`SymphonyDebugHUD`（`Show`／`Hide`、`AddText(Func<string>)`と`RemoveText`の対、時間指定の一時表示）、`SymphonyStopWatch`（`Start`／`Stop`による計測、未開始IDの警告）をPlayモードで確認できるサンプルシーンとスクリプト。`package.json`の`samples`へ`Debugger Sample`として追加。

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

## [1.20.20] - 2025-05-14

### Update
- SceneLoader

## [1.20.19] - 2025-05-06

### Fix
- AssemblyGenerator
- EnumGenerator

## [1.20.18] - 2025-05-05

### Update
- PauseManager

## [1.20.17] - 2025-05-02

### Fix
- PackageInitializer

## [1.20.16] - 2025-04-29

### Update
- PackageInitializer
- SymphonyConfigManager

## [1.20.15] - 2025-04-05

### Fix
- AutoEnumGeneratorConfig
- SymphonyAdministrator

## [1.20.14] - 2025-04-05

### Update
- SceneLoader

## [1.20.13] - 2025-04-05

### Fix
- PackageInitializer

## [1.20.12] - 2025-04-05

### Update
- AudioManager

## [1.20.11] - 2025-04-05

### Fix
- AutoEnumGenerator

## [1.20.10] - 2025-04-05

### Fix
- PackageInitializer

## [1.20.9] - 2025-03-12

### Update
- SymphonyAdministrator

## [1.20.8] - 2025-03-12

### Fix
- EnumGenerator

## [1.20.7] - 2025-03-12

### Add
- PackageInitializer

## [1.20.6] - 2025-03-10

### Fix
- SymphonyConfigLocator
- SymphonyEditorConfigLocator

## [1.20.5] - 2025-03-10
### Fix
- EnumGenerator

## [1.20.4] - 2025-03-10
### Add
- AssemblyGenerator

## [1.20.3] - 2025-03-10

### Update
- SymphonyPackageLoader

## [1.20.2] - 2025-03-08

### Update
- FolderGenerator

## [1.20.1] - 2025-03-05
### Fix
- EnumGenerator

## [1.20.0] - 2025-03-05
### Update
- AutoEnumGenerator
- AutoEnumGeneratorConfig

## [1.19.21] - 2025-03-05

### Add
- TagsAndLayersPostProcessor

## [1.19.20] - 2025-03-05

### Update
- ServiceLocator

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
- FolderGenerator

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
- ReadOnlyAttribute

## [1.18.11] - 2025-02-24

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

## [1.16.11] - 2025-02-19
### Fix
- ServiceLocator

## [1.16.10] - 2025-02-19
### Add
- SymphonySingleton
### Update
- ServiceLocator

## [1.16.9] - 2025-02-18

### Update
- PauseManager

## [1.16.8] - 2025-02-18

### Update
- SymphonyVisualElement

## [1.16.7] - 2025-02-18

### Update
- PauseManager

## [1.16.6] - 2025-02-18

### Update
- ServiceLocator

## [1.16.5] - 2025-02-17
### Fix
- SymphonyVisualElement

## [1.16.4] - 2025-02-17
### Update
- ServiceLocator

## [1.16.3] - 2025-02-17

### Update
- SymphonyVisualElement

## [1.16.2] - 2025-02-17
### Fix
- ServiceLocator
- SceneLoader
- SaveDataSystem
- PauseManager

## [1.16.1] - 2025-02-17
### Update
- ServiceLocator

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

## [1.01.1] - 2025-02-13
### Fix
- SingletonDirector
- SymphonyTask

## [1.01.0] - 2025-02-13
### Update
- SymphonyStopWatch

## [1.00.0] - 2025-02-13

### Add
- SaveDataSystem
- SceneLoader
- SingletonDirector
- SymphonyDebugLog
- SymphonyStopWatch
- SymphonyTask
