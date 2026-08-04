# Editor機能

Symphony Frameworkが提供するEditor拡張の正本です。何をするもので、どこから開き、設定がどこに保存されるかを記載します。

Runtime APIの使い方は[README.md](../README.md)のクイックスタートを参照してください。アセンブリ構成と初期化順は[Architecture.md](./Architecture.md)にあります。

Editor機能はGUI操作を入口とするため、この文書にコード例は載せません。

## 一覧

メニューの基準パスは `Tools/SymphonyFrameWork/`、ウィンドウは `Window/SymphonyFrameWork/`、Project Settingsは `SymphonyFrameWork` です。

| 機能 | 入口 |
| --- | --- |
| [Symphony Administrator](#symphony-administrator) | `Window > SymphonyFrameWork > Symphony Administrator` |
| [Framework設定](#framework設定) | `Project Settings > SymphonyFrameWork` |
| [Save System設定](#save-system設定) | `Project Settings > SymphonyFrameWork > Save System` |
| [Asset Store Tools Packager設定](#asset-store-tools-packager設定) | `Project Settings > SymphonyFrameWork > Asset Store Tools Packager` |
| [Asset Store Tools Packager](#asset-store-tools-packager) | `Tools > SymphonyFrameWork > ExportAssetStoreToolsFolder` |
| [AutoEnumGenerator](#autoenumgenerator) | 自動実行。手動生成はSymphony Administratorから |
| [FolderGenerator](#foldergenerator) | `Tools > SymphonyFrameWork > FolderGenerator` |
| [AssemblyGenerator](#assemblygenerator) | メニューなし。他のEditor機能から呼ばれる |
| [SymphonyPackageLoader](#symphonypackageloader) | `Tools > SymphonyFrameWork > SymphonyPackageLoader` |
| [SymphonyDebugHUD](#symphonydebughud) | `Tools > SymphonyFrameWork > SymphonyDebugHUD > Show` / `Hide` |
| [ログのファイル出力](#ログのファイル出力) | 自動実行 |
| [SymphonyMcpTools](#symphonymcptools) | メニューなし。外部ツールから呼ぶ |
| [アセット保護](#アセット保護) | 自動実行。強さは`Project Settings > SymphonyFrameWork` |
| [Inspector属性](#inspector属性) | 利用側のフィールドへ属性を付ける |
| [設定アセットの自動生成](#設定アセットの自動生成) | 自動実行 |
| [Editorの初期化](#editorの初期化) | 自動実行 |

### 設定ファイルの置き場

| ファイル | 内容 | 版管理 |
| --- | --- | --- |
| `ProjectSettings/Packages/symphonyframework/AutoEnumGeneratorConfig.asset` | enum自動生成の有効・無効 | 含める（プロジェクト共有） |
| `ProjectSettings/Packages/symphonyframework/AssetStoreToolsPackagerData.asset` | Packagerの入出力パス | 含める（プロジェクト共有） |
| `UserSettings/SymphonyFrameWork/SymphonyUserSettingConfig.asset` | アセット保護の強さ、Service Locatorのログ設定 | **含めない（開発者ごと）** |
| `Assets/Resources/SymphonyFrameWork/*.asset` | `SceneLoadConfig` / `AudioConfig` / `SaveDataConfig` | 含める |
| `<Asset Store Tools Path>/PackagerConfig.json` | Packagerの除外フォルダと強制包含拡張子 | 含める |
| `<Frameworkルート>/Cache/Log.txt` | `SymphonyDebugLogger`の出力 | **含めない（生成物）** |

---

## Symphony Administrator

Frameworkの各サブシステムの状態を1つのウィンドウで確認する管理パネルです。UI Toolkitで構成されています。

**入口**: `Window > SymphonyFrameWork > Symphony Administrator`

**パネル**:

| パネル | 内容 |
| --- | --- |
| Service Locate | 登録済みインスタンスの一覧と登録状態 |
| Scene Load | ロード済みシーンと進行中のロード |
| Save Data | セーブデータの登録内容 |
| Pause | ポーズ状態の確認と切り替え |
| Auto Enum Generator | enumの手動生成ボタンと自動生成の有効・無効 |

**注意点**:

- Runtimeの状態を表示するパネルは、Play Mode中のみ内容を持ちます。Edit Modeでは未接続状態を表示します。
- 表示はViewModelの変更通知で更新されます。ウィンドウを開いている間のポーリングは行いません。

---

## Framework設定

Framework全体の開発者ごとの設定です。

**入口**: `Project Settings > SymphonyFrameWork`

**項目**:

| 項目 | 内容 |
| --- | --- |
| Asset Protection Mode | Framework配下のアセット移動に対する保護の強さ。`Enabled` / `Warning` / `Disabled` |
| Service Locatorのログ設定 | インスタンスの登録ログ・取得ログをそれぞれ出力するか |

**保存先**: `UserSettings/SymphonyFrameWork/SymphonyUserSettingConfig.asset`

**注意点**:

- `UserSettings/` は開発者ごとの設定です。**版管理へ含めないでください。** 他の開発者の保護モードやログ設定を上書きします。

---

## Save System設定

`SaveStore` が使用するセーブローダーを選択します。

**入口**: `Project Settings > SymphonyFrameWork > Save System`

**項目**:

| 項目 | 内容 |
| --- | --- |
| Loader | `SaveDataLoaderStrategy` を継承したローダー。`[SerializeReference]` で選択する |
| Current Loader Type | 現在設定されているローダーの型名（読み取り専用） |

**保存先**: `Assets/Resources/SymphonyFrameWork/SaveDataConfig.asset`

**注意点**:

- ローダーが未設定の場合、`SaveStore` は既定の `JsonUtility` ローダーへフォールバックします。警告が表示されます。
- 独自ローダーは `SaveDataLoaderStrategy` を継承してください。共通の検証とデータ復旧は基底クラスが担当します。
- ローダーを変更すると、その場で `SaveStore` が読み直します。

---

## Asset Store Tools Packager設定

Packagerが使う入出力パスと、パッケージへ何を詰めるかの設定です。

**入口**: `Project Settings > SymphonyFrameWork > Asset Store Tools Packager`

**項目**:

| 項目 | 内容 | 保存先 |
| --- | --- | --- |
| Asset Store Tools Path | パッケージ化の対象となるフォルダ。既定は `Assets/AssetStoreTools` | `ProjectSettings/Packages/symphonyframework/AssetStoreToolsPackagerData.asset` |
| Exported Packages Path | 出力先フォルダ。既定は `ExportedPackages` | 同上 |
| Ignored Directories | パッケージ対象から除外するフォルダ名 | `<Asset Store Tools Path>/PackagerConfig.json` |
| Force Include Extensions | 依存関係に載らなくても必ず含める拡張子 | 同上 |

**設定の分担**: 「どこにあるか」（パス）がProject Settings、「何を詰めるか」（除外・強制包含）が `PackagerConfig.json` です。`PackagerConfig.json` は `Assets/` 配下にあるため、利用側のリポジトリで版管理し、手で編集できます。

**注意点**:

- `Asset Store Tools Path` を変更しても `PackagerConfig.json` は自動で読み直されません。`Reload` ボタンで明示的に読み込んでください。入力途中のパスごとに設定ファイルを生成しないための仕様です。
- `Force Include Extensions` の既定値には `.cs` / `.asmdef` / `.asmref` と、ネイティブプラグインのバイナリ（`.dll` / `.so` / `.a` / `.dylib` / `.aar` / `.bundle` / `.framework` / `.jslib`）が入っています。`.asmdef` と `.asmref` はUnityの依存関係グラフに載らないため、ここに無いとパッケージから落ちます。

---

## Asset Store Tools Packager

`Asset Store Tools Path` 配下の各フォルダを `.unitypackage` として出力します。Asset Store由来のアセットを別プロジェクトへ持ち込むためのツールです。

**入口**: `Tools > SymphonyFrameWork > ExportAssetStoreToolsFolder`

**操作**:

1. 出力するフォルダをチェックボックスで選ぶ。`PackagerConfig.json` の `Ignored Directories` に入っているフォルダは `(Ignored)` と表示され、選択できません
2. 出力形式を選ぶ

   | 項目 | 内容 |
   | --- | --- |
   | Export Mode `Singles` | 選んだフォルダを個別の `.unitypackage` として出力する |
   | Export Mode `Combine` | 選んだフォルダを1つの `.unitypackage` にまとめて出力する |
   | Create ZIP File | 出力フォルダ全体をZIP化する |
   | Used Dependencies | プロジェクト内で実際に使用しているアセットと、強制包含拡張子だけに絞る |

3. `Export Selected Directories` を押すと、**出力せずに確認ウィンドウが開きます。** パッケージへ含まれるアセットが階層表示されます
4. `Export` で実行、`Cancel` で中止

**出力先**: `<Exported Packages Path>/Export_AssetStoreToolsPackage_<日時>/`

**注意点**:

- 確認ウィンドウで「（対象アセットなし）」と表示されたフォルダは、出力時に警告になります。`Used Dependencies` を有効にしていて、そのフォルダのアセットがプロジェクト内で1つも使われていない場合に起きます。
- `Used Dependencies` は `AssetDatabase.GetDependencies` で依存関係を追います。この依存関係グラフに載らないファイル（`.asmdef` など）は `Force Include Extensions` で補います。
- `.bundle` と `.framework` はUnityが単一アセットとして扱うため、フォルダに見えても中身のファイルではなくフォルダごと出力されます。

---

## AutoEnumGenerator

Build Settingsのシーン一覧、タグ、レイヤー、Audio Groupの変更を検知して、対応するenumを自動生成します。

**入口**: 自動実行。手動生成はSymphony Administratorの `Auto Enum Generator` パネルから

**生成物**: `Assets/Scripts/SymphonyFrameWork/`

| ファイル | 生成元 |
| --- | --- |
| `SceneListEnum.cs` | Build Settingsのシーン一覧（**登録順が値になります**） |
| `TagsEnum.cs` | `ProjectSettings/TagManager.asset` のタグ |
| `LayersEnum.cs` | 同上のレイヤー（フラグenum） |
| `AudioGroupTypeEnum.cs` | `AudioConfig` のグループ名 |
| `SymphonyFrameWork.Enum.asmdef` | 上記を含むアセンブリ |

**設定**: `ProjectSettings/Packages/symphonyframework/AutoEnumGeneratorConfig.asset`。シーン・タグ・レイヤーそれぞれについて自動更新の有効・無効を切り替えられます。

**変更の検知**: `TagsAndLayersPostProcessor`（`AssetPostprocessor`）がタグ・レイヤー設定ファイルの変更を、`EditorBuildSettings.sceneListChanged` がシーン一覧の変更を検知します。検知した変更は `SymphonyEditorOrchestrator` へ通知され、まとめて1回処理されます。

**注意点**:

- **生成されたenumを手で編集しないでください。** 再生成で失われます。
- 利用側のasmdefから生成enumを直接使う場合は、`SymphonyFrameWork.Enum` を参照に追加してください。
- `SymphonyFrameWork.asmdef` から `SymphonyFrameWork.Enum` への参照は、Editor起動時に自動で注入されます。

---

## FolderGenerator

Markdownで定義したフォルダ構成を `Assets/` 配下へ生成します。プロジェクト開始時の雛形づくりに使います。

**入口**: `Tools > SymphonyFrameWork > FolderGenerator`

**定義ファイル**: `<Frameworkルート>/Editor/Generator/FolderGenerate/FolderStructure.md`

既に存在するフォルダは作り直しません。生成したフォルダはダイアログとConsoleに一覧で出ます。

---

## AssemblyGenerator

Assembly Definition（`.asmdef`）の生成と、既存asmdefへのGUID参照追加を行います。

**入口**: メニューはありません。`PackageInitializer` などの他のEditor機能から呼ばれます。

**注意点**:

- 既に存在する `.asmdef` は上書きしません。
- 参照の追加はGUID重複を避けます。同じ参照を二重に追加しません。

---

## SymphonyPackageLoader

Frameworkが推奨するUnity公式パッケージの導入状況を確認し、不足分の導入をダイアログで確認します。

**入口**: `Tools > SymphonyFrameWork > SymphonyPackageLoader`

**対象パッケージの定義**: `<Frameworkルート>/Editor/PackageLoader/PackageList.txt`（1行1パッケージ）

**注意点**:

- Package Managerの初期化が終わっていない場合は何もしません。
- 導入は利用者の確認を経てから実行されます。無断でインストールしません。

---

## SymphonyDebugHUD

FPS、メモリ使用量、任意テキストをGame Viewへ重ねて表示します。

**入口**: `Tools > SymphonyFrameWork > SymphonyDebugHUD > Show` / `Hide`

HUD本体はRuntimeの機能です。このメニューはEditorからの表示・非表示の入口だけを提供します。

---

## ログのファイル出力

`SymphonyDebugLogger` の出力をファイルへ書き出します。

**入口**: 自動実行（`SymphonyEditorOrchestrator` が起動時に開始します）

**出力先**: `<Frameworkルート>/Cache/Log.txt`

Frameworkの実配置パスを解決してその直下へ書くため、UPM経由（`Library/PackageCache/`）でもAssets直置きでも同じ位置関係になります。

**注意点**:

- `Cache/` は生成物です。**版管理へ含めないでください。**
- 書き込みは定期的にまとめて行われます。1行ごとにファイルを開きません。

---

## SymphonyMcpTools

MCPや自動化スクリプトから、SymphonyのRuntime状態をJSON文字列で読み取るためのツール群です。

**入口**: メニューはありません。外部ツールから `SymphonyFrameWork.Editor.Debugger.SymphonyMcpTools` の各メソッドを呼びます。

Service Locator、Scene Loader、Save Dataなどの状態を取得できます。戻り値は必ず有効なJSON文字列です。Play Mode外や未初期化の場合も、その旨を含むJSONを返します。

**注意点**:

- 読み取り専用です。Runtimeの状態を変更しません。
- 人が状態を確認する場合はSymphony Administratorを使ってください。

---

## アセット保護

`SymphonyAssetProtector`（`AssetPostprocessor`）が、Framework配下のアセット移動を検知して差し戻します。誤ってFramework本体を動かし、利用側プロジェクトのGUID参照を壊すことを防ぎます。

**入口**: 自動実行。強さは `Project Settings > SymphonyFrameWork` の `Asset Protection Mode`

| モード | 挙動 |
| --- | --- |
| `Enabled`（既定） | 移動を常に差し戻す。1回だけダイアログで通知する |
| `Warning` | ダイアログで、続行するか差し戻すかを選ばせる |
| `Disabled` | 移動を通し、Consoleへ通常ログを出す |

**注意点**:

- 意図してFramework配下のファイルを動かす場合は、`Asset Protection Mode` を `Warning` または `Disabled` にしてから操作してください。
- 保護の対象はFrameworkルート（`Packages/symphonyframework` または `Assets/SymphonyFrameWork`）とその配下です。

---

## Inspector属性

利用側のフィールドへ付けて、Inspectorの表示を変える属性です。属性の定義はRuntime、描画はEditorにあります。

| 属性 | 効果 |
| --- | --- |
| `[ReadOnly]` | Inspectorで編集できないようにする |
| `[DisplayText]` | フィールドの上に説明テキストを表示する |
| `[TagSelector]` | 文字列フィールドをタグのドロップダウンにする |
| `[SceneNameSelector]` | 文字列フィールドをシーン名のドロップダウンにする |
| `[SubclassSelector]` | `[SerializeReference]` フィールドで、派生型をドロップダウンから選べるようにする |

`[SubclassSelector]` は `[SerializeReference]` と組み合わせて使います。単独では効果がありません。

---

## 設定アセットの自動生成

`SymphonyConfigManager` が、Framework動作に必要な設定アセットの存在を保証します。

**入口**: 自動実行（`SymphonyEditorOrchestrator` の初期化から呼ばれます）

**対象**:

| 種別 | アセット | 置き場 |
| --- | --- | --- |
| Runtime（`ScriptableObject`） | `SceneLoadConfig` / `AudioConfig` / `SaveDataConfig` | `Assets/Resources/SymphonyFrameWork/` |
| Editor（`ScriptableSingleton`） | `AutoEnumGeneratorConfig` | `ProjectSettings/Packages/symphonyframework/` |
| Editor（`ScriptableSingleton`） | `SymphonyUserSettingConfig` | `UserSettings/SymphonyFrameWork/` |

**注意点**:

- **Runtime用のConfigは `internal` です。** コードから型として参照できません。InspectorとProject Settingsから設定してください。
- 生成された設定アセットを複製しないでください。Frameworkは決まった位置の1つだけを読みます。

---

## Editorの初期化

`SymphonyEditorOrchestrator` が、Unity EditorのホストライフサイクルをFrameworkの初期化・終了フェーズへ変換します。各Editorモジュールはこれ経由で開始・終了します。

詳細は[Architecture.md の「起動と終了」](./Architecture.md)を参照してください。

**注意点**:

- `AssetPostprocessor` は任意のタイミングで呼ばれるため、各PostProcessorは変更の種類を記録してOrchestratorへ通知するだけにしています。ファイル生成と `AssetDatabase.Refresh` はOrchestratorがまとめて1回だけ実行します。
- Editor起動時に複数のモジュールがアセットを変更しても、`AssetDatabase.Refresh` は最終段階で必要な場合に1回だけ走ります。

---

## 関連ドキュメント

- [README.md](../README.md) — 機能一覧、インストール、Runtime APIのクイックスタート
- [Architecture.md](./Architecture.md) — アセンブリ構成、初期化順、公開型の関係
- [Deprecations.md](./Deprecations.md) — 非推奨APIと削除予定
- [CHANGELOG.md](../CHANGELOG.md) — 変更履歴
