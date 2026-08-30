using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using SymphonyFrameWork.System.SceneBlock;
using SymphonyFrameWork.System.SceneLoad;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     Scene Blockの層順ロードと、ブロック外ロードと共存する保持規則を検証する。
    /// </summary>
    public sealed class SceneBlockServiceTests
    {
        /// <summary>
        ///     依存の無いシーンを先に、依存のあるシーンを後の層でロードする。
        /// </summary>
        [Test]
        public void LoadBlock_Layers_LoadsInDependencyOrder()
        {
            bool result = Load("Town", 1, TownEntries());

            Assert.That(result, Is.True);
            Assert.That(_loader.LoadCalls, Has.Count.EqualTo(2));
            Assert.That(SceneNamesOf(_loader.LoadCalls[0]), Is.EqualTo(new[] { "Base" }));
            Assert.That(SceneNamesOf(_loader.LoadCalls[1]), Is.EqualTo(new[] { "Npc", "Props" }));
        }

        /// <summary>
        ///     同じ層のシーンは1回の呼び出しでまとめて要求する。
        /// </summary>
        [Test]
        public void LoadBlock_SameLayer_RequestsScenesTogether()
        {
            Load("Town", 1, TownEntries());

            Assert.That(_loader.LoadCalls[1], Has.Length.EqualTo(2));
        }

        /// <summary>
        ///     エントリの優先度をロード要求へそのまま渡す。
        /// </summary>
        [Test]
        public void LoadBlock_Priority_IsCarriedToRequest()
        {
            SceneBlockEntry[] entries = { new("Base", null, 10) };

            Load("Town", 1, entries);

            Assert.That(_loader.LoadCalls[0][0].Priority, Is.EqualTo(10));
        }

        /// <summary>
        ///     他のブロックが保持しているシーンは再ロードしない。
        /// </summary>
        [Test]
        public void LoadBlock_AlreadyLoadedByAnotherBlock_DoesNotReload()
        {
            Load("Town", 1, new SceneBlockEntry[] { new("Base"), new("Shared") });
            _loader.LoadCalls.Clear();

            Load("Dungeon", 2, new SceneBlockEntry[] { new("Cave"), new("Shared") });

            Assert.That(SceneNamesOf(_loader.LoadCalls[0]), Is.EqualTo(new[] { "Cave" }));
        }

        /// <summary>
        ///     ブロック外でロード済みのシーンは、ブロックのアンロードでも残す。
        /// </summary>
        [Test]
        public void LoadBlock_LoadedOutsideBlock_MarksExternalHold()
        {
            // 利用側がSceneLoaderで直接ロードした状態を作る。
            _loader.LoadedScenes.Add("Shared");

            Load("Town", 1, new SceneBlockEntry[] { new("Base"), new("Shared") });
            Unload("Town", 1);

            Assert.That(_loader.UnloadCalls[0], Is.EqualTo(new[] { "Base" }));
        }

        /// <summary>
        ///     保持が空になったシーンだけをアンロードする。
        /// </summary>
        [Test]
        public void UnloadBlock_LastHolder_UnloadsScene()
        {
            Load("Town", 1, new SceneBlockEntry[] { new("Base") });

            bool result = Unload("Town", 1);

            Assert.That(result, Is.True);
            Assert.That(_loader.UnloadCalls[0], Is.EqualTo(new[] { "Base" }));
            Assert.That(_loader.LoadedScenes, Does.Not.Contain("Base"));
        }

        /// <summary>
        ///     他のブロックが保持しているシーンはアンロードしない。
        /// </summary>
        [Test]
        public void UnloadBlock_OtherBlockStillHolds_KeepsScene()
        {
            Load("Town", 1, new SceneBlockEntry[] { new("Base"), new("Shared") });
            Load("Dungeon", 2, new SceneBlockEntry[] { new("Cave"), new("Shared") });

            Unload("Town", 1);

            Assert.That(_loader.UnloadCalls[0], Is.EqualTo(new[] { "Base" }));
            Assert.That(_loader.LoadedScenes, Does.Contain("Shared"));
        }

        /// <summary>
        ///     永続指定のシーンはブロックのアンロードで落とさない。
        /// </summary>
        [Test]
        public void UnloadBlock_PersistentEntry_KeepsScene()
        {
            SceneBlockEntry[] entries = { new("Base", null, 0, true), new("Props", new[] { "Base" }) };

            Load("Town", 1, entries);
            Unload("Town", 1);

            Assert.That(_loader.UnloadCalls[0], Is.EqualTo(new[] { "Props" }));
            Assert.That(_loader.LoadedScenes, Does.Contain("Base"));
        }

        /// <summary>
        ///     追跡していないブロックのアンロードは成功として扱う。
        /// </summary>
        [Test]
        public void UnloadBlock_NotTracked_ReturnsTrueWithoutRequest()
        {
            bool result = Unload("Town", 1);

            Assert.That(result, Is.True);
            Assert.That(_loader.UnloadCalls, Is.Empty);
        }

        /// <summary>
        ///     循環依存のブロックはロードを1件も開始しない。
        /// </summary>
        [Test]
        public void LoadBlock_CyclicGraph_ThrowsPlanException()
        {
            SceneBlockEntry[] entries = { new("A", new[] { "B" }), new("B", new[] { "A" }) };

            Assert.That(
                () => Load("Town", 1, entries),
                Throws.TypeOf<SceneBlockPlanException>());
            Assert.That(_loader.LoadCalls, Is.Empty);
        }

        /// <summary>
        ///     ロードに失敗しても、成立済みのシーンは保持したまま残す。
        /// </summary>
        [Test]
        public void LoadBlock_SceneLoadFails_ReturnsFalseAndKeepsHeldScenes()
        {
            _loader.FailOnScene = "Props";

            bool result = Load("Town", 1, new SceneBlockEntry[] { new("Base"), new("Props", new[] { "Base" }) });

            Assert.That(result, Is.False);
            Assert.That(_loader.LoadedScenes, Does.Contain("Base"));

            // 保持が残っているため、後からアンロードで片付けられる。
            Unload("Town", 1);
            Assert.That(_loader.UnloadCalls[0], Is.EqualTo(new[] { "Base" }));
        }

        /// <summary>
        ///     キャンセルしてもロード済みのシーンを巻き戻さない。
        /// </summary>
        [Test]
        public void LoadBlock_Canceled_DoesNotUnloadLoadedScenes()
        {
            _loader.CancelOnScene = "Props";

            Assert.That(
                () => Load("Town", 1, new SceneBlockEntry[] { new("Base"), new("Props", new[] { "Base" }) }),
                Throws.InstanceOf<OperationCanceledException>());

            Assert.That(_loader.LoadedScenes, Does.Contain("Base"));
            Assert.That(_loader.UnloadCalls, Is.Empty);
        }

        /// <summary>
        ///     同じアセットの再ロードは追加のロードを行わない。
        /// </summary>
        [Test]
        public void LoadBlock_SameAssetTwice_ReturnsTrueWithoutReload()
        {
            Load("Town", 1, TownEntries());
            _loader.LoadCalls.Clear();

            bool result = Load("Town", 1, TownEntries());

            Assert.That(result, Is.True);
            Assert.That(_loader.LoadCalls, Is.Empty);
        }

        /// <summary>
        ///     同名の別アセットは、保持が混ざる前に例外で止める。
        /// </summary>
        [Test]
        public void LoadBlock_DuplicateBlockName_ThrowsInvalidOperation()
        {
            Load("Town", 1, TownEntries());

            Assert.That(
                () => Load("Town", 2, TownEntries()),
                Throws.TypeOf<InvalidOperationException>());
        }

        /// <summary>
        ///     進捗は単調非減少で、完了時に1へ到達する。
        /// </summary>
        [Test]
        public void LoadBlock_Progress_ReportsMonotonicallyToOne()
        {
            List<float> reported = new();

            Load("Town", 1, TownEntries(), new BlockProgressRecorder(reported));

            Assert.That(reported, Is.Not.Empty);
            Assert.That(reported[reported.Count - 1], Is.EqualTo(1f).Within(0.0001f));

            for (int index = 1; index < reported.Count; index++)
            {
                Assert.That(reported[index], Is.GreaterThanOrEqualTo(reported[index - 1]));
            }
        }

        /// <summary>
        ///     状態が変わるたびに変更通知を発行する。
        /// </summary>
        [Test]
        public void LoadBlock_StateChanges_RaisesNotification()
        {
            int notificationCount = 0;
            _service.OnStateChanged += () => notificationCount++;

            Load("Town", 1, TownEntries());

            Assert.That(notificationCount, Is.GreaterThan(0));
        }

        /// <summary> 各テストで使う偽のシーン操作。 </summary>
        private FakeBlockSceneLoader _loader;

        /// <summary> 検証対象のService。 </summary>
        private SceneBlockService _service;

        /// <summary> 検証対象が使う追跡状態。 </summary>
        private SceneBlockRegistry _registry;

        /// <summary>
        ///     テストごとに追跡状態と偽のシーン操作を作り直す。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _loader = new FakeBlockSceneLoader();
            _registry = new SceneBlockRegistry();
            _service = new SceneBlockService(_registry, _loader);
        }

        /// <summary>
        ///     依存が1段ある標準的なブロックのエントリを作る。
        /// </summary>
        /// <returns> Base に Props と Npc が依存するエントリ一覧。 </returns>
        private static SceneBlockEntry[] TownEntries() => new SceneBlockEntry[]
        {
            new("Base"),
            new("Props", new[] { "Base" }),
            new("Npc", new[] { "Base" }),
        };

        /// <summary>
        ///     ロードを同期的に実行する。
        /// </summary>
        /// <param name="blockName"> ブロック名。 </param>
        /// <param name="assetInstanceId"> アセットのインスタンスID。 </param>
        /// <param name="entries"> エントリ一覧。 </param>
        /// <param name="progress"> 進捗の通知先。 </param>
        /// <returns> ロードに成功した場合はtrue。 </returns>
        private bool Load(
            string blockName,
            int assetInstanceId,
            IReadOnlyList<SceneBlockEntry> entries,
            IProgress<float> progress = null)
        {
            // 偽のシーン操作は完了済みTaskを返すため、この呼び出しは同期的に終わる。
            return _service
                .LoadBlock(blockName, assetInstanceId, entries, progress, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        ///     アンロードを同期的に実行する。
        /// </summary>
        /// <param name="blockName"> ブロック名。 </param>
        /// <param name="assetInstanceId"> アセットのインスタンスID。 </param>
        /// <returns> アンロードに成功した場合はtrue。 </returns>
        private bool Unload(string blockName, int assetInstanceId)
        {
            return _service
                .UnloadBlock(blockName, assetInstanceId, null, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        ///     ロード要求からシーン名だけを取り出す。
        /// </summary>
        /// <param name="requests"> ロード要求。 </param>
        /// <returns> シーン名の配列。 </returns>
        private static string[] SceneNamesOf(SceneLoadRequest[] requests)
        {
            string[] sceneNames = new string[requests.Length];
            for (int index = 0; index < requests.Length; index++)
            {
                sceneNames[index] = requests[index].SceneName;
            }

            return sceneNames;
        }

        /// <summary> 通知された進捗を記録するだけの通知先。 </summary>
        private sealed class BlockProgressRecorder : IProgress<float>
        {
            /// <summary>
            ///     記録先の一覧を指定して生成する。
            /// </summary>
            /// <param name="values"> 記録先。 </param>
            internal BlockProgressRecorder(List<float> values)
            {
                _values = values;
            }

            /// <summary>
            ///     進捗を記録する。
            /// </summary>
            /// <param name="value"> 通知された進捗。 </param>
            public void Report(float value) => _values.Add(value);

            private readonly List<float> _values;
        }

        /// <summary> 実際のシーンロードを伴わない検証用のシーン操作。 </summary>
        private sealed class FakeBlockSceneLoader : IBlockSceneLoader
        {
            /// <summary> ロード済みとみなすシーン名。 </summary>
            internal readonly HashSet<string> LoadedScenes = new(StringComparer.Ordinal);

            /// <summary> 受け取ったロード要求。層ごとに1件記録する。 </summary>
            internal readonly List<SceneLoadRequest[]> LoadCalls = new();

            /// <summary> 受け取ったアンロード要求。層ごとに1件記録する。 </summary>
            internal readonly List<string[]> UnloadCalls = new();

            /// <summary> このシーンを含む層のロードを失敗させる。 </summary>
            internal string FailOnScene;

            /// <summary> このシーンを含む層のロードをキャンセルする。 </summary>
            internal string CancelOnScene;

            /// <summary>
            ///     ロード済みとみなすかを返す。
            /// </summary>
            /// <param name="sceneName"> 確認するシーン名。 </param>
            /// <returns> ロード済みの場合はtrue。 </returns>
            public bool IsSceneLoaded(string sceneName) => LoadedScenes.Contains(sceneName);

            /// <summary>
            ///     ロード要求を記録し、指定に応じて失敗またはキャンセルを返す。
            /// </summary>
            /// <param name="requests"> ロード要求。 </param>
            /// <param name="progress"> 進捗の通知先。 </param>
            /// <param name="token"> 中断用トークン。 </param>
            /// <returns> ロード結果。 </returns>
            public Task<bool> LoadScenesAsync(
                IReadOnlyList<SceneLoadRequest> requests,
                IProgress<float> progress,
                CancellationToken token)
            {
                SceneLoadRequest[] snapshot = new SceneLoadRequest[requests.Count];
                for (int index = 0; index < requests.Count; index++) { snapshot[index] = requests[index]; }

                LoadCalls.Add(snapshot);

                if (Contains(snapshot, CancelOnScene))
                {
                    return Task.FromCanceled<bool>(new CancellationToken(true));
                }

                progress?.Report(1f);

                if (Contains(snapshot, FailOnScene)) { return Task.FromResult(false); }

                foreach (SceneLoadRequest request in snapshot) { LoadedScenes.Add(request.SceneName); }

                return Task.FromResult(true);
            }

            /// <summary>
            ///     アンロード要求を記録し、ロード済み集合から取り除く。
            /// </summary>
            /// <param name="sceneNames"> アンロード対象。 </param>
            /// <param name="progress"> 進捗の通知先。 </param>
            /// <param name="token"> 中断用トークン。 </param>
            /// <returns> アンロード結果。 </returns>
            public Task<bool> UnloadScenesAsync(
                IReadOnlyList<string> sceneNames,
                IProgress<float> progress,
                CancellationToken token)
            {
                string[] snapshot = new string[sceneNames.Count];
                for (int index = 0; index < sceneNames.Count; index++) { snapshot[index] = sceneNames[index]; }

                UnloadCalls.Add(snapshot);
                progress?.Report(1f);

                foreach (string sceneName in snapshot) { LoadedScenes.Remove(sceneName); }

                return Task.FromResult(true);
            }

            /// <summary>
            ///     指定したシーン名が要求へ含まれるか判定する。
            /// </summary>
            /// <param name="requests"> 判定対象の要求。 </param>
            /// <param name="sceneName"> 探すシーン名。未指定の場合は常にfalse。 </param>
            /// <returns> 含まれる場合はtrue。 </returns>
            private static bool Contains(SceneLoadRequest[] requests, string sceneName)
            {
                if (sceneName == null) { return false; }

                foreach (SceneLoadRequest request in requests)
                {
                    if (string.Equals(request.SceneName, sceneName, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
