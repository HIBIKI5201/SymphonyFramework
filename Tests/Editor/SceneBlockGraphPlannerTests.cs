using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

using SymphonyFrameWork.System.SceneBlock;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     SceneBlockGraphPlannerのトポロジカル層とグラフ検証を検証する。
    /// </summary>
    public sealed class SceneBlockGraphPlannerTests
    {
        /// <summary>
        ///     直列依存を1ノードずつの連続した層へ分割する。
        /// </summary>
        [Test]
        public void Plan_LinearChain_ReturnsSequentialLayers()
        {
            string[] nodeIds = { "A", "B", "C" };
            SceneBlockEdge[] edges =
            {
                new("A", "B"),
                new("B", "C")
            };

            SceneBlockPlanResult result = SceneBlockGraphPlanner.Plan(nodeIds, edges);

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

        /// <summary>
        ///     同時に実行可能になるノードを同じ層へまとめる。
        /// </summary>
        [Test]
        public void Plan_DiamondDependency_GroupsIndependentNodesInSameLayer()
        {
            string[] nodeIds = { "E", "C", "A", "D", "B" };
            SceneBlockEdge[] edges =
            {
                new("A", "B"),
                new("A", "C"),
                new("B", "D"),
                new("C", "E")
            };

            SceneBlockPlanResult result = SceneBlockGraphPlanner.Plan(nodeIds, edges);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(
                result.Layers,
                Is.EqualTo(new[]
                {
                    new[] { "A" },
                    new[] { "B", "C" },
                    new[] { "D", "E" }
                }));
        }

        /// <summary>
        ///     空グラフを層の無い成功結果として返す。
        /// </summary>
        [Test]
        public void Plan_NoNodesNoEdges_ReturnsSuccessWithNoLayers()
        {
            SceneBlockPlanResult result = SceneBlockGraphPlanner.Plan(
                Array.Empty<string>(),
                Array.Empty<SceneBlockEdge>());

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Layers, Is.Empty);
            Assert.That(result.Errors, Is.Empty);
        }

        /// <summary>
        ///     孤立ノードを1件だけの層として返す。
        /// </summary>
        [Test]
        public void Plan_IsolatedNode_ReturnsSingleLayer()
        {
            SceneBlockPlanResult result = SceneBlockGraphPlanner.Plan(
                new[] { "A" },
                Array.Empty<SceneBlockEdge>());

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Layers, Is.EqualTo(new[] { new[] { "A" } }));
        }

        /// <summary>
        ///     重複するノード識別子をDuplicateNodeとして返す。
        /// </summary>
        [Test]
        public void Plan_DuplicateNodeId_ReturnsDuplicateNodeError()
        {
            SceneBlockPlanResult result = SceneBlockGraphPlanner.Plan(
                new[] { "A", "A" },
                Array.Empty<SceneBlockEdge>());

            SceneBlockPlanError error = result.Errors.Single(
                candidate => candidate.Kind == SceneBlockPlanErrorEnum.DuplicateNode);
            Assert.That(error.NodeIds, Is.EqualTo(new[] { "A" }));
        }

        /// <summary>
        ///     自身を参照する依存辺をSelfDependencyとして返す。
        /// </summary>
        [Test]
        public void Plan_SelfDependency_ReturnsSelfDependencyError()
        {
            SceneBlockPlanResult result = SceneBlockGraphPlanner.Plan(
                new[] { "A" },
                new[] { new SceneBlockEdge("A", "A") });

            SceneBlockPlanError error = result.Errors.Single(
                candidate => candidate.Kind == SceneBlockPlanErrorEnum.SelfDependency);
            Assert.That(error.NodeIds, Is.EqualTo(new[] { "A" }));
        }

        /// <summary>
        ///     存在しないノードへの依存辺をMissingReferenceとして返す。
        /// </summary>
        [Test]
        public void Plan_EdgeReferencesMissingNode_ReturnsMissingReferenceError()
        {
            SceneBlockPlanResult result = SceneBlockGraphPlanner.Plan(
                new[] { "A" },
                new[] { new SceneBlockEdge("A", "Ghost") });

            SceneBlockPlanError error = result.Errors.Single(
                candidate => candidate.Kind == SceneBlockPlanErrorEnum.MissingReference);
            Assert.That(error.NodeIds, Is.EqualTo(new[] { "Ghost" }));
        }

        /// <summary>
        ///     複数の辺から参照される同じ欠落識別子を1件へ集約する。
        /// </summary>
        [Test]
        public void Plan_EdgeReferencedByMultipleEdges_ReturnsSingleMissingReferenceError()
        {
            SceneBlockPlanResult result = SceneBlockGraphPlanner.Plan(
                new[] { "A", "B" },
                new[]
                {
                    new SceneBlockEdge("A", "Ghost"),
                    new SceneBlockEdge("B", "Ghost")
                });

            int missingReferenceCount = result.Errors.Count(
                error => error.Kind == SceneBlockPlanErrorEnum.MissingReference);
            Assert.That(missingReferenceCount, Is.EqualTo(1));
        }

        /// <summary>
        ///     2ノードの循環依存を両ノードを含むCyclicDependencyとして返す。
        /// </summary>
        [Test]
        public void Plan_TwoNodeCycle_ReturnsCyclicDependencyError()
        {
            SceneBlockPlanResult result = SceneBlockGraphPlanner.Plan(
                new[] { "A", "B" },
                new[]
                {
                    new SceneBlockEdge("A", "B"),
                    new SceneBlockEdge("B", "A")
                });

            SceneBlockPlanError error = result.Errors.Single(
                candidate => candidate.Kind == SceneBlockPlanErrorEnum.CyclicDependency);
            Assert.That(error.NodeIds, Is.EqualTo(new[] { "A", "B" }));
        }

        /// <summary>
        ///     3ノードの循環依存を全ノードを含むCyclicDependencyとして返す。
        /// </summary>
        [Test]
        public void Plan_ThreeNodeCycle_ReturnsCyclicDependencyErrorWithAllInvolvedNodes()
        {
            SceneBlockPlanResult result = SceneBlockGraphPlanner.Plan(
                new[] { "C", "A", "B" },
                new[]
                {
                    new SceneBlockEdge("A", "B"),
                    new SceneBlockEdge("B", "C"),
                    new SceneBlockEdge("C", "A")
                });

            SceneBlockPlanError error = result.Errors.Single(
                candidate => candidate.Kind == SceneBlockPlanErrorEnum.CyclicDependency);
            Assert.That(error.NodeIds, Is.EqualTo(new[] { "A", "B", "C" }));
        }

        /// <summary>
        ///     循環に無関係な孤立ノードをCyclicDependencyへ含めない。
        /// </summary>
        [Test]
        public void Plan_CycleWithUnrelatedValidNode_DoesNotAffectIndependentNode()
        {
            SceneBlockPlanResult result = SceneBlockGraphPlanner.Plan(
                new[] { "A", "B", "X" },
                new[]
                {
                    new SceneBlockEdge("A", "B"),
                    new SceneBlockEdge("B", "A")
                });

            SceneBlockPlanError error = result.Errors.Single(
                candidate => candidate.Kind == SceneBlockPlanErrorEnum.CyclicDependency);
            Assert.That(error.NodeIds, Is.EqualTo(new[] { "A", "B" }));
        }

        /// <summary>
        ///     自己依存辺を循環検出から除外して無関係な循環エラーを作らない。
        /// </summary>
        [Test]
        public void Plan_SelfDependencyEdge_ExcludedFromCycleDetection()
        {
            SceneBlockPlanResult result = SceneBlockGraphPlanner.Plan(
                new[] { "A", "B" },
                new[]
                {
                    new SceneBlockEdge("A", "A"),
                    new SceneBlockEdge("A", "B")
                });

            Assert.That(
                result.Errors.Any(
                    error => error.Kind == SceneBlockPlanErrorEnum.CyclicDependency),
                Is.False);
            Assert.That(
                result.Errors.Single().Kind,
                Is.EqualTo(SceneBlockPlanErrorEnum.SelfDependency));
        }

        /// <summary>
        ///     nullのノード一覧をArgumentNullExceptionとして拒否する。
        /// </summary>
        [Test]
        public void Plan_NullNodeIds_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                SceneBlockGraphPlanner.Plan(
                    null,
                    Array.Empty<SceneBlockEdge>()));
        }

        /// <summary>
        ///     null、空、空白のノード識別子をArgumentExceptionとして拒否する。
        /// </summary>
        /// <param name="nodeId"> 無効なノード識別子。 </param>
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Plan_NodeIdIsNullOrWhitespace_ThrowsArgumentException(string nodeId)
        {
            Assert.Throws<ArgumentException>(() =>
                SceneBlockGraphPlanner.Plan(
                    new[] { nodeId },
                    Array.Empty<SceneBlockEdge>()));
        }
    }
}
