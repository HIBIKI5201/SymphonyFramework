using System.Threading.Tasks;

using NUnit.Framework;

using SymphonyFramework.Scenes;

using UnityEngine;

namespace SymphonyFramework.Tests
{
    /// <summary>
    ///     AttaccaがSuiteの保持中シーンを単体アンロードから守る契約を検証する。
    /// </summary>
    public sealed class AttaccaTests
    {
        /// <summary>
        ///     Suiteが保持中のシーンは、何もアンロードせず失敗として返す。
        /// </summary>
        [Test]
        public async Task UnloadAsync_SceneHeldBySuite_ReturnsFalse()
        {
            Attacca.Initialize();
            Attacca.SetSuiteHoldQuery(sceneName => sceneName == "Field");

            Awaitable<bool> unload = Attacca.UnloadAsync("Field");

            Assert.That(await unload, Is.False);
        }

        /// <summary>
        ///     複数指定に保持中のシーンが1つでも含まれる場合は、どれもアンロードしない。
        /// </summary>
        [Test]
        public async Task UnloadAsync_ScenesContainHeldScene_ReturnsFalse()
        {
            Attacca.Initialize();
            Attacca.SetSuiteHoldQuery(sceneName => sceneName == "Field");

            Awaitable<bool> unload = Attacca.UnloadAsync(new[] { "Title", "Field" });

            Assert.That(await unload, Is.False);
        }

        /// <summary>
        ///     ResetRuntimeStateは保持の判定も外し、次回Play Modeへ持ち越さない。
        /// </summary>
        [Test]
        public void ResetRuntimeState_ClearsSuiteHoldQuery()
        {
            Attacca.Initialize();
            Attacca.SetSuiteHoldQuery(_ => true);

            Attacca.ResetRuntimeState();

            Assert.That(Attacca.IsSuiteHoldQuerySet, Is.False);
        }

        /// <summary>
        ///     テストごとに未初期化の状態から始める。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            Attacca.ResetRuntimeState();
        }

        /// <summary>
        ///     テストで初期化した状態を残さない。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            Attacca.ResetRuntimeState();
        }
    }
}
