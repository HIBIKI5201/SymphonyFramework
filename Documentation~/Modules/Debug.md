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

`SymphonyStopWatch`はIDで計測区間を識別します。`Stop`でログへ出力されます。

```csharp
using SymphonyFrameWork.Debugger;

SymphonyStopWatch.Start("SceneLoad");
// 計測したい処理
SymphonyStopWatch.Stop("SceneLoad");
```

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
