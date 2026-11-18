# AutoEnumGenerator

Build Settingsのシーン一覧、タグ、レイヤー、Audio Groupからenumを自動生成します。

## 目的と範囲

シーン名・タグ名・レイヤー・Audio Groupを文字列で書かずに済むようにし、綴りの誤りをコンパイル時に見つけられるようにします。

- **すること**: 変更を検知したenumの再生成、C#の識別子にできない名前の解決、生成先アセンブリの用意と参照の注入
- **しないこと**: 生成したenumの手での編集の保持（再生成で上書きする）、ポーズカテゴリーの生成（[Pause Manager](./PauseManager.md)）

## 入口

| 項目 | 内容 |
| --- | --- |
| namespace | `SymphonyFramework.Editor` |
| 主な公開型 | `AutoEnumGenerator` / `EnumGenerator` / `AutoEnumGeneratorConfig` |
| メニューパス | 自動実行。手動生成は `Window > SymphonyFramework > Symphony Administrator` の `Auto Enum Generator` パネル |
| 設定の保存先 | `ProjectSettings/Packages/symphonyframework/AutoEnumGeneratorConfig.asset` |

## 仕組みの要点

1. **元の設定が変わると、自動で再生成します。** シーン・タグ・レイヤーはそれぞれ設定で自動更新を止められます。
2. **シーンのenumの値はBuild Settingsの登録順です。** シーンの並びを変えると値も変わります。
3. **予約語は除外せず`@`を付けて残します。** シーンを足しただけでenumからシーンが消えないようにするためです。

## 機能一覧

| ID | 生成物 | 生成元 |
| --- | --- | --- |
| ENUM-01 | `SceneListEnum.cs` | Build Settingsのシーン一覧（**登録順が値になる**） |
| ENUM-02 | `TagsEnum.cs` | `ProjectSettings/TagManager.asset` のタグ |
| ENUM-03 | `LayersEnum.cs` | 同上のレイヤー（フラグenum） |
| ENUM-04 | `AudioGroupTypeEnum.cs` | `AudioConfig` のグループ名 |
| ENUM-05 | `SymphonyFramework.Enum.asmdef` | 上記を含むアセンブリ |

生成先は `Assets/Scripts/SymphonyFramework/` です。

## Editor機能

**入口**: 自動実行。手動生成はSymphony Administratorの `Auto Enum Generator` パネルから

**設定**: `ProjectSettings/Packages/symphonyframework/AutoEnumGeneratorConfig.asset`。シーン・タグ・レイヤーそれぞれについて自動更新の有効・無効を切り替えられます。

**変更の検知**: タグ・レイヤーの設定ファイルとBuild Settingsのシーン一覧の変更を検知し、続けて起きた変更はまとめて1回だけ生成します。

**補助メニュー**: `Tools > SymphonyFramework > Debug > CreateResourcesFolder` は、生成先の `Resources` フォルダを手動で作り直すためのものです。通常は自動生成に任せてください。

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
- 利用側のasmdefから生成enumを直接使う場合は、`SymphonyFramework.Enum` を参照に追加してください。
- `SymphonyFramework.asmdef` から `SymphonyFramework.Enum` への参照は、Editor起動時に自動で注入されます。

## 確かめ方

- Build Settingsへシーンを足した後、`SceneListEnum.cs` に列挙子が増えていることを見る。
- 増えない場合は、設定で自動更新が有効か、Consoleに除外の警告が出ていないかを見る。手動で生成し直すときはSymphony Administratorの `Auto Enum Generator` パネルを使う。

## 関連

- [Project Structure Tools](./ProjectStructureTools.md)
- [EditorTools.md](../EditorTools.md)
- [Architecture.md](../Architecture.md)
- [CHANGELOG.md](../../CHANGELOG.md)
