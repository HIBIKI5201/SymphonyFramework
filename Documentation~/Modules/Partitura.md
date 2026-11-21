# Partitura

共有したいインスタンスを型で登録し、必要な場所から型で取得します（Service Locator）。名前はイタリア語で「総譜」。全パートを1冊にまとめた楽譜のように、どのサービスでも型で引けます。

## 目的と範囲

シーンやクラスをまたいで使うインスタンスを、参照を手で受け渡さずに取り出せるようにします。

- **すること**: インスタンスの登録・解除・取得、登録の待機、コンストラクタ・`IInjectable`への依存の注入、登録状態の一覧
- **しないこと**: 依存の再帰的な生成（登録済みのインスタンスだけを渡す）、`CreateInstance`で作ったインスタンスの自動登録、シーンのロード（[Attacca](./Attacca.md)）

## 入口

| 項目 | 内容 |
| --- | --- |
| namespace | `SymphonyFramework.Services` |
| 主な公開型 | `Partitura` / `ServiceComponent` / `PartituraReference<T>` / `IInjectable` / `ServiceLifetimeEnum` / `ServiceRegistrationInfo` / `ServiceNotRegisteredException` |
| メニューパス | `Window > SymphonyFramework > Symphony Administrator` の `Partitura` パネル |
| 設定の保存先 | `UserSettings/SymphonyFramework/SymphonyUserSettingConfig.asset`（Partituraのログ設定） |

## 仕組みの要点

1. **1つの型につき1つのインスタンスを登録します。** 同じ型を2つ目に登録しようとすると`Register`は`false`を返し、候補は呼び出し側の持ち物のまま残ります。
2. **登録方式は2つです。** `ServiceLifetimeEnum.Scene`は参照だけを登録し、`ServiceLifetimeEnum.Persistent`はComponentを永続管理オブジェクトの配下へ移します。
3. **注入の経路は、生成を誰が握るかで選びます。** 自分で生成するピュアC#の型は`Partitura.CreateInstance<T>()`、Unityが生成する`MonoBehaviour`は`IInjectable<T...>`、実行時に`Instantiate`したものは`Partitura.Inject(...)`です。
4. **登録状態はスナップショットで読みます。** `GetRegistrationInfos`と`TryGetRegistrationInfo`が返す`ServiceRegistrationInfo`は、取得した時点の値から変わりません。

## 機能一覧

| ID | 機能 | 使うAPI | 結果 |
| --- | --- | --- | --- |
| SL-01 | 登録と解除 | `Register` / `Unregister` / `Destroy` | 型ごとに1件を登録する。`Destroy`は登録を解除し、インスタンスも解放する。インスタンスを渡す`Unregister(instance)`・`Destroy(instance)`は、現在の登録と同じ参照のときだけ動く |
| SL-02 | 重複時の自動解放 | `RegisterWithAutoDispose` | 型が重複したとき、候補を`Dispose`またはGameObject破棄する |
| SL-03 | 取得 | `Get` / `TryGet` | 必須なら`Get`（無ければ例外）、任意なら`TryGet`（無ければ`false`） |
| SL-04 | 登録の待機 | `GetAsync` / `TryGetAsync` / `WhenRegistered` | 登録されるまで待つ、または登録後に処理を呼ぶ |
| SL-05 | コンストラクタ注入 | `Partitura.CreateInstance<T>()` | 引数を登録済みのインスタンスから解決して生成する |
| SL-06 | `IInjectable`への注入 | `Partitura.Inject(...)` | `Inject`メソッドへ登録済みのインスタンスを渡す。シーンのルートには`Attacca`が自動で行う |
| SL-07 | Inspectorからの登録 | `ServiceComponent` | `Auto Register`を有効にしたComponentを、同じフレームの`Start`より前に登録する |
| SL-08 | 取得結果のキャッシュ | `PartituraReference<T>` | 初回だけ`Partitura`から取得し、以降は保持した参照を返す。保持した参照が破棄済みなら取り直す |
| SL-09 | 登録状態の一覧 | `GetRegistrationInfos` / `TryGetRegistrationInfo` / `IsRegistered` | 型、インスタンス、登録方式のスナップショットを返す。`IsRegistered(instance)`は同じ参照が登録されているかを返す |

## どの入口を使うか

| したいこと | 使うAPI |
| --- | --- |
| 必ずあるはずのサービスを取る | `Partitura.Get<T>()` |
| 無いかもしれないサービスを取る | `Partitura.TryGet<T>(out T)` |
| 登録されるまで待って取る | `Partitura.GetAsync<T>()` / `Partitura.TryGetAsync<T>()` |
| 登録されたら処理する | `Partitura.WhenRegistered<T>(Action<T>)` |
| フィールドに持って何度も取る | `PartituraReference<T>` |
| ピュアC#の型を依存つきで作る | `Partitura.CreateInstance<T>()` |
| `MonoBehaviour`へ依存を渡す | `IInjectable<T...>`（シーンのルートは自動）、`Partitura.Inject(...)` |

## クイックスタート

登録と解除は、同じライフサイクルの対として書きます。

```csharp
using SymphonyFramework.Services;
using UnityEngine;

public sealed class GameSession : MonoBehaviour
{
    private void OnEnable()
    {
        Partitura.Register(this, ServiceLifetimeEnum.Scene);
    }

    private void OnDisable()
    {
        Partitura.Unregister(this);
    }
}
```

```csharp
GameSession session = Partitura.Get<GameSession>();
GameSession awaited = await Partitura.GetAsync<GameSession>(
    grace: 10,
    token: destroyCancellationToken);
```

ピュアC#の型は`CreateInstance`で生成すると、コンストラクタの引数が登録済みのインスタンスから解決されます。

```csharp
public sealed class BattleService
{
    private readonly SaveService _saveService;
    private readonly AudioPlayer _audioPlayer;

    // publicなコンストラクタのうち、引数が最も多いものが使われる。
    // 既定値を持つ引数は、登録が無ければ既定値になる。
    public BattleService(SaveService saveService, AudioPlayer audioPlayer, int retryCount = 3)
    {
        _saveService = saveService;
        _audioPlayer = audioPlayer;
    }
}

BattleService service = Partitura.CreateInstance<BattleService>();
```

## うまくいかないとき

| 起きること | この機能の動き | 対処 |
| --- | --- | --- |
| 同じ型を2回登録した | `Register`が`false`を返す。候補は解放しない | 古い登録を解除してから登録する。候補を捨ててよいなら`RegisterWithAutoDispose`を使う |
| 必須の型が未登録 | `Get`が`ServiceNotRegisteredException`を投げる | 任意の依存なら`TryGet`を使う |
| 待機が期限を過ぎた | `GetAsync`が`TimeoutException`を投げる。`TryGetAsync`は`false`を返す | 登録側が`grace`（秒）の間に登録しているか、Partituraパネルで確かめる |
| 待機をキャンセルした | `OperationCanceledException`を投げる（`TryGetAsync`も同じ） | 呼び出し側で捕捉する |
| `CreateInstance`の引数を解決できない | 既定値の無い引数が未登録なら`ServiceNotRegisteredException`。インスタンスは作らない | 依存を先に登録するか、引数へ既定値を付ける |
| `CreateInstance`の候補が決まらない | 引数の数が同じ`public`コンストラクタが並ぶと`InvalidOperationException` | `public`なコンストラクタを1つに絞る |
| Play Mode終了後に解除・照会した | `Unregister`、`Destroy`、`IsRegistered`、`TryGet`、`GetRegistrationInfos`、`TryGetRegistrationInfo`は`false`・空一覧を返す | 何もしなくてよい。`OnDestroy`や`OnDisable`で初期化状態を確認する必要は無い |
| 初期化前に登録・必須取得・待機をした | `Register`系、`Get`、`GetAsync`、`TryGetAsync`、`WhenRegistered`が`SymphonyNotInitializedException`を投げる | Edit Modeや、Play Mode開始時の初期化より前から呼んでいないか確かめる |

## 守ること

- 基本形は`OnEnable`で`Register`、`OnDisable`で`Unregister`。`Register`はその場で反映される。
- `ServiceLifetimeEnum.Persistent`はComponentを管理オブジェクトの配下へ移すため、シーンローカルなオブジェクトには使わない。
- 任意の依存は`TryGet<T>`、必須の依存は`Get<T>`を使う。`null`を返す取得は無い。
- 実行時に`Instantiate`したオブジェクトには`Partitura.Inject(...)`を自分で呼ぶ。`Attacca`が自動で注入するのは、ロードしたシーンのルートにある`IInjectable<T...>`だけである。
- `CreateInstance`の戻り値を共有したい場合は、自分で`Register`へ渡す。
- 6.x の`ServiceLocator`・`ServiceInjector`からの移行方法（名前の対応表）は[CHANGELOG.md](../../CHANGELOG.md)の`### Breaking`にある。

## Editor機能

### Partitura パネル

**入口**: `Window > SymphonyFramework > Symphony Administrator`

| パネル | 内容 |
| --- | --- |
| Partitura | 登録済みインスタンスの一覧と登録状態 |

Play Mode中のみ内容を持ちます。Edit Modeでは未接続状態を表示します。

### ログ設定

**入口**: `Project Settings > SymphonyFramework`

| 項目 | 内容 |
| --- | --- |
| Register | インスタンスの登録ログを出力するか |
| Get | インスタンスの取得ログを出力するか |
| Unregister / Destroy | インスタンスの解除・破棄ログを出力するか |

**保存先**: `UserSettings/SymphonyFramework/SymphonyUserSettingConfig.asset`。開発者ごとの設定のため、**版管理へ含めないでください。**

## 確かめ方

- Play Mode中に`Partitura`パネルを開き、登録した型が一覧に出ることを確かめる。
- 登録や取得の順序を追う場合は、ログ設定の`Register`と`Get`を有効にしてConsoleを見る。
- コードから確かめる場合は`Partitura.GetRegistrationInfos()`の結果をログへ出す。

## 関連

- [Attacca](./Attacca.md)
- [Architecture.md](../Architecture.md)
- [Deprecations.md](../Deprecations.md)
- [CHANGELOG.md](../../CHANGELOG.md)
