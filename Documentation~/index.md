# Symphony Framework Documentation

Symphony Frameworkの文書の入口です。**モジュールごとの文書は1ファイルで完結**しており、使い方、実装時の注意、対応するEditor機能、内部構造がまとまっています。

はじめて使う場合は[README](../README.md)から読んでください。インストール、初期設定、最小のコード例があります。

## モジュール

| モジュール | 内容 |
| --- | --- |
| [Service Locator](./Modules/ServiceLocator.md) | サービスの登録と取得。非同期での待機 |
| [Scene Loader](./Modules/SceneLoader.md) | シーンの追加ロードとActive Sceneの切り替え |
| [Scene Block](./Modules/SceneBlock.md) | 複数シーンをブロック単位で依存順にロードする |
| [Save Data System](./Modules/SaveDataSystem.md) | セーブデータの登録、保存、読み込み |
| [Audio Manager](./Modules/AudioManager.md) | AudioSourceの取得とグループ単位の音量制御 |
| [Pause Manager](./Modules/PauseManager.md) | カテゴリー単位のポーズと、ポーズに追従する待機処理 |
| [Debug](./Modules/Debug.md) | ログ出力、画面上のHUD、処理時間の計測 |
| [Utility](./Modules/Utility.md) | Component取得、Tween、その他の補助 |
| [Inspector属性](./Modules/InspectorAttributes.md) | Inspectorの表示を変える属性 |
| [AutoEnumGenerator](./Modules/AutoEnumGenerator.md) | 文字列の一覧からenumを生成する |
| [Asset Store Tools Packager](./Modules/AssetStoreToolsPackager.md) | Asset Store向けのパッケージ出力 |
| [Project Structure Tools](./Modules/ProjectStructureTools.md) | プロジェクトのフォルダ構成の生成と検査 |

## 全体

| 文書 | 内容 |
| --- | --- |
| [README](../README.md) | 概要、インストール、初期設定、クイックスタート |
| [Editor機能](./EditorTools.md) | Symphony Frameworkが追加するEditor拡張の索引 |
| [パッケージ構成](./Architecture.md) | レイヤーと公開型どうしの関係 |
| [非推奨APIと削除予定](./Deprecations.md) | `[Obsolete]` を付けたAPIと、その移行先 |
| [変更履歴](../CHANGELOG.md) | バージョンごとの追加・変更・修正 |

## AIエージェント向け

| 文書 | 内容 |
| --- | --- |
| [API索引](./AgentUsage.md) | namespaceと入口の型から読むべきモジュール文書を引く |
| [検証手順](./AgentVerification.md) | 利用側プロジェクトで書いたコードの確認手順 |

常時守るルールは、パッケージルートの `AGENTS.md` にあります。

## この文書について

**正本はMarkdownで、HTMLは生成物です。** 内容の修正は `Assets/SymphonyFrameWork/Documentation~/` のMarkdownへ行い、`python scripts/build_module_docs.py` で再生成してください。

**モジュール文書へのリンクは上の一覧の中だけに置いてください。** 本文から張ると、一覧の網羅と並び順を確かめる検査に引っかかります。
