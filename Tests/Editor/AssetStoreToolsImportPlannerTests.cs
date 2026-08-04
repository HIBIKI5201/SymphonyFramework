using NUnit.Framework;

using SymphonyFrameWork.Editor;

using System.Collections.Generic;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     マニフェストとローカルのリビジョンから、インポート候補と状態を組み立てる処理を検証する。
    /// </summary>
    public sealed class AssetStoreToolsImportPlannerTests
    {
        /// <summary> 未導入のパッケージは新規として扱う。 </summary>
        [Test]
        public void ResolveState_NoLocalVersion_IsNew()
        {
            AssetStoreToolsImportStateEnum state =
                AssetStoreToolsImportPlanner.ResolveState(1, null);

            Assert.That(state, Is.EqualTo(AssetStoreToolsImportStateEnum.New));
        }

        /// <summary> ローカルが古い場合は更新として扱う。 </summary>
        [Test]
        public void ResolveState_LocalOlder_IsUpdated()
        {
            AssetStoreToolsImportStateEnum state =
                AssetStoreToolsImportPlanner.ResolveState(3, 2);

            Assert.That(state, Is.EqualTo(AssetStoreToolsImportStateEnum.Updated));
        }

        /// <summary> リビジョンが一致する場合は最新として扱う。 </summary>
        [Test]
        public void ResolveState_LocalSame_IsUpToDate()
        {
            AssetStoreToolsImportStateEnum state =
                AssetStoreToolsImportPlanner.ResolveState(3, 3);

            Assert.That(state, Is.EqualTo(AssetStoreToolsImportStateEnum.UpToDate));
        }

        /// <summary> ローカルの方が新しい場合は巻き戻しになるため区別する。 </summary>
        [Test]
        public void ResolveState_LocalNewer_IsNewer()
        {
            AssetStoreToolsImportStateEnum state =
                AssetStoreToolsImportPlanner.ResolveState(2, 5);

            Assert.That(state, Is.EqualTo(AssetStoreToolsImportStateEnum.Newer));
        }

        /// <summary> 新規と更新は既定で選択する。 </summary>
        [Test]
        public void IsSelectedByDefault_NewAndUpdated_AreTrue()
        {
            Assert.That(
                AssetStoreToolsImportPlanner.IsSelectedByDefault(
                    AssetStoreToolsImportStateEnum.New),
                Is.True);
            Assert.That(
                AssetStoreToolsImportPlanner.IsSelectedByDefault(
                    AssetStoreToolsImportStateEnum.Updated),
                Is.True);
        }

        /// <summary> 最新とローカルが新しいものは既定で選択しない。 </summary>
        [Test]
        public void IsSelectedByDefault_UpToDateAndNewer_AreFalse()
        {
            Assert.That(
                AssetStoreToolsImportPlanner.IsSelectedByDefault(
                    AssetStoreToolsImportStateEnum.UpToDate),
                Is.False);
            Assert.That(
                AssetStoreToolsImportPlanner.IsSelectedByDefault(
                    AssetStoreToolsImportStateEnum.Newer),
                Is.False);
        }

        /// <summary> ローカルに無いパッケージは新規かつ選択済みになる。 </summary>
        [Test]
        public void Build_MissingLocalVersion_IsNewAndSelected()
        {
            IReadOnlyList<AssetStoreToolsImportCandidate> candidates =
                AssetStoreToolsImportPlanner.Build(
                    CreateManifest("Foo", 1),
                    new Dictionary<string, int>());

            Assert.That(candidates.Count, Is.EqualTo(1));
            Assert.That(candidates[0].State, Is.EqualTo(AssetStoreToolsImportStateEnum.New));
            Assert.That(candidates[0].IsSelected, Is.True);
            Assert.That(candidates[0].LocalVersion, Is.Null);
        }

        /// <summary> ローカルが古いパッケージは更新かつ選択済みになる。 </summary>
        [Test]
        public void Build_OlderLocalVersion_IsUpdatedAndSelected()
        {
            IReadOnlyList<AssetStoreToolsImportCandidate> candidates =
                AssetStoreToolsImportPlanner.Build(
                    CreateManifest("Foo", 4),
                    new Dictionary<string, int> { ["Foo"] = 2 });

            Assert.That(candidates[0].State, Is.EqualTo(AssetStoreToolsImportStateEnum.Updated));
            Assert.That(candidates[0].IsSelected, Is.True);
            Assert.That(candidates[0].LocalVersion, Is.EqualTo(2));
        }

        /// <summary> リビジョンが一致するパッケージは選択しない。 </summary>
        [Test]
        public void Build_SameLocalVersion_IsUpToDateAndNotSelected()
        {
            IReadOnlyList<AssetStoreToolsImportCandidate> candidates =
                AssetStoreToolsImportPlanner.Build(
                    CreateManifest("Foo", 4),
                    new Dictionary<string, int> { ["Foo"] = 4 });

            Assert.That(candidates[0].State, Is.EqualTo(AssetStoreToolsImportStateEnum.UpToDate));
            Assert.That(candidates[0].IsSelected, Is.False);
        }

        /// <summary> ローカルの方が新しいパッケージは既定で選択しない。 </summary>
        [Test]
        public void Build_NewerLocalVersion_IsNewerAndNotSelected()
        {
            IReadOnlyList<AssetStoreToolsImportCandidate> candidates =
                AssetStoreToolsImportPlanner.Build(
                    CreateManifest("Foo", 2),
                    new Dictionary<string, int> { ["Foo"] = 7 });

            Assert.That(candidates[0].State, Is.EqualTo(AssetStoreToolsImportStateEnum.Newer));
            Assert.That(candidates[0].IsSelected, Is.False);
        }

        /// <summary> ディレクトリ名の大文字小文字は区別しない。 </summary>
        [Test]
        public void Build_NameIsCaseInsensitive_MatchesLocalVersion()
        {
            IReadOnlyList<AssetStoreToolsImportCandidate> candidates =
                AssetStoreToolsImportPlanner.Build(
                    CreateManifest("Demigiant", 3),
                    new Dictionary<string, int> { ["DEMIGIANT"] = 3 });

            Assert.That(candidates[0].State, Is.EqualTo(AssetStoreToolsImportStateEnum.UpToDate));
        }

        /// <summary> 名前が空のエントリはインポート先を特定できないため捨てる。 </summary>
        [Test]
        public void Build_EntryWithEmptyName_IsSkipped()
        {
            var manifest = new AssetStoreToolsPackageManifest
            {
                Packages = new List<AssetStoreToolsPackageManifestEntry>
                {
                    new() { Name = "  ", Version = 1, FileName = "Foo.unitypackage" },
                    new() { Name = "Foo", Version = 1, FileName = "Foo.unitypackage" },
                },
            };

            IReadOnlyList<AssetStoreToolsImportCandidate> candidates =
                AssetStoreToolsImportPlanner.Build(manifest, new Dictionary<string, int>());

            Assert.That(candidates.Count, Is.EqualTo(1));
            Assert.That(candidates[0].Name, Is.EqualTo("Foo"));
        }

        /// <summary> ファイル名が空のエントリはインポートできないため捨てる。 </summary>
        [Test]
        public void Build_EntryWithEmptyFileName_IsSkipped()
        {
            var manifest = new AssetStoreToolsPackageManifest
            {
                Packages = new List<AssetStoreToolsPackageManifestEntry>
                {
                    new() { Name = "Foo", Version = 1, FileName = string.Empty },
                },
            };

            IReadOnlyList<AssetStoreToolsImportCandidate> candidates =
                AssetStoreToolsImportPlanner.Build(manifest, new Dictionary<string, int>());

            Assert.That(candidates, Is.Empty);
        }

        /// <summary> マニフェストを読み込めなかった場合は空一覧になる。 </summary>
        [Test]
        public void Build_NullManifest_ReturnsEmpty()
        {
            IReadOnlyList<AssetStoreToolsImportCandidate> candidates =
                AssetStoreToolsImportPlanner.Build(null, new Dictionary<string, int>());

            Assert.That(candidates, Is.Empty);
        }

        /// <summary> ローカルのリビジョン一覧が無くても新規として扱える。 </summary>
        [Test]
        public void Build_NullLocalVersions_IsNew()
        {
            IReadOnlyList<AssetStoreToolsImportCandidate> candidates =
                AssetStoreToolsImportPlanner.Build(CreateManifest("Foo", 1), null);

            Assert.That(candidates[0].State, Is.EqualTo(AssetStoreToolsImportStateEnum.New));
        }

        /// <summary> 1件だけ持つマニフェストを生成する。 </summary>
        /// <param name="name"> パッケージ対象ディレクトリの名前。 </param>
        /// <param name="version"> マニフェストが持つリビジョン。 </param>
        /// <returns> 指定した1件を持つマニフェスト。 </returns>
        private static AssetStoreToolsPackageManifest CreateManifest(string name, int version) => new()
        {
            Packages = new List<AssetStoreToolsPackageManifestEntry>
            {
                new()
                {
                    Name = name,
                    Version = version,
                    FileName = $"{name}.unitypackage",
                },
            },
        };
    }
}
