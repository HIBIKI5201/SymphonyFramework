# Inspector属性

利用側のフィールドへ付けて、Inspectorの表示を変える属性です。属性の定義はRuntime、描画はEditorにあります。

## 入口

| 項目 | 内容 |
| --- | --- |
| namespace | `SymphonyFrameWork.Attribute`、`SymphonyFrameWork.Editor` |
| 主な公開型 | `[ReadOnly]` / `[DisplayText]` / `[TagSelector]` / `[SceneNameSelector]` / `[SubclassSelector]` と対応するDrawer |
| メニューパス | 利用側のフィールドへ属性を付ける |
| 設定の保存先 | なし |

## クイックスタート

`[TagSelector]` / `[SceneNameSelector]` / `[SubclassSelector]` は、引数を渡さなければプロジェクトの候補をすべてドロップダウンへ並べます。候補を絞り込むメソッドの名前を渡すと、`true` を返した候補だけが残ります。属性の引数はコンパイル時定数に限られるため、デリゲートではなく**メソッド名**で指定します。`nameof` を使えば、綴りの誤りをコンパイル時に見つけられます。

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

フィルターメソッドの条件は次のとおりです。

| 項目 | 内容 |
| --- | --- |
| 宣言場所 | そのフィールドを持つMonoBehaviourまたはScriptableObjectの型か、その基底型 |
| シグネチャ | `bool <名前>(string)`。`[SubclassSelector]` だけは `bool <名前>(Type)` |
| アクセス修飾子 | 問わない。`private` でよい |
| static / instance | どちらでもよい。instanceの場合は編集中のオブジェクトを対象に呼ぶ |
| 戻り値 | `true` を返した候補だけがドロップダウンへ残る |

## 実装時の注意

- **`[SubclassSelector]` は `[SerializeReference]` と組み合わせて使います。** 単独では効果がありません。
- **フィルターメソッドは、フィールドを持つ`[Serializable]`クラスではなくホスト側の型へ書きます。** 入れ子のクラスに書いても見つかりません。
- **メソッドが見つからない、シグネチャが違う、フィルターが例外を投げた場合は、ドロップダウンの代わりに理由がInspectorへ表示されます。** 毎フレーム描画されるためログは出しません。表示が出ている間もシリアライズ済みの値は書き換わりません。
- **フィルターが候補をすべて落とした場合も、ドロップダウンは表示されず値は保持されます。**
- `[SubclassSelector]` の先頭にある未設定（`<null>`）の候補は、フィルターへ渡されず常に残ります。
- **複数のオブジェクトを同時に選択している場合、instanceのフィルターは先頭のオブジェクトの状態で判定します。** オブジェクトごとに結果が変わるフィルターは、複数選択時に意図しない候補が出ることがあります。
- 候補は描画のたびに判定されるため、フィルターの中で重い処理やI/Oを行わないでください。

## Editor機能

| 属性 | 効果 |
| --- | --- |
| `[ReadOnly]` | Inspectorで編集できないようにする |
| `[DisplayText]` | フィールドの上に説明テキストを表示する |
| `[TagSelector]` | 文字列フィールドをタグのドロップダウンにする |
| `[SceneNameSelector]` | 文字列フィールドをシーン名のドロップダウンにする |
| `[SubclassSelector]` | `[SerializeReference]` フィールドで、派生型をドロップダウンから選べるようにする |

対応するDrawerは`ReadOnlyDrawer`、`DisplayTextDecoratorDrawer`、`TagSelectorDrawer`、`SceneNameSelectorDrawer`、`SubclassSelectorDrawer`です。

## 関連

- [Deprecations.md](../Deprecations.md)
- [CHANGELOG.md](../../CHANGELOG.md)
