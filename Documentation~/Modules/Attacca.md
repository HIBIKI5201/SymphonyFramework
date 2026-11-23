# Attacca

シーンを1つずつ、または名前を並べてロード・アンロードします。進捗、優先度によるActive Sceneの切り替え、ロードしたシーンへの依存注入と初期化の待機も扱います。名前はイタリア語で「切れ目なく次へ」。楽章の間を止めずに続ける指示のように、シーンを途切れさせずにつなぎます。

## 目的と範囲

シーン遷移の入口を1つにし、ロードしたシーンへの依存注入と初期化の完了までを待てるようにします。

- **すること**: シーン名を指定したロード・アンロード（単体・複数）、進捗の通知、優先度によるActive Sceneの選択、ルートGameObjectへの依存注入と`IInitializeAsync`の待機、追跡中のシーンの状態の照会、Play開始時の初期シーンと初期Suiteのロード
- **しないこと**: 依存関係を持つシーン群のまとめたロード（[Suite](./Suite.md)）、Build Settingsへのシーンの登録、ロード画面の表示

## 入口

| 項目 | 内容 |
| --- | --- |
| namespace | `SymphonyFramework.Scenes` |
| 主な公開型 | `Attacca` / `SceneLoadRequest` / `SceneLoadInfo` / `SceneLoadStateEnum` / `IInitializeAsync` / `SceneInitializationException` |
| メニューパス | `Window > SymphonyFramework > Symphony Administrator` の `Attacca` パネル |
| 設定の保存先 | `Assets/Resources/SymphonyFramework/SceneLoadConfig.asset` |

## 仕組みの要点

1. **シーンはいつもAdditiveでロードします。** `LoadSceneMode.Single`を指定すると、対象をAdditiveでロードした後に他の追跡シーンをアンロードします。
2. **Active Sceneは優先度で決まります。** ロードしたシーンの優先度が今のActive Scene以上ならActiveにします。`SceneLoadRequest`はシーン名と優先度を1つの値として持ちます。
3. **ロードはシーンの初期化が終わるまで完了しません。** ルートGameObjectの`IInjectable<T...>`へ[Partitura](./Partitura.md)から依存を注入し、`IInitializeAsync`の完了を待ってから`true`を返します。
4. **[Suite](./Suite.md)が保持しているシーンは、Attaccaからはアンロードしません。** 警告をログへ出して`false`を返します。Suiteのシーンは`Suite.UnloadAsync`で片付けます。
5. **状態はスナップショットで読みます。** `GetSceneInfos`と`TryGetSceneInfo`が返す`SceneLoadInfo`は、取得した時点の値から変わりません。

## 機能一覧

| ID | 機能 | 使うAPI | 結果 |
| --- | --- | --- | --- |
| SCN-01 | ロード | `LoadAsync`（`SceneLoadRequest` / シーン名 / それぞれの配列） | 成功で`true`。進捗を`IProgress<float>`へ通知する |
| SCN-02 | アンロード | `UnloadAsync`（シーン名 / シーン名の配列） | 成功で`true`。Suiteが保持中のシーンを含む場合は何もせず`false` |
| SCN-03 | Active Sceneの切り替え | `SetActiveScene` | 指定した追跡シーンをActiveにする |
| SCN-04 | ロード済みシーンの追跡 | `RegisterLoadedScene` | `Attacca`を通さずにロードされたシーンを、優先度付きで追跡へ加える |
| SCN-05 | ロード完了の待機 | `WaitForLoadAsync` / `WhenLoaded` | 指定シーンのロード完了まで待つ、または完了後に1回だけ処理を呼ぶ |
| SCN-06 | 状態の照会 | `GetSceneInfos` / `TryGetSceneInfo` / `TryGetState` / `IsLoaded` / `TryGetScene` | 名前、状態、優先度、進捗、Activeかどうかを返す |
| SCN-07 | 初期化の待機 | `IInitializeAsync`（利用側が実装） | ロード完了の前に`InitializeAsync`を待つ |
| SCN-08 | Play開始時の初期シーン処理 | `SceneLoadConfig`、Main Toolbarの`Symphony Framework > Scene Init` | 初期シーン、続いて初期Suiteをロードし、それ以外の追跡シーンをアンロードする |

## クイックスタート

対象シーンをBuild Settingsへ追加してから、名前を指定してロードします。

```csharp
using System;
using SymphonyFramework.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;

public async void OpenGameScene()
{
    var request = new SceneLoadRequest("Game", priority: 10);
    IProgress<float> progress = new Progress<float>(
        value => Debug.Log($"Loading: {value:P0}"));

    bool succeeded = await Attacca.LoadAsync(
        request,
        progress,
        mode: LoadSceneMode.Additive,
        token: destroyCancellationToken);

    if (succeeded)
    {
        Attacca.SetActiveScene("Game");
    }
}
```

複数シーンは`SceneLoadRequest[]`を`LoadAsync`へ渡すと、シーン名と優先度の対応を崩さずにロードできます。

## Play開始時の初期シーン処理

`SceneLoadConfig.asset`の「エディタでの再生時にシーンをリセットしてロードを実行するか」が有効なとき、Play開始直後に次の順で処理します。

1. 「初期化時にロードするシーン」を設定順にロードする。
2. 「初期化時にロードするSuite」を設定順にロードする。
3. 1と2のシーン、「リセットしてもアンロードされないシーン」のどれにも入らない追跡シーンをアンロードする。

1か2のどちらかに失敗した場合は、3のアンロードを行いません。置き換え先が揃わないまま今のシーンを捨てないためです。

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
| Suiteが保持しているシーンをアンロードした | 警告をログへ出し、何もせず`false`を返す | `Suite.UnloadAsync`でSuiteごとアンロードする |
| `SceneManager.LoadScene`で遷移した | 優先度、依存注入、`IInitializeAsync`が働かない | `Attacca`を使う。既にロード済みのシーンは`RegisterLoadedScene`で追跡へ加える |

## 守ること

- シーン名をコードへ書く前に、Build SettingsのScene Listにあることを確かめる。[AutoEnumGenerator](./AutoEnumGenerator.md)の`SceneListEnum`を使うと綴りの誤りを防げる。
- シーン遷移は原則`Attacca`か`Suite`を使う。
- 今のシーンを残したい場合は`LoadSceneMode.Additive`を使う。
- 追跡中のシーン名が分からないときは`GetSceneInfos()`、既知のシーンだけを調べるときは`TryGetSceneInfo`を使う。

## Editor機能

**入口**: `Window > SymphonyFramework > Symphony Administrator`

| パネル・メニュー | 内容 |
| --- | --- |
| Attacca | ロード済みシーンと進行中のロード |
| `SceneLoadConfig`のInspector | 初期シーン、初期Suite、アンロードしないシーンを設定する |
| Main Toolbar の `Symphony Framework > Scene Init` | 次回のPlay Mode開始時に初期シーン処理を実行するか切り替える |

`Attacca`パネルはPlay Mode中のみ内容を持ちます。Edit Modeでは未接続状態を表示します。

`Scene Init`はUnity 6000.3以降で使えます。メインツールバー右側の音符アイコン付き`Symphony Framework`プルダウンから選びます。変更は`SceneLoadConfig.asset`へ保存され、Inspectorの「エディタでの再生時にシーンをリセットしてロードを実行するか」と同じ設定です。Play Mode開始後の変更は、次のPlay Mode開始から反映されます。

## 確かめ方

- Play Mode中に`Attacca`パネルを開き、ロードしたシーンの状態が`Complete`になり、Activeのシーンが想定どおりかを見る。
- コードから確かめる場合は`Attacca.GetSceneInfos()`の結果をログへ出す。

## 関連

- [Partitura](./Partitura.md)
- [Suite](./Suite.md)
- [Architecture.md](../Architecture.md)
- [Deprecations.md](../Deprecations.md)
- [CHANGELOG.md](../../CHANGELOG.md)
