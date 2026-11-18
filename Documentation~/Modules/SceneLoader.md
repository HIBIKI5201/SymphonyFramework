# Scene Loader

シーンの非同期ロードとアンロード、進捗、複数シーン、優先度によるActive Sceneの切り替えをまとめて扱います。

## 目的と範囲

シーン遷移の入口を1つにし、ロードしたシーンへの依存注入と初期化の完了までを待てるようにします。

- **すること**: シーン名を指定したロード・アンロード（単体・複数）、進捗の通知、優先度によるActive Sceneの選択、ルートGameObjectへの依存注入と`IInitializeAsync`の待機、追跡中のシーンの状態の照会
- **しないこと**: 依存関係を持つシーン群のまとめたロード（[Scene Block](./SceneBlock.md)）、Build Settingsへのシーンの登録、ロード画面の表示

## 入口

| 項目 | 内容 |
| --- | --- |
| namespace | `SymphonyFramework.System.SceneLoad` |
| 主な公開型 | `SceneLoader` / `SceneLoadRequest` / `SceneLoadInfo` / `SceneLoadStateEnum` / `IInitializeAsync` / `SceneInitializationException` |
| メニューパス | `Window > SymphonyFramework > Symphony Administrator` の `Scene Load` パネル |
| 設定の保存先 | `Assets/Resources/SymphonyFramework/SceneLoadConfig.asset` |

## 仕組みの要点

1. **シーンはいつもAdditiveでロードします。** `LoadSceneMode.Single`を指定すると、対象をAdditiveでロードした後に他の追跡シーンをアンロードします。
2. **Active Sceneは優先度で決まります。** ロードしたシーンの優先度が今のActive Scene以上ならActiveにします。`SceneLoadRequest`はシーン名と優先度を1つの値として持ちます。
3. **ロードはシーンの初期化が終わるまで完了しません。** ルートGameObjectの`IInjectable<T...>`へ[Partitura](./Partitura.md)から依存を注入し、`IInitializeAsync`の完了を待ってから`true`を返します。
4. **状態はスナップショットで読みます。** `GetSceneInfos`と`TryGetSceneInfo`が返す`SceneLoadInfo`は、取得した時点の値から変わりません。

## 機能一覧

| ID | 機能 | 使うAPI | 結果 |
| --- | --- | --- | --- |
| SCN-01 | ロード | `LoadSceneAsync` / `LoadScenesAsync` | 成功で`true`。進捗を`IProgress<float>`へ通知する |
| SCN-02 | アンロード | `UnloadSceneAsync` / `UnloadScenesAsync` | 成功で`true` |
| SCN-03 | Active Sceneの切り替え | `SetActiveScene` | 指定した追跡シーンをActiveにする |
| SCN-04 | ロード済みシーンの追跡 | `RegisterLoadedScene` | `SceneLoader`を通さずにロードされたシーンを、優先度付きで追跡へ加える |
| SCN-05 | ロード完了の待機 | `WaitForLoadSceneAsync` / `RegisterAfterSceneLoad` | 指定シーンのロード完了まで待つ、または完了後に1回だけ処理を呼ぶ |
| SCN-06 | 状態の照会 | `GetSceneInfos` / `TryGetSceneInfo` / `TryGetState` / `IsExist` / `GetExistScene` | 名前、状態、優先度、進捗、Activeかどうかを返す |
| SCN-07 | 初期化の待機 | `IInitializeAsync`（利用側が実装） | ロード完了の前に`InitializeAsync`を待つ |
| SCN-08 | Play開始時の初期シーン処理 | Main Toolbarの`Symphony Framework > Scene Init` | 次回のPlay Mode開始時に、初期シーンの処理を行うか切り替える |

## クイックスタート

対象シーンをBuild Settingsへ追加してから、名前を指定してロードします。

```csharp
using System;
using SymphonyFramework.System.SceneLoad;
using UnityEngine;
using UnityEngine.SceneManagement;

public async void OpenGameScene()
{
    var request = new SceneLoadRequest("Game", priority: 10);
    IProgress<float> progress = new Progress<float>(
        value => Debug.Log($"Loading: {value:P0}"));

    bool succeeded = await SceneLoader.LoadSceneAsync(
        request,
        progress,
        mode: LoadSceneMode.Additive,
        token: destroyCancellationToken);

    if (succeeded)
    {
        SceneLoader.SetActiveScene("Game");
    }
}
```

複数シーンは`SceneLoadRequest[]`を`LoadScenesAsync`へ渡すと、シーン名と優先度の対応を崩さずにロードできます。シーン名と`Action<float>`を受け取るoverloadも互換のために残っています。

## 状態

`SceneLoadInfo.State`は`SceneLoadStateEnum`の値をとります。

| 状態 | 意味 |
| --- | --- |
| `None` | 追跡していない |
| `Loading` | ロード中 |
| `Complete` | ロードが完了した |
| `Unloading` | アンロード中 |

## うまくいかないとき

| 起きること | この機能の動き | 対処 |
| --- | --- | --- |
| シーンがBuild Settingsに無い | `false`を返す | Build SettingsのScene Listへ追加する |
| 依存注入か`IInitializeAsync`が失敗した | `SceneInitializationException`を投げる | 内側の例外を見て、注入する型が登録済みか、初期化処理が例外を投げていないかを確かめる |
| シーン名がnull・空・空白 | `ArgumentException`を投げる | 呼び出し側の値を直す |
| `SceneManager.LoadScene`で遷移した | 優先度、依存注入、`IInitializeAsync`が働かない | `SceneLoader`を使う。既にロード済みのシーンは`RegisterLoadedScene`で追跡へ加える |

## 守ること

- シーン名をコードへ書く前に、Build SettingsのScene Listにあることを確かめる。[AutoEnumGenerator](./AutoEnumGenerator.md)の`SceneListEnum`を使うと綴りの誤りを防げる。
- シーン遷移は原則`SceneLoader`を使う。
- 今のシーンを残したい場合は`LoadSceneMode.Additive`を使う。
- 追跡中のシーン名が分からないときは`GetSceneInfos()`、既知のシーンだけを調べるときは`TryGetSceneInfo`を使う。

## Editor機能

**入口**: `Window > SymphonyFramework > Symphony Administrator`

| パネル・メニュー | 内容 |
| --- | --- |
| Scene Load | ロード済みシーンと進行中のロード |
| Main Toolbar の `Symphony Framework > Scene Init` | 次回のPlay Mode開始時に初期シーン処理を実行するか切り替える |

`Scene Load`パネルはPlay Mode中のみ内容を持ちます。Edit Modeでは未接続状態を表示します。

`Scene Init`はUnity 6000.3以降で使えます。メインツールバー右側の音符アイコン付き`Symphony Framework`プルダウンから選びます。変更は`SceneLoadConfig.asset`へ保存され、Inspectorの「エディタでの再生時にシーンをリセットしてロードを実行するか」と同じ設定です。Play Mode開始後の変更は、次のPlay Mode開始から反映されます。

## 確かめ方

- Play Mode中に`Scene Load`パネルを開き、ロードしたシーンの状態が`Complete`になり、Activeのシーンが想定どおりかを見る。
- コードから確かめる場合は`SceneLoader.GetSceneInfos()`の結果をログへ出す。

## 関連

- [Partitura](./Partitura.md)
- [Scene Block](./SceneBlock.md)
- [Architecture.md](../Architecture.md)
- [Deprecations.md](../Deprecations.md)
- [CHANGELOG.md](../../CHANGELOG.md)
