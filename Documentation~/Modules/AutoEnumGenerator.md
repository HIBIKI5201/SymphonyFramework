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

**注意点**:

- **生成されたenumを手で編集しないでください。** 再生成で失われます。
- 利用側のasmdefから生成enumを直接使う場合は、`SymphonyFrameWork.Enum` を参照に追加してください。
- `SymphonyFrameWork.asmdef` から `SymphonyFrameWork.Enum` への参照は、Editor起動時に自動で注入されます。

## 関連

- [Project Structure Tools](./ProjectStructureTools.md)
- [EditorTools.md](../EditorTools.md)
- [Architecture.md](../Architecture.md)
- [CHANGELOG.md](../../CHANGELOG.md)
