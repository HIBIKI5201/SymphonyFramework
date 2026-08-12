# Project Structure Tools

プロジェクトのフォルダ構成、Assembly Definition、推奨Unityパッケージの導入を補助します。

## 入口

| 項目 | 内容 |
| --- | --- |
| namespace | `SymphonyFrameWork.Editor` |
| 主な公開型 | `FolderGenerator` / `AssemblyGenerator` / `AssemblyDefinitionData` / `SymphonyPackageLoader` |
| メニューパス | `Tools > SymphonyFrameWork > FolderGenerator` / `SymphonyPackageLoader` |
| 設定の保存先 | `FolderStructure.md` / `PackageList.txt`。Assembly Definitionは指定パスへ生成 |

## Editor機能

### FolderGenerator

Markdownで定義したフォルダ構成を `Assets/` 配下へ生成します。プロジェクト開始時の雛形づくりに使います。

**入口**: `Tools > SymphonyFrameWork > FolderGenerator`

**定義ファイル**: `<Frameworkルート>/Editor/Generator/FolderGenerate/FolderStructure.md`

既に存在するフォルダは作り直しません。生成したフォルダはダイアログとConsoleに一覧で出ます。

### AssemblyGenerator

Assembly Definition（`.asmdef`）の生成と、既存asmdefへのGUID参照追加を行います。

**入口**: メニューはありません。`PackageInitializer` などの他のEditor機能から呼ばれます。

**注意点**:

- 既に存在する `.asmdef` は上書きしません。
- 参照の追加はGUID重複を避けます。同じ参照を二重に追加しません。

### SymphonyPackageLoader

Frameworkが推奨するUnity公式パッケージの導入状況を確認し、不足分の導入をダイアログで確認します。

**入口**: `Tools > SymphonyFrameWork > SymphonyPackageLoader`

**対象パッケージの定義**: `<Frameworkルート>/Editor/PackageLoader/PackageList.txt`（1行1パッケージ）

**注意点**:

- Package Managerの初期化が終わっていない場合は何もしません。
- 導入は利用者の確認を経てから実行されます。無断でインストールしません。

## 関連

- [AutoEnumGenerator](./AutoEnumGenerator.md)
- [EditorTools.md](../EditorTools.md)
- [Architecture.md](../Architecture.md)
- [CHANGELOG.md](../../CHANGELOG.md)
