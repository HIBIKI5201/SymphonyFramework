# AIエージェント向け検証手順

この文書は、Symphony Frameworkを利用するコードの検証手順をまとめたものです。パッケージ本体ではなく、利用側プロジェクトの`Assets/`配下で確認します。

## 基本方針

1. 既存のサンプルで確認できる場合は、独自の検証コードよりサンプルを優先する。
2. 独自コードが必要なら`Assets/Scripts/_SymphonyVerify/`など用途が分かる一時フォルダへ置く。
3. コンパイル、Play Mode、必要ならビルドの順に確認する。
4. 検証後は一時コードと生成したテストデータを削除する。

パッケージ本体（`Packages/symphonyframework/`または`Assets/SymphonyFrameWork/`）へ検証コードを書き込まないでください。

## 1. コンパイルとasmdef

- 利用側asmdefが`SymphonyFrameWork`を参照していることを確認する。
- 自動生成enumを使う場合だけ`SymphonyFrameWork.Enum`も参照する。
- 利用するモジュールの型が解決でき、コンパイルエラーがないことを確認する。

最小の型解決確認では、Editor専用スクリプトから次の型を参照すれば主要なRuntimeアセンブリとnamespaceを確認できます。

```csharp
Debug.Log(typeof(ServiceLocator).FullName);
Debug.Log(typeof(SceneLoader).FullName);
Debug.Log(typeof(AudioManager).FullName);
Debug.Log(typeof(SaveStore).FullName);
```

型を解決できない場合は、まずasmdef参照と`using`を確認してください。各namespaceは[AgentUsage.md](./AgentUsage.md#apiの参照先)にあります。

## 2. 設定アセットと自動生成コード

コンパイルまたはPlay Mode開始後、利用する機能に必要なファイルが生成されているか確認します。

```text
Assets/Resources/SymphonyFrameWork/SceneLoadConfig.asset
Assets/Resources/SymphonyFrameWork/AudioConfig.asset
Assets/Resources/SymphonyFrameWork/SaveDataConfig.asset
Assets/Scripts/SymphonyFrameWork/SceneListEnum.cs
Assets/Scripts/SymphonyFrameWork/TagsEnum.cs
Assets/Scripts/SymphonyFrameWork/LayersEnum.cs
Assets/Scripts/SymphonyFrameWork/AudioGroupTypeEnum.cs
```

存在しない場合は`Window > SymphonyFrameWork > Symphony Administrator`を開くか、Play Modeへ入って生成処理を実行します。生成物は手で編集しません。

## 3. Play Mode

Package Managerから該当する[`Samples/Runtime`](../Samples/Runtime)をインポートし、サンプルシーンをPlayするのが最短です。

- Service Locator: 登録、取得、解除が成立し、シーンを跨いだ不要な参照が残らない。
- Scene Loader: 対象シーンをBuild Settingsへ追加し、ロード、Active Scene切り替え、アンロードが成功する。
- Save Data System: 保存、再ロード、削除が成功し、検証後にデータを削除する。
- Audio Manager: AudioMixerとグループを設定し、再生と音量変更を確認する。
- Pause Manager: ポーズ中に対応待機が進まず、解除後に再開する。

利用プロジェクトでDomain Reloadを無効にしている場合は、Play Modeの開始・終了を2回繰り返し、static状態にゴースト参照が残らないことも確認します。

## 4. Editorから内部状態を確認する

uLoopMCPの`execute-dynamic-code`などEditorコードを実行できる環境では、`SymphonyMcpTools`から状態をJSONで取得できます。状態確認だけを目的とし、Runtimeコードへ組み込まないでください。

```csharp
using SymphonyFrameWork.Editor.Debugger;

string serviceLocatorJson = SymphonyMcpTools.GetServiceLocatorJson();
string sceneLoaderJson = SymphonyMcpTools.GetSceneLoaderJson();
string saveDataJson = SymphonyMcpTools.GetSaveDataJson();
string pauseJson = SymphonyMcpTools.GetPauseJson();
```

- 未初期化時は例外ではなく`"initialized": false`を含むJSONを返す。
- 読み取り失敗時は`"error"`を含むJSONを返す。
- Save Dataの出力に`SaveDataContent`の内容は含まれない。
- `SymphonyMcpTools`はEditorアセンブリ専用であり、Playerビルドから参照できない。
- Service Locatorの`effectiveLocateType`は、登録時の値ではなく現在の状態から導いた実効値。

## 5. ビルド

- フレームワーク管理用のBootstrapシーンは不要。
- `SceneLoader`で遷移するシーンはBuild Settingsへ含める。
- `Scene 'X' couldn't be loaded`が出た場合は、最初にScene Listへの登録を確認する。
- Runtime asmdefから`SymphonyFrameWork.Editor`を参照していないことを確認する。

## 6. 後始末

- `Assets/Scripts/_SymphonyVerify/`などの一時コードと対応する`.meta`を削除する。
- 検証用のセーブデータ、シーン、設定変更を残さない。
- パッケージ本体に差分がないことを確認する。
