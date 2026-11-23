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

### 7.0.0（6.x の名前からの転送）

6.x（名前空間 `SymphonyFrameWork.*`）から上げた利用側が、まず警告付きでコンパイルできるように残した転送です。移行方法は [Migration7.md](./Migration7.md) にまとめています。置き場は `Runtime/Obsolete/<旧名前空間>/` です。

| 対象（`SymphonyFrameWork.*`） | 移行先 | 非推奨にした版 | 削除予定 | 備考 |
| --- | --- | --- | --- | --- |
| `System.ServiceLocate.ServiceLocator` | `Partitura` | 7.0.0 | 8.0.0 | `GetInstance` は 6.x と同じく未登録で `null`。インスタンスを渡す `DestroyInstance` / `IsExistInstance` は型だけで処理する |
| `System.ServiceLocate.ServiceInjector` | `Partitura.Inject` / `CreateInstance` | 7.0.0 | 8.0.0 | |
| `System.ServiceLocate.SymphonyLocateObject<T>` | `PartituraReference<T>` | 7.0.0 | 8.0.0 | `GetInstance` は未登録で `null` |
| `System.ServiceLocate.IInjectable` / `IInjectable<T0…T3>` | `SymphonyFramework.Services.IInjectable` | 7.0.0 | 8.0.0 | 新しい型を継承する。明示的な実装は対象外 |
| `System.ServiceLocate.LocateTypeEnum` | `ServiceLifetimeEnum` | 7.0.0 | 8.0.0 | 値の順序は同じ |
| `System.SceneLoad.SceneLoader` | `Attacca` | 7.0.0 | 8.0.0 | `Action<float>` の進捗は `IProgress<float>` へ包む |
| `System.SceneBlock.SceneBlockLoader` | `Suite` | 7.0.0 | 8.0.0 | 引数と戻り値は `SuiteAsset` / `SuiteInfo` |
| `System.SaveSystem.SaveStore` | `Manuscript` | 7.0.0 | 8.0.0 | `GetEntries` は未初期化で例外（6.19.0 の変更） |
| `System.SaveSystem.SaveDataContent` | `SymphonyFramework.SaveData.SaveDataContent` | 7.0.0 | 8.0.0 | 新しい型を継承する |
| `System.SaveSystem.SaveDataLoaderStrategy` / `PlayerPrefsSaveDataLoaderStrategy` | `SymphonyFramework.SaveData` の同名の型 | 7.0.0 | 8.0.0 | 旧 `SaveDataContent` を受け取る抽象メソッドへ橋渡しする |
| `System.AudioManager` | `Sonority` | 7.0.0 | 8.0.0 | `GetAudioSource` は未登録で `null` |
| `System.PauseManager`（入れ子の `IPausable` を含む） | `Fermata`、`IPausable` | 7.0.0 | 8.0.0 | 旧 `IPausable` は既定カテゴリーとして扱う |
| 6.x の名前空間ごとの目印（internal） | — | 7.0.0 | 8.0.0 | `using SymphonyFrameWork.X;` を通すためだけの空の型。`[MovedFrom]`（API Updater と書き換えツール `Migrate 6.x Names` の対応表に使う）は 8.0.0 以降も残す |

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

3.0.0での破壊的変更は[CHANGELOG.md](../CHANGELOG.md)の `### Breaking` を参照してください。

### 6.20.0（7.0.0 の命名整理 Round 5）

| 対象 | 移行先 | 非推奨にした版 | 備考 |
| --- | --- | --- | --- |
| `SymphonyDebugLogger.DirectLog(string, LogKindEnum)` | `SymphonyDebugLogger.LogDirect` | 記録なし | |
| `SymphonyDebugLogger.TextLog(LogKindEnum, bool)` | `SymphonyDebugLogger.LogText` | 記録なし | |
| `SymphonyDebugLogger.CheckComponentNull<T>(T)` | `SymphonyDebugLogger.LogAndCheckComponentNull` | 記録なし | 拡張メソッド |
| `SymphonyDebugLogger.IsComponentNotNull<T>(T)` | `SymphonyDebugLogger.LogAndCheckComponentNull` | 記録なし | 拡張メソッド。戻り値の意味が逆（nullならtrue）になる点に注意 |
| `SymphonyTween.TweeningLerp<T>(...)` | `SymphonyTween.Tweening` | 記録なし | 移行先は `Awaitable` を返す |
| `SymphonyTween.TweeningCurve<T>(...)` | `SymphonyTween.Tweening`（`curve` 引数） | 記録なし | 同上 |
| `EditorSymphonyConstant.ASSET_STORE_TOOLS_IGNORE_FILE` | `EditorSymphonyConstant.ASSET_STORE_TOOLS_CONFIG_FILE_NAME` | 3.1.0 | `ignore.txt` から `PackagerConfig.json` への移行処理も削除した |
| `AssetStoreToolsPackager.PackageModeEnum`（`Combine` を含む） | `AssetStoreToolsPackagePipeline` | 3.4.0 / 3.7.0 | 統合パッケージ出力は `AssetStoreToolsCombinePackageStrategy` として残している |
| `AssetStoreToolsPackager.Export(string[], PackageModeEnum, bool, bool)` | `Export(string[], AssetStoreToolsPackagePipeline)` | 3.7.0 | |
| `AssetStoreToolsPackageContext` | `AssetStoreToolsPackageExportContext` | 3.6.0 | |
| `PauseManager.Pause` | `Fermata.SetPauseAll` / `Fermata.IsPausedAny` | 6.12.0 | `PauseManager` は `Fermata` へ改名した。旧名の転送は 7.0.0 で入れる |
| `PauseManager.OnPauseChanged` | `Fermata.AddPauseChangedHandler(Action<bool>)` / `RemovePauseChangedHandler(Action<bool>)` | 6.12.0 | 同上 |
| カテゴリーを指定しない `PauseManager.Pausable*` 6件 | `Fermata.NextFrameAsync` / `WaitForSeconds` / `WaitForSecondsAsync` / `WaitUntilAsync` / `DestroyAsync` / `InvokeAsync` | 6.12.0 | **非推奨を取り消し、全カテゴリーを対象にする正式なAPIとして改名した。** どれか1つでもポーズ中なら止まる（動作は 6.12.0 以降と同じ） |

---

## 関連ドキュメント

- [CHANGELOG.md](../CHANGELOG.md) — 変更履歴。非推奨化・削除の時系列
- [README.md](../README.md) — 現行APIのクイックスタート
- [AgentUsage.md](./AgentUsage.md) — namespaceと入口の型から引くAPI索引
