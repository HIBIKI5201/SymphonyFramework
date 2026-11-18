# Inspector属性

利用側のフィールドへ付けて、Inspectorの表示を変える属性です。

## 目的と範囲

文字列のタグ名・シーン名や`[SerializeReference]`の型を、手入力ではなく候補から選べるようにします。

- **すること**: 読み取り専用表示、説明テキストの表示、タグ・シーン名・派生型のドロップダウン化、候補の絞り込み
- **しないこと**: 実行時の値の検証（属性はInspectorの表示だけを変える）、候補にない値の自動修正

## 入口

| 項目 | 内容 |
| --- | --- |
| namespace | `SymphonyFrameWork.Attribute`、`SymphonyFrameWork.Editor` |
| 主な公開型 | `[ReadOnly]` / `[DisplayText]` / `[TagSelector]` / `[SceneNameSelector]` / `[SubclassSelector]` と対応するDrawer |
| メニューパス | 利用側のフィールドへ属性を付ける |
| 設定の保存先 | なし |

## 仕組みの要点

1. **属性の定義はRuntime、描画はEditorにあります。** Playerビルドへ入るのは属性だけです。
2. **候補の絞り込みはメソッド名で指定します。** 属性の引数はコンパイル時定数に限られるため、デリゲートは渡せません。`nameof`を使うと綴りの誤りをコンパイル時に見つけられます。
3. **絞り込みに失敗しても値は書き換えません。** ドロップダウンの代わりに理由を表示し、シリアライズ済みの値を残します。

## 機能一覧

| ID | 属性 | 付ける対象 | 結果 |
| --- | --- | --- | --- |
| ATTR-01 | `[ReadOnly]` | 任意のフィールド | Inspectorで編集できなくする |
| ATTR-02 | `[DisplayText]` | 任意のフィールド | フィールドの上に説明テキストを出す |
| ATTR-03 | `[TagSelector]` | `string` | タグのドロップダウンにする。絞り込みメソッドを渡せる |
| ATTR-04 | `[SceneNameSelector]` | `string` | Build Settingsのシーン名のドロップダウンにする。絞り込みメソッドを渡せる |
| ATTR-05 | `[SubclassSelector]` | `[SerializeReference]`のフィールド | 派生型のドロップダウンにする。絞り込みメソッドを渡せる |

## クイックスタート

`[TagSelector]` / `[SceneNameSelector]` / `[SubclassSelector]` は、引数を渡さなければプロジェクトの候補をすべてドロップダウンへ並べます。候補を絞り込むメソッドの名前を渡すと、`true` を返した候補だけが残ります。

```csharp
using SymphonyFrameWork.Attribute;
using System;
using UnityEngine;

public sealed class EnemySpawner : MonoBehaviour
{
    [SerializeField, TagSelector]
    private string _ownerTag;

    [SerializeField, TagSelector(nameof(IsEnemyTag))]
    private string _targetTag;

    [SerializeField, SceneNameSelector(nameof(IsBattleScene))]
    private string _battleScene;

    [SerializeReference, SubclassSelector(filterMethodName: nameof(IsRuntimeStrategy))]
    private ISpawnStrategy _strategy;

    [SerializeField]
    private string _scenePrefix = "Battle_";

    private static bool IsEnemyTag(string tag) => tag.StartsWith("Enemy");

    private static bool IsRuntimeStrategy(Type type) => !type.Name.StartsWith("Debug");

    private bool IsBattleScene(string sceneName) => sceneName.StartsWith(_scenePrefix);
}
```

絞り込みメソッドの条件は次のとおりです。

| 項目 | 内容 |
| --- | --- |
| 宣言場所 | そのフィールドを持つMonoBehaviourまたはScriptableObjectの型か、その基底型 |
| シグネチャ | `bool <名前>(string)`。`[SubclassSelector]` だけは `bool <名前>(Type)` |
| アクセス修飾子 | 問わない。`private` でよい |
| static / instance | どちらでもよい。instanceの場合は編集中のオブジェクトを対象に呼ぶ |
| 戻り値 | `true` を返した候補だけがドロップダウンへ残る |

## うまくいかないとき

| 起きること | この機能の動き | 対処 |
| --- | --- | --- |
| 絞り込みメソッドが見つからない、シグネチャが違う、例外を投げた | ドロップダウンの代わりに理由をInspectorへ表示する。ログは出さない（毎フレーム描画されるため） | 下の条件表に合わせてメソッドを直す |
| 絞り込みで候補が0件になった | ドロップダウンを出さず、値を残す | 絞り込みの条件を見直す |
| `[SubclassSelector]`が効かない | `[SerializeReference]`が無いと何もしない | `[SerializeReference]`と一緒に付ける |
| 複数選択で候補がおかしい | instanceの絞り込みは先頭のオブジェクトの状態で判定する | オブジェクトごとに結果が変わる絞り込みは複数選択で使わない |

## 守ること

- 絞り込みメソッドは、フィールドを持つ`[Serializable]`クラスではなく、MonoBehaviourかScriptableObjectの側（またはその基底型）へ書く。入れ子のクラスに書いても見つからない。
- 候補は描画のたびに判定されるため、絞り込みの中で重い処理やI/Oを行わない。
- `[SubclassSelector]`の先頭の未設定（`<null>`）は絞り込みへ渡されず、いつも残る。

## Editor機能

属性ごとの描画は、対応するDrawer（`ReadOnlyDrawer`、`DisplayTextDecoratorDrawer`、`TagSelectorDrawer`、`SceneNameSelectorDrawer`、`SubclassSelectorDrawer`）が行います。メニューや設定はありません。

## 確かめ方

- 属性を付けたコンポーネントをInspectorで開き、ドロップダウンに意図した候補だけが並ぶことを見る。
- 理由の表示が出ていないことを確かめる。出ている場合はその文面が原因を示す。

## 関連

- [Deprecations.md](../Deprecations.md)
- [CHANGELOG.md](../../CHANGELOG.md)
