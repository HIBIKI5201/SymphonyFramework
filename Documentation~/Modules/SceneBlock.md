# Scene Block

まとめてロードする複数シーンと、その依存関係を1つのアセットとして定義し、依存順にロードします。`SceneLoader`による単体シーンのロードと併用できます。

## 入口

| 項目 | 内容 |
| --- | --- |
| namespace | `SymphonyFrameWork.System.SceneBlock` |
| 主な公開型 | `SceneBlockLoader` / `SceneBlockAsset` / `SceneBlockEntry` / `SceneBlockInfo` / `SceneBlockLoadStateEnum` / `SceneBlockPlanException` |
| メニューパス | `Assets > Create > Symphony Framework > Scene Block` |
| 設定の保存先 | 利用側プロジェクトの `SceneBlockAsset` アセット |

## クイックスタート

`Assets > Create > Symphony Framework > Scene Block` でアセットを作り、Inspectorでエントリを並べます。

| フィールド | 内容 |
| --- | --- |
| Block Name | ブロックの識別名。空の場合はアセット名を使う |
| Entries | このブロックがロードするシーンの一覧 |
| Entries > Scene Name | ロードするシーン名。Build Settings のシーンから選ぶ |
| Entries > Depends On | **このシーンより先にロードを完了させるシーン名。** 同じブロック内のシーンを指す |
| Entries > Priority | Active Scene 選択に使用する優先度。`SceneLoadRequest` の優先度と同じ意味 |
| Entries > Is Persistent | ブロックをアンロードしてもこのシーンを残す |

`Depends On` に何も書かないシーンは、互いに依存しないため同時にロードできます。次の例では `Town_Base` を先に読み、`Town_Props` と `Town_Npc` を同時に読み、最後に `Town_Ui` を読む3段階になります。

```text
Scene Block: TownBlock
├─ Town_Base   Depends On: []             Priority: 10  Is Persistent: ✔
├─ Town_Props  Depends On: [Town_Base]    Priority: 0
├─ Town_Npc    Depends On: [Town_Base]    Priority: 0
└─ Town_Ui     Depends On: [Town_Props]   Priority: 0
```

アセットを `SceneBlockLoader` へ渡すとロードできます。

```csharp
using SymphonyFrameWork.System.SceneBlock;
using UnityEngine;

public sealed class BlockSwitcher : MonoBehaviour
{
    [SerializeField] private SceneBlockAsset _townBlock;
    [SerializeField] private SceneBlockAsset _dungeonBlock;

    private async Awaitable EnterDungeonAsync()
    {
        // 先に次をロードしてから前をアンロードすると、共通シーンの保持が途切れない。
        await SceneBlockLoader.LoadAsync(_dungeonBlock, token: destroyCancellationToken);
        await SceneBlockLoader.UnloadAsync(_townBlock, token: destroyCancellationToken);
    }
}
```

| メソッド | 内容 |
| --- | --- |
| `LoadAsync` | ブロックの全シーンを依存順にロードする。進捗はブロック全体で0から1へ通知する |
| `UnloadAsync` | ブロックの保持を解き、保持が残らないシーンだけをアンロードする |
| `IsLoaded` | ブロックが追跡中か確認する |
| `TryGetBlockInfo` / `GetBlockInfos` | ブロック名、状態、進捗、保持シーン、層の数を取得する |

### シーンを保持する規則

**シーンごとに「どのブロックが保持しているか」を追跡し、保持が空になったときだけアンロードします。**

| 状況 | ロード時 | アンロード時 |
| --- | --- | --- |
| どのブロックも保持しておらず、ロードもされていない | ロードして保持へ加える | 保持が空になればアンロードする |
| 他のブロックが保持している | ロードせず保持だけ加える | 保持が残るのでアンロードしない |
| ブロック外でロード済み | ロードせず保持だけ加える | **アンロードしない** |
| `Is Persistent` が有効 | 通常どおりロードする | **アンロードしない** |

**ブロックのロードを開始した時点で既にロードされていたシーンは、利用側の所有物として扱います。** `SceneLoader.LoadSceneAsync` で直接ロードしたシーンや、起動時の初期シーンがこれに当たります。そのシーンはどのブロックのアンロードでも落ちないため、落とす場合は `SceneLoader.UnloadSceneAsync` を呼んでください。

## 実装時の注意

- **`Depends On` は「先に読むシーン」を書きます。** 「このシーンの後に読むシーン」ではありません。`A` の後に `B` を読みたい場合は、`B` のエントリへ `A` を書きます。
- **依存の相手は同じブロックのエントリに存在している必要があります。** 存在しないシーン名を書くと `SceneBlockPlanException` になります。
- **`SceneBlockPlanException` は検出した異常をすべて報告します。** `Descriptions` に全件が入り、`Message` にも件数と全文が含まれます。1件直すたびに読み込み直す必要はありません。
- 検出する異常は、シーン名の重複、自分自身への依存、存在しないシーンへの依存、循環依存、シーン名や依存が空の4種類です。
- **同じシーンを複数のブロックへ書けます。** 1つのシーンが複数のブロックに属することは異常ではありません。
- エントリの並び順はロード順に影響しません。順序は `Depends On` だけで決まります。
- ブロックが読むシーンは、利用側の Build Settings に登録されている必要があります。
- **ブロックを切り替えるときは「次をロード → 前をアンロード」の順にしてください。** 逆順にすると、両方のブロックに属するシーンの保持が一度空になり、アンロードと再ロードが走ります。
- **ロードに失敗したりキャンセルしたりしても、ロード済みのシーンは巻き戻しません。** 保持は成立した分だけ記録され、ブロックの状態は `Loading` のまま残ります。`UnloadAsync` を呼べば片付きます。
- **同じ名前のブロックを2つロードすると `InvalidOperationException` になります。** 保持がどちらのアセットのものか判別できなくなるためです。同じアセットの再ロードは何もせず成功します。
- 進捗は層ごとではなくブロック全体で 0 から 1 へ通知します。
- `SceneBlockLoader` は Additive のロードだけを行います。既存のシーンを一掃する場合は `SceneLoader` 側の API を使ってください。

## 内部構造

`SceneBlockLoader` は `SceneLoader` の内部Serviceを土台にして動きます。`SymphonyOrchestrator` が `SceneLoader` の初期化後に `SceneBlockLoader` を初期化し、終了処理は登録の逆順で実行します。

| 層 | 型 | 役割 |
| --- | --- | --- |
| Adaptor | `SceneBlockLoader` | 公開エントリポイント |
| Application | `SceneBlockService` | 層順のロードと保持規則の実行 |
| Application | `SceneBlockRegistry` | ブロックの追跡と、シーンごとの保持元の記録 |
| Domain | `SceneBlockLoadEntity` | ブロック1件の実行計画と状態 |
| Domain | `SceneBlockGraphPlanner` | 依存グラフの検証とトポロジカル層の算出 |

## 関連

- [Scene Loader](./SceneLoader.md)
- Scene Block Sample（Package Managerからインポートできます）
- [CHANGELOG.md](../../CHANGELOG.md)
