using System;
using System.Collections;
using System.Collections.Generic;

using NUnit.Framework;

using SymphonyFramework.Exceptions;
using SymphonyFramework.Pause;

namespace SymphonyFramework.Tests
{
    /// <summary>
    ///     Fermataのカテゴリー版公開APIを検証する。
    /// </summary>
    /// <remarks>
    ///     staticな状態を持つため、テストごとに初期化と破棄を対で行う。
    ///     Domain Reloadが無効なプロジェクトのため、後始末を怠ると次のテストへ状態が漏れる。
    /// </remarks>
    public sealed class FermataTests
    {
        /// <summary> 検証用のカテゴリー。 </summary>
        private interface IGameplayCategory : IPausable { }

        /// <summary> 検証用のカテゴリー。 </summary>
        private interface IUiCategory : IPausable { }

        /// <summary> カテゴリーではない検証用の具象型。 </summary>
        private sealed class ConcretePausable : IPausable
        {
            public void Pause() { }
            public void Resume() { }
        }

        /// <summary> ゲームプレイカテゴリーの検証用ポーズ対象。 </summary>
        private sealed class GameplayPausable : IGameplayCategory
        {
            public int PauseCount { get; private set; }
            public int ResumeCount { get; private set; }

            public void Pause() => PauseCount++;
            public void Resume() => ResumeCount++;
        }

        /// <summary> 前のテストのstatic状態を持ち越さない。 </summary>
        [SetUp]
        public void SetUp()
        {
            Fermata.ResetRuntimeState();
        }

        /// <summary> 次のテストへstatic状態を残さない。 </summary>
        [TearDown]
        public void TearDown()
        {
            Fermata.ResetRuntimeState();
        }

        /// <summary> 未初期化のカテゴリー操作は初期化前アクセスとして拒否する。 </summary>
        [Test]
        public void SetPause_NotInitialized_Throws()
        {
            Assert.That(
                () => Fermata.SetPause<IGameplayCategory>(true),
                Throws.TypeOf<SymphonyNotInitializedException>());
        }

        /// <summary> 未初期化の状態照会は初期化前アクセスとして拒否する。 </summary>
        [Test]
        public void IsPaused_NotInitialized_Throws()
        {
            Assert.That(
                () => Fermata.IsPaused<IGameplayCategory>(),
                Throws.TypeOf<SymphonyNotInitializedException>());
        }

        /// <summary> 未初期化の一括操作は初期化前アクセスとして拒否する。 </summary>
        [Test]
        public void SetPauseAll_NotInitialized_Throws()
        {
            Assert.That(
                () => Fermata.SetPauseAll(true),
                Throws.TypeOf<SymphonyNotInitializedException>());
            Assert.That(
                () => Fermata.IsPausedAny(),
                Throws.TypeOf<SymphonyNotInitializedException>());
        }

        /// <summary> 未初期化のカテゴリー情報取得は初期化前アクセスとして拒否する。 </summary>
        [Test]
        public void GetPauseInfo_NotInitialized_Throws()
        {
            Assert.That(
                () => Fermata.GetPauseInfo<IGameplayCategory>(),
                Throws.TypeOf<SymphonyNotInitializedException>());
        }

        /// <summary>
        ///     カテゴリーではない具象型の指定は拒否する。
        /// </summary>
        /// <remarks>
        ///     **型制約だけでは表現できない。** <c>where TCategory : IPausable</c> は
        ///     IPausableを実装した具象クラスも通してしまう。
        /// </remarks>
        [Test]
        public void SetPause_ConcreteType_Throws()
        {
            Fermata.Initialize();

            Assert.That(
                () => Fermata.SetPause<ConcretePausable>(true),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => Fermata.IsPaused<ConcretePausable>(),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => Fermata.GetPauseInfo<ConcretePausable>(),
                Throws.TypeOf<ArgumentException>());
        }

        /// <summary>
        ///     IPausable自身は既定カテゴリーとして指定できる。
        /// </summary>
        /// <remarks> カテゴリーを明示していない対象を指す。 </remarks>
        [Test]
        public void SetPause_IPausableItself_IsAllowed()
        {
            Fermata.Initialize();

            Assert.DoesNotThrow(() => Fermata.SetPause<IPausable>(true));
            Assert.That(Fermata.IsPaused<IPausable>(), Is.True);
        }

        /// <summary> カテゴリーごとに独立してポーズ状態を持つ。 </summary>
        [Test]
        public void SetPause_PerCategory_IsIndependent()
        {
            Fermata.Initialize();

            Fermata.SetPause<IGameplayCategory>(true);

            Assert.That(Fermata.IsPaused<IGameplayCategory>(), Is.True);
            Assert.That(Fermata.IsPaused<IUiCategory>(), Is.False);
            Assert.That(Fermata.IsPausedAny(), Is.True);
        }

        /// <summary> 一括操作は全カテゴリーへ届く。 </summary>
        [Test]
        public void SetPauseAll_AffectsEveryCategory()
        {
            Fermata.Initialize();
            GameplayPausable pausable = new();
            Fermata.Register(pausable);

            Fermata.SetPauseAll(true);

            Assert.That(Fermata.IsPaused<IGameplayCategory>(), Is.True);
            Assert.That(pausable.PauseCount, Is.EqualTo(1));
        }

        /// <summary> 一括解除で全カテゴリーが解除される。 </summary>
        [Test]
        public void SetPauseAll_False_ReleasesEveryCategory()
        {
            Fermata.Initialize();
            Fermata.SetPause<IGameplayCategory>(true);
            Fermata.SetPause<IUiCategory>(true);

            Fermata.SetPauseAll(false);

            Assert.That(Fermata.IsPausedAny(), Is.False);
        }

        /// <summary> カテゴリー単位の購読者へ、そのカテゴリーの変化だけが届く。 </summary>
        [Test]
        public void AddPauseChangedHandler_ReceivesOnlyOwnCategory()
        {
            Fermata.Initialize();
            List<bool> received = new();
            Fermata.AddPauseChangedHandler<IGameplayCategory>(received.Add);

            Fermata.SetPause<IUiCategory>(true);
            Fermata.SetPause<IGameplayCategory>(true);

            Assert.That(received, Is.EqualTo(new[] { true }));
        }

        /// <summary> 購読解除後は通知が届かない。 </summary>
        [Test]
        public void RemovePauseChangedHandler_StopsNotification()
        {
            Fermata.Initialize();
            List<bool> received = new();
            Action<bool> handler = received.Add;
            Fermata.AddPauseChangedHandler<IGameplayCategory>(handler);
            Fermata.RemovePauseChangedHandler<IGameplayCategory>(handler);

            Fermata.SetPause<IGameplayCategory>(true);

            Assert.That(received, Is.Empty);
        }

        /// <summary> カテゴリー単位の管理状態を取得できる。 </summary>
        [Test]
        public void GetPauseInfo_Category_ReflectsCategoryState()
        {
            Fermata.Initialize();
            Fermata.Register(new GameplayPausable());
            Fermata.SetPause<IGameplayCategory>(true);

            PauseInfo info = Fermata.GetPauseInfo<IGameplayCategory>();

            Assert.That(info.IsPaused, Is.True);
            Assert.That(info.PausableSubscriberCount, Is.EqualTo(1));
            Assert.That(info.Category, Is.EqualTo(typeof(IGameplayCategory)));
        }

        /// <summary> 全体の管理状態はカテゴリーを持たない。 </summary>
        [Test]
        public void GetPauseInfo_Overall_HasNoCategory()
        {
            Fermata.Initialize();

            PauseInfo info = Fermata.GetPauseInfo();

            Assert.That(info.Category, Is.Null);
        }

        /// <summary>
        ///     カテゴリーを指定しない通知は、全カテゴリーをまとめた状態の変化だけを受け取る。
        /// </summary>
        /// <remarks> 2つ目のカテゴリーを止めても「いずれかがポーズ中」は変わらないため通知しない。 </remarks>
        [Test]
        public void AddPauseChangedHandler_WithoutCategory_ReceivesOverallChanges()
        {
            Fermata.Initialize();
            List<bool> received = new();
            Action<bool> handler = received.Add;
            Fermata.AddPauseChangedHandler(handler);

            Fermata.SetPause<IGameplayCategory>(true);
            Fermata.SetPause<IUiCategory>(true);
            Fermata.SetPauseAll(false);
            Fermata.RemovePauseChangedHandler(handler);
            Fermata.SetPauseAll(true);

            Assert.That(received, Is.EqualTo(new[] { true, false }));
        }

        /// <summary> カテゴリーを指定しない通知も、未初期化では例外を投げる。 </summary>
        [Test]
        public void AddPauseChangedHandler_WithoutCategory_NotInitialized_Throws()
        {
            Assert.Throws<SymphonyNotInitializedException>(() => Fermata.AddPauseChangedHandler(_ => { }));
        }

        /// <summary>
        ///     待機系APIでもカテゴリーではない具象型は拒否する。
        /// </summary>
        /// <remarks>
        ///     **検証は同期部分で行う。** Awaitableを返した後に投げると、
        ///     呼び出し元のtry/catchでは捕まえられない。
        /// </remarks>
        [Test]
        public void Waiters_ConcreteType_ThrowSynchronously()
        {
            Fermata.Initialize();

            Assert.That(
                () => Fermata.NextFrameAsync<ConcretePausable>(),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => Fermata.WaitForSecondsAsync<ConcretePausable>(0.1f),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => Fermata.WaitUntilAsync<ConcretePausable>(() => true),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => Fermata.InvokeAsync<ConcretePausable>(() => { }, 0.1f),
                Throws.TypeOf<ArgumentException>());
        }

        /// <summary>
        ///     WaitForSecondsの検証は最初のMoveNextまで遅延する。
        /// </summary>
        /// <remarks>
        ///     イテレータであるという従来からの挙動を保っている。
        ///     Enumeratorを受け取るだけでは何も起きない。
        /// </remarks>
        [Test]
        public void WaitForSeconds_ValidationIsDeferredToFirstMoveNext()
        {
            Fermata.Initialize();

            IEnumerator enumerator = Fermata.WaitForSeconds<ConcretePausable>(0.1f);

            Assert.That(enumerator, Is.Not.Null, "Enumeratorを受け取るだけでは投げない。");
            Assert.That(() => enumerator.MoveNext(), Throws.TypeOf<ArgumentException>());
        }

        /// <summary>
        ///     ResetRuntimeStateでカテゴリー状態と購読が消える。
        /// </summary>
        /// <remarks>
        ///     Domain Reloadが無効なため、Play Mode終了時にstatic状態は自動で戻らない。
        /// </remarks>
        [Test]
        public void ResetRuntimeState_ClearsCategoriesAndUninitializes()
        {
            Fermata.Initialize();
            Fermata.SetPause<IGameplayCategory>(true);

            Fermata.ResetRuntimeState();

            Assert.That(
                () => Fermata.IsPaused<IGameplayCategory>(),
                Throws.TypeOf<SymphonyNotInitializedException>());

            Fermata.Initialize();
            Assert.That(Fermata.IsPaused<IGameplayCategory>(), Is.False);
        }
    }
}
