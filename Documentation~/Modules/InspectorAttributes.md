# Inspector属性

利用側のフィールドへ付けて、Inspectorの表示を変える属性です。属性の定義はRuntime、描画はEditorにあります。

## 入口

| 項目 | 内容 |
| --- | --- |
| namespace | `SymphonyFrameWork.Attribute`、`SymphonyFrameWork.Editor` |
| 主な公開型 | `[ReadOnly]` / `[DisplayText]` / `[TagSelector]` / `[SceneNameSelector]` / `[SubclassSelector]` と対応するDrawer |
| メニューパス | 利用側のフィールドへ属性を付ける |
| 設定の保存先 | なし |

## Editor機能

| 属性 | 効果 |
| --- | --- |
| `[ReadOnly]` | Inspectorで編集できないようにする |
| `[DisplayText]` | フィールドの上に説明テキストを表示する |
| `[TagSelector]` | 文字列フィールドをタグのドロップダウンにする |
| `[SceneNameSelector]` | 文字列フィールドをシーン名のドロップダウンにする |
| `[SubclassSelector]` | `[SerializeReference]` フィールドで、派生型をドロップダウンから選べるようにする |

`[SubclassSelector]` は `[SerializeReference]` と組み合わせて使います。単独では効果がありません。

対応するDrawerは`ReadOnlyDrawer`、`DisplayTextDecoratorDrawer`、`TagSelectorDrawer`、`SceneNameSelectorDrawer`、`SubclassSelectorDrawer`です。

## 関連

- [Deprecations.md](../Deprecations.md)
- [CHANGELOG.md](../../CHANGELOG.md)
