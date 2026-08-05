# 非推奨APIと削除予定

`[Obsolete]` を付けた公開APIと、その削除予定の一覧です。**「今なお非推奨で、まだ消えていないもの」の正本**として扱います。

[CHANGELOG.md](../CHANGELOG.md)との分担:

| 文書 | 役割 |
| --- | --- |
| CHANGELOG.md | **時系列の記録。** どの版で非推奨にしたか、どの版で削除したか |
| この文書 | **現在形の一覧。** 今どれが非推奨で、いつ消えるのか |

CHANGELOGだけでは、非推奨化した版まで遡らないと一覧できず、その後で削除されたのかも分かりません。非推奨化のたびに両方へ書きます。

---

## 削除予定の一覧

| 対象 | 場所 | 移行先 | 非推奨にした版 | 削除予定 | 備考 |
| --- | --- | --- | --- | --- | --- |
| `SymphonyDebugLogger.DirectLog(string, LogKindEnum)` | `Runtime/Debug/SymphonyDebugLogger.cs` | `SymphonyDebugLogger.LogDirect` | 記録なし | **未定** | 非推奨にした版がCHANGELOGから特定できていない |
| `SymphonyDebugLogger.TextLog(LogKindEnum, bool)` | `Runtime/Debug/SymphonyDebugLogger.cs` | `SymphonyDebugLogger.LogText` | 記録なし | **未定** | 同上 |
| `SymphonyDebugLogger.CheckComponentNull<T>(T)` | `Runtime/Debug/SymphonyDebugLogger.cs` | `SymphonyDebugLogger.LogAndCheckComponentNull` | 記録なし | **未定** | 拡張メソッド。同上 |
| `SymphonyDebugLogger.IsComponentNotNull<T>(T)` | `Runtime/Debug/SymphonyDebugLogger.cs` | `SymphonyDebugLogger.LogAndCheckComponentNull` | 記録なし | **未定** | 拡張メソッド。「安全性が保障されていない」ことを理由に非推奨化されている。同上 |
| `SymphonyTween.TweeningLerp<T>(...)` | `Runtime/Utility/SymphonyTween.cs` | `SymphonyTween.Tweening` | 記録なし | **未定** | `Task` を返す旧型式。移行先は `Awaitable` を返す |
| `SymphonyTween.TweeningCurve<T>(...)` | `Runtime/Utility/SymphonyTween.cs` | `SymphonyTween.Tweening` | 記録なし | **未定** | 同上 |
| `EditorSymphonyConstant.ASSET_STORE_TOOLS_IGNORE_FILE` | `Core/Editor/EditorSymphonyConstant.cs` | `EditorSymphonyConstant.ASSET_STORE_TOOLS_CONFIG_FILE_NAME` | 3.1.0 | 次のメジャー更新 | 削除時は `AssetStoreToolsPackagerConfigStore` の `ignore.txt` 移行処理（`IGNORE_FILE_NAME`、`ReadIgnoreFile`、`CreateConfigFile` の移行分岐）も一緒に削除する |
| `AssetStoreToolsPackager.PackageModeEnum.Combine` | `Editor/Generator/AssetStoreToolsPackager/AssetStoreToolsPackager.cs` | `AssetStoreToolsCombinePackageStrategy` | 3.4.0 | 次のメジャー更新 | **enum の選択肢としては消すが、統合パッケージ出力そのものは Strategy として残す。** 下記「Combineの削除手順」を参照 |
| `AssetStoreToolsPackageContext` | `Editor/Generator/AssetStoreToolsPackager/AssetStoreToolsPackageContext.cs` | `AssetStoreToolsPackageExportContext` | 3.6.0 | 次のメジャー更新 | `ref struct` はフィールドへ保持できず、パイプラインの拡張点へ渡せない。3.6.0 以降フレームワーク内部では使用していない |

### Combineの削除手順

`Combine` を削除すると `PackageModeEnum` に `Singles` と `Nothing` しか残らず、enum 自体が意味を失います。**次のメジャー更新で enum ごと削除します。** そのとき一緒に直す箇所を記録しておきます。

**削除するのは enum の選択肢であって、統合パッケージ出力の機能ではありません。** 3.6.0 で `AssetStoreToolsCombinePackageStrategy` として残しました。「差分インポートの単位にならない」という 3.4.0 の判断は変わらないため、**既定テンプレートには含めず、必要な利用者が自分でパイプラインへ追加する**位置付けです（Issue #124）。この Strategy に `[Obsolete]` は付けていません。

| 対象 | 変更内容 |
| --- | --- |
| `AssetStoreToolsPackager.PackageModeEnum` | 削除する |
| `AssetStoreToolsPackager.Export(string[], PackageModeEnum, bool, bool)` | 削除する。代替は `Export(string[], AssetStoreToolsPackagePipeline)`（3.6.0 で追加済み） |
| `AssetStoreToolsPackager.CreatePlan(string[], PackageModeEnum, bool, bool)` | 削除する |
| `AssetStoreToolsPackager.CreateStepsFromOptions` と `LEGACY_PIPELINE_NAME` | 削除する |
| `AssetStoreToolsPackager` 内の `#pragma warning disable CS0618` | 抑止ごと削除する |
| `AssetStoreToolsPackageWindow` | `Export Mode` の `EnumFlagsField` と `DrawCombineDeprecationHelp` を削除する（3.7.0 でパイプラインのポップアップへ置き換え予定） |
| ワークスペースの `Documentation/DesignPhilosophy.md` | enum 命名の実例から `PackageModeEnum` を外す |

**旧シグネチャを `[Obsolete]` のシムとして残せません。** `PackageModeEnum` 自体が消えるため、旧シグネチャは型として成立しないためです。メジャー更新で直接削除し、CHANGELOG の `### Breaking` へ移行方法を明記します。

### 「未定」について

**削除予定が「未定」の項目は、判断がまだ行われていないことを表します。** 空欄にせず未定と書くのは、判断が必要な項目が何件あるかを見えるようにするためです。

現在6件が未定です。**CHANGELOGの `### Deprecated` 節をすべて確認しましたが、これら6件の非推奨化はどの版にも記載がありませんでした。** 記載があるのは `SaveDataLoader`（2.20.0）、`SaveDataRegistry`（2.19.0）、`SymphonyTask`（2.6.0）、旧namespaceのFacade（2.2.0）、`ASSET_STORE_TOOLS_IGNORE_FILE`（3.1.0）で、いずれも別の対象です。

非推奨化がCHANGELOGへ記載されないまま残っていたことが、この文書を作った理由の1つです。次のメジャー更新を計画するときに、6件の削除時期をまとめて判断してください。

---

## 削除の方針

- **`[Obsolete]` を付けたら、同じ変更でこの文書へ行を追加します。** 削除予定が決まっていない場合は「未定」と書きます。書かずに済ませないでください。
- 非推奨化と同時に、CHANGELOGの `### Deprecated` へ移行方法を書きます。
- **削除は必ずメジャー更新で行います。** `public` / `protected` メンバーの削除は破壊的変更です。
- 削除したら、この文書の行を[削除済み](#削除済み)へ移し、CHANGELOGの `### Breaking` へ移行方法を書きます。
- 名前・シグネチャ・意味が同時に変わり、旧契約を安全に維持できないAPIは `[Obsolete]` のシムを残さず、メジャー更新で直接削除します。その場合も、削除予定としてこの文書へ先に載せます。

`[Obsolete]` は `error: false`（警告）で付けます。利用側のコンパイルを止めず、移行期間を確保するためです。

---

## 削除済み

削除が完了したものをここへ移します。移行が終わっているかの確認に使ってください。

現時点で削除済みの項目はありません。3.0.0での破壊的変更は[CHANGELOG.md](../CHANGELOG.md)の `### Breaking` を参照してください。

---

## 関連ドキュメント

- [CHANGELOG.md](../CHANGELOG.md) — 変更履歴。非推奨化・削除の時系列
- [README.md](../README.md) — 現行APIのクイックスタート
- [AgentUsage.md](./AgentUsage.md) — AIエージェントが利用コードを書くときの注意事項
