# Service Locator

共有したいインスタンスを型で登録し、必要な場所から型で取得します。

## 目的と範囲

シーンやクラスをまたいで使うインスタンスを、参照を手で受け渡さずに取り出せるようにします。

- **すること**: インスタンスの登録・解除・取得、登録の待機、コンストラクタ・`IInjectable`への依存の注入、登録状態の一覧
- **しないこと**: 依存の再帰的な生成（登録済みのインスタンスだけを渡す）、`CreateInstance`で作ったインスタンスの自動登録、シーンのロード（[Scene Loader](./SceneLoader.md)）

## 入口

| 項目 | 内容 |
| --- | --- |
| namespace | `SymphonyFrameWork.System.ServiceLocate` |
| 主な公開型 | `ServiceLocator` / `ServiceInjector` / `ServiceLocateComponent` / `SymphonyLocateObject<T>` / `IInjectable` / `LocateTypeEnum` / `ServiceRegistrationInfo` / `ServiceNotRegisteredException` |
| メニューパス | `Window > SymphonyFrameWork > Symphony Administrator` の `Service Locate` パネル |
| 設定の保存先 | `UserSettings/SymphonyFrameWork/SymphonyUserSettingConfig.asset`（Service Locatorのログ設定） |

## 仕組みの要点

1. **1つの型につき1つのインスタンスを登録します。** 同じ型を2つ目に登録しようとすると`RegisterInstance`は`false`を返し、候補は呼び出し側の持ち物のまま残ります。
2. **登録方式は2つです。** `LocateTypeEnum.Locator`は参照だけを登録し、`LocateTypeEnum.Singleton`はComponentを永続管理オブジェクトの配下へ移します。
3. **注入の経路は、生成を誰が握るかで選びます。** 自分で生成するピュアC#の型は`ServiceInjector.CreateInstance<T>()`、Unityが生成する`MonoBehaviour`は`IInjectable<T...>`、実行時に`Instantiate`したものは`ServiceInjector.Inject(...)`です。
4. **登録状態はスナップショットで読みます。** `GetRegistrationInfos`と`TryGetRegistrationInfo`が返す`ServiceRegistrationInfo`は、取得した時点の値から変わりません。

## 機能一覧

| ID | 機能 | 使うAPI | 結果 |
| --- | --- | --- | --- |
| SL-01 | 登録と解除 | `RegisterInstance` / `UnregisterInstance` / `DestroyInstance` | 型ごとに1件を登録する。`DestroyInstance`は登録を解除し、インスタンスも解放する |
| SL-02 | 重複時の自動解放 | `RegisterInstanceWithAutoDispose` | 型が重複したとき、候補を`Dispose`またはGameObject破棄する |
| SL-03 | 取得 | `GetRequiredInstance` / `TryGetInstance` / `GetInstance` | 必須なら例外、任意なら`false`、互換用は`null`を返す |
| SL-04 | 登録の待機 | `GetInstanceAsync` / `TryGetInstanceAsync` / `RegisterAfterLocate` | 登録されるまで待つ、または登録後に処理を呼ぶ |
| SL-05 | コンストラクタ注入 | `ServiceInjector.CreateInstance<T>()` | 引数を登録済みのインスタンスから解決して生成する |
| SL-06 | `IInjectable`への注入 | `ServiceInjector.Inject(...)` | `Inject`メソッドへ登録済みのインスタンスを渡す。シーンのルートには`SceneLoader`が自動で行う |
| SL-07 | Inspectorからの登録 | `ServiceLocateComponent` | `Auto Register`を有効にしたComponentを、同じフレームの`Start`より前に登録する |
| SL-08 | 取得結果のキャッシュ | `SymphonyLocateObject<T>` | 初回だけ`ServiceLocator`から取得し、以降は保持した参照を返す。保持した参照が破棄済みなら取り直す |
| SL-09 | 登録状態の一覧 | `GetRegistrationInfos` / `TryGetRegistrationInfo` / `IsExistInstance` | 型、インスタンス、登録方式のスナップショットを返す |

## クイックスタート

登録と解除は、同じライフサイクルの対として書きます。

```csharp
using SymphonyFrameWork.System.ServiceLocate;
using UnityEngine;

public sealed class GameSession : MonoBehaviour
{
    private void OnEnable()
    {
        ServiceLocator.RegisterInstance(this, LocateTypeEnum.Locator);
    }

    private void OnDisable()
    {
        ServiceLocator.UnregisterInstance(this);
    }
}
```

```csharp
GameSession session = ServiceLocator.GetRequiredInstance<GameSession>();
GameSession awaited = await ServiceLocator.GetInstanceAsync<GameSession>(
    grace: 10,
    token: destroyCancellationToken);
```

ピュアC#の型は`CreateInstance`で生成すると、コンストラクタの引数が登録済みのインスタンスから解決されます。

```csharp
public sealed class BattleService
{
    private readonly SaveStore _saveStore;
    private readonly AudioPlayer _audioPlayer;

    // publicなコンストラクタのうち、引数が最も多いものが使われる。
    // 既定値を持つ引数は、登録が無ければ既定値になる。
    public BattleService(SaveStore saveStore, AudioPlayer audioPlayer, int retryCount = 3)
    {
        _saveStore = saveStore;
        _audioPlayer = audioPlayer;
    }
}

BattleService service = ServiceInjector.CreateInstance<BattleService>();
```

## うまくいかないとき

| 起きること | この機能の動き | 対処 |
| --- | --- | --- |
| 同じ型を2回登録した | `RegisterInstance`が`false`を返す。候補は解放しない | 古い登録を解除してから登録する。候補を捨ててよいなら`RegisterInstanceWithAutoDispose`を使う |
| 必須の型が未登録 | `GetRequiredInstance`が`ServiceNotRegisteredException`を投げる | 任意の依存なら`TryGetInstance`を使う |
| 待機が期限を過ぎた | `GetInstanceAsync`が`TimeoutException`を投げる。`TryGetInstanceAsync`は`false`を返す | 登録側が`grace`（秒）の間に登録しているか、Service Locateパネルで確かめる |
| 待機をキャンセルした | `OperationCanceledException`を投げる（`TryGetInstanceAsync`も同じ） | 呼び出し側で捕捉する |
| `CreateInstance`の引数を解決できない | 既定値の無い引数が未登録なら`ServiceNotRegisteredException`。インスタンスは作らない | 依存を先に登録するか、引数へ既定値を付ける |
| `CreateInstance`の候補が決まらない | 引数の数が同じ`public`コンストラクタが並ぶと`InvalidOperationException` | `public`なコンストラクタを1つに絞る |
| Play Mode終了後に解除・照会した | `UnregisterInstance`、`DestroyInstance`、`IsExistInstance`、`GetInstance`、`TryGetInstance`、`GetRegistrationInfos`、`TryGetRegistrationInfo`は`false`・`null`・空一覧を返す | 何もしなくてよい。`OnDestroy`や`OnDisable`で初期化状態を確認する必要は無い |
| 初期化前に登録・必須取得・待機をした | `RegisterInstance`系、`GetRequiredInstance`、`GetInstanceAsync`、`TryGetInstanceAsync`、`RegisterAfterLocate`が`SymphonyNotInitializedException`を投げる | Edit Modeや、Play Mode開始時の初期化より前から呼んでいないか確かめる |

## 守ること

- 基本形は`OnEnable`で`RegisterInstance`、`OnDisable`で`UnregisterInstance`。`RegisterInstance`はその場で反映される。
- `LocateTypeEnum.Singleton`はComponentを管理オブジェクトの配下へ移すため、シーンローカルなオブジェクトには使わない。
- 任意の依存は`TryGetInstance<T>`、必須の依存は`GetRequiredInstance<T>`を使う。`GetInstance<T>`は`null`を許す既存コード向けである。
- 実行時に`Instantiate`したオブジェクトには`ServiceInjector.Inject(...)`を自分で呼ぶ。`SceneLoader`が自動で注入するのは、ロードしたシーンのルートにある`IInjectable<T...>`だけである。
- `CreateInstance`の戻り値を共有したい場合は、自分で`RegisterInstance`へ渡す。
- `ServiceLocateComponent`・`SymphonyLocateObject<T>`・`IInjectable`は5.0.0でこのnamespaceへ移った。それより前の版からの移行方法は[CHANGELOG.md](../../CHANGELOG.md)の`### Breaking`にある。

## Editor機能

### Service Locate パネル

**入口**: `Window > SymphonyFrameWork > Symphony Administrator`

| パネル | 内容 |
| --- | --- |
| Service Locate | 登録済みインスタンスの一覧と登録状態 |

Play Mode中のみ内容を持ちます。Edit Modeでは未接続状態を表示します。

### ログ設定

**入口**: `Project Settings > SymphonyFrameWork`

| 項目 | 内容 |
| --- | --- |
| Set Instance | インスタンスの登録ログを出力するか |
| Get Instance | インスタンスの取得ログを出力するか |
| Destroy Instance | インスタンスの破棄ログを出力するか |

**保存先**: `UserSettings/SymphonyFrameWork/SymphonyUserSettingConfig.asset`。開発者ごとの設定のため、**版管理へ含めないでください。**

## 確かめ方

- Play Mode中に`Service Locate`パネルを開き、登録した型が一覧に出ることを確かめる。
- 登録や取得の順序を追う場合は、ログ設定の`Set Instance`と`Get Instance`を有効にしてConsoleを見る。
- コードから確かめる場合は`ServiceLocator.GetRegistrationInfos()`の結果をログへ出す。

## 関連

- [Scene Loader](./SceneLoader.md)
- [Architecture.md](../Architecture.md)
- [Deprecations.md](../Deprecations.md)
- [CHANGELOG.md](../../CHANGELOG.md)
