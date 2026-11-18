# Utility

非同期処理、Tween、文字列、Component、VisualElementを補助するクラス群です。特定のサブシステムに依存せず、単体で使えます。

## 目的と範囲

Unity標準のAPIだけでは書き方が長くなる処理を、短く安全に書けるようにします。

- **すること**: `Awaitable`の合成・待機・`Task`との橋渡し、時間指定の値の補間、リッチテキストの色タグ操作、Componentの取得と追加、UI ToolkitのVisualElementの基底class
- **しないこと**: `Awaitable`の`WhenAny`（未完了側と例外を安全に片付ける一般的な決まりを定められないため）、ポーズへの追従（[Pause Manager](./PauseManager.md)の待機APIを使う）

## 入口

| 項目 | 内容 |
| --- | --- |
| namespace | `SymphonyFramework.Utility`（`SymphonyAwaitable` / `SymphonyTween` / `SymphonyVisualElement`）、`SymphonyFramework`（`SymphonyStringUtil` / `SymphonyComponentUtil`） |
| 主な公開型 | `SymphonyAwaitable` / `SymphonyTween` / `SymphonyStringUtil` / `SymphonyComponentUtil` / `SymphonyVisualElement` |
| メニューパス | なし |
| 設定の保存先 | なし |

## 機能一覧

| ID | 型 | 機能 |
| --- | --- | --- |
| UTIL-01 | `SymphonyAwaitable` | Unityの`Awaitable`へ、完了済みの値、合成（`WhenAll`）、条件待機、タイムアウト、`Task`との橋渡し（`AsTask`）、バックグラウンド実行を足す |
| UTIL-02 | `SymphonyTween` | 指定した時間の間、`AnimationCurve`またはLerpで値を毎フレーム更新する |
| UTIL-03 | `SymphonyStringUtil` | リッチテキストのカラータグを挿入・削除する |
| UTIL-04 | `SymphonyComponentUtil` | Componentの取得と追加、自身を除く子からの取得を行う |
| UTIL-05 | `SymphonyVisualElement` | UI ToolkitのVisualElementをC#から制御する基底class |

## 守ること

- **`Awaitable`は1回しか`await`できず、保存も共有もできない。** フィールドへ持たず、その場で待つ。複数の待機は`SymphonyAwaitable.WhenAll`、`Task`と混ぜる場合は`SymphonyAwaitable.AsTask`を使う。
- 同期で終わる`Awaitable`を返す場合は`SymphonyAwaitable.Completed()`と`SymphonyAwaitable.FromResult(value)`を返し、`null`を返さない。
- `SymphonyComponentUtil.GetComponentInChildrenExcludeSelf`は、既定では非アクティブな子を探さない。含める場合だけ`includeInactive: true`を指定する。

## 関連

- [Pause Manager](./PauseManager.md) — ポーズ対応の待機API
- [Save Data System](./SaveDataSystem.md) — 独自ローダーが返す`Awaitable`
- [Deprecations.md](../Deprecations.md)
- [CHANGELOG.md](../../CHANGELOG.md)
