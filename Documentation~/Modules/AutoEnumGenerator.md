# AutoEnumGenerator

Build Settingsのシーン一覧、タグ、レイヤー、Audio Groupからenumを自動生成します。

## 入口

| 項目 | 内容 |
| --- | --- |
| namespace | `SymphonyFrameWork.Editor` |
| 主な公開型 | `AutoEnumGenerator` / `EnumGenerator` / `AutoEnumGeneratorConfig` |
| メニューパス | 自動実行。手動生成は `Window > SymphonyFrameWork > Symphony Administrator` の `Auto Enum Generator` パネル |
| 設定の保存先 | `ProjectSettings/Packages/symphonyframework/AutoEnumGeneratorConfig.asset` |

## Editor機能

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

**補助メニュー**: `Tools > SymphonyFrameWork > Debug > CreateResourcesFolder` は、生成先の `Resources` フォルダを手動で作り直すためのものです。通常は自動生成に任せてください。

**列挙子名の解決**: シーン名・タグ名・グループ名は、そのままではC#の識別子にできないことがあります。生成前に次のように解決します。

| 候補 | 生成される列挙子 | 備考 |
| --- | --- | --- |
| `Title` | `Title` | そのまま使えます |
| `class`、`int` など**C#の予約語** | `@class`、`@int` | `@` を前置してエスケープします。**名前は消えません。**利用側は `SceneListEnum.@class` と書きます |
| `var`、`value`、`record` など文脈キーワード | `var`、`value`、`record` | 識別子として使えるため、そのままです |
| `1st Scene` のように識別子にできない文字列 | 生成されません | 警告を出して除外します |
| `value__` | 生成されません | コンパイラがenumの値の格納に使う名前で、`@` を付けても列挙子にできません |

解決後に同じ名前になる候補（`class` と `@class` など）は1つの列挙子にまとめ、最初に現れた並び順を保ちます。

**注意点**:

- **生成されたenumを手で編集しないでください。** 再生成で失われます。
- **予約語を含む候補は、除外ではなくエスケープされます。** シーンを追加しただけで `SceneListEnum` からそのシーンが消える事態を避けるためです。
- 利用側のasmdefから生成enumを直接使う場合は、`SymphonyFrameWork.Enum` を参照に追加してください。
- `SymphonyFrameWork.asmdef` から `SymphonyFrameWork.Enum` への参照は、Editor起動時に自動で注入されます。

## 関連

- [Project Structure Tools](./ProjectStructureTools.md)
- [EditorTools.md](../EditorTools.md)
- [Architecture.md](../Architecture.md)
- [CHANGELOG.md](../../CHANGELOG.md)
