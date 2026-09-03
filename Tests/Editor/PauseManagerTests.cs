using System;
using System.Collections;
using System.Collections.Generic;

using NUnit.Framework;

using SymphonyFrameWork.Exceptions;
using SymphonyFrameWork.System;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     PauseManagerのカテゴリー版公開APIを検証する。
    /// </summary>
    /// <remarks>
    ///     staticな状態を持つため、テストごとに初期化と破棄を対で行う。
    ///     Domain Reloadが無効なプロジェクトのため、後始末を怠ると次のテストへ状態が漏れる。
    /// </remarks>
    public sealed class PauseManagerTests
    {
        /// <summary> 検証用のカテゴリー。 </summary>
        private interface IGameplayCategory : PauseManager.IPausable { }

        /// <summary> 検証用のカテゴリー。 </summary>
        private interface IUiCategory : PauseManager.IPausable { }

        /// <summary> カテゴリーではない検証用の具象型。 </summary>
        private sealed class ConcretePausable : PauseManager.IPausable
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
            PauseManager.ResetRuntimeState();
        }

        /// <summary> 次のテストへstatic状態を残さない。 </summary>
        [TearDown]
        public void TearDown()
        {
            PauseManager.ResetRuntimeState();
        }

        /// <summary> 未初期化のカテゴリー操作は初期化前アクセスとして拒否する。 </summary>
        [Test]
        public void SetPause_NotInitialized_Throws()
        {
            Assert.That(
                () => PauseManager.SetPause<IGameplayCategory>(true),
                Throws.TypeOf<SymphonyNotInitializedException>());
        }

        /// <summary> 未初期化の状態照会は初期化前アクセスとして拒否する。 </summary>
        [Test]
        public void IsPaused_NotInitialized_Throws()
        {
            Assert.That(
                () => PauseManager.IsPaused<IGameplayCategory>(),
                Throws.TypeOf<SymphonyNotInitializedException>());
        }

        /// <summary> 未初期化の一括操作は初期化前アクセスとして拒否する。 </summary>
        [Test]
        public void SetPauseAll_NotInitialized_Throws()
        {
            Assert.That(
                () => PauseManager.SetPauseAll(true),
                Throws.TypeOf<SymphonyNotInitializedException>());
            Assert.That(
                () => PauseManager.IsPausedAny(),
                Throws.TypeOf<SymphonyNotInitializedException>());
        }

        /// <summary> 未初期化のカテゴリー情報取得は初期化前アクセスとして拒否する。 </summary>
        [Test]
        public void GetPauseInfo_NotInitialized_Throws()
        {
            Assert.That(
                () => PauseManager.GetPauseInfo<IGameplayCategory>(),
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
            PauseManager.Initialize();

            Assert.That(
                () => PauseManager.SetPause<ConcretePausable>(true),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => PauseManager.IsPaused<ConcretePausable>(),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => PauseManager.GetPauseInfo<ConcretePausable>(),
                Throws.TypeOf<ArgumentException>());
        }

        /// <summary>
        ///     IPausable自身は既定カテゴリーとして指定できる。
        /// </summary>
        /// <remarks> カテゴリーを明示していない対象を指す。 </remarks>
        [Test]
        public void SetPause_IPausableItself_IsAllowed()
        {
            PauseManager.Initialize();

            Assert.DoesNotThrow(() => PauseManager.SetPause<PauseManager.IPausable>(true));
            Assert.That(PauseManager.IsPaused<PauseManager.IPausable>(), Is.True);
        }

        /// <summary> カテゴリーごとに独立してポーズ状態を持つ。 </summary>
        [Test]
        public void SetPause_PerCategory_IsIndependent()
        {
            PauseManager.Initialize();

            PauseManager.SetPause<IGameplayCategory>(true);

            Assert.That(PauseManager.IsPaused<IGameplayCategory>(), Is.True);
            Assert.That(PauseManager.IsPaused<IUiCategory>(), Is.False);
            Assert.That(PauseManager.IsPausedAny(), Is.True);
        }

        /// <summary> 一括操作は全カテゴリーへ届く。 </summary>
        [Test]
        public void SetPauseAll_AffectsEveryCategory()
        {
            PauseManager.Initialize();
            GameplayPausable pausable = new();
            PauseManager.IPausable.RegisterPauseManager(pausable);

            PauseManager.SetPauseAll(true);

            Assert.That(PauseManager.IsPaused<IGameplayCategory>(), Is.True);
            Assert.That(pausable.PauseCount, Is.EqualTo(1));
        }

        /// <summary> 一括解除で全カテゴリーが解除される。 </summary>
        [Test]
        public void SetPauseAll_False_ReleasesEveryCategory()
        {
            PauseManager.Initialize();
            PauseManager.SetPause<IGameplayCategory>(true);
            PauseManager.SetPause<IUiCategory>(true);

            PauseManager.SetPauseAll(false);

            Assert.That(PauseManager.IsPausedAny(), Is.False);
        }

        /// <summary> カテゴリー単位の購読者へ、そのカテゴリーの変化だけが届く。 </summary>
        [Test]
        public void AddPauseChangedHandler_ReceivesOnlyOwnCategory()
        {
            PauseManager.Initialize();
            List<bool> received = new();
            PauseManager.AddPauseChangedHandler<IGameplayCategory>(received.Add);

            PauseManager.SetPause<IUiCategory>(true);
            PauseManager.SetPause<IGameplayCategory>(true);

            Assert.That(received, Is.EqualTo(new[] { true }));
        }

        /// <summary> 購読解除後は通知が届かない。 </summary>
        [Test]
        public void RemovePauseChangedHandler_StopsNotification()
        {
            PauseManager.Initialize();
            List<bool> received = new();
            Action<bool> handler = received.Add;
            PauseManager.AddPauseChangedHandler<IGameplayCategory>(handler);
            PauseManager.RemovePauseChangedHandler<IGameplayCategory>(handler);

            PauseManager.SetPause<IGameplayCategory>(true);

            Assert.That(received, Is.Empty);
        }

        /// <summary> カテゴリー単位の管理状態を取得できる。 </summary>
        [Test]
        public void GetPauseInfo_Category_ReflectsCategoryState()
        {
            PauseManager.Initialize();
            PauseManager.IPausable.RegisterPauseManager(new GameplayPausable());
            PauseManager.SetPause<IGameplayCategory>(true);

            PauseInfo info = PauseManager.GetPauseInfo<IGameplayCategory>();

            Assert.That(info.IsPaused, Is.True);
            Assert.That(info.PausableSubscriberCount, Is.EqualTo(1));
            Assert.That(info.Category, Is.EqualTo(typeof(IGameplayCategory)));
        }

        /// <summary> 全体の管理状態はカテゴリーを持たない。 </summary>
        [Test]
        public void GetPauseInfo_Overall_HasNoCategory()
        {
            PauseManager.Initialize();

            PauseInfo info = PauseManager.GetPauseInfo();

            Assert.That(info.Category, Is.Null);
        }

        /// <summary>
        ///     既存の Pause プロパティは全カテゴリーへ対応する。
        /// </summary>
        /// <remarks> 従来の利用コードの挙動を変えないことを保証する。 </remarks>
        [Test]
        public void Pause_Property_MatchesSetPauseAll()
        {
            PauseManager.Initialize();
            GameplayPausable pausable = new();
            PauseManager.IPausable.RegisterPauseManager(pausable);

            // 非推奨APIの互換性そのものを検証するテストであるため、警告だけを抑止する。
#pragma warning disable 618
            PauseManager.Pause = true;

            Assert.That(PauseManager.Pause, Is.True);
            Assert.That(PauseManager.IsPaused<IGameplayCategory>(), Is.True);
            Assert.That(pausable.PauseCount, Is.EqualTo(1));

            PauseManager.Pause = false;

            Assert.That(PauseManager.Pause, Is.False);
#pragma warning restore 618
            Assert.That(pausable.ResumeCount, Is.EqualTo(1));
        }

        /// <summary>
        ///     待機系APIでもカテゴリーではない具象型は拒否する。
        /// </summary>
        /// <remarks>
        ///     **検証は同期部分で行う。** Awaitableを返した後に投げると、
        ///     呼び出し元のtry/catchでは捕まえられない。
        /// </remarks>
        [Test]
        public void PausableWaiters_ConcreteType_ThrowSynchronously()
        {
            PauseManager.Initialize();

            Assert.That(
                () => PauseManager.PausableNextFrameAsync<ConcretePausable>(),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => PauseManager.PausableWaitForSecondAsync<ConcretePausable>(0.1f),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => PauseManager.PausableWaitUntil<ConcretePausable>(() => true),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => PauseManager.PausableInvoke<ConcretePausable>(() => { }, 0.1f),
                Throws.TypeOf<ArgumentException>());
        }

        /// <summary>
        ///     PausableWaitForSecondの検証は最初のMoveNextまで遅延する。
        /// </summary>
        /// <remarks>
        ///     イテレータであるという従来からの挙動を保っている。
        ///     Enumeratorを受け取るだけでは何も起きない。
        /// </remarks>
        [Test]
        public void PausableWaitForSecond_ValidationIsDeferredToFirstMoveNext()
        {
            PauseManager.Initialize();

            IEnumerator enumerator = PauseManager.PausableWaitForSecond<ConcretePausable>(0.1f);

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
            PauseManager.Initialize();
            PauseManager.SetPause<IGameplayCategory>(true);

            PauseManager.ResetRuntimeState();

            Assert.That(
                () => PauseManager.IsPaused<IGameplayCategory>(),
                Throws.TypeOf<SymphonyNotInitializedException>());

            PauseManager.Initialize();
            Assert.That(PauseManager.IsPaused<IGameplayCategory>(), Is.False);
        }
    }
}
