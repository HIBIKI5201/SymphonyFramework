# Scene Block

まとめてロードする複数シーンと、その依存関係を1つのアセットとして定義します。定義はRuntime、検証はアセットを読み込むフレームワーク側が行います。

## 入口

| 項目 | 内容 |
| --- | --- |
| namespace | `SymphonyFrameWork.System.SceneBlock` |
| 主な公開型 | `SceneBlockAsset` / `SceneBlockEntry` / `SceneBlockPlanException` |
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

依存関係が解決できないアセットを読み込むと、`SceneBlockPlanException` が発生します。

```csharp
try
{
    // ブロックを読み込む処理
}
catch (SceneBlockPlanException exception)
{
    foreach (string description in exception.Descriptions)
    {
        Debug.LogError(description);
    }
}
```

## 実装時の注意

- **`Depends On` は「先に読むシーン」を書きます。** 「このシーンの後に読むシーン」ではありません。`A` の後に `B` を読みたい場合は、`B` のエントリへ `A` を書きます。
- **依存の相手は同じブロックのエントリに存在している必要があります。** 存在しないシーン名を書くと `SceneBlockPlanException` になります。
- **`SceneBlockPlanException` は検出した異常をすべて報告します。** `Descriptions` に全件が入り、`Message` にも件数と全文が含まれます。1件直すたびに読み込み直す必要はありません。
- 検出する異常は、シーン名の重複、自分自身への依存、存在しないシーンへの依存、循環依存、シーン名や依存が空の4種類です。
- **同じシーンを複数のブロックへ書けます。** 1つのシーンが複数のブロックに属することは異常ではありません。
- エントリの並び順はロード順に影響しません。順序は `Depends On` だけで決まります。
- ブロックが読むシーンは、利用側の Build Settings に登録されている必要があります。

## 関連

- [Scene Loader](./SceneLoader.md)
- [CHANGELOG.md](../../CHANGELOG.md)
