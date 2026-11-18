# AIエージェント向けAPI索引

この文書は、Symphony Frameworkを導入したプロジェクトの`Assets/`配下へ利用コードを書くAIエージェントが、**目的のnamespaceと入口の型から読むべきモジュール文書を引く**ための索引です。

APIの説明、コード例、実装時に誤りやすい判断は各モジュール文書を正本とします。必要なモジュール文書だけを読んでください。

- 常時守るルールは[AGENTS.md](../AGENTS.md)の`## 1. 常に守ること`にあります。この文書には複製しません。
- パッケージ本体を開発する場合は、[SymphonyWorkspaceのCONTRIBUTING.md](https://github.com/HIBIKI5201/SymphonyWorkspace/blob/dev/Documentation/CONTRIBUTING.md)に従います。

## APIの参照先

| 機能 | namespace | 主な入口 | モジュール文書 |
| --- | --- | --- | --- |
| Partitura（Partitura） | `SymphonyFramework.Services` | `Partitura` | [Partitura](./Modules/Partitura.md) |
| Scene Loader | `SymphonyFramework.System.SceneLoad` | `SceneLoader` | [Scene Loader](./Modules/SceneLoader.md) |
| Save Data System | `SymphonyFramework.System.SaveSystem` | `SaveStore`, `SaveDataContent`, `SaveDataLoaderStrategy` | [Save Data System](./Modules/SaveDataSystem.md) |
| Audio Manager | `SymphonyFramework.System` | `AudioManager` | [Audio Manager](./Modules/AudioManager.md) |
| Pause Manager | `SymphonyFramework.System` | `PauseManager` | [Pause Manager](./Modules/PauseManager.md) |

## 非推奨APIと移行

非推奨APIの移行先と削除予定は[Deprecations.md](./Deprecations.md)を正本とします。非推奨化した経緯と時期は[CHANGELOG.md](../CHANGELOG.md)を参照してください。古いコードを修正するときは、現在のバージョンのソースとこの2つを確認し、過去バージョンのnamespaceやAPI一覧をこの文書へ固定しないでください。
