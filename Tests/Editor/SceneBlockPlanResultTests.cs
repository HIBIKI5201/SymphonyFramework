using System;
using System.Collections.Generic;

using NUnit.Framework;

using SymphonyFrameWork.System.SceneBlock;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     SceneBlockPlanResultの成功・失敗Factoryの契約を検証する。
    /// </summary>
    public sealed class SceneBlockPlanResultTests
    {
        /// <summary>
        ///     成功結果が実行層だけを保持する。
        /// </summary>
        [Test]
        public void Success_WithLayers_IsSuccessAndHasNoErrors()
        {
            IReadOnlyList<IReadOnlyList<string>> layers = new IReadOnlyList<string>[]
            {
                new[] { "A" },
                new[] { "B", "C" }
            };

            SceneBlockPlanResult result = SceneBlockPlanResult.Success(layers);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Layers, Is.EqualTo(layers));
            Assert.That(result.Errors, Is.Empty);
        }

        /// <summary>
        ///     失敗結果が検証エラーだけを保持する。
        /// </summary>
        [Test]
        public void Failure_WithErrors_IsNotSuccessAndHasNoLayers()
        {
            IReadOnlyList<SceneBlockPlanError> errors = new[]
            {
                new SceneBlockPlanError(
                    SceneBlockPlanErrorEnum.DuplicateNode,
                    new[] { "A" })
            };

            SceneBlockPlanResult result = SceneBlockPlanResult.Failure(errors);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Layers, Is.Empty);
            Assert.That(result.Errors, Is.EqualTo(errors));
        }
    }
}
