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

        /// <summary> テストの間だけ存在させるアセット。 </summary>
        private SceneBlockAsset _asset;

        /// <summary>
        ///     テストごとに生成したアセットを破棄する。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            // ScriptableObjectはGCで消えないため、テストごとに明示的に破棄する。
            if (_asset != null) { Object.DestroyImmediate(_asset); }

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
