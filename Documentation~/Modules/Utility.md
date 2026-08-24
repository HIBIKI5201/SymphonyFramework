# Utility

非同期処理、Tween、文字列、Component、VisualElementを補助するUtilityです。特定のサブシステムに依存せず、単体で使えます。

## 入口

| 項目 | 内容 |
| --- | --- |
| namespace | `SymphonyFrameWork.Utility`（`SymphonyAwaitable` / `SymphonyTween` / `SymphonyVisualElement`）、`SymphonyFrameWork`（`SymphonyStringUtil` / `SymphonyComponentUtil`） |
| 主な公開型 | `SymphonyAwaitable` / `SymphonyTween` / `SymphonyStringUtil` / `SymphonyComponentUtil` / `SymphonyVisualElement` |
| メニューパス | なし |
| 設定の保存先 | なし |

| 型 | 用途 |
| --- | --- |
| `SymphonyAwaitable` | Unityの`Awaitable`へ、完了済み値、合成、条件待機、タイムアウト、`Task`ブリッジ、バックグラウンド実行を足す |
| `SymphonyTween` | 指定した時間の間、`AnimationCurve`またはLerpで値を毎フレーム更新する |
| `SymphonyStringUtil` | リッチテキストのカラータグを挿入・削除する |
| `SymphonyComponentUtil` | Componentの取得と追加、自身を除く子からの取得を行う |
| `SymphonyVisualElement` | UI ToolkitのVisualElementをC#から制御する基底class |

`SymphonyAwaitable`は`WhenAny`を提供しません。未完了側の`Awaitable`と例外を安全に消費する一般契約を定められないためです。

## 実装時の注意

- **`Awaitable`は1回しか`await`できず、保存も共有もできない。** フィールドへ持たず、その場で待機する。複数待機は`SymphonyAwaitable.WhenAll`、`Task`と混ぜる場合は`SymphonyAwaitable.AsTask`を使う。
- 同期的に完了する`Awaitable`を返す場合は`SymphonyAwaitable.Completed()`と`SymphonyAwaitable.FromResult(value)`を使い、`null`を返さない。
- `SymphonyComponentUtil.GetComponentInChildrenExcludeSelf`は、既定では非アクティブな子を検索しない。非アクティブな子も含める場合だけ`includeInactive: true`を指定する。

## 関連

- [Pause Manager](./PauseManager.md) — ポーズ対応の待機API
- [Save Data System](./SaveDataSystem.md) — 独自ローダーが返す`Awaitable`
- [Deprecations.md](../Deprecations.md)
- [CHANGELOG.md](../../CHANGELOG.md)
