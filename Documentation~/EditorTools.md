# Editor機能

Symphony Frameworkが提供するEditor拡張の索引です。モジュールごとの詳細は[Modules/](./Modules/)を参照してください。

Runtime APIの使い方は[Modules/](./Modules/)の各モジュール文書にあります。アセンブリ構成と初期化順は[Architecture.md](./Architecture.md)を参照してください。

Editor機能はGUI操作を入口とするため、この文書にコード例は載せません。

## 一覧

メニューの基準パスは `Tools/SymphonyFrameWork/`、ウィンドウは `Window/SymphonyFrameWork/`、Project Settingsは `SymphonyFrameWork` です。

| 機能 | 入口 |
| --- | --- |
| [メインツールバー拡張](#メインツールバー拡張) | Main Toolbar の音符アイコン付き`Symphony Framework`プルダウン |
| [Symphony Administrator](#symphony-administrator) | `Window > SymphonyFrameWork > Symphony Administrator` |
| [ドキュメント表示](#ドキュメント表示) | `Window > SymphonyFrameWork > Documentation`、各Project Settings画面の `ドキュメントを開く` |
| [Framework設定](#framework設定) | `Project Settings > SymphonyFrameWork` |
| [Save System設定](./Modules/SaveDataSystem.md#editor機能) | `Project Settings > SymphonyFrameWork > Save System` |
| [Asset Store Tools Packager設定](./Modules/AssetStoreToolsPackager.md#asset-store-tools-packager設定) | `Project Settings > SymphonyFrameWork > Asset Store Tools Packager` |
| [Asset Store Tools Packager](./Modules/AssetStoreToolsPackager.md#asset-store-tools-packager) | `Tools > SymphonyFrameWork > ExportAssetStoreToolsFolder` |
| [出力手順のパイプライン](./Modules/AssetStoreToolsPackager.md#出力手順のパイプライン) | `Assets > Create > SymphonyFrameWork > Asset Store Tools Package Pipeline` |
| [AutoEnumGenerator](./Modules/AutoEnumGenerator.md) | 自動実行。手動生成はSymphony Administratorから |
| [FolderGenerator](./Modules/ProjectStructureTools.md#foldergenerator) | `Tools > SymphonyFrameWork > FolderGenerator` |
| [AssemblyGenerator](./Modules/ProjectStructureTools.md#assemblygenerator) | メニューなし。他のEditor機能から呼ばれる |
| [SymphonyPackageLoader](./Modules/ProjectStructureTools.md#symphonypackageloader) | `Tools > SymphonyFrameWork > SymphonyPackageLoader` |
| [SymphonyDebugHUD](./Modules/Debug.md#symphonydebughud) | `Shift + D + P`、`Tools > SymphonyFrameWork > SymphonyDebugHUD > Show` / `Hide` |
| [ログのファイル出力](./Modules/Debug.md#ログのファイル出力) | 自動実行 |
| [SymphonyMcpTools](./Modules/Debug.md#symphonymcptools) | メニューなし。外部ツールから呼ぶ |
| [アセット保護](#アセット保護) | 自動実行。強さは`Project Settings > SymphonyFrameWork` |
| [Inspector属性](./Modules/InspectorAttributes.md#editor機能) | 利用側のフィールドへ属性を付ける |
| [設定アセットの自動生成](#設定アセットの自動生成) | 自動実行 |
| [Editorの初期化](#editorの初期化) | 自動実行 |

### 設定ファイルの置き場

| ファイル | 内容 | 版管理 |
| --- | --- | --- |
| `ProjectSettings/Packages/symphonyframework/AutoEnumGeneratorConfig.asset` | enum自動生成の有効・無効 | 含める（プロジェクト共有） |
| `ProjectSettings/Packages/symphonyframework/AssetStoreToolsPackagerData.asset` | Packagerの入出力パス | 含める（プロジェクト共有） |
| `UserSettings/SymphonyFrameWork/SymphonyUserSettingConfig.asset` | アセット保護の強さ、Service Locatorのログ設定、Save DataのPlay Mode持ち越し | **含めない（開発者ごと）** |
| `Assets/Resources/SymphonyFrameWork/*.asset` | `SceneLoadConfig` / `AudioConfig` / `SaveDataConfig` / `DebugHUDConfig` | 含める |
| `<Asset Store Tools Path>/PackagerConfig.json` | Packagerの除外フォルダと強制包含拡張子 | 含める |
| `<Asset Store Tools Path>/PackageVersions.json` | ディレクトリごとの現在リビジョン | 含める |
| `<Asset Store Tools Path>/<ディレクトリ>/ExportedVersion.json` | そのパッケージを出力した時点のリビジョン | 含める |
| `<Frameworkルート>/Cache/Log.txt` | `SymphonyDebugLogger`の出力 | **含めない（生成物）** |

---

## メインツールバー拡張

Unity 6000.3以降の公式Main Toolbar APIを使用し、日常的なEditor操作を音符アイコン付き`Symphony Framework`プルダウンへまとめます。外部のToolbar拡張パッケージやUnity内部型への反射には依存しません。

| 項目 | 内容 |
| --- | --- |
| `Scene Init` | 次回のPlay Mode開始時に初期シーン処理を実行するか切り替える |

`Scene Init`は選択のたびにチェック状態を反転します。`Assets/Resources/SymphonyFrameWork/SceneLoadConfig.asset`の設定を変更するため、プロジェクト共有の差分になります。プルダウンを非表示にした場合は、メインツールバーのコンテキストメニューにある`Symphony Framework`グループから再表示できます。

---

## Symphony Administrator

Frameworkの各サブシステムの状態を1つのウィンドウで確認する管理パネルです。UI Toolkitで構成されています。

**入口**: `Window > SymphonyFrameWork > Symphony Administrator`

**パネル**:

| パネル | 内容 |
| --- | --- |
| Service Locate | 登録済みインスタンスの一覧と登録状態 |
| Scene Load | ロード済みシーンと進行中のロード |
| Save Data | 全対応型の一覧、Registry／Window専用インスタンスの接続状態、非接続編集とPlay Modeへの持ち越し |
| Pause | ポーズ状態の確認と切り替え |
| Debug HUD | 初期化・利用可否・表示状態・追加テキスト登録数の確認とShow / Hide |
| Auto Enum Generator | enumの手動生成ボタンと自動生成の有効・無効 |

各パネルの詳細は、対応する[モジュール文書](./Modules/)にあります。**各パネルの右上にある `ドキュメント` を押すと、そのモジュールの文書がブラウザで開きます。**

**注意点**:

- 6つのパネルは `SymphonyFrameWork.Editor` 名前空間の登録済みUXMLカスタム要素として構築されます。
- Runtimeの状態を表示するパネルは、Play Mode中にRegistryへ接続します。Save DataはEdit ModeでもWindow専用インスタンスを編集・保存できます。
- 表示はViewModelの変更通知で更新されます。ウィンドウを開いている間のポーリングは行いません。

---

## ドキュメント表示

パッケージへ同梱したドキュメントをブラウザで開きます。オフラインで読めます。

**入口**:

| 操作 | 開く文書 |
| --- | --- |
| `Window > SymphonyFrameWork > Documentation` | 索引（`Documentation~/Html/index.html`） |
| Symphony Administrator の各パネル右上の `ドキュメント` | そのパネルに対応するモジュール文書 |
| `Project Settings > SymphonyFrameWork` の `ドキュメントを開く` | この文書（Editor機能） |
| `Project Settings > SymphonyFrameWork > Save System` の `ドキュメントを開く` | [Save Data System](./Modules/SaveDataSystem.md) |
| `Project Settings > SymphonyFrameWork > Asset Store Tools Packager` の `ドキュメントを開く` | [Asset Store Tools Packager](./Modules/AssetStoreToolsPackager.md) |

**コードから開く**: `SymphonyFrameWork.Editor.SymphonyDocumentation.Open(SymphonyDocumentPageEnum)`。Editor専用の公開APIです。

```csharp
SymphonyDocumentation.Open(SymphonyDocumentPageEnum.SceneLoader);
```

**注意点**:

- **同梱HTMLが見つからない場合は、GitHub上の正本Markdownを開き、Consoleへ警告を出します。** 開けないことで作業を止めないためで、`Open` は例外を投げません。
- **フォールバック先は `main` ブランチです。** リポジトリにバージョンタグが無いため、導入バージョンで固定できません。
- HTMLは生成物です。内容の修正は `Documentation~/` のMarkdownへ行い、開発用リポジトリの `scripts/build_module_docs.py` で再生成してください。

---

## Framework設定

Framework全体の個人設定とプロジェクト共有設定です。

**入口**: `Project Settings > SymphonyFrameWork`

**項目**:

| 項目 | 内容 |
| --- | --- |
| Asset Protection Mode | Framework配下のアセット移動に対する保護の強さ。`Enabled` / `Warning` / `Disabled` |
| Service Locator Logs | 登録、取得、破棄のEditorログを開発者ごとに切り替える |
| Debug HUD Shortcut | EditorとDevelopment BuildでHUD表示を切り替えるInput Action。既定は`Shift + D + P` |

**保存先**:

| 設定 | ファイル |
| --- | --- |
| Asset Protection / Service Locator Logs | `UserSettings/SymphonyFrameWork/SymphonyUserSettingConfig.asset` |
| Debug HUD Shortcut | `Assets/Resources/SymphonyFrameWork/DebugHUDConfig.asset` |

**注意点**:

- `UserSettings/` は開発者ごとの設定です。**版管理へ含めないでください。** 他の開発者の保護モードやログ設定を上書きします。
- `DebugHUDConfig.asset` はプラットフォームごとのBindingをチームで共有する設定です。版管理へ含めてください。

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
- **Project Settingsの画面を開いただけでは生成されません。** 設定画面は未生成である旨と生成ボタンを表示するだけで、生成そのものは `SymphonyEditorOrchestrator` の入口を通ります。`AssetDatabase.Refresh` を1回へまとめる集約の外側でアセットが変わらないようにするためです。

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
- [Modules/](./Modules/) — モジュールごとのEditor機能と利用方法
- [Architecture.md](./Architecture.md) — アセンブリ構成、初期化順、公開型の関係
- [Deprecations.md](./Deprecations.md) — 非推奨APIと削除予定
- [CHANGELOG.md](../CHANGELOG.md) — 変更履歴
