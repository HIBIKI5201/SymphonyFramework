# Asset Store Tools Packager

Asset Store Tools配下のフォルダをパッケージ化し、別プロジェクトへの差分インポートを補助します。

## 入口

| 項目 | 内容 |
| --- | --- |
| namespace | `SymphonyFrameWork.Editor` |
| 主な公開型 | `AssetStoreToolsPackager` / `AssetStoreToolsPackagerData` / `AssetStoreToolsPackagePipeline` / `AssetStoreToolsPackageStepStrategy` |
| メニューパス | `Tools > SymphonyFrameWork > ExportAssetStoreToolsFolder` |
| 設定の保存先 | `ProjectSettings/Packages/symphonyframework/AssetStoreToolsPackagerData.asset` / `<Asset Store Tools Path>/PackagerConfig.json` |

## Editor機能

### Asset Store Tools Packager設定

Packagerが使う入出力パスと、パッケージへ何を詰めるかの設定です。

**入口**: `Project Settings > SymphonyFrameWork > Asset Store Tools Packager`

**項目**:

| 項目 | 内容 | 保存先 |
| --- | --- | --- |
| Asset Store Tools Path | パッケージ化の対象となるフォルダ。既定は `Assets/AssetStoreTools` | `ProjectSettings/Packages/symphonyframework/AssetStoreToolsPackagerData.asset` |
| Exported Packages Path | 出力先フォルダ。既定は `ExportedPackages` | 同上 |
| Export Pipelines | Packagerウィンドウで選べる出力パイプライン | 同上 |
| Ignored Directories | パッケージ対象から除外するフォルダ名 | `<Asset Store Tools Path>/PackagerConfig.json` |
| Force Include Extensions | 依存関係に載らなくても必ず含める拡張子 | 同上 |

**設定の分担**: 「どこにあるか」（パス）と「どう出すか」（パイプライン）がProject Settings、「何を詰めるか」（除外・強制包含）が `PackagerConfig.json` です。`PackagerConfig.json` は `Assets/` 配下にあるため、利用側のリポジトリで版管理し、手で編集できます。

**Export Pipelines**:

- `Create Default Pipeline` を押すと、`Singles → Used Dependencies → Create ZIP` のテンプレートを `Assets/Editor/SymphonyFrameWork/Configs/` へ生成してアサインします。**同名のアセットがある場合は上書きせず別名で作ります。**
- `Add` で空の要素を足し、既存のパイプラインアセットをアサインできます。
- **パイプラインは自動生成しません。** 利用側のリポジトリへ意図しないアセットを増やさないためで、Runtimeの設定アセットが自動生成されるのは Framework の動作に必須だからです。パイプラインは「使う人が構成するもの」です。
- パス項目と同じく、**変更した時点で保存します。** `PackagerConfig.json` のような `Save` / `Reload` はありません。

**注意点**:

- `Asset Store Tools Path` を変更しても `PackagerConfig.json` は自動で読み直されません。`Reload` ボタンで明示的に読み込んでください。入力途中のパスごとに設定ファイルを生成しないための仕様です。
- `Force Include Extensions` の既定値には `.cs` / `.asmdef` / `.asmref` と、ネイティブプラグインのバイナリ（`.dll` / `.so` / `.a` / `.dylib` / `.aar` / `.bundle` / `.framework` / `.jslib`）が入っています。`.asmdef` と `.asmref` はUnityの依存関係グラフに載らないため、ここに無いとパッケージから落ちます。

### Asset Store Tools Packager

`Asset Store Tools Path` 配下の各フォルダを `.unitypackage` として出力します。Asset Store由来のアセットを別プロジェクトへ持ち込むためのツールです。

**入口**: `Tools > SymphonyFrameWork > ExportAssetStoreToolsFolder`

ウィンドウは **Export** と **Import** の2タブです。Export で `.unitypackage` を出力し、Import で別プロジェクトの出力物のうち更新されたものだけを取り込みます。

#### Export タブ

**操作**:

1. 出力するフォルダをチェックボックスで選ぶ。`PackagerConfig.json` の `Ignored Directories` に入っているフォルダは `(Ignored)` と表示され、選択できません
2. `Export Pipeline` で出力手順を選ぶ。選択肢は Project Settings の `Export Pipelines` にアサインしたアセットで、表示名はアセット名です
3. `Export Selected Directories` を押すと、**出力せずに確認ウィンドウが開きます。** パイプライン名、手順の並び、パッケージへ含まれるアセットの階層が表示されます
4. `Export` で実行、`Cancel` で中止

**出力先**: `<Exported Packages Path>/Export_AssetStoreToolsPackage_<日時>/`

出力が完了すると、Console の完了ログに従来の相対パスがリンクとして表示されます。リンクをクリックすると、OS のファイルブラウザーで出力先フォルダを開けます。

**注意点**:

- **パイプラインが1つもアサインされていない場合、Export ボタンは表示されません。** `Open Project Settings` から設定画面へ移動し、`Create Default Pipeline` を押してください。
- Export タブへ切り替えたとき、パイプラインの一覧を読み直します。ウィンドウを開いたまま Project Settings を変更することがあるためです。反映されない場合は `Refresh` を押してください。
- **3.6.0 までの `Export Mode` / `Create ZIP File` / `Used Dependencies` は 3.7.0 で無くなりました。** 同じ内容は `Create Default Pipeline` が生成するテンプレートで表現されます（→[非推奨APIと削除予定](../Deprecations.md)）。

#### 出力手順のパイプライン

3.6.0 から、出力手順を順序付きの Strategy 列として組み替えられます。

**入口**: `Assets > Create > SymphonyFrameWork > Asset Store Tools Package Pipeline`、または `Project Settings > SymphonyFrameWork > Asset Store Tools Packager` の `Create Default Pipeline`

作成したアセットの `Steps` へ、サブクラスセレクターで手順を並べます。ウィンドウから選べるようにするには、Project Settings の `Export Pipelines` へアサインします。

| 手順 | 段階 | 役割 |
| --- | --- | --- |
| `AssetStoreToolsUsedDependenciesStrategy` | `Plan` | 使用中アセットと強制包含拡張子だけへ絞る |
| `AssetStoreToolsSinglePackageStrategy` | `Execute` | ディレクトリごとに出力し、`PackageManifest.json` を書く |
| `AssetStoreToolsCombinePackageStrategy` | `Execute` | 全ディレクトリを1つへまとめる |
| `AssetStoreToolsCreateZipStrategy` | `Execute` | 出力フォルダをZIP化する |

**実行順**: パイプライン全体で「全手順の `Plan`」→「全手順の `Execute`」の順に走り、各段階の中では並び順どおりに実行されます。**絞り込みは並びのどこにあっても出力より先に走ります。**

**注意点**:

- **`Execute` 段階の順序は利用者の責任です。** `AssetStoreToolsCreateZipStrategy` を出力より前へ置くと、空のフォルダを圧縮します。フレームワークは並べ替えません。確認ウィンドウが並び順をそのまま表示します。
- **`AssetStoreToolsCombinePackageStrategy` は差分インポートの対象になりません。** ディレクトリ単位で取り出せないためです。この手順だけのパイプラインでは `PackageManifest.json` が作られず、実行時に警告が出ます。
- **1つの手順が例外を投げても、記録して次の手順へ進みます。** 自作手順の失敗でフレームワーク側の出力まで止めないためです。
- コードから実行する場合は `AssetStoreToolsPackager.Export(string[], AssetStoreToolsPackagePipeline)` を呼びます。Project Settings へのアサインは不要です。

**自作の手順を追加する**: `AssetStoreToolsPackageStepStrategy` を継承し、`[Serializable]` を付けます。置き場は `SymphonyFrameWork.Editor` を参照する Editor 用 asmdef です。

```csharp
[Serializable]
public sealed class NotifyStrategy : AssetStoreToolsPackageStepStrategy
{
    public override string DisplayName => "Notify";

    // 別アセンブリからのオーバーライドは protected で宣言する。
    // protected internal のままではコンパイルエラー（CS0507）になる。
    protected override void Execute(AssetStoreToolsPackageExportContext context)
    {
        // context.ExportFullPath へ出力済み。
    }
}
```

出力対象を絞り込む手順は `Plan` をオーバーライドし、`plan.Entries` の各要素へ `FilterAssetPaths` を呼びます。**絞り込みしかできません。** 手順がアセットを追加できると、確認ウィンドウで提示した内容より多くのものが出力され得るためです。

#### Import タブ

出力済みパッケージのうち、**このプロジェクトで更新されたものだけ**を取り込みます。

**操作**:

1. `Select Folder` で、`PackageManifest.json` を含む取り込み元の出力済みフォルダを選ぶ。`Exported Packages Path` の外にコピーしたフォルダも指定できます
2. 一覧に各パッケージの状態とリビジョンの変化が出ます

    | 状態 | 意味 | 既定の選択 |
    | --- | --- | --- |
    | `New` | このプロジェクトへまだ導入されていない | 選択する |
    | `Updated` | 導入済みだが、出力側の方が新しい | 選択する |
    | `UpToDate` | リビジョンが一致している | 選択しない |
    | `Newer` | ローカルの方が新しい | **選択しない** |

3. `Select Updated` で `New` と `Updated` だけを選び直せます。`Deselect All` で全解除します
4. `Import Selected Packages` で選択したものを順に取り込みます

**判定のしかた**: 出力先フォルダの `PackageManifest.json` にあるリビジョンと、ローカルの `<Asset Store Tools Path>/<ディレクトリ>/ExportedVersion.json` のリビジョンを突き合わせます。ローカルにファイルが無ければ未導入（`New`）です。

**注意点**:

- **`Newer` は既定で選択しません。** ローカルの方が新しいものを取り込むと、意図しない巻き戻しになるためです。必要な場合は手で選んでください。
- `Exported Packages Path` はフォルダ選択画面の初期位置に使います。取り込み元はその配下に限定されません。
- `PackageManifest.json` が無い出力済みフォルダは取り込めません。統合パッケージ（`Combine`）だけで出力した場合がこれにあたります。
- 取り込むと、パッケージ同梱の `ExportedVersion.json` が更新され、次回の比較へ反映されます。

#### バージョンログ

どのパッケージが前回から変わったのかを判定するために、リビジョン番号を記録します。

| ファイル | 置き場 | 意味 | 誰が書くか |
| --- | --- | --- | --- |
| `PackageVersions.json` | `Asset Store Tools Path` 直下 | **このプロジェクトでの**各ディレクトリの現在リビジョン | ディレクトリ配下の変更を検知して自動で加算 |
| `ExportedVersion.json` | 各ディレクトリ直下 | **そのパッケージを出力した時点の**リビジョン | パッケージ出力時に生成し、パッケージへ同梱する |
| `PackageManifest.json` | 出力先フォルダ | その回に出力したパッケージ名とリビジョンの一覧 | パッケージ出力時に生成する |

2種類のログは意味が違います。`PackageVersions.json` は「編集の履歴」、`ExportedVersion.json` は「出荷時点のスナップショット」です。`ExportedVersion.json` はパッケージへ同梱されるため、**インポート先のプロジェクトでは「今そこに入っているパッケージのリビジョン」**を表します。これと `PackageManifest.json` を突き合わせれば、インポートが必要なパッケージだけを判別できます。

**リビジョンの加算**: `AssetStoreToolsVersionPostProcessor`（`AssetPostprocessor`）が対象ディレクトリ配下の変更を検知し、`SymphonyEditorOrchestrator` へ通知します。実際の書き出しはOrchestratorがまとめて1回だけ行います。

**注意点**:

- **`PackageVersions.json` と `ExportedVersion.json` は版管理へ含めてください。** インポート先での比較に使います。
- リビジョンは「実際には変わっていないのに増える」ことがあります。Unityが再インポートしただけでも加算されるためです。これは**不要なインポートが1回余分に起きる方向**の誤りで、「変わったのに増えない」逆方向の誤りは起きません。
- **統合パッケージ（`Export Mode = Combine`）は差分インポートの対象になりません。** ディレクトリ単位で取り出せないためです。`Combine` だけで出力すると `PackageManifest.json` は作られず、Consoleへ警告が出ます。**`Combine` は廃止予定です**（3.4.0で非推奨、次のメジャー更新で削除）。`Singles` を使用してください。
- リビジョンを手で編集する場合、負の値は0として扱われます。

#### 出力全般の注意点

- 確認ウィンドウで「（対象アセットなし）」と表示されたフォルダは、出力時に警告になります。`Used Dependencies` を有効にしていて、そのフォルダのアセットがプロジェクト内で1つも使われていない場合に起きます。
- `Used Dependencies` は `AssetDatabase.GetDependencies` で依存関係を追います。この依存関係グラフに載らないファイル（`.asmdef` など）は `Force Include Extensions` で補います。
- `.bundle` と `.framework` はUnityが単一アセットとして扱うため、フォルダに見えても中身のファイルではなくフォルダごと出力されます。

## 関連

- [Inspector属性](./InspectorAttributes.md)
- [EditorTools.md](../EditorTools.md)
- [Deprecations.md](../Deprecations.md)
- [CHANGELOG.md](../../CHANGELOG.md)
