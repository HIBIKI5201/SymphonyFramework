using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

using SymphonyFramework.Scenes;

namespace SymphonyFramework.Tests
{
    /// <summary>
    ///     SuiteGraphPlannerのトポロジカル層とグラフ検証を検証する。
    /// </summary>
    public sealed class SuiteGraphPlannerTests
    {
        /// <summary>
        ///     直列依存を1ノードずつの連続した層へ分割する。
        /// </summary>
        [Test]
        public void Plan_LinearChain_ReturnsSequentialLayers()
        {
            string[] nodeIds = { "A", "B", "C" };
            SuiteEdge[] edges =
            {
                new("A", "B"),
                new("B", "C")
            };

            SuitePlanResult result = SuiteGraphPlanner.Plan(nodeIds, edges);

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
            SuiteEdge[] edges =
            {
                new("A", "B"),
                new("A", "C"),
                new("B", "D"),
                new("C", "E")
            };

            SuitePlanResult result = SuiteGraphPlanner.Plan(nodeIds, edges);

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
            SuitePlanResult result = SuiteGraphPlanner.Plan(
                Array.Empty<string>(),
                Array.Empty<SuiteEdge>());

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
            SuitePlanResult result = SuiteGraphPlanner.Plan(
                new[] { "A" },
                Array.Empty<SuiteEdge>());

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Layers, Is.EqualTo(new[] { new[] { "A" } }));
        }

        /// <summary>
        ///     重複するノード識別子をDuplicateNodeとして返す。
        /// </summary>
        [Test]
        public void Plan_DuplicateNodeId_ReturnsDuplicateNodeError()
        {
            SuitePlanResult result = SuiteGraphPlanner.Plan(
                new[] { "A", "A" },
                Array.Empty<SuiteEdge>());

            SuitePlanError error = result.Errors.Single(
                candidate => candidate.Kind == SuitePlanErrorEnum.DuplicateNode);
            Assert.That(error.NodeIds, Is.EqualTo(new[] { "A" }));
        }

        /// <summary>
        ///     自身を参照する依存辺をSelfDependencyとして返す。
        /// </summary>
        [Test]
        public void Plan_SelfDependency_ReturnsSelfDependencyError()
        {
            SuitePlanResult result = SuiteGraphPlanner.Plan(
                new[] { "A" },
                new[] { new SuiteEdge("A", "A") });

            SuitePlanError error = result.Errors.Single(
                candidate => candidate.Kind == SuitePlanErrorEnum.SelfDependency);
            Assert.That(error.NodeIds, Is.EqualTo(new[] { "A" }));
        }

        /// <summary>
        ///     存在しないノードへの依存辺をMissingReferenceとして返す。
        /// </summary>
        [Test]
        public void Plan_EdgeReferencesMissingNode_ReturnsMissingReferenceError()
        {
            SuitePlanResult result = SuiteGraphPlanner.Plan(
                new[] { "A" },
                new[] { new SuiteEdge("A", "Ghost") });

            SuitePlanError error = result.Errors.Single(
                candidate => candidate.Kind == SuitePlanErrorEnum.MissingReference);
            Assert.That(error.NodeIds, Is.EqualTo(new[] { "Ghost" }));
        }

        /// <summary>
        ///     複数の辺から参照される同じ欠落識別子を1件へ集約する。
        /// </summary>
        [Test]
        public void Plan_EdgeReferencedByMultipleEdges_ReturnsSingleMissingReferenceError()
        {
            SuitePlanResult result = SuiteGraphPlanner.Plan(
                new[] { "A", "B" },
                new[]
                {
                    new SuiteEdge("A", "Ghost"),
                    new SuiteEdge("B", "Ghost")
                });

            int missingReferenceCount = result.Errors.Count(
                error => error.Kind == SuitePlanErrorEnum.MissingReference);
            Assert.That(missingReferenceCount, Is.EqualTo(1));
        }

        /// <summary>
        ///     2ノードの循環依存を両ノードを含むCyclicDependencyとして返す。
        /// </summary>
        [Test]
        public void Plan_TwoNodeCycle_ReturnsCyclicDependencyError()
        {
            SuitePlanResult result = SuiteGraphPlanner.Plan(
                new[] { "A", "B" },
                new[]
                {
                    new SuiteEdge("A", "B"),
                    new SuiteEdge("B", "A")
                });

            SuitePlanError error = result.Errors.Single(
                candidate => candidate.Kind == SuitePlanErrorEnum.CyclicDependency);
            Assert.That(error.NodeIds, Is.EqualTo(new[] { "A", "B" }));
        }

        /// <summary>
        ///     3ノードの循環依存を全ノードを含むCyclicDependencyとして返す。
        /// </summary>
        [Test]
        public void Plan_ThreeNodeCycle_ReturnsCyclicDependencyErrorWithAllInvolvedNodes()
        {
            SuitePlanResult result = SuiteGraphPlanner.Plan(
                new[] { "C", "A", "B" },
                new[]
                {
                    new SuiteEdge("A", "B"),
                    new SuiteEdge("B", "C"),
                    new SuiteEdge("C", "A")
                });

            SuitePlanError error = result.Errors.Single(
                candidate => candidate.Kind == SuitePlanErrorEnum.CyclicDependency);
            Assert.That(error.NodeIds, Is.EqualTo(new[] { "A", "B", "C" }));
        }

        /// <summary>
        ///     循環に無関係な孤立ノードをCyclicDependencyへ含めない。
        /// </summary>
        [Test]
        public void Plan_CycleWithUnrelatedValidNode_DoesNotAffectIndependentNode()
        {
            SuitePlanResult result = SuiteGraphPlanner.Plan(
                new[] { "A", "B", "X" },
                new[]
                {
                    new SuiteEdge("A", "B"),
                    new SuiteEdge("B", "A")
                });

            SuitePlanError error = result.Errors.Single(
                candidate => candidate.Kind == SuitePlanErrorEnum.CyclicDependency);
            Assert.That(error.NodeIds, Is.EqualTo(new[] { "A", "B" }));
        }

        /// <summary>
        ///     自己依存辺を循環検出から除外して無関係な循環エラーを作らない。
        /// </summary>
        [Test]
        public void Plan_SelfDependencyEdge_ExcludedFromCycleDetection()
        {
            SuitePlanResult result = SuiteGraphPlanner.Plan(
                new[] { "A", "B" },
                new[]
                {
                    new SuiteEdge("A", "A"),
                    new SuiteEdge("A", "B")
                });

            Assert.That(
                result.Errors.Any(
                    error => error.Kind == SuitePlanErrorEnum.CyclicDependency),
                Is.False);
            Assert.That(
                result.Errors.Single().Kind,
                Is.EqualTo(SuitePlanErrorEnum.SelfDependency));
        }

        /// <summary>
        ///     nullのノード一覧をArgumentNullExceptionとして拒否する。
        /// </summary>
        [Test]
        public void Plan_NullNodeIds_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                SuiteGraphPlanner.Plan(
                    null,
                    Array.Empty<SuiteEdge>()));
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
                SuiteGraphPlanner.Plan(
                    new[] { nodeId },
                    Array.Empty<SuiteEdge>()));
        }
    }
}
