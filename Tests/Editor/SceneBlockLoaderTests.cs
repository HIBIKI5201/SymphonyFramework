using System;

using NUnit.Framework;

using SymphonyFrameWork.Exceptions;
using SymphonyFrameWork.System.SceneBlock;
using SymphonyFrameWork.System.SceneLoad;

using UnityEngine;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     Scene Block Loaderの初期化状態と引数検証を検証する。
    /// </summary>
    public sealed class SceneBlockLoaderTests
    {
        /// <summary>
        ///     未初期化のロードは初期化前アクセスとして拒否する。
        /// </summary>
        [Test]
        public void LoadAsync_NotInitialized_Throws()
        {
            Assert.That(
                () => SceneBlockLoader.LoadAsync(null),
                Throws.TypeOf<SymphonyNotInitializedException>());
        }

        /// <summary>
        ///     未初期化のアンロードは初期化前アクセスとして拒否する。
        /// </summary>
        [Test]
        public void UnloadAsync_NotInitialized_Throws()
        {
            Assert.That(
                () => SceneBlockLoader.UnloadAsync(null),
                Throws.TypeOf<SymphonyNotInitializedException>());
        }

        /// <summary>
        ///     未初期化の状態照会は初期化前アクセスとして拒否する。
        /// </summary>
        [Test]
        public void GetBlockInfos_NotInitialized_Throws()
        {
            Assert.That(
                () => SceneBlockLoader.GetBlockInfos(),
                Throws.TypeOf<SymphonyNotInitializedException>());
        }

        /// <summary>
        ///     依存が欠けた初期化は結線の誤りとして拒否する。
        /// </summary>
        [Test]
        public void Initialize_NullSceneLoadService_Throws()
        {
            Assert.That(
                () => SceneBlockLoader.Initialize(null),
                Throws.TypeOf<ArgumentNullException>());
        }

        /// <summary>
        ///     初期化直後は追跡中のブロックが無い。
        /// </summary>
        [Test]
        public void GetBlockInfos_AfterInitialize_IsEmpty()
        {
            Initialize();

            Assert.That(SceneBlockLoader.GetBlockInfos(), Is.Empty);
            Assert.That(SceneBlockLoader.IsLoaded("Town"), Is.False);
            Assert.That(SceneBlockLoader.TryGetBlockInfo("Town", out _), Is.False);
        }

        /// <summary>
        ///     ロード対象が無い場合は非同期処理を開始せずに拒否する。
        /// </summary>
        [Test]
        public void LoadAsync_NullBlock_Throws()
        {
            Initialize();

            Assert.That(
                () => SceneBlockLoader.LoadAsync(null),
                Throws.TypeOf<ArgumentNullException>());
        }

        /// <summary>
        ///     エントリの無いブロックは開始前に拒否する。
        /// </summary>
        [Test]
        public void LoadAsync_EmptyBlock_Throws()
        {
            Initialize();
            _asset = ScriptableObject.CreateInstance<SceneBlockAsset>();
            _asset.name = "EmptyBlock";

            Assert.That(
                () => SceneBlockLoader.LoadAsync(_asset),
                Throws.TypeOf<ArgumentException>());
        }

        /// <summary>
        ///     ResetRuntimeStateで未初期化へ戻す。
        /// </summary>
        [Test]
        public void ResetRuntimeState_AfterInitialize_MakesUninitialized()
        {
            Initialize();
            Assert.That(SceneBlockLoader.IsInitialized, Is.True);

            SceneBlockLoader.ResetRuntimeState();

            Assert.That(SceneBlockLoader.IsInitialized, Is.False);
        }

        /// <summary> テストの間だけ存在させるアセット。 </summary>
        private SceneBlockAsset _asset;

        /// <summary>
        ///     テストごとに未初期化の状態から始める。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            // Domain Reloadが無効な環境でも、前のテストの状態を持ち越さない。
            SceneBlockLoader.ResetRuntimeState();
        }

        /// <summary>
        ///     テストごとの初期化と生成物を破棄する。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            SceneBlockLoader.ResetRuntimeState();

            if (_asset != null) { UnityEngine.Object.DestroyImmediate(_asset); }

            _asset = null;
        }

        /// <summary>
        ///     実際のシーン操作を伴わないScene Load Serviceで初期化する。
        /// </summary>
        private static void Initialize()
        {
            // 初期化状態の検証だけを行うため、Serviceは生成するだけで操作しない。
            SceneBlockLoader.Initialize(new SceneLoadService(new SceneLoadRegistry(), new UnitySceneLoader()));
        }
    }
}
