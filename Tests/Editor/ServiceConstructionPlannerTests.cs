using System;
using System.Collections.Generic;
using System.Reflection;

using NUnit.Framework;

using SymphonyFrameWork.System.ServiceLocate;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     コンストラクタ注入で使うコンストラクタの選択と、引数の解決を検証する。
    /// </summary>
    /// <remarks>
    ///     Service Locatorを初期化せず、解決手段を辞書で差し替えて呼ぶ。
    /// </remarks>
    public sealed class ServiceConstructionPlannerTests
    {
        /// <summary>
        ///     publicなコンストラクタが1つならそれを使う。
        /// </summary>
        [Test]
        public void SelectConstructor_SinglePublicConstructor_IsSelected()
        {
            ConstructorInfo constructor = ServiceConstructionPlanner.SelectConstructor(typeof(TwoDependencies));

            Assert.That(constructor.GetParameters(), Has.Length.EqualTo(2));
        }

        /// <summary>
        ///     複数ある場合は引数が最も多いものを使う。
        /// </summary>
        [Test]
        public void SelectConstructor_MultipleConstructors_SelectsMostParameters()
        {
            ConstructorInfo constructor = ServiceConstructionPlanner.SelectConstructor(typeof(OverloadedConstructors));

            Assert.That(constructor.GetParameters(), Has.Length.EqualTo(2));
        }

        /// <summary>
        ///     引数が最多のものが並んだ場合は決められないため拒否する。
        /// </summary>
        [Test]
        public void SelectConstructor_TiedParameterCount_Throws()
        {
            Assert.That(
                () => ServiceConstructionPlanner.SelectConstructor(typeof(TiedConstructors)),
                Throws.TypeOf<InvalidOperationException>());
        }

        /// <summary>
        ///     publicなコンストラクタが無い型は生成できない。
        /// </summary>
        [Test]
        public void SelectConstructor_NoPublicConstructor_Throws()
        {
            Assert.That(
                () => ServiceConstructionPlanner.SelectConstructor(typeof(PrivateConstructorOnly)),
                Throws.TypeOf<InvalidOperationException>());
        }

        /// <summary>
        ///     依存の無い型は引数無しのコンストラクタを使う。
        /// </summary>
        [Test]
        public void SelectConstructor_ParameterlessConstructor_IsSelected()
        {
            ConstructorInfo constructor = ServiceConstructionPlanner.SelectConstructor(typeof(NoDependency));

            Assert.That(constructor.GetParameters(), Is.Empty);
        }

        /// <summary>
        ///     登録済みの依存を宣言順で解決する。
        /// </summary>
        [Test]
        public void ResolveArguments_RegisteredServices_AreResolved()
        {
            AlphaDependency alpha = new();
            BetaDependency beta = new();
            ConstructorInfo constructor = ServiceConstructionPlanner.SelectConstructor(typeof(TwoDependencies));

            object[] arguments = ServiceConstructionPlanner.ResolveArguments(
                constructor,
                CreateResolver(alpha, beta));

            Assert.That(arguments, Has.Length.EqualTo(2));
            Assert.That(arguments[0], Is.SameAs(alpha));
            Assert.That(arguments[1], Is.SameAs(beta));
        }

        /// <summary>
        ///     登録が無くても既定値があればそれを使う。
        /// </summary>
        [Test]
        public void ResolveArguments_MissingWithDefault_UsesDefaultValue()
        {
            AlphaDependency alpha = new();
            ConstructorInfo constructor = ServiceConstructionPlanner.SelectConstructor(typeof(WithDefaultValue));

            object[] arguments = ServiceConstructionPlanner.ResolveArguments(
                constructor,
                CreateResolver(alpha, null));

            Assert.That(arguments[0], Is.SameAs(alpha));
            Assert.That(arguments[1], Is.EqualTo(3));
        }

        /// <summary>
        ///     解決できず既定値も無い引数は、未登録として型つきで通知する。
        /// </summary>
        [Test]
        public void ResolveArguments_MissingWithoutDefault_Throws()
        {
            ConstructorInfo constructor = ServiceConstructionPlanner.SelectConstructor(typeof(TwoDependencies));

            ServiceNotRegisteredException exception = Assert.Throws<ServiceNotRegisteredException>(
                () => ServiceConstructionPlanner.ResolveArguments(
                    constructor,
                    CreateResolver(new AlphaDependency(), null)));

            Assert.That(exception.ServiceType, Is.EqualTo(typeof(BetaDependency)));
        }

        /// <summary>
        ///     引数の無いコンストラクタは空の配列を返す。
        /// </summary>
        [Test]
        public void ResolveArguments_Parameterless_ReturnsEmpty()
        {
            ConstructorInfo constructor = ServiceConstructionPlanner.SelectConstructor(typeof(NoDependency));

            object[] arguments = ServiceConstructionPlanner.ResolveArguments(
                constructor,
                _ => null);

            Assert.That(arguments, Is.Empty);
        }

        /// <summary>
        ///     解決手段の欠落は呼び出し契約の違反として拒否する。
        /// </summary>
        [Test]
        public void ResolveArguments_NullResolve_Throws()
        {
            ConstructorInfo constructor = ServiceConstructionPlanner.SelectConstructor(typeof(NoDependency));

            Assert.That(
                () => ServiceConstructionPlanner.ResolveArguments(constructor, null),
                Throws.TypeOf<ArgumentNullException>());
        }

        /// <summary>
        ///     生成できない型を種類ごとに拒否する。
        /// </summary>
        [Test]
        public void ValidateCreatableType_UncreatableTypes_Throw()
        {
            Assert.That(
                () => ServiceConstructionPlanner.ValidateCreatableType(null, "type"),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(
                () => ServiceConstructionPlanner.ValidateCreatableType(typeof(AbstractDependency), "type"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => ServiceConstructionPlanner.ValidateCreatableType(typeof(IDisposable), "type"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => ServiceConstructionPlanner.ValidateCreatableType(typeof(int), "type"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => ServiceConstructionPlanner.ValidateCreatableType(typeof(List<>), "type"),
                Throws.TypeOf<ArgumentException>());
        }

        /// <summary>
        ///     生成できる型は検証を通す。
        /// </summary>
        [Test]
        public void ValidateCreatableType_ConcreteClass_DoesNotThrow()
        {
            Assert.That(
                () => ServiceConstructionPlanner.ValidateCreatableType(typeof(NoDependency), "type"),
                Throws.Nothing);
        }

        /// <summary>
        ///     指定した依存だけを返す解決手段を作る。
        /// </summary>
        /// <param name="alpha"> AlphaDependencyとして返す値。 </param>
        /// <param name="beta"> BetaDependencyとして返す値。 </param>
        /// <returns> 型から登録済みインスタンスを返す処理。 </returns>
        private static Func<Type, object> CreateResolver(AlphaDependency alpha, BetaDependency beta)
        {
            Dictionary<Type, object> instances = new();
            if (alpha != null) { instances.Add(typeof(AlphaDependency), alpha); }
            if (beta != null) { instances.Add(typeof(BetaDependency), beta); }

            return serviceType => instances.TryGetValue(serviceType, out object instance) ? instance : null;
        }

        /// <summary> 検証用の依存その1。 </summary>
        private sealed class AlphaDependency { }

        /// <summary> 検証用の依存その2。 </summary>
        private sealed class BetaDependency { }

        /// <summary> 抽象型の検証に使う型。 </summary>
        private abstract class AbstractDependency { }

        /// <summary> 依存を持たない検証用の型。 </summary>
        private sealed class NoDependency { }

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

        /// <summary> 引数の数が異なるコンストラクタを持つ検証用の型。 </summary>
        private sealed class OverloadedConstructors
        {
            /// <summary>
            ///     依存を1件受け取る。
            /// </summary>
            /// <param name="alpha"> 依存その1。 </param>
            public OverloadedConstructors(AlphaDependency alpha) { }

            /// <summary>
            ///     依存を2件受け取る。
            /// </summary>
            /// <param name="alpha"> 依存その1。 </param>
            /// <param name="beta"> 依存その2。 </param>
            public OverloadedConstructors(AlphaDependency alpha, BetaDependency beta) { }
        }

        /// <summary> 引数の数が同じコンストラクタを2つ持つ検証用の型。 </summary>
        private sealed class TiedConstructors
        {
            /// <summary>
            ///     依存その1だけを受け取る。
            /// </summary>
            /// <param name="alpha"> 依存その1。 </param>
            public TiedConstructors(AlphaDependency alpha) { }

            /// <summary>
            ///     依存その2だけを受け取る。
            /// </summary>
            /// <param name="beta"> 依存その2。 </param>
            public TiedConstructors(BetaDependency beta) { }
        }

        /// <summary> publicなコンストラクタを持たない検証用の型。 </summary>
        private sealed class PrivateConstructorOnly
        {
            /// <summary>
            ///     外部からの生成を許可しない。
            /// </summary>
            private PrivateConstructorOnly() { }
        }

        /// <summary> 既定値付きの引数を持つ検証用の型。 </summary>
        private sealed class WithDefaultValue
        {
            /// <summary>
            ///     依存と設定値を受け取る。
            /// </summary>
            /// <param name="alpha"> 依存その1。 </param>
            /// <param name="retryCount"> 設定値。 </param>
            public WithDefaultValue(AlphaDependency alpha, int retryCount = 3) { }
        }
    }
}
