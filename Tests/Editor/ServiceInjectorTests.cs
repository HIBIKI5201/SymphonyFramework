using System;

using NUnit.Framework;

using SymphonyFrameWork.System.ServiceLocate;

using UnityEngine;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     ServiceInjectorのコンストラクタ注入と、既存の自動注入が両立することを検証する。
    /// </summary>
    public sealed class ServiceInjectorTests
    {
        /// <summary>
        ///     登録済みの依存をコンストラクタへ渡して生成する。
        /// </summary>
        [Test]
        public void CreateInstance_RegisteredDependencies_AreInjected()
        {
            AlphaDependency alpha = new();
            BetaDependency beta = new();
            ServiceLocator.RegisterInstance(typeof(AlphaDependency), alpha, LocateTypeEnum.Locator);
            ServiceLocator.RegisterInstance(typeof(BetaDependency), beta, LocateTypeEnum.Locator);

            TwoDependencies instance = ServiceInjector.CreateInstance<TwoDependencies>();

            Assert.That(instance.Alpha, Is.SameAs(alpha));
            Assert.That(instance.Beta, Is.SameAs(beta));
        }

        /// <summary>
        ///     依存の無い型は登録が無くても生成できる。
        /// </summary>
        [Test]
        public void CreateInstance_Parameterless_CreatesInstance()
        {
            NoDependency instance = ServiceInjector.CreateInstance<NoDependency>();

            Assert.That(instance, Is.Not.Null);
        }

        /// <summary>
        ///     依存が未登録の場合は、生成せずに型つきで通知する。
        /// </summary>
        [Test]
        public void CreateInstance_MissingDependency_Throws()
        {
            ServiceLocator.RegisterInstance(typeof(AlphaDependency), new AlphaDependency(), LocateTypeEnum.Locator);

            ServiceNotRegisteredException exception = Assert.Throws<ServiceNotRegisteredException>(
                () => ServiceInjector.CreateInstance<TwoDependencies>());

            Assert.That(exception.ServiceType, Is.EqualTo(typeof(BetaDependency)));
        }

        /// <summary>
        ///     Service Locatorの登録を破棄すると、以後は未登録として扱う。
        /// </summary>
        /// <remarks>
        ///     Domain Reloadが無効な環境で、古い登録を掴み続けないことを固定する。
        /// </remarks>
        [Test]
        public void CreateInstance_AfterResetRuntimeState_TreatsDependenciesAsMissing()
        {
            ServiceLocator.RegisterInstance(typeof(AlphaDependency), new AlphaDependency(), LocateTypeEnum.Locator);
            ServiceLocator.ResetRuntimeState();

            Assert.That(
                () => ServiceInjector.CreateInstance<OneDependency>(),
                Throws.TypeOf<ServiceNotRegisteredException>());
        }

        /// <summary>
        ///     生成対象が無い呼び出しは拒否する。
        /// </summary>
        [Test]
        public void CreateInstance_NullType_Throws()
        {
            Assert.That(
                () => ServiceInjector.CreateInstance(null),
                Throws.TypeOf<ArgumentNullException>());
        }

        /// <summary>
        ///     抽象型は生成できない。
        /// </summary>
        [Test]
        public void CreateInstance_AbstractType_Throws()
        {
            Assert.That(
                () => ServiceInjector.CreateInstance(typeof(AbstractDependency)),
                Throws.TypeOf<ArgumentException>());
        }

        /// <summary>
        ///     interfaceは生成できない。
        /// </summary>
        [Test]
        public void CreateInstance_InterfaceType_Throws()
        {
            Assert.That(
                () => ServiceInjector.CreateInstance(typeof(IDisposable)),
                Throws.TypeOf<ArgumentException>());
        }

        /// <summary>
        ///     コンストラクタが投げた例外を、包まずそのまま伝播する。
        /// </summary>
        [Test]
        public void CreateInstance_ConstructorThrows_PropagatesOriginalException()
        {
            Assert.That(
                () => ServiceInjector.CreateInstance<ThrowingConstructor>(),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo(ThrowingConstructor.MESSAGE));
        }

        /// <summary>
        ///     登録が無く既定値がある引数は、既定値のまま生成する。
        /// </summary>
        [Test]
        public void CreateInstance_MissingWithDefault_UsesDefaultValue()
        {
            ServiceLocator.RegisterInstance(typeof(AlphaDependency), new AlphaDependency(), LocateTypeEnum.Locator);

            WithDefaultValue instance = ServiceInjector.CreateInstance<WithDefaultValue>();

            Assert.That(instance.RetryCount, Is.EqualTo(3));
        }

        /// <summary>
        ///     CreateInstanceを足しても、既存の自動注入の対応表は壊れない。
        /// </summary>
        /// <remarks>
        ///     対応表は「Injectという名前の公開staticメソッド」だけを集める前提に依存している。
        /// </remarks>
        [Test]
        public void TryAutoInject_AfterAddingCreateInstance_StillInjects()
        {
            AlphaDependency alpha = new();
            ServiceLocator.RegisterInstance(typeof(AlphaDependency), alpha, LocateTypeEnum.Locator);
            InjectableTarget target = new();

            bool result = ServiceInjector.TryAutoInject(target);

            Assert.That(result, Is.True);
            Assert.That(target.Alpha, Is.SameAs(alpha));
        }

        /// <summary> テスト用のService Host。 </summary>
        private GameObject _hostObject;

        /// <summary>
        ///     各テスト用のService HostでService Locatorを初期化する。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _hostObject = new GameObject("Service Injector Test Host");
            ServiceHostComponent host = _hostObject.AddComponent<ServiceHostComponent>();
            ServiceLocator.Initialize(host);
        }

        /// <summary>
        ///     static状態とテスト用GameObjectを解放する。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            ServiceLocator.ResetRuntimeState();
            if (_hostObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_hostObject);
            }

            _hostObject = null;
        }

        /// <summary> 検証用の依存その1。 </summary>
        private sealed class AlphaDependency { }

        /// <summary> 検証用の依存その2。 </summary>
        private sealed class BetaDependency { }

        /// <summary> 抽象型の検証に使う型。 </summary>
        private abstract class AbstractDependency { }

        /// <summary> 依存を持たない検証用の型。 </summary>
        private sealed class NoDependency { }

        /// <summary> 依存を1件受け取る検証用の型。 </summary>
        private sealed class OneDependency
        {
            /// <summary>
            ///     依存を1件受け取る。
            /// </summary>
            /// <param name="alpha"> 依存その1。 </param>
            public OneDependency(AlphaDependency alpha) { }
        }

        /// <summary> 依存を2件受け取る検証用の型。 </summary>
        private sealed class TwoDependencies
        {
            /// <summary>
            ///     依存を2件受け取る。
            /// </summary>
            /// <param name="alpha"> 依存その1。 </param>
            /// <param name="beta"> 依存その2。 </param>
            public TwoDependencies(AlphaDependency alpha, BetaDependency beta)
            {
                Alpha = alpha;
                Beta = beta;
            }

            /// <summary> 受け取った依存その1。 </summary>
            internal AlphaDependency Alpha { get; }

            /// <summary> 受け取った依存その2。 </summary>
            internal BetaDependency Beta { get; }
        }

        /// <summary> 既定値付きの引数を持つ検証用の型。 </summary>
        private sealed class WithDefaultValue
        {
            /// <summary>
            ///     依存と設定値を受け取る。
            /// </summary>
            /// <param name="alpha"> 依存その1。 </param>
            /// <param name="retryCount"> 設定値。 </param>
            public WithDefaultValue(AlphaDependency alpha, int retryCount = 3)
            {
                RetryCount = retryCount;
            }

            /// <summary> 受け取った設定値。 </summary>
            internal int RetryCount { get; }
        }

        /// <summary> コンストラクタで必ず例外を投げる検証用の型。 </summary>
        private sealed class ThrowingConstructor
        {
            /// <summary> 投げる例外のメッセージ。 </summary>
            internal const string MESSAGE = "コンストラクタの検証用例外";

            /// <summary>
            ///     常に例外を投げる。
            /// </summary>
            public ThrowingConstructor()
            {
                throw new InvalidOperationException(MESSAGE);
            }
        }

        /// <summary> 既存の自動注入経路を確認する検証用の型。 </summary>
        private sealed class InjectableTarget : IInjectable<AlphaDependency>
        {
            /// <summary> 注入された依存その1。 </summary>
            internal AlphaDependency Alpha { get; private set; }

            /// <summary>
            ///     依存を受け取って保持する。
            /// </summary>
            /// <param name="alpha"> 注入される依存その1。 </param>
            public void Inject(AlphaDependency alpha)
            {
                Alpha = alpha;
            }
        }
    }
}
