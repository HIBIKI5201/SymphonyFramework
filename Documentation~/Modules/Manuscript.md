# Manuscript

保存したいデータを型単位で読み込み、保持、保存、削除します。名前は音楽の「手稿譜」。演奏を残すために書き留めた楽譜のように、データ型ごとに書き残し、読み戻します。

## 目的と範囲

セーブデータを型ごとに1つのインスタンスとして持ち、保存先の違いを意識せずに読み書きできるようにします。

- **すること**: 型単位の読み込み・キャッシュ・保存・削除・存在確認、保存先の差し替え（`SaveDataLoaderStrategy`）、保存先の失敗の診断
- **しないこと**: 暗黙の同期読み込み（`Get<T>()`は読み込み済みの値だけを返す）、複数スロットの管理、暗号化（必要なら独自ローダーで行う）

## 入口

| 項目 | 内容 |
| --- | --- |
| namespace | `SymphonyFramework.SaveData` |
| 主な公開型 | `Manuscript` / `SaveDataContent` / `SaveDataLoaderStrategy` / `SaveDataEntryInfo` / `SaveDataOperationException` |
| メニューパス | `Project Settings > SymphonyFramework > Manuscript`、`Window > SymphonyFramework > Symphony Administrator` の `Manuscript` パネル |
| 設定の保存先 | `Assets/Resources/SymphonyFramework/SaveDataConfig.asset` |

## 仕組みの要点

1. **データは型ごとに1つのインスタンスとしてキャッシュされます。** `LoadAsync<T>()`が返すインスタンスを編集し、`SaveAsync<T>()`でそのまま保存します。
2. **初回は必ず`await Manuscript.LoadAsync<T>()`で読みます。** `Get<T>()`は読み込み済みの値だけを返します。非同期で読むローダーを同期で待つと完了できなくなるためです。
3. **保存先はローダーで決まります。** 既定はJSONをPlayerPrefsへ保存します。ファイルやクラウドへ保存する場合は`SaveDataLoaderStrategy`を継承し、Project Settingsで選びます。

## 機能一覧

| ID | 機能 | 使うAPI | 結果 |
| --- | --- | --- | --- |
| SAVE-01 | 読み込み | `Manuscript.LoadAsync<T>()` | 保存先から読み、キャッシュしたインスタンスを返す |
| SAVE-02 | 取得 | `Manuscript.Get<T>()` / `IsLoaded<T>()` | 読み込み済みのインスタンスを返す・読み込み済みか調べる |
| SAVE-03 | 保存 | `Manuscript.SaveAsync<T>()` | キャッシュしたインスタンスを保存先へ書く |
| SAVE-04 | 削除 | `Manuscript.DeleteAsync<T>()` | 保存値を消し、キャッシュを既定値へ戻す |
| SAVE-05 | 存在確認 | `Manuscript.Exists<T>()` | 保存先にデータがあるか調べる |
| SAVE-06 | キャッシュの一覧 | `Manuscript.GetEntries()` | 型名の昇順に並んだ`SaveDataEntryInfo`のスナップショット。未初期化なら他のAPIと同じく`SymphonyNotInitializedException` |
| SAVE-07 | 保存先の差し替え | `SaveDataLoaderStrategy`（利用側が継承） | Project Settingsの選択肢へ自動で並ぶ |

各APIには`Type`を受け取る版もあります。

## クイックスタート

```csharp
using System;
using SymphonyFramework.SaveData;

[Serializable]
public sealed class PlayerData : SaveDataContent
{
    public int Level = 1;
    public int Gold;
}
```

```csharp
PlayerData data = await Manuscript.LoadAsync<PlayerData>();
data.Gold += 100;
await Manuscript.SaveAsync<PlayerData>();

if (Manuscript.IsLoaded<PlayerData>())
{
    PlayerData cached = Manuscript.Get<PlayerData>();
}
```

## うまくいかないとき

| 起きること | この機能の動き | 対処 |
| --- | --- | --- |
| 未読み込みの型を`Get<T>()`した | `InvalidOperationException`を投げる | 先に`await Manuscript.LoadAsync<T>()`を呼ぶ。分からないときは`IsLoaded<T>()`で調べる |
| 初期化前に呼んだ（`GetEntries()`を含む） | `SymphonyNotInitializedException`を投げる | 呼び出しを初期化後へ移す。Play前にも動く表示（Editorの拡張など）は例外を受け取り、未初期化として表示する |
| 保存先で失敗した | `SaveDataOperationException`を投げる | `Operation`、`DataType`、`LoaderType`、`InnerException`で、どの操作・型・ローダーで失敗したかを見る |
| 処理をキャンセルした | `OperationCanceledException`のまま伝える | 呼び出し側で捕捉する |
| ローダーが未設定 | 警告を出し、既定の`JsonUtility`ローダーを使う | Project SettingsのManuscriptでローダーを選ぶ |

## 守ること

- 保存型は`SaveDataContent`を継承した、引数なしコンストラクタを持つ具象classにする。
- `Manuscript`がキャッシュするインスタンスを編集する。別のインスタンスを作って二重に管理しない。
- **`Data != null`を「読み込み済み」の判定に使わない。** キャッシュは初回アクセス時に既定値で作られる。読み込み済みかは`SaveDataEntryInfo.IsLoaded`で調べる。
- 独自ローダーの`LoadJsonAsync`／`SaveJsonAsync`／`DeleteCoreAsync`は`Awaitable`を返す。同期で終わる場合は`SymphonyAwaitable.Completed()`と`SymphonyAwaitable.FromResult(json)`を返し、`null`を返さない。
- 独自ローダーの設定ScriptableObjectを手で作らない。Project Settingsの選択肢へ自動で並ぶ。
- 旧`SaveDataRegistry`は3.0.0で削除した。`Manuscript`（6.x までの`SaveStore`）を使う。

## Editor機能

### Manuscript設定

**入口**: `Project Settings > SymphonyFramework > Manuscript`

| 項目 | 内容 |
| --- | --- |
| Loader | `SaveDataLoaderStrategy`を継承したローダー。`[SerializeReference]`で選ぶ |
| Current Loader Type | 今のローダーの型名（読み取り専用） |
| Managed Save Data Types | Save Dataパネルへ表示する型のチェックボックス。名前空間の階層で並ぶ |

**保存先**: ローダーは`Assets/Resources/SymphonyFramework/SaveDataConfig.asset`、管理対象のチェックは`ProjectSettings/Packages/symphonyframework/SaveDataVisibilityConfig.asset`

- ローダーを変えると、その場で`Manuscript`が読み直します。
- `SaveDataConfig`がまだ無い状態で開くと、未生成である旨と`設定アセットを生成`ボタンが出ます。画面を開くだけでは生成しません。

#### Managed Save Data Types

`SaveDataContent`を継承した型は、どのアセンブリにあってもこの一覧へ並びます。チェックを外した型はSave Dataパネルへ出ません。

| 規則 | 内容 |
| --- | --- |
| 既定値 | テスト用アセンブリ（`nunit.framework`または`UnityEngine.TestRunner`を参照する）の型は`false`、それ以外は`true` |
| 並び | 名前空間の階層。分岐の無い名前空間は1行にまとめる |
| 名前空間の行 | 配下の型をまとめて切り替える。混在しているときは混在表示になり、押すとすべて管理対象になる |
| 保存 | 操作して切り替えた型だけを保存する。画面を開くだけでは設定ファイルは増えない |
| 効く範囲 | **Editorの表示だけ。** `Manuscript`などの実行時APIはチェックに関係なく動く |

### Manuscript パネル

**入口**: `Window > SymphonyFramework > Symphony Administrator`

Managed Save Data Typesでチェックが入っている型を一覧にし、選んだ型をInspectorで編集できます。設定を変えると、開き直さずに一覧が入れ替わります。

Inspectorの編集先はランプで示します。

| ランプ | 編集先 | Load | Save | Delete |
| --- | --- | --- | --- | --- |
| 緑 | 実行中の`Manuscript`のインスタンス | 保存先から読み直す | 保存する | 保存値を消して既定値へ戻す |
| 黄 | パネル専用のインスタンス（保存値を読み込み済み） | パネル専用へ読み込む | 保存する。`Manuscript`へは登録しない | 保存値を消して作り直す |
| 赤 | パネル専用の新しいインスタンス | 同上 | 同上 | 同上 |

ロード中は赤のまま`Loading…`を表示し、終わるまで編集できません。パネル専用のインスタンスを編集すると`未保存の変更あり`が出ます。`Play Mode へ持ち越す`を有効にすると、Play Modeに入るときにその内容を保存先へ書き出します。この設定は開発者ごとの`UserSettings/SymphonyFramework/SymphonyUserSettingConfig.asset`に保存され、既定は無効です。

## 確かめ方

- Play Mode中にSave Dataパネルで型を選び、ランプが緑で、`LoadAsync`後の値が表示されることを見る。
- Play Modeに入らずに保存値を確かめる場合は、パネルの`Load`で読み込む（黄になる）。
- コードから確かめる場合は`Manuscript.GetEntries()`の`IsLoaded`と`SaveDate`をログへ出す。

## 関連

- [Utility](./Utility.md)
- [Architecture.md](../Architecture.md)
- [Deprecations.md](../Deprecations.md)
- [CHANGELOG.md](../../CHANGELOG.md)
