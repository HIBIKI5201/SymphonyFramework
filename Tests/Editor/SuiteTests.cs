using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using NUnit.Framework;

using SymphonyFramework.Exceptions;
using SymphonyFramework.Scenes;

using UnityEditor;

using UnityEngine;
using UnityEngine.TestTools;

namespace SymphonyFramework.Tests
{
    /// <summary>
    ///     Suiteの初期化状態と引数検証を検証する。
    /// </summary>
    public sealed class SuiteTests
    {
        /// <summary>
        ///     未初期化のロードは初期化前アクセスとして拒否する。
        /// </summary>
        [Test]
        public void LoadAsync_NotInitialized_Throws()
        {
            Assert.That(
                () => Suite.LoadAsync(null),
                Throws.TypeOf<SymphonyNotInitializedException>());
        }

        /// <summary>
        ///     未初期化のアンロードは初期化前アクセスとして拒否する。
        /// </summary>
        [Test]
        public void UnloadAsync_NotInitialized_Throws()
        {
            Assert.That(
                () => Suite.UnloadAsync(null),
                Throws.TypeOf<SymphonyNotInitializedException>());
        }

        /// <summary>
        ///     未初期化の状態照会は初期化前アクセスとして拒否する。
        /// </summary>
        [Test]
        public void GetBlockInfos_NotInitialized_Throws()
        {
            Assert.That(
                () => Suite.GetInfos(),
                Throws.TypeOf<SymphonyNotInitializedException>());
        }

        /// <summary>
        ///     依存が欠けた初期化は結線の誤りとして拒否する。
        /// </summary>
        [Test]
        public void Initialize_NullSceneLoadService_Throws()
        {
            Assert.That(
                () => Suite.Initialize(null),
                Throws.TypeOf<ArgumentNullException>());
        }

        /// <summary>
        ///     初期化直後は追跡中のブロックが無い。
        /// </summary>
        [Test]
        public void GetBlockInfos_AfterInitialize_IsEmpty()
        {
            Initialize();

            Assert.That(Suite.GetInfos(), Is.Empty);
            Assert.That(Suite.IsLoaded("Town"), Is.False);
            Assert.That(Suite.TryGetInfo("Town", out _), Is.False);
        }

        /// <summary>
        ///     ロード対象が無い場合は非同期処理を開始せずに拒否する。
        /// </summary>
        [Test]
        public void LoadAsync_NullBlock_Throws()
        {
            Initialize();

            Assert.That(
                () => Suite.LoadAsync(null),
                Throws.TypeOf<ArgumentNullException>());
        }

        /// <summary>
        ///     エントリの無いブロックは開始前に拒否する。
        /// </summary>
        [Test]
        public void LoadAsync_EmptyBlock_Throws()
        {
            Initialize();
            _asset = ScriptableObject.CreateInstance<SuiteAsset>();
            _asset.name = "EmptyBlock";

            Assert.That(
                () => Suite.LoadAsync(_asset),
                Throws.TypeOf<ArgumentException>());
        }

        /// <summary>
        ///     ResetRuntimeStateで未初期化へ戻す。
        /// </summary>
        [Test]
        public void ResetRuntimeState_AfterInitialize_MakesUninitialized()
        {
            Initialize();
            Assert.That(Suite.IsInitialized, Is.True);

            Suite.ResetRuntimeState();

            Assert.That(Suite.IsInitialized, Is.False);
        }

        /// <summary>
        ///     起動時のSuiteから、整理で残すシーン名を重複なく集める。
        /// </summary>
        [Test]
        public void CollectSceneNames_MultipleSuites_ReturnsDistinctSceneNames()
        {
            _asset = ScriptableObject.CreateInstance<SuiteAsset>();
            _otherAsset = ScriptableObject.CreateInstance<SuiteAsset>();
            WriteSceneNames(_asset, "Field", "UI");
            WriteSceneNames(_otherAsset, "UI", "Battle");

            Assert.That(
                Suite.CollectSceneNames(new[] { _asset, null, _otherAsset }),
                Is.EquivalentTo(new[] { "Field", "UI", "Battle" }));
        }

        /// <summary>
        ///     起動時のSuiteが無い場合は何もせず成功とする。
        /// </summary>
        [Test]
        public async Task LoadInitialAsync_NoSuites_ReturnsTrue()
        {
            Initialize();

            Assert.That(await Suite.LoadInitialAsync(null), Is.True);
            Assert.That(await Suite.LoadInitialAsync(new SuiteAsset[] { null }), Is.True);
        }

        /// <summary>
        ///     起動時のSuiteが不正な場合は例外を投げずにログへ出し、失敗として返す。
        /// </summary>
        [Test]
        public async Task LoadInitialAsync_EmptySuite_LogsAndReturnsFalse()
        {
            Initialize();
            _asset = ScriptableObject.CreateInstance<SuiteAsset>();
            _asset.name = "EmptyBlock";

            LogAssert.Expect(LogType.Exception, new Regex("エントリがありません"));

            Assert.That(await Suite.LoadInitialAsync(new[] { _asset }), Is.False);
        }

        /// <summary> テストの間だけ存在させるアセット。 </summary>
        private SuiteAsset _asset;

        /// <summary> 複数のSuiteを扱うテストで使う2つ目のアセット。 </summary>
        private SuiteAsset _otherAsset;

        /// <summary>
        ///     テストごとに未初期化の状態から始める。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            // Domain Reloadが無効な環境でも、前のテストの状態を持ち越さない。
            Suite.ResetRuntimeState();
        }

        /// <summary>
        ///     テストごとの初期化と生成物を破棄する。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            Suite.ResetRuntimeState();

            if (_asset != null) { UnityEngine.Object.DestroyImmediate(_asset); }
            if (_otherAsset != null) { UnityEngine.Object.DestroyImmediate(_otherAsset); }

            _asset = null;
            _otherAsset = null;
        }

        /// <summary>
        ///     Inspectorと同じ経路でエントリのシーン名を書き込む。
        /// </summary>
        /// <param name="asset"> 書き込む対象のアセット。 </param>
        /// <param name="sceneNames"> エントリのシーン名。 </param>
        private static void WriteSceneNames(SuiteAsset asset, params string[] sceneNames)
        {
            SerializedObject serialized = new(asset);
            SerializedProperty entries = serialized.FindProperty("_entries");
            entries.arraySize = sceneNames.Length;

            for (int i = 0; i < sceneNames.Length; i++)
            {
                entries.GetArrayElementAtIndex(i).FindPropertyRelative("_sceneName").stringValue = sceneNames[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        ///     実際のシーン操作を伴わないScene Load Serviceで初期化する。
        /// </summary>
        private static void Initialize()
        {
            // 初期化状態の検証だけを行うため、Serviceは生成するだけで操作しない。
            Suite.Initialize(new SceneLoadService(new SceneLoadRegistry(), new UnitySceneLoader()));
        }
    }
}
