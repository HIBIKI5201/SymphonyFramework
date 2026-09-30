# Build Validation

UXML/USSの依存切れとフォント設定の不備を、手動実行またはプレイヤービルド前に検出するEditor機能です。

## 入口

| 操作 | 場所 |
| --- | --- |
| 設定 | `Project Settings > SymphonyFrameWork > Build Validation` |
| UXMLの手動検証 | `Tools > SymphonyFrameWork > Build Validation > Validate UXML Dependencies` |
| フォントの手動検証 | `Tools > SymphonyFrameWork > Build Validation > Validate Fonts` |

## Editor機能

### 設定

**ビルド前の検証は、UXML・Fontともに既定で無効です。** まず手動検証で結果を確認し、必要な方の `Validate On Build` を有効にしてください。
手動検証は `Validate On Build` の値にかかわらず実行され、結果をConsoleとダイアログへ表示します。

| UXMLの項目 | 既定値 | 内容 |
| --- | --- | --- |
| Validate On Build | false | ビルド前にUXMLを検証し、不備があればビルドを停止する |
| Force Reimport | false | 依存先からUXML/USSを同期再インポートする |
| Roots | 空 | 起点の `VisualTreeAsset` と必須要素名の組。空ならAssets内の全UXMLを起点にする |
| Roots > Asset | 未設定 | 検証するUXML。登録した行が未設定の場合はエラーにする |
| Roots > Required Element Names | 空 | 展開結果に必要な要素の `name`。空なら追加の必須名検査をしない |

| Fontの項目 | 既定値 | 内容 |
| --- | --- | --- |
| Validate On Build | false | ビルド前にフォントを検証し、不備があればビルドを停止する |
| TextMesh Pro | true | Assets内で最初に見つかったTMP Settingsを検証する |
| UI Toolkit | true | 指定したText Settingsと全Panel Settingsの参照を検証する |
| UI Toolkit Text Settings | 未設定 | 全Panel Settingsが共通で参照するText Settings |
| Required Characters | 空 | 既定フォントに必要な文字。空なら文字検査をしない |
| Require Multi Atlas | false | DynamicフォントにMulti Atlas Texturesを要求する |

設定は画面を開いたときに作成され、編集内容は自動で保存されます。画面を開き直したときと、表示中の再描画時に設定値を読み直します。

| 保存先 | 版管理 |
| --- | --- |
| `ProjectSettings/Packages/symphonyframework/BuildValidationConfig.asset` | 含める（プロジェクト共有） |

### UXML/USSの検証

起点から `Template` / `Style` の `src` を再帰的にたどります。`project://database/` のURIと、参照元からの相対パスに対応します。

| 検査 | エラーになる状態 |
| --- | --- |
| 参照文字列 | `src` が空、Assets外、UXML/USS以外への参照 |
| ソースとmeta | 参照先ファイルまたは `.meta` が無い |
| GUID | `src` に書かれたGUIDが参照先の実GUIDと一致しない |
| 循環 | 同じ依存経路で既に訪問中のアセットを再び参照する |
| インポート結果 | UXML/USSを読み込めない、またはインポートエラーがある |
| ソースの要素名 | `Template` を除くソース上の名前付き要素が `CloneTree()` 後に無い |
| 必須要素名 | Rootsで指定した名前が `CloneTree()` 後に無い |

`Force Reimport` は重い処理なので、通常は無効のまま使用できます。ソースや依存に不備を検出した枝では再インポートを控え、エラーを報告します。

### フォントの検証

| 検査 | エラーになる状態 |
| --- | --- |
| Text Settings | Default Font Assetが未設定、または既定フォントがDynamicなのにClear Dynamic Data On Buildが有効（Staticの既定フォントには作用しないため検査しない） |
| Dynamicフォント | フォント側のClear Dynamic Data On Buildが有効 |
| Multi Atlas | Require Multi Atlasが有効なのにMulti Atlas Texturesが無効 |
| 元フォント | DynamicフォントにSource Font Fileが設定されていない |
| ビルド依存 | 既定フォント、またはDynamicの元フォントがText Settingsの依存に含まれない |
| 必須文字 | Character Tableにも、DynamicのSource Font Fileにも文字が無い |
| Panel Settings | 共通のText Settingsを参照していない |
| シリアライズ項目 | 必要なプロパティが無い、または型が異なり検証できない |

TMP SettingsがAssets内に無い場合は、TMPの検査を省略した理由を結果へ残します。TMPのアセンブリへの参照追加は不要です。

UI Toolkit Text Settingsが未設定の場合は、全Panel Settingsが最初のPanel Settingsと同じ参照を持つかだけを検査します。この場合、既定フォントや必須文字の検査は行いません。

StaticフォントはCharacter Tableだけで必須文字を判定します。Dynamicでは元フォントから生成できる文字も認め、DynamicOSでは元フォントの設定とビルド依存の検査を適用しません。

Source Font Fileの文字検査は公開APIの `Font.HasCharacter(char)` を使用します。補助面の文字はCharacter Tableに保存されている場合に確認できます。

### 対象と結果

検索対象は `Assets/` に限り、Packages内の設定は列挙しません。起点のUXMLと検証するText SettingsはAssets内に置いてください。
Text SettingsがAssets外、または既定フォントがPackages内の場合は、その設定の検査を省略して理由を結果に残します。

不備は最初の1件で止めず、一覧として報告します。ビルド時は有効な検証の結果をまとめ、不備があれば1回の `BuildFailedException` で停止します。

XML構文エラーなどである起点の検査を続けられなくても、その理由を記録して他の起点を検証します。手動検証はビルドを開始しません。

## 内部構造

| 型 | 役割 |
| --- | --- |
| BuildValidationConfig / BuildValidationSettingProvider | 設定の保存とProject Settings画面 |
| BuildValidationMenu / BuildValidationBuildProcessor | 手動実行とビルド前処理の入口 |
| UxmlDependencyValidator / UxmlSourceReference | Unity上の依存検査と、Unity APIを使わない参照文字列の解析 |
| FontSettingsValidator / FontSnapshot | SerializedObjectからの読み取りと、取得した値だけによる規則判定 |
| BuildValidationReport | エラー一覧と結果メッセージ |

実装はすべてEditorアセンブリ内にあり、検証用の公開APIは追加しません。自動初期化や常時ポーリングも行いません。

## 関連

- [Editor機能](../EditorTools.md)
- [ドキュメント索引](../index.md)
