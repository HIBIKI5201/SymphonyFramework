# Scene Block

まとめてロードする複数シーンと、その依存関係を1つのアセットとして定義し、依存順にロードします。

## 目的と範囲

「町」「ダンジョン」のような、いくつものシーンで成り立つ場面を1回の呼び出しで切り替えられるようにします。

- **すること**: 依存関係の検証、依存順（段階ごと）のロード、ブロック単位のアンロード、複数ブロックで共有するシーンの保持の管理
- **しないこと**: Additive以外のロード（既存のシーンを一掃するときは[Scene Loader](./SceneLoader.md)）、失敗時の巻き戻し、ブロックの外でロードされたシーンのアンロード

## 入口

| 項目 | 内容 |
| --- | --- |
| namespace | `SymphonyFrameWork.System.SceneBlock` |
| 主な公開型 | `SceneBlockLoader` / `SceneBlockAsset` / `SceneBlockEntry` / `SceneBlockInfo` / `SceneBlockLoadStateEnum` / `SceneBlockPlanException` |
| メニューパス | `Assets > Create > Symphony Framework > Scene Block` |
| 設定の保存先 | 利用側プロジェクトの `SceneBlockAsset` アセット |

## 仕組みの要点

1. **ロード順は`Depends On`だけで決まります。** 依存の無いシーンどうしは同じ段階で同時にロードし、エントリの並び順は影響しません。
2. **シーンごとに「どのブロックが保持しているか」を記録し、保持が空になったときだけアンロードします。** 同じシーンを複数のブロックへ書けます。
3. **ブロックのロードを始めた時点で既にロードされていたシーンは、利用側の持ち物として扱います。** どのブロックのアンロードでも落ちません。
4. **内部では[Scene Loader](./SceneLoader.md)でロードします。** 依存注入、`IInitializeAsync`、優先度によるActive Sceneの選択はScene Loaderと同じに働きます。

## 機能一覧

| ID | 機能 | 使うAPI・操作 | 結果 |
| --- | --- | --- | --- |
| BLK-01 | ブロックの定義 | `Assets > Create > Symphony Framework > Scene Block` | シーン、依存、優先度、残すかどうかを持つアセットを作る |
| BLK-02 | 依存の検証 | `SceneBlockAsset`のInspector | ロードの段階数と各段階のシーン、または検出した異常をすべて表示する |
| BLK-03 | ロード | `SceneBlockLoader.LoadAsync` | 全シーンを依存順にロードする。進捗はブロック全体で0から1 |
| BLK-04 | アンロード | `SceneBlockLoader.UnloadAsync` | ブロックの保持を解き、保持が残らないシーンだけをアンロードする |
| BLK-05 | 状態の照会 | `IsLoaded` / `TryGetBlockInfo` / `GetBlockInfos` | ブロック名、状態、進捗、保持シーン、段階数を返す |

アセットの項目は次のとおりです。

| フィールド | 内容 |
| --- | --- |
| Block Name | ブロックの識別名。空ならアセット名を使う |
| Entries > Scene Name | ロードするシーン名。Build Settingsのシーンから選ぶ |
| Entries > Depends On | **このシーンより先にロードを完了させるシーン名。** 候補は同じブロックのシーンだけ |
| Entries > Priority | Active Sceneの選択に使う優先度。`SceneLoadRequest`と同じ意味 |
| Entries > Is Persistent | ブロックをアンロードしてもこのシーンを残す |

## クイックスタート

次の定義では、`Town_Base`を先に読み、`Town_Props`と`Town_Npc`を同時に読み、最後に`Town_Ui`を読む3段階になります。

```text
Scene Block: TownBlock
├─ Town_Base   Depends On: []             Priority: 10  Is Persistent: ✔
├─ Town_Props  Depends On: [Town_Base]    Priority: 0
├─ Town_Npc    Depends On: [Town_Base]    Priority: 0
└─ Town_Ui     Depends On: [Town_Props]   Priority: 0
```

```csharp
using SymphonyFrameWork.System.SceneBlock;
using UnityEngine;

public sealed class BlockSwitcher : MonoBehaviour
{
    [SerializeField] private SceneBlockAsset _townBlock;
    [SerializeField] private SceneBlockAsset _dungeonBlock;

    private async Awaitable EnterDungeonAsync()
    {
        // 次をロードしてから前をアンロードすると、共通シーンの保持が途切れない。
        await SceneBlockLoader.LoadAsync(_dungeonBlock, token: destroyCancellationToken);
        await SceneBlockLoader.UnloadAsync(_townBlock, token: destroyCancellationToken);
    }
}
```

## 状態

`SceneBlockInfo.State`は`SceneBlockLoadStateEnum`の値をとります。

| 状態 | 意味 |
| --- | --- |
| `None` | 追跡していない |
| `Loading` | ブロックのシーンをロード中。失敗・キャンセルした後もこの状態のまま残る |
| `Complete` | ブロックの全シーンのロードが完了した |
| `Unloading` | ブロックのシーンをアンロード中 |

シーンごとの保持は次の規則で決まります。

| 状況 | ロード時 | アンロード時 |
| --- | --- | --- |
| どのブロックも保持しておらず、ロードもされていない | ロードして保持へ加える | 保持が空になればアンロードする |
| 他のブロックが保持している | ロードせず保持だけ加える | 保持が残るのでアンロードしない |
| ブロックの外でロード済み | ロードせず保持だけ加える | **アンロードしない** |
| `Is Persistent`が有効 | 通常どおりロードする | **アンロードしない** |

## うまくいかないとき

| 起きること | この機能の動き | 対処 |
| --- | --- | --- |
| シーン名の重複、自分への依存、存在しないシーンへの依存、循環依存、空のシーン名・依存 | `SceneBlockPlanException`を投げる。`Descriptions`と`Message`に検出した異常がすべて入る | Inspectorの検証結果を見て、全件をまとめて直す |
| ロードに失敗した、キャンセルした | ロード済みのシーンを巻き戻さない。成立した保持だけを記録し、状態は`Loading`のまま | `LoadAsync`をもう一度呼ぶと未完了の段階から再試行する。`UnloadAsync`を呼ぶと成立済みの保持を片付ける |
| 同じ名前のブロックを2つロードした | `InvalidOperationException`を投げる | `Block Name`を重ならないようにする |
| ロード中にアンロード、アンロード中にロードした | `InvalidOperationException`を投げる | 前の処理を待ってから呼ぶ |
| ロード完了済みの同じアセットをロードした | 何もせず成功する | 対処は要らない |
| ブロックの外でロードしたシーンが残る | どのブロックのアンロードでも落とさない | `SceneLoader.UnloadSceneAsync`で落とす |

## 守ること

- **`Depends On`には「先に読むシーン」を書く。** `A`の後に`B`を読みたい場合は、`B`のエントリへ`A`を書く。
- ブロックを切り替えるときは「次をロード → 前をアンロード」の順にする。逆にすると、両方に属するシーンの保持が一度空になり、アンロードと再ロードが走る。
- ブロックが読むシーンは、利用側のBuild Settingsへ登録する。

## Editor機能

| 機能 | 内容 |
| --- | --- |
| Symphony Administrator の Scene Block パネル | Play Mode中、追跡しているブロックの名前、状態、進捗、段階数、保持しているシーン名を一覧表示する |
| `SceneBlockAsset`のInspector | 依存の検証結果を表示する。解決できる場合は段階数と各段階のシーン名、できない場合は検出した異常をすべて並べる |
| `Depends On`の候補の絞り込み | 同じブロックに登録済みのシーンだけを選択肢に出す。保存済みの依存は、エントリから消えた後も選択肢に残る |

`Depends On`の候補は、エントリが1件も無いブロックでは空になります。先に`Scene Name`を埋めてください。

## 確かめ方

- Play Modeに入る前に`SceneBlockAsset`のInspectorを開き、段階数と各段階のシーンが意図どおりかを見る。循環や存在しない依存はここで分かる。
- Play Mode中に`Scene Block`パネルを開き、ブロックの状態が`Complete`になり、保持しているシーンが想定どおりかを見る。
- 利用例はPackage Managerからインポートできる Scene Block Sample にある。

## 関連

- [Scene Loader](./SceneLoader.md)
- [CHANGELOG.md](../../CHANGELOG.md)
