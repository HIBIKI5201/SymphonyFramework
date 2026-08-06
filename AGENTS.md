# AGENTS.md — Symphony Framework 利用ガイド

このファイルは、Symphony Frameworkを導入したプロジェクトで作業するAIエージェント向けの**ドキュメント導線と常時ルール**です。API説明やコード例を重複させず、必要な文書の該当箇所だけを読めるようにしています。

パッケージは通常`Packages/symphonyframework/`、ソース導入時は`Assets/SymphonyFrameWork/`にあります。以下の相対リンクはどちらの配置でもパッケージルートから解決してください。

## 0. 作業内容ごとの参照先

| やること | 読むもの |
| --- | --- |
| 機能、インストール、初期設定を知る | [README.md](./README.md)の「主な機能」「インストール」「初期設定」 |
| Service Locator、Scene Loader、Save Data、Audio、Pauseを使う | [README.md](./README.md)の該当クイックスタート → [AgentUsage.md](./Documentation~/AgentUsage.md)の該当モジュール |
| アセンブリ、フォルダ、初期化順、公開型の関係を調べる | [Architecture.md](./Documentation~/Architecture.md) |
| Editor・デバッグ機能を使う | [EditorTools.md](./Documentation~/EditorTools.md)。索引だけなら[README.md `Editor・デバッグ支援`](./README.md#editorデバッグ支援) |
| 利用コードをコンパイル・Play Mode・ビルドで確認する | [AgentVerification.md](./Documentation~/AgentVerification.md) |
| 非推奨APIと移行方法、削除予定を調べる | [Deprecations.md](./Documentation~/Deprecations.md) |
| バージョン差分の経緯を調べる | [CHANGELOG.md](./CHANGELOG.md) |
| フレームワーク本体を開発・変更する | [SymphonyWorkspaceのCONTRIBUTING.md](https://github.com/HIBIKI5201/SymphonyWorkspace/blob/dev/Documentation/CONTRIBUTING.md) |

## 1. 常に守ること

1. `Packages/manifest.json`またはパッケージの`package.json`で、実際の導入バージョンを確認する。
2. 利用側コードだけを`Assets/`へ書き、`Packages/symphonyframework/`や`Assets/SymphonyFrameWork/`のパッケージ本体を直接編集しない。
3. 利用側asmdefから`SymphonyFrameWork`を参照する。自動生成enumを使う場合だけ`SymphonyFrameWork.Enum`も参照する。
4. `SceneLoadConfig`、`AudioConfig`、`SaveDataConfig`は`internal`である。InspectorまたはProject Settingsから設定し、型として参照しない。
5. 主要APIはstatic Facadeである。`new`や`.Instance`を使わない。
6. 初期化は`SymphonyOrchestrator`が自動実行する。Bootstrap用GameObjectや専用シーンを作らない。
7. Runtimeコードから`SymphonyFrameWork.Editor`を参照しない。
8. 登録APIと解除APIを同じライフサイクルの対として実装する。

## 2. 文書の読み方

- READMEのクイックスタートをAPI利用例の正本とする。コード例をAGENTS.mdや他の案内文書へ複製しない。
- 実装前は[AgentUsage.md](./Documentation~/AgentUsage.md)から対象モジュールの節だけを読む。
- 検証時だけ[AgentVerification.md](./Documentation~/AgentVerification.md)を読む。
- 文書と導入済みコードが食い違う場合は、現在のバージョンの公開シグネチャ、XMLドキュメント、CHANGELOGを確認する。推測で古いAPIへ合わせない。

## 3. 禁止事項

- パッケージ本体や自動生成enum・設定アセットを手で編集する。
- `internal` APIへリフレクション等で依存する。
- Editor専用APIをRuntime asmdefへ入れる。
- 検証用コード、シーン、セーブデータを利用プロジェクトへ残す。
