# Project Structure Tools

プロジェクトのフォルダ構成、Assembly Definition、推奨Unityパッケージの導入を補助します。

## 目的と範囲

新しいプロジェクトを始めるときの雛形づくりを、手作業なしで済ませられるようにします。

- **すること**: 定義ファイルに従ったフォルダの生成、asmdefの生成とGUID参照の追加、推奨パッケージの不足の確認と導入
- **しないこと**: 既存のフォルダやasmdefの上書き、利用者の確認を経ないパッケージの導入

## 入口

| 項目 | 内容 |
| --- | --- |
| namespace | `SymphonyFramework.Editor` |
| 主な公開型 | `FolderGenerator` / `AssemblyGenerator` / `AssemblyDefinitionData` / `SymphonyPackageLoader` |
| メニューパス | `Tools > SymphonyFramework > FolderGenerator` / `SymphonyPackageLoader` |
| 設定の保存先 | `FolderStructure.md` / `PackageList.txt`。Assembly Definitionは指定パスへ生成 |

## 機能一覧

| ID | 機能 | 入口 | 結果 |
| --- | --- | --- | --- |
| PST-01 | フォルダの生成 | `Tools > SymphonyFramework > FolderGenerator` | `FolderStructure.md` の構成を `Assets/` に作る。既存のフォルダは作り直さない |
| PST-02 | asmdefの生成 | 他のEditor機能から呼ばれる（メニューなし） | 無いasmdefだけを作り、参照を重複なく足す |
| PST-03 | 推奨パッケージの導入 | `Tools > SymphonyFramework > SymphonyPackageLoader` | 不足分をダイアログで確かめてから導入する |

## Editor機能

### FolderGenerator

Markdownで定義したフォルダ構成を `Assets/` 配下へ生成します。プロジェクト開始時の雛形づくりに使います。

**入口**: `Tools > SymphonyFramework > FolderGenerator`

**定義ファイル**: `<Frameworkルート>/Editor/Generator/FolderGenerate/FolderStructure.md`

既に存在するフォルダは作り直しません。生成したフォルダはダイアログとConsoleに一覧で出ます。

### AssemblyGenerator

Assembly Definition（`.asmdef`）の生成と、既存asmdefへのGUID参照追加を行います。

**入口**: メニューはありません。Frameworkの初期設定など、他のEditor機能から呼ばれます。

**注意点**:

- 既に存在する `.asmdef` は上書きしません。
- 参照の追加はGUID重複を避けます。同じ参照を二重に追加しません。

### SymphonyPackageLoader

Frameworkが推奨するUnity公式パッケージの導入状況を確認し、不足分の導入をダイアログで確認します。

**入口**: `Tools > SymphonyFramework > SymphonyPackageLoader`

**対象パッケージの定義**: `<Frameworkルート>/Editor/PackageLoader/PackageList.txt`（1行1パッケージ）

**注意点**:

- Package Managerの初期化が終わっていない場合は何もしません。
- 導入は利用者の確認を経てから実行されます。無断でインストールしません。

## 確かめ方

- FolderGeneratorの実行後、ダイアログとConsoleに出た一覧のフォルダが `Assets/` にあることを見る。
- SymphonyPackageLoaderの実行後、Package Managerで対象パッケージが導入済みになっていることを見る。

## 関連

- [AutoEnumGenerator](./AutoEnumGenerator.md)
- [EditorTools.md](../EditorTools.md)
- [Architecture.md](../Architecture.md)
- [CHANGELOG.md](../../CHANGELOG.md)
