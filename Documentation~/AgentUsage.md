# AIエージェント向け利用上の注意

この文書は、Symphony Frameworkを導入したプロジェクトの`Assets/`配下へ利用コードを書くAIエージェント向けです。APIの説明、コード例、実装時に誤りやすい判断は各モジュール文書を正本とします。

必要なモジュール文書だけを読んでください。パッケージ本体を開発する場合はこの文書ではなく、[SymphonyWorkspaceのCONTRIBUTING.md](https://github.com/HIBIKI5201/SymphonyWorkspace/blob/dev/Documentation/CONTRIBUTING.md)に従います。

## 共通の前提

- `Packages/manifest.json`またはパッケージの`package.json`で導入バージョンを確認し、移行情報が必要なら[CHANGELOG.md](../CHANGELOG.md)を読む。
- asmdefを使う利用側コードは`SymphonyFrameWork`を参照する。自動生成enumを直接使う場合だけ`SymphonyFrameWork.Enum`も参照する。
- `ServiceLocator`、`SceneLoader`、`SaveStore`、`AudioManager`、`PauseManager`はstatic Facadeである。`new`や`.Instance`は使わない。
- `SymphonyOrchestrator`が最初のシーンより前に自動初期化する。Bootstrap用GameObjectや専用シーンを作らない。
- `SceneLoadConfig`、`AudioConfig`、`SaveDataConfig`は`internal`である。型として参照せず、InspectorまたはProject Settingsから設定する。
- `SymphonyFrameWork.Editor`はEditor専用である。Runtime asmdefやPlayerビルド対象コードから参照しない。
- パッケージ直下の`Cache/Log.txt`はEditor用の生成キャッシュである。編集・コミットせず、不要なら削除してよい。

## APIの参照先

| 機能 | namespace | 主な入口 | モジュール文書 |
| --- | --- | --- | --- |
| Service Locator | `SymphonyFrameWork.System.ServiceLocate` | `ServiceLocator`, `ServiceInjector` | [Service Locator](./Modules/ServiceLocator.md) |
| Scene Loader | `SymphonyFrameWork.System.SceneLoad` | `SceneLoader` | [Scene Loader](./Modules/SceneLoader.md) |
| Save Data System | `SymphonyFrameWork.System.SaveSystem` | `SaveStore`, `SaveDataContent`, `SaveDataLoaderStrategy` | [Save Data System](./Modules/SaveDataSystem.md) |
| Audio Manager | `SymphonyFrameWork.System` | `AudioManager` | [Audio Manager](./Modules/AudioManager.md) |
| Pause Manager | `SymphonyFrameWork.System` | `PauseManager` | [Pause Manager](./Modules/PauseManager.md) |

## 非推奨APIと移行

非推奨APIの移行先と削除予定は[Deprecations.md](./Deprecations.md)を正本とします。非推奨化した経緯と時期は[CHANGELOG.md](../CHANGELOG.md)を参照してください。古いコードを修正するときは、現在のバージョンのソースとこの2つを確認し、過去バージョンのnamespaceやAPI一覧をこの文書へ固定しないでください。
