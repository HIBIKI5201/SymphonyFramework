# Debug

Game ViewのHUD、ログ、処理時間計測、Runtime状態の診断を提供します。

## 入口

| 項目 | 内容 |
| --- | --- |
| namespace | `SymphonyFrameWork.Debugger.HUD`、`SymphonyFrameWork.Debugger.Logger`、`SymphonyFrameWork.Debugger`、`SymphonyFrameWork.Editor.Debugger` |
| 主な公開型 | `SymphonyDebugHUD` / `SymphonyDebugLogger` / `SymphonyStopWatch` / `LogKindEnum` / `SymphonyMcpTools` |
| メニューパス | `Tools > SymphonyFrameWork > SymphonyDebugHUD > Show` / `Hide` |
| 出力先 | `<Frameworkルート>/Cache/Log.txt`（`SymphonyDebugLogger`のファイル出力） |

| 型 | 用途 |
| --- | --- |
| `SymphonyDebugHUD` | FPS、メモリ使用量、任意テキストのGame View表示 |
| `SymphonyDebugLogger` | 複数行ログとEditorでの`Cache/Log.txt`出力 |
| `SymphonyStopWatch` | ID単位の簡易処理時間計測 |
| `SymphonyMcpTools` | MCPや自動化スクリプトからのRuntime状態の読み取り |

## クイックスタート

`SymphonyDebugLogger`は、1行ずつ出力する`LogDirect`と、複数行を溜めてからまとめて出力する`AddText`／`NewText`／`LogText`を持ちます。

```csharp
using SymphonyFrameWork.Debugger.Logger;

SymphonyDebugLogger.LogDirect("初期化しました");

SymphonyDebugLogger.NewText("ロード結果");
SymphonyDebugLogger.AddText("Scene: Game");
SymphonyDebugLogger.AddText("Progress: 100%");
SymphonyDebugLogger.LogText();
```

例外は`LogException`で出力します。Unity Consoleへはスタックトレース付きの例外として出し、ファイル出力へは型名と理由を1行で残します。

```csharp
try { /* 失敗しうる処理 */ }
catch (Exception exception) { SymphonyDebugLogger.LogException(exception); }
```

`SymphonyStopWatch`はIDで計測区間を識別します。`Stop`でログへ出力されます。

```csharp
using SymphonyFrameWork.Debugger;

SymphonyStopWatch.Start("SceneLoad");
// 計測したい処理
SymphonyStopWatch.Stop("SceneLoad");
```

## 実装時の注意

### nullの診断には`LogAndCheckComponentNull`を使う

参照が使えるかを警告付きで確認するには、拡張メソッドの`LogAndCheckComponentNull`を使います。nullなら警告を出したうえで`true`を返します。

```csharp
using SymphonyFrameWork.Debugger.Logger;

if (_renderer.LogAndCheckComponentNull()) { return; }
```

**`UnityEngine.Object`を渡した場合は、`Destroy`済みの参照もnullとして報告します。** マネージド参照が生きていても、Unityの比較演算子がnullとみなす状態なら`true`です。Unityオブジェクト以外の参照は通常の参照比較で判定します。

旧APIの`CheckComponentNull`と`IsComponentNotNull`は非推奨です。移行先と削除予定は[Deprecations.md](../Deprecations.md)にあります。

## Editor機能

### SymphonyDebugHUD

FPS、メモリ使用量、任意テキストをGame Viewへ重ねて表示します。

**入口**: `Tools > SymphonyFrameWork > SymphonyDebugHUD > Show` / `Hide`

HUD本体はRuntimeの機能です。このメニューはEditorからの表示・非表示の入口だけを提供します。

### ログのファイル出力

`SymphonyDebugLogger` の出力をファイルへ書き出します。

**入口**: 自動実行（`SymphonyEditorOrchestrator` が起動時に開始します）

**出力先**: `<Frameworkルート>/Cache/Log.txt`

Frameworkの実配置パスを解決してその直下へ書くため、UPM経由（`Library/PackageCache/`）でもAssets直置きでも同じ位置関係になります。

**注意点**:

- `Cache/` は生成物です。**版管理へ含めないでください。**
- 書き込みは定期的にまとめて行われます。1行ごとにファイルを開きません。
- **Framework自身が出すログは、すべてこの経路を通ります。** Framework内のコードは`UnityEngine.Debug`を直接呼びません。Consoleに出たFrameworkのログは`Log.txt`にも残っている、と考えて構いません。
- **`LogException`はスタックトレースをファイルへ書きません。** 1件を1行として書き出すためで、型名と理由だけが残ります。スタックトレースはConsoleで確認してください。
- Core層（`SymphonyFrameWork.Core`アセンブリ）はRuntimeのロガーを参照できないため、内部の中継口を経由してこの出力先へ載ります。上位層の初期化前に発生したCore層のログだけは、Unity標準の出力へ落ちて`Log.txt`に残りません。

### SymphonyMcpTools

MCPや自動化スクリプトから、SymphonyのRuntime状態をJSON文字列で読み取るためのツール群です。

**入口**: メニューはありません。外部ツールから `SymphonyFrameWork.Editor.Debugger.SymphonyMcpTools` の各メソッドを呼びます。

Service Locator、Scene Loader、Save Dataなどの状態を取得できます。戻り値は必ず有効なJSON文字列です。Play Mode外や未初期化の場合も、その旨を含むJSONを返します。

**注意点**:

- 読み取り専用です。Runtimeの状態を変更しません。
- 人が状態を確認する場合はSymphony Administratorを使ってください。

## 関連

- [Service Locator](./ServiceLocator.md)
- [Scene Loader](./SceneLoader.md)
- [Save Data System](./SaveDataSystem.md)
- [Deprecations.md](../Deprecations.md)
- [CHANGELOG.md](../../CHANGELOG.md)
