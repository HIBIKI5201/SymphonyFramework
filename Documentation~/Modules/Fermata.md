# Fermata

**カテゴリーごとにポーズ状態を切り替え**、通知やポーズに追従する待機処理を使えるようにします。

## 目的と範囲

「UIは動かしたままゲームプレイだけ止める」のように、止める対象を分けてポーズできるようにします。

- **すること**: カテゴリー単位・全体のポーズ切り替えと照会、`IPausable`への`Pause()`／`Resume()`の通知、ポーズに追従する待機（Coroutine、非同期、遅延Destroy、遅延Invoke）
- **しないこと**: `Time.timeScale`の変更、Animator・物理・パーティクルの停止（`Pause()`の中で利用側が行う）、ポーズ画面の表示

## 入口

| 項目 | 内容 |
| --- | --- |
| namespace | `SymphonyFramework.Pause` |
| 主な公開型 | `Fermata` / `IPausable` / `PauseInfo` |
| メニューパス | `Window > SymphonyFramework > Symphony Administrator` の `Pause` パネル |
| 設定の保存先 | `Project Settings > SymphonyFramework > Pause Category`（カテゴリー名の登録） |

## 仕組みの要点

1. **カテゴリーは`IPausable`を継承した空のinterfaceで表します。** 対象が属するカテゴリーは、実装しているinterfaceから自動で決まります。
2. **どれか1つでもポーズなら`Pause()`、すべて解除されたときだけ`Resume()`が呼ばれます。** 対象ごとに停止要因の数を持つため、2つ止めて片方だけ解除しても再開しません。
3. **派生カテゴリーの対象は基底カテゴリーにも属します。** `IPausable`を直接実装した対象は既定カテゴリーに属し、`SetPauseAll`でも`SetPause<IPausable>`でも止まります。どのカテゴリーにも属さない対象は生まれません。
4. **通知は値が変わったときだけ出ます。** 同じ値を2回設定しても`Pause()`は1回しか呼ばれません。

## 機能一覧

| ID | 機能 | 使うAPI | 結果 |
| --- | --- | --- | --- |
| PAUSE-01 | 対象の登録 | `Fermata.Register` / `Fermata.Unregister` | 実装しているカテゴリーすべてに登録する。重複登録しても通知は1回 |
| PAUSE-02 | カテゴリーのポーズ | `SetPause<TCategory>` / `IsPaused<TCategory>` | そのカテゴリー（と派生カテゴリー）の対象だけを止める・調べる |
| PAUSE-03 | 全体のポーズ | `SetPauseAll` / `IsPausedAny` | すべてのカテゴリーを止める・どれか1つでも止まっているか調べる |
| PAUSE-04 | 変化の購読 | `AddPauseChangedHandler<TCategory>` / `RemovePauseChangedHandler<TCategory>` | カテゴリーの状態が変わったときに`bool`を受け取る。型引数を省くと「どれか1つでもポーズ中か」の変化を受け取る |
| PAUSE-05 | ポーズに追従する待機 | `WaitForSecondsAsync` / `WaitForSeconds` / `NextFrameAsync` / `WaitUntilAsync` | ポーズ中の時間を数えずに待つ。`WaitUntilAsync`はポーズ中なら条件が成立しても再開しない |
| PAUSE-06 | ポーズに追従する遅延処理 | `DestroyAsync` / `InvokeAsync` | ポーズ中を除いた時間の後に破棄・呼び出しする |
| PAUSE-07 | 状態の照会 | `GetPauseInfo<TCategory>()` / `GetPauseInfo()` | 状態と件数のスナップショット。全体の場合`PauseInfo.Category`は`null` |
| PAUSE-08 | カテゴリーの生成 | `Project Settings > SymphonyFramework > Pause Category` | 登録した名前から空のinterfaceを生成する |

待機、遅延処理、変化の購読には、カテゴリーを指定する版（`<TCategory>`）と指定しない版があります。**指定しない版はすべてのカテゴリーを対象にし、どれか1つでもポーズ中なら止まります。**

## クイックスタート

カテゴリーを定義します。手で書いても、Project Settingsから生成しても構いません。

```csharp
using SymphonyFramework.Pause;

public interface IGameplayPausable : IPausable { }
public interface IUiPausable : IPausable { }
```

止めたい対象は、属したいカテゴリーを実装します。

```csharp
public sealed class Enemy : MonoBehaviour, IGameplayPausable
{
    private void OnEnable() => Fermata.Register(this);
    private void OnDisable() => Fermata.Unregister(this);

    public void Pause() { /* 停止処理 */ }
    public void Resume() { /* 再開処理 */ }
}
```

操作、購読、待機はカテゴリーを型引数で指定します。

```csharp
Fermata.SetPause<IGameplayPausable>(true);
bool paused = Fermata.IsPaused<IGameplayPausable>();

// eventは型パラメータを持てないため、購読はメソッドで行う
Fermata.AddPauseChangedHandler<IGameplayPausable>(HandlePauseChanged);

await Fermata.WaitForSecondsAsync<IGameplayPausable>(1.0f, destroyCancellationToken);

// 型引数を省くと、すべてのカテゴリーが対象になる（どれか1つでもポーズ中なら止まる）
await Fermata.WaitForSecondsAsync(1.0f, destroyCancellationToken);
```

## うまくいかないとき

| 起きること | この機能の動き | 対処 |
| --- | --- | --- |
| `SetPause<T>`へ具象クラスを渡した | `ArgumentException`を投げる | カテゴリーのinterfaceを渡す |
| ポーズ中に対象を登録した | さかのぼって`Pause()`を呼ばない。解除時に`Resume()`だけが届く | 登録時に`IsPaused<T>()`を見て、自分で止める |
| 同じ値を再設定した | 通知しない | 再設定を通知の起点にしない |
| 購読者が例外を投げた | 握り潰さず、状態を設定した側へ伝える | 購読側で処理する |
| 解除後も呼ばれ続ける | 登録が残っている | `GetPauseInfo<T>()`の購読件数で解除し忘れを探す |

## 守ること

- 登録は有効化時、解除は無効化時に行う。登録時にカテゴリーを渡す必要は無い。
- ポーズ中も止める待機には`Fermata.WaitForSecondsAsync`などを使い、`Task.Delay`や通常の`WaitForSeconds`で代用しない。
- **非同期の待機APIが返す`Awaitable`は1回しか`await`できない。** フィールドへ持たず、その場で待つ。複数の待機は`SymphonyAwaitable.WhenAll`、`Task`と混ぜる場合は`SymphonyAwaitable.AsTask`を使う。
- 全体の設定と照会は`SetPauseAll`と`IsPausedAny`を使う。

## Editor機能

### Pause パネル

**入口**: `Window > SymphonyFramework > Symphony Administrator`

| パネル | 内容 |
| --- | --- |
| Pause | 全体のポーズ状態の確認と切り替え、`IPausable`の購読件数 |
| Pause > Categories | カテゴリーごとのポーズ状態と、属する対象の件数。表示名の昇順で並ぶ |

Play Mode中のみ内容を持ちます。Edit Modeでは未接続状態を表示します。

### ポーズカテゴリーの生成

**入口**: `Project Settings > SymphonyFramework > Pause Category`

カテゴリー名を登録して「設定を保存してinterfaceを生成」を押すと、`IPausable`を継承した空のinterfaceを生成します。**名前は`I`と`Pausable`で挟まれます**（`Gameplay` → `IGameplayPausable`）。生成される名前は入力欄の隣に表示されます。

| 規則 | 内容 |
| --- | --- |
| 生成先 | `Assets/Scripts/SymphonyFramework/PauseCategory/`。自動生成enumのフォルダの下にあり、`SymphonyFramework.Enum`アセンブリに含まれる |
| 名前 | C#の識別子にできない名前は警告して除外する。同じinterface名になる候補は1件だけ残す |
| 削除 | **設定から消してもファイルは削除しない。** 実装しているコードのコンパイルを壊さないためで、残っているものは警告で知らせる |

生成物は自動生成enumと同じ`SymphonyFramework.Enum`アセンブリに入ります。このアセンブリは`SymphonyFramework`を参照し、`SymphonyFramework`側はこのアセンブリを参照しません。利用側のasmdefから使う場合は`SymphonyFramework.Enum`を参照に足してください。6.19.0以前の専用asmdef（`SymphonyFramework.PauseCategory`）は、Editorの起動時に生成物を書き換えたうえで削除します。

## 確かめ方

- Play Modeで`Pause > Categories`を開き、止めたいカテゴリーの対象件数が想定どおりかを見る。
- パネルからポーズを切り替え、対象の`Pause()`／`Resume()`が呼ばれることを確かめる。
- 解除し忘れは、シーンを切り替えた後に購読件数が減っているかで確かめる。

## 関連

- [Utility](./Utility.md)
- [Architecture.md](../Architecture.md)
- [Deprecations.md](../Deprecations.md)
- [CHANGELOG.md](../../CHANGELOG.md)
