# Symphony Framework 設計思想

このドキュメントは、Symphony Frameworkの設計判断に共通する価値観、依存方向、責務分割、初期化とライフサイクルの考え方を定義します。具体的な書式や命名は [`CodeGuidelines.md`](./CodeGuidelines.md) を参照してください。

既存実装と異なる箇所は、互換性を壊して一括変更するのではなく、新機能と改修対象から段階的に適用します。

## 目標

Symphony Frameworkは、次の性質を持つUnity向け基盤を目指します。

- プロジェクト規模が大きくなっても機能を分離して拡張できる。
- ゲーム仕様、保存先、入力元、表示方法などの変更へ柔軟に対応できる。
- 複数人が並行して変更しても、責務と影響範囲を判断しやすい。
- Unityの利便性を活かしながら、Unityライフサイクルへの過度な依存を避ける。
- 利用側プロジェクトへ特定のゲーム設計やデータ構造を強制しない。

## 採用する原則

設計判断では、次の原則を目的に応じて使用します。原則を適用すること自体を目的にはしません。

- Clean Architecture: 重要なルールを外部の詳細から守る。
- Domain-Driven Design: 意味のある値と操作を型や語彙として表現する。
- SOLID: 変更理由を分け、抽象を介して依存方向を制御する。
- GRASP: 責務を適切な情報と役割を持つ型へ割り当てる。
- KISS: 将来予測だけを根拠に複雑な抽象を増やさない。
- Command-Query Separation: 状態変更と情報取得を区別する。
- MVVM: UIの状態とUnity上の表示を分離する必要がある場合に使用する。

設計が競合した場合は、正しさ、理解しやすさ、変更容易性、計測済みの性能要件の順で判断します。

## パッケージの依存方向

パッケージ全体では、依存を次の方向に限定します。

```text
Samples ───────┐
               v
Editor ────> Runtime ────> Core

利用側プロジェクト ────> Symphony Framework
```

- `Core` はRuntime、Editor、Samplesへ依存しない。
- `Runtime` はCoreへ依存できるが、Editorへ依存しない。
- `Editor` はCoreとRuntimeへ依存できる。
- `Samples` は公開されたRuntime APIだけを使用し、製品コードから参照されない。
- Symphony Frameworkから利用側プロジェクトの具象型を参照しない。
- 外部パッケージ固有の処理は境界へ寄せ、可能な場合はinterfaceの背後へ隠す。
- asmdefで依存方向を表現し、循環参照を許可しない。

## 概念レイヤー

大きな機能を設計する場合は、次の概念レイヤーで責務を分けます。これは必ず同名のディレクトリを作る規則ではありません。機能の規模が小さい場合は、責務が混ざらない範囲で型数を抑えます。

### Domain

機能の値、状態、不変条件、状態遷移を表します。

- 可能な限りピュアC#で実装する。
- `Vector2` などUnityの値型は利用できるが、`MonoBehaviour` の継承やScene上の存在を前提にしない。
- 外部I/O、Service Locator、Resources、Addressablesを参照しない。
- 不正な状態を生成できないコンストラクタと操作を設計する。

### Application

Domainを組み合わせ、利用者が実行したいユースケースを表します。

- DomainとApplication自身が定義する契約へ依存する。
- 保存、ロード、表示、Unityオブジェクト操作の具象実装へ直接依存しない。
- 入力を検証し、処理結果を戻り値として明確に返す。

### Adaptor

Applicationと外部入力・表示の形式を相互変換します。

- ControllerはCommandを受け、Applicationの処理を呼び出す。
- PresenterはQuery結果をView向けの形式へ変換する。
- DTO、Output Port、View Modelのinterfaceなど、境界を越える契約を定義する。
- ゲームルールを重複実装しない。

### View

GameObject、UI Toolkit、UGUI、AudioSourceなどUnity上の表示と入力を担当します。

- DomainやApplicationを直接変更せず、Adaptorの契約を介する。
- 表示状態はView Modelから受け取り、ユーザー入力はControllerやSignalへ渡す。
- Unityライフサイクルは表示と入力の開始・停止に限定する。

### Infrastructure

永続化、外部パッケージ、データベース、Resources、Addressablesなどの技術的詳細を担当します。

- Applicationなど内側のレイヤーが定義したRepositoryやLoaderのinterfaceを実装する。
- Unityアセットを、内側のレイヤーが理解できる型へ変換する。
- ロードしたハンドル、ストリーム、購読などの所有者と解放方法を明確にする。

### Composition

具象型を生成し、依存性注入、初期化順、公開、終了処理を担当します。

- Domain、Application、Adaptor、View、Infrastructureの具象型を結合する。
- `SymphonyCoreSystem` はパッケージ全体のComposition Rootとして扱う。
- 他レイヤーに具象型の組み立てやService Locator登録を分散させない。
- 初期化に失敗した場合は、部分的に構築された状態を残さない。

## 依存性逆転

内側のルールから外側の詳細を利用したい場合は、内側にinterfaceを定義し、外側が実装します。Compositionが具象実装を注入します。

```text
Application ──定義──> ISaveDataLoader
                           ^
                           │ 実装
Infrastructure ────────────┘
                           ^
                           │ 注入
Composition ───────────────┘
```

- interfaceは、実装を交換する必要、テスト境界、依存方向の逆転がある場所に作る。
- 実装が1つという理由だけでinterfaceを禁止しないが、将来使うかもしれないという理由だけでも追加しない。
- interfaceのメソッドは利用側が必要とする最小単位にする。
- 具象型の詳細をinterfaceの引数や戻り値へ漏らさない。

## クラス設計

### Entity

Entityは、同一性とライフサイクルを持つ可変の参照型です。

- `class` で実装する。
- 状態は読み取り専用プロパティとして公開する。
- public setterを設けず、`ChangeXxx`、`RecordXxx` など意図を表すメソッドで変更する。
- 変更メソッド内で不変条件を守る。

```csharp
public sealed class LoadOperationEntity
{
    public LoadOperationEntity(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("IDを指定してください。", nameof(id));
        }

        Id = id;
    }

    public string Id { get; }
    public float Progress { get; private set; }

    public void ReportProgress(float progress)
    {
        Progress = Mathf.Clamp01(progress);
    }
}
```

### Value Object

Value Objectは、値そのものに意味があり、不変である型です。

- 原則として `readonly struct` で実装する。
- コンストラクタで範囲、null、有限値などを検証する。
- 値として比較する必要がある場合は `IEquatable<T>` を実装する。
- 順序に意味がある場合だけ `IComparable<T>` と比較演算子を実装する。
- 単位や制約が異なる値を、同じprimitive型のまま受け渡さない。

### DTO

DTOは、境界を越えて更新データを渡す不変の値です。

- 原則として `readonly struct` を使用する。
- 同期呼び出しの一時データで、ヒープ保持や非同期境界を越えないことが保証される場合は `readonly ref struct` を使用できる。
- DTOへロジックや外部参照を持たせない。
- Viewへ渡す場合は、必要な表示データだけを含める。

### RepositoryとLoader

- Repositoryは、呼び出し側にとってのデータ集合へのアクセスを表す。
- Loaderは、特定データの読み書きや変換手順を表す。
- 契約は利用する内側のレイヤー、具象実装はInfrastructureへ置く。
- キャッシュする場合は、生成、更新、無効化、解放の条件をAPIとして明確にする。

### Use Case、Service、Factory

- Use Caseは、利用者が達成したい1つの操作をApplication上で表す。
- Serviceは、1つのEntityへ自然に属さないDomainまたはApplicationの処理を担当する。
- Factoryは、生成手順や不変条件が複雑なDomain／Applicationオブジェクトを構築する。
- Factoryを単なる `new` の置き換えにせず、生成ルール、実装選択、依存解決がある場合に使用する。
- これらの型からViewやInfrastructureの具象型を生成しない。

### ConfigとAsset

- Configは、表示や技術的機能だけで完結する設定に使用する。
- Assetは、UnityのAuthoringデータをDomainやApplicationの型へ変換する入口に使用する。
- `ScriptableObject` のシリアライズ値は外部から変更させず、読み取り専用プロパティを公開する。
- 種類が増えるデータには抽象基底型とfactory methodを用意し、`[SerializeReference, SubclassSelector]` による選択を検討する。
- Assetから生成したRuntimeオブジェクトへ、変更可能なシリアライズ状態を共有しない。

### PresenterとController

- Presenterは状態を問い合わせ、表示用データへ変換する。原則として状態を変更しない。
- Controllerはユーザー操作や外部イベントをCommandとして受け取り、状態変更を開始する。
- ViewがApplicationの具象型を直接知る必要がない構造にする。

### View Model、Signal、Spawner

- View ModelはUIに表示する状態を保持し、PresenterからDTOを受け取って更新する。
- DTOを同期的に参照するだけの場合は、コピーを避ける必要性を確認した上で `in` 引数を使用できる。
- SignalはView側の入力や表示イベントを境界の外へ通知する。グローバルなEvent Busとして使用しない。
- SpawnerはViewオブジェクトの生成手順を担当し、FactoryとしてPrefab、親Transform、初期表示を組み立てる。
- View ModelとSignalの購読は、Viewの有効期間と対になる解除処理を持つ。

### RegistryとContainer

- Registryは、型やIDに対応するオブジェクトを検索する責務だけを持つ。
- Containerは、1つの機能モジュールが外部へ公開するサービス群をまとめる。
- Containerのプロパティは読み取り専用にし、構築後に依存を差し替えない。
- 巨大なService Locator代替としてContainerへ無関係なサービスを集めない。

### InitializerとDebugger

- InitializerはCompositionに置き、生成、依存性注入、登録、購読を順序立てて行う。
- Debuggerは製品ロジックから分離し、EditorまたはDevelopビルドでだけ診断情報と操作を提供する。
- Debuggerを通して製品状態を変更する場合も、通常のCommandや公開APIを経由し、不変条件を迂回しない。

## CommandとQuery

- Queryは状態を返し、観測可能な状態変更を行わない。
- Commandは状態を変更し、必要な場合だけ成功可否や結果を返す。
- property getterでロード、保存、登録、ログ出力などの副作用を起こさない。
- `GetXxx` と `SetXxx` を機械的に作らず、利用者の意図を表す操作名を選ぶ。
- `TryXxx` は失敗が通常の分岐である場合に使い、例外の代替として乱用しない。

## 初期化ライフサイクル

複数の依存を持つ新しいシステムでは、初期化を次のフェーズへ分けます。

```text
Init → ResourceLoadAsync → Build → Ready

Shutdown ← 登録と購読を逆順に解除
```

| フェーズ | 責務 |
| --- | --- |
| `Init` | 単体で完結する初期値と内部状態の準備 |
| `ResourceLoadAsync` | ファイル、Resources、Addressablesなどの非同期ロード |
| `Build` | 具象型の生成、依存性注入、サービス登録 |
| `Ready` | 他モジュールとの接続、イベント購読、利用開始 |
| `Shutdown` | 購読解除、登録解除、ハンドル解放、破棄 |

- モジュールは名前と実行順を持ち、同じフェーズを実行順に処理する。
- いずれかのフェーズが失敗した場合は後続フェーズを実行しない。
- `Shutdown` は構築順の逆順で実行する。
- 各フェーズは繰り返し呼び出されても二重登録や二重解放を起こさないよう設計する。
- 単純な機能へ形式的な5フェーズを強制しない。状態と依存が複雑になった時点で導入する。

## 依存性注入とService Locator

- ピュアC#クラスのモジュール内依存には、コンストラクタ注入を第一候補とする。
- Inspector参照はViewやCompositionなどUnity境界に限定する。
- `ServiceLocator` はCompositionでのサービス公開と、独立モジュール間の接続に使用する。
- DomainやApplicationの処理途中でService Locatorを直接検索しない。
- 取得には可能な限り `TryGetInstance<T>` を使用し、初期化失敗を明示的に処理する。
- 登録した型と所有者を記録し、`Shutdown` または対応するUnityライフサイクルで登録解除する。
- 複数サービスを公開するモジュールでは、機能単位のContainerにまとめる。

## Unityとの境界

UnityEngineの型を使うことと、Unityライフサイクルへ依存することを区別します。

- DomainやApplicationで `Vector2`、`Color` などの値型を使うことは許容する。
- `MonoBehaviour`、Scene、GameObjectの存在を前提にする処理はViewまたはCompositionへ置く。
- Unityコールバックは処理の入口とし、重要なロジックを通常のC#メソッドへ委譲する。
- `Awake` や `Start` の暗黙の実行順に依存せず、Compositionが明示的に初期化する。
- 実行順が必要な場合は数値を散在させず、定数またはモジュールの `Order` で表現する。
- Domain Reloadが無効でも、static状態を明示的にリセットできるようにする。

## 非同期処理の型

非同期型はレイヤーと用途に応じて選択します。

- UnityフレームやUnityライフサイクルに密接なComposition処理は `Awaitable`／`Awaitable<T>` を使用する。
- Application、Adaptor、公開ライブラリAPIは `Task<T>`／`ValueTask<T>` を使用する。
- 同期完了が多く、呼び出し側が1回だけawaitする軽量APIには `ValueTask<T>` を検討する。
- 複数回の参照、`Task.WhenAll`、外部ライブラリとの連携が必要な場合は `Task<T>` を使用する。
- `Awaitable` とTask系の橋渡しはCompositionやInfrastructureなどの境界に限定する。
- すべての中断可能な処理へ `CancellationToken` を伝播する。
- キャンセル、失敗、正常なfalseを混同しない。

## 状態とリソースの所有権

すべての可変状態と解放が必要なリソースには、所有者を1つ決めます。

- 作成した側が、破棄または所有権移譲の責任を持つ。
- イベントを購読した側が購読解除する。
- Addressablesのhandleを保持した側がreleaseする。
- CancellationTokenSourceを生成した側がdisposeする。
- Service Locatorへ登録したCompositionが登録解除する。
- Registryやcacheは、無効化とリセットのAPIを持つ。
- 所有権を移す場合は、型名、引数名、XMLドキュメントのいずれかで明示する。

## 開発コードの分離

- Runtimeにはマスタービルドで必要なコードだけを含める。
- Editor拡張はEditor専用asmdefへ置く。
- デバッグ入力や検証用ComponentはDevelop用asmdefへ分離し、Releaseビルドへ含めない。
- 技術検証や先行研究のデモは製品Runtimeから分離し、製品コードから参照しない。
- Sampleは公開APIの利用例として保ち、内部APIへ依存させない。

## 避ける設計

- すべての処理を1つのManagerやMonoBehaviourへ集める。
- DomainやApplicationからScene、GameObject、PlayerPrefs、Addressablesを直接操作する。
- Service Locatorを任意の場所から参照し、依存を隠す。
- public setterで不変条件を迂回できる状態を公開する。
- property getterからロードやキャッシュ生成などの重い副作用を起こす。
- 将来必要になるかもしれないという理由だけでinterface、factory、genericを追加する。
- 初期化順を `Awake`、`Start`、Script Execution Orderの偶然に依存させる。
- 登録、購読、ロードだけを実装し、解除、解放、キャンセルを実装しない。
- Editor、Develop、SampleのコードをRuntimeへ混在させる。

## 設計判断のチェックリスト

新しい型や機能を追加する前に確認します。

- [ ] この型が変更される理由は1つか。
- [ ] この責務は現在のレイヤーとディレクトリに属するか。
- [ ] 依存方向は内側のルールを外側の詳細から守っているか。
- [ ] interfaceは実際の境界または交換理由を表しているか。
- [ ] 状態変更とQueryが分離されているか。
- [ ] 不正な状態を型またはコンストラクタで防げるか。
- [ ] 初期化、利用開始、終了の順序が明示されているか。
- [ ] 登録、購読、handle、tokenの所有者と解放方法が決まっているか。
- [ ] Unityライフサイクルへ依存しなくてもテスト可能な部分を分離したか。
- [ ] 小さな問題に対して過剰な抽象化を導入していないか。
