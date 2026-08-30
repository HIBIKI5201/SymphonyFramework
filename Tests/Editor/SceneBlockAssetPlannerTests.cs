using System;
using System.Linq;

using NUnit.Framework;

using SymphonyFrameWork.System.SceneBlock;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     SceneBlockAssetPlannerのAuthoring正規化とDomain委譲を検証する。
    /// </summary>
    public sealed class SceneBlockAssetPlannerTests
    {
        /// <summary> 直列依存をDomainと同じ連続した層として返す。 </summary>
        [Test]
        public void Plan_ValidLinearChain_ReturnsSequentialLayers()
        {
            string[] sceneIds = { "A", "B", "C" };
            SceneBlockEdgeAuthoring[] edges =
            {
                new("A", "B"),
                new("B", "C")
            };

            SceneBlockPlanResult result = SceneBlockAssetPlanner.Plan(sceneIds, edges);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(
                result.Layers,
                Is.EqualTo(new[]
                {
                    new[] { "A" },
                    new[] { "B" },
                    new[] { "C" }
                }));
        }

        /// <summary> nullのシーン一覧を空一覧として扱う。 </summary>
        [Test]
        public void Plan_NullSceneIds_TreatedAsEmpty()
        {
            SceneBlockPlanResult result = SceneBlockAssetPlanner.Plan(
                null,
                Array.Empty<SceneBlockEdgeAuthoring>());

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Layers, Is.Empty);
        }

        /// <summary> nullの依存辺一覧を空一覧として扱う。 </summary>
        [Test]
        public void Plan_NullEdges_TreatedAsEmpty()
        {
            SceneBlockPlanResult result = SceneBlockAssetPlanner.Plan(
                new[] { "A" },
                null);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Layers, Is.EqualTo(new[] { new[] { "A" } }));
        }

        /// <summary> null、空、空白のシーン識別子をグラフから除外する。 </summary>
        [Test]
        public void Plan_BlankSceneIdEntries_ExcludedFromGraph()
        {
            string[] sceneIds = { "A", "", "  ", null };

            SceneBlockPlanResult result = SceneBlockAssetPlanner.Plan(
                sceneIds,
                Array.Empty<SceneBlockEdgeAuthoring>());

            Assert.That(result.Layers, Is.EqualTo(new[] { new[] { "A" } }));
            Assert.That(result.Errors, Is.Empty);
        }

        /// <summary> 片端が空白の依存辺をグラフから除外する。 </summary>
        [Test]
        public void Plan_EdgeWithBlankEndpoint_ExcludedFromGraph()
        {
            SceneBlockEdgeAuthoring[] edges =
            {
                new("A", ""),
                new(null, "B")
            };

            SceneBlockPlanResult result = SceneBlockAssetPlanner.Plan(
                new[] { "A", "B" },
                edges);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Layers, Is.EqualTo(new[] { new[] { "A", "B" } }));
        }

        /// <summary> 実在しない非空白識別子をMissingReferenceとして返す。 </summary>
        [Test]
        public void Plan_EdgeReferencingMissingRealSceneId_ReturnsMissingReferenceError()
        {
            SceneBlockPlanResult result = SceneBlockAssetPlanner.Plan(
                new[] { "A" },
                new[] { new SceneBlockEdgeAuthoring("A", "Ghost") });

            SceneBlockPlanError error = result.Errors.Single(
                candidate => candidate.Kind == SceneBlockPlanErrorEnum.MissingReference);
            Assert.That(error.NodeIds, Is.EqualTo(new[] { "Ghost" }));
        }

        /// <summary> 重複するシーン識別子をDuplicateNodeとして返す。 </summary>
        [Test]
        public void Plan_DuplicateSceneIds_ReturnsDuplicateNodeError()
        {
            SceneBlockPlanResult result = SceneBlockAssetPlanner.Plan(
                new[] { "A", "A" },
                Array.Empty<SceneBlockEdgeAuthoring>());

            int duplicateNodeErrorCount = result.Errors.Count(
                error => error.Kind == SceneBlockPlanErrorEnum.DuplicateNode);
            Assert.That(duplicateNodeErrorCount, Is.EqualTo(1));
        }
    }
}
