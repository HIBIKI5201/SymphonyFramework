using System;

using NUnit.Framework;

using SymphonyFrameWork.System.SceneBlock;

using UnityEditor;

using UnityEngine;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     SceneBlockAssetがシリアライズ済みの値を読み取り専用で公開する契約を検証する。
    /// </summary>
    public sealed class SceneBlockAssetTests
    {
        /// <summary>
        ///     未設定のブロック名はアセット名で代替する。
        /// </summary>
        [Test]
        public void BlockName_NotSpecified_FallsBackToAssetName()
        {
            SceneBlockAsset asset = CreateAsset("TownBlock");

            Assert.That(asset.BlockName, Is.EqualTo("TownBlock"));
        }

        /// <summary>
        ///     ブロック名を設定した場合はその値を使う。
        /// </summary>
        [Test]
        public void BlockName_Specified_UsesSerializedValue()
        {
            SceneBlockAsset asset = CreateAsset("TownBlock");

            SerializedObject serialized = new(asset);
            serialized.FindProperty("_blockName").stringValue = "Town";
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(asset.BlockName, Is.EqualTo("Town"));
        }

        /// <summary>
        ///     エントリを設定していない場合は空の一覧を返す。
        /// </summary>
        [Test]
        public void Entries_NotSpecified_IsEmpty()
        {
            SceneBlockAsset asset = CreateAsset("TownBlock");

            Assert.That(asset.Entries, Is.Empty);
        }

        /// <summary>
        ///     Inspectorから設定した値をエントリとして読み取れる。
        /// </summary>
        /// <remarks>
        ///     シリアライズ経由で組み立てることで、フィールド名の変更にも気づけるようにする。
        /// </remarks>
        [Test]
        public void Entries_SerializedValues_AreExposed()
        {
            SceneBlockAsset asset = CreateAsset("TownBlock");

            SerializedObject serialized = new(asset);
            SerializedProperty entries = serialized.FindProperty("_entries");
            entries.arraySize = 1;

            SerializedProperty entry = entries.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("_sceneName").stringValue = "Town_Props";
            entry.FindPropertyRelative("_priority").intValue = 7;
            entry.FindPropertyRelative("_isPersistent").boolValue = true;

            SerializedProperty dependsOn = entry.FindPropertyRelative("_dependsOn");
            dependsOn.arraySize = 1;
            dependsOn.GetArrayElementAtIndex(0).stringValue = "Town_Base";

            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(asset.Entries, Has.Count.EqualTo(1));
            Assert.That(asset.Entries[0].SceneName, Is.EqualTo("Town_Props"));
            Assert.That(asset.Entries[0].DependsOn, Is.EqualTo(new[] { "Town_Base" }));
            Assert.That(asset.Entries[0].Priority, Is.EqualTo(7));
            Assert.That(asset.Entries[0].IsPersistent, Is.True);
        }

        /// <summary>
        ///     ブロックに登録済みのシーンは依存先の候補になる。
        /// </summary>
        [Test]
        public void CanDependOnScene_RegisteredScene_IsCandidate()
        {
            SceneBlockAsset asset = CreateAsset("TownBlock");
            WriteEntries(asset, ("Town_Base", Array.Empty<string>()), ("Town_Props", new[] { "Town_Base" }));

            Assert.That(asset.CanDependOnScene("Town_Base"), Is.True);
            Assert.That(asset.CanDependOnScene("Town_Props"), Is.True);
        }

        /// <summary>
        ///     ブロックの外にあるシーンは依存先の候補にしない。
        /// </summary>
        /// <remarks>
        ///     選んだ後にMissingReferenceで弾かれる形を避けるための絞り込みである。
        /// </remarks>
        [Test]
        public void CanDependOnScene_SceneOutsideBlock_IsNotCandidate()
        {
            SceneBlockAsset asset = CreateAsset("TownBlock");
            WriteEntries(asset, ("Town_Base", Array.Empty<string>()));

            Assert.That(asset.CanDependOnScene("Dungeon_Base"), Is.False);
        }

        /// <summary>
        ///     既に依存として保存済みのシーン名は、エントリに無くても候補に残す。
        /// </summary>
        /// <remarks>
        ///     **候補から外すと、Drawerが保存済みの値を先頭の候補へ書き換えてしまう。**
        ///     外れた依存であることは検証側が知らせるため、値そのものは保持する。
        /// </remarks>
        [Test]
        public void CanDependOnScene_AlreadySavedDependency_StaysCandidate()
        {
            SceneBlockAsset asset = CreateAsset("TownBlock");
            WriteEntries(asset, ("Town_Props", new[] { "Removed_Base" }));

            Assert.That(asset.CanDependOnScene("Removed_Base"), Is.True);
        }

        /// <summary>
        ///     空のシーン名は候補にしない。
        /// </summary>
        [Test]
        public void CanDependOnScene_BlankName_IsNotCandidate()
        {
            SceneBlockAsset asset = CreateAsset("TownBlock");
            WriteEntries(asset, ("Town_Base", Array.Empty<string>()));

            Assert.That(asset.CanDependOnScene(null), Is.False);
            Assert.That(asset.CanDependOnScene(string.Empty), Is.False);
            Assert.That(asset.CanDependOnScene("   "), Is.False);
        }

        /// <summary>
        ///     エントリが無いアセットは、どのシーンも依存先の候補にしない。
        /// </summary>
        [Test]
        public void CanDependOnScene_NoEntries_IsNotCandidate()
        {
            SceneBlockAsset asset = CreateAsset("TownBlock");

            Assert.That(asset.CanDependOnScene("Town_Base"), Is.False);
        }

        /// <summary>
        ///     シリアライズ経由でエントリを書き込む。
        /// </summary>
        /// <param name="asset"> 書き込む対象のアセット。 </param>
        /// <param name="entries"> シーン名と依存一覧の組。 </param>
        /// <remarks>
        ///     Inspectorから設定した場合と同じ経路を通し、フィールド名の変更にも気づけるようにする。
        /// </remarks>
        private static void WriteEntries(
            SceneBlockAsset asset,
            params (string SceneName, string[] DependsOn)[] entries)
        {
            SerializedObject serialized = new(asset);
            SerializedProperty serializedEntries = serialized.FindProperty("_entries");
            serializedEntries.arraySize = entries.Length;

            for (int index = 0; index < entries.Length; index++)
            {
                SerializedProperty entry = serializedEntries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("_sceneName").stringValue = entries[index].SceneName;

                SerializedProperty dependsOn = entry.FindPropertyRelative("_dependsOn");
                dependsOn.arraySize = entries[index].DependsOn.Length;
                for (int dependencyIndex = 0; dependencyIndex < entries[index].DependsOn.Length; dependencyIndex++)
                {
                    dependsOn.GetArrayElementAtIndex(dependencyIndex).stringValue =
                        entries[index].DependsOn[dependencyIndex];
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary> テストの間だけ存在させるアセット。 </summary>
        private SceneBlockAsset _asset;

        /// <summary>
        ///     テストごとに生成したアセットを破棄する。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            // ScriptableObjectはGCで消えないため、テストごとに明示的に破棄する。
            if (_asset != null) { UnityEngine.Object.DestroyImmediate(_asset); }

            _asset = null;
        }

        /// <summary>
        ///     破棄対象として記録しながら検証用アセットを生成する。
        /// </summary>
        /// <param name="assetName"> 生成するアセットの名前。 </param>
        /// <returns> 生成したアセット。 </returns>
        private SceneBlockAsset CreateAsset(string assetName)
        {
            _asset = ScriptableObject.CreateInstance<SceneBlockAsset>();
            _asset.name = assetName;

            return _asset;
        }
    }
}
