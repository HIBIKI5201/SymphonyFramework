# Debug

Game ViewのHUD、ログ、処理時間の計測、Runtime状態の読み取りを提供します。

## 目的と範囲

開発中に状態を見たり記録したりする道具を、利用側で作らずに使えるようにします。

- **すること**: FPS・メモリ・任意テキストのHUD表示、複数行ログとファイル出力、ID単位の処理時間計測、nullの診断、MCPや自動化スクリプト向けの状態の読み取り
- **しないこと**: リリースビルドでのHUD表示（Development Buildだけ）、ログの外部送信、ログの機密値の除去

## 入口

| 項目 | 内容 |
| --- | --- |
| namespace | `SymphonyFramework.Debugger.HUD`、`SymphonyFramework.Debugger.Logger`、`SymphonyFramework.Debugger`、`SymphonyFramework.Editor.Debugger` |
| 主な公開型 | `SymphonyDebugHUD` / `SymphonyDebugLogger` / `SymphonyStopWatch` / `LogKindEnum` / `SymphonyMcpTools` |
| HUD Shortcut | `Shift + D + P`（`Project Settings > SymphonyFramework`で変更可能） |
| メニューパス | `Tools > SymphonyFramework > SymphonyDebugHUD > Show` / `Hide` |
| 出力先 | `<Project>/Library/SymphonyFramework/Cache/Log.txt`（`SymphonyDebugLogger`のファイル出力） |

## 仕組みの要点

1. **HUDはUnity EditorとDevelopment Buildでだけ動きます。** それ以外のPlayerでは`Show`、`Hide`、`AddText`、`RemoveText`は何もせず、HUD用のGameObjectも作りません。
2. **Frameworkのログはすべて`SymphonyDebugLogger`を通ります。** Consoleに出たFrameworkのログは、Editorでは`Log.txt`にも残ります。
3. **エラーログには`[SymphonyFramework v<版>]`が付きます。** 不具合の報告で、ログ1行から版が分かるようにするためです。通常ログと警告には付きません。
4. **`SymphonyMcpTools`は読み取り専用です。** 戻り値はいつも有効なJSON文字列で、Play Mode外や未初期化のときもその旨をJSONで返します。

## 機能一覧

| ID | 機能 | 使うAPI | 結果 |
| --- | --- | --- | --- |
| DBG-01 | HUDの表示 | `SymphonyDebugHUD.Show` / `Hide`、`Shift + D + P` | FPS、メモリ、登録したテキストをGame Viewへ重ねる |
| DBG-02 | HUDへのテキスト追加 | `SymphonyDebugHUD.AddText` / `RemoveText` | テキストを登録する。表示は切り替えない。`Hide`後も残る |
| DBG-03 | 1行ログ | `SymphonyDebugLogger.LogDirect` | ConsoleとEditorのファイルへ出す |
| DBG-04 | 複数行ログ | `NewText` / `AddText` / `LogText` | 溜めた行を1件として出す |
| DBG-05 | 例外ログ | `SymphonyDebugLogger.LogException` | Consoleへはスタックトレース付き、ファイルへは型名と理由を1行で出す |
| DBG-06 | nullの診断 | `LogAndCheckComponentNull`（拡張メソッド） | nullなら警告を出して`true`を返す |
| DBG-07 | 処理時間の計測 | `SymphonyStopWatch.Start` / `Stop` | IDごとの経過時間を`Stop`でログへ出す |
| DBG-08 | 状態の読み取り | `SymphonyMcpTools`の各メソッド | Partitura、Scene Loader、Save Dataなどの状態をJSONで返す |
| DBG-09 | ログファイルの読み取り | `SymphonyMcpTools.GetLogFileJson(lines)` | 書き込み待ちを出し切ってから、直近の行（既定200、1〜1000）をJSONで返す |

## クイックスタート

```csharp
using SymphonyFramework.Debugger;
using SymphonyFramework.Debugger.Logger;

SymphonyDebugLogger.LogDirect("初期化しました");

SymphonyDebugLogger.NewText("ロード結果");
SymphonyDebugLogger.AddText("Scene: Game");
SymphonyDebugLogger.AddText("Progress: 100%");
SymphonyDebugLogger.LogText();

SymphonyStopWatch.Start("SceneLoad");
// 計測したい処理
SymphonyStopWatch.Stop("SceneLoad");

if (_renderer.LogAndCheckComponentNull()) { return; }
```

## うまくいかないとき

| 起きること | この機能の動き | 対処 |
| --- | --- | --- |
| HUDが出ない | `AddText`だけでは表示しない。Development Buildでないと何もしない | `Shift + D + P`か`Show()`で表示する。Playerでは Development Build にする |
| `Destroy`済みのオブジェクトを診断した | `UnityEngine.Object`ならnullとして警告し`true`を返す | 意図どおり。Unityオブジェクト以外は通常の参照比較になる |
| ファイルにスタックトレースが無い | `LogException`はファイルへ1行だけ書く | スタックトレースはConsoleで見る |
| 起動直後のFrameworkのログがファイルに無い | Core層のログは、上位層の初期化前だとUnity標準の出力へ落ちる | Consoleで見る |
| ログファイルがまだ無い | `GetLogFileJson`が`exists: false`と空配列を返す。読めないときは`error`を含める | Editorでログが出た後に読む |

## 守ること

- ログにパスワード、トークン、個人情報、セーブデータの機密値を出さない。`SymphonyMcpTools`から外部の自動化へ同じ内容が渡る。
- `Library/SymphonyFramework/Cache/`は生成物である。`Library`とともに版管理の対象外にする。
- 旧APIの`CheckComponentNull`と`IsComponentNotNull`は非推奨である。移行先は[Deprecations.md](../Deprecations.md)にある。

## Editor機能

| 機能 | 入口 | 内容 |
| --- | --- | --- |
| Debug HUD Shortcut | `Project Settings > SymphonyFramework > Debug HUD Shortcut` | HUDのShortcutをInput SystemのInput Actionとして編集する。Gamepadなどの Binding も足せる |
| Debug HUD パネル | `Window > SymphonyFramework > Symphony Administrator > Debug HUD` | 初期化状態、利用できる環境か、表示中か、追加テキストの数を見て、表示・非表示を切り替える |
| ログのファイル出力 | 自動（Editorの起動時に始まる） | `<Project>/Library/SymphonyFramework/Cache/Log.txt`へ定期的にまとめて書く。UPM経由でもAssets直置きでも同じ場所 |

Debug HUDパネルはEdit Modeでは`-`を表示し、Show / Hideは押せません。通常ビルドの条件では`Available: False`になり、ボタンは無効です。

## 確かめ方

- Play Modeで`Shift + D + P`を押し、HUDが出ることを確かめる。
- ログは`Log.txt`を開くか、`SymphonyMcpTools.GetLogFileJson(50)`で直近の行を読む。
- 人が状態を確かめるときはSymphony Administrator、自動化から確かめるときは`SymphonyMcpTools`を使う。

## 関連

- [Partitura](./Partitura.md)
- [Scene Loader](./SceneLoader.md)
- [Save Data System](./SaveDataSystem.md)
- [Deprecations.md](../Deprecations.md)
- [CHANGELOG.md](../../CHANGELOG.md)
