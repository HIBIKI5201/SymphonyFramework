using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

using SymphonyFramework.Attribute;
using SymphonyFramework.Core;
using SymphonyFramework.Editor;
using SymphonyFramework.Editor.Debugger;
using SymphonyFramework.Exceptions;
using SymphonyFramework.Pause;
using SymphonyFramework.SaveData;
using SymphonyFramework.Scenes;
using SymphonyFramework.Services;
using SymphonyFramework.Utility;

using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

using LegacyPause = SymphonyFrameWork.System;
using LegacySave = SymphonyFrameWork.System.SaveSystem;
using LegacyServices = SymphonyFrameWork.System.ServiceLocate;

using Object = UnityEngine.Object;

// 旧APIの転送そのものを検証するため、旧APIの参照を警告しない。
#pragma warning disable CS0618

namespace SymphonyFramework.Tests
{
    /// <summary>
    ///     6.x（名前空間 <c>SymphonyFrameWork.*</c>）からの互換層を検証する（7.0.0、#227）。
    /// </summary>
    /// <remarks>
    ///     旧名の転送クラスは <c>Runtime/Obsolete/</c> にあり、公開型の網羅検査の対象外にしている。
    ///     代わりにこのテストで、旧名が解決できることと、新しい Facade と同じ結果を返すことを確かめる。
    /// </remarks>
    public sealed class Version7CompatibilityTests
    {
        #region 旧名前空間と [MovedFrom]

        /// <summary> 6.x の名前空間はすべて残っており、<c>using</c> の行がコンパイルできる。 </summary>
        [TestCase("SymphonyFrameWork")]
        [TestCase("SymphonyFrameWork.Attribute")]
        [TestCase("SymphonyFrameWork.Config")]
        [TestCase("SymphonyFrameWork.Core")]
        [TestCase("SymphonyFrameWork.Debugger")]
        [TestCase("SymphonyFrameWork.Debugger.HUD")]
        [TestCase("SymphonyFrameWork.Debugger.Logger")]
        [TestCase("SymphonyFrameWork.Editor")]
        [TestCase("SymphonyFrameWork.Editor.Debugger")]
        [TestCase("SymphonyFrameWork.Editor.SettingProvider")]
        [TestCase("SymphonyFrameWork.Exceptions")]
        [TestCase("SymphonyFrameWork.Utility")]
        [TestCase("SymphonyFrameWork.System")]
        [TestCase("SymphonyFrameWork.System.SaveSystem")]
        [TestCase("SymphonyFrameWork.System.SceneBlock")]
        [TestCase("SymphonyFrameWork.System.SceneLoad")]
        [TestCase("SymphonyFrameWork.System.ServiceLocate")]
        public void LegacyNamespace_StillExists(string legacyNamespace)
        {
            bool exists = FrameworkAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Any(type => type.Namespace == legacyNamespace);

            Assert.That(exists, Is.True, $"6.x の名前空間 {legacyNamespace} が残っていません。");
        }

        /// <summary> 名前空間だけ移った型は、6.x の名前を指す [MovedFrom] を持つ。 </summary>
        [TestCase(typeof(ReadOnlyAttribute), "SymphonyFrameWork.Attribute", "SymphonyFrameWork", null)]
        [TestCase(typeof(SymphonyTween), "SymphonyFrameWork.Utility", "SymphonyFrameWork", null)]
        [TestCase(typeof(SymphonyNotInitializedException), "SymphonyFrameWork.Exceptions", "SymphonyFrameWork", null)]
        [TestCase(typeof(PauseInfo), "SymphonyFrameWork.System", "SymphonyFrameWork", null)]
        [TestCase(typeof(IInitializeAsync), "SymphonyFrameWork.System.SceneLoad", "SymphonyFrameWork", null)]
        [TestCase(typeof(SceneLoadRequest), "SymphonyFrameWork.System.SceneLoad", "SymphonyFrameWork", null)]
        [TestCase(typeof(SaveDataEntryInfo), "SymphonyFrameWork.System.SaveSystem", "SymphonyFrameWork", null)]
        [TestCase(typeof(ServiceRegistrationInfo), "SymphonyFrameWork.System.ServiceLocate", "SymphonyFrameWork", null)]
        [TestCase(typeof(SymphonyConstant), "SymphonyFrameWork.Core", "SymphonyFrameWork.Core", null)]
        [TestCase(typeof(SymphonyConfigManager), "SymphonyFrameWork.Editor", "SymphonyFrameWork.Editor", null)]
        [TestCase(typeof(SymphonyMcpTools), "SymphonyFrameWork.Editor.Debugger", "SymphonyFrameWork.Editor", null)]
        public void NamespaceOnlyMovedType_HasMovedFrom(
            Type type,
            string sourceNamespace,
            string sourceAssembly,
            string sourceClassName)
        {
            AssertMovedFrom(type, sourceNamespace, sourceAssembly, sourceClassName);
        }

        /// <summary> 名前も変わった型は、6.x の型名も [MovedFrom] に持つ。 </summary>
        [TestCase(typeof(SuiteAsset), "SymphonyFrameWork.System.SceneBlock", "SceneBlockAsset")]
        [TestCase(typeof(SuiteEntry), "SymphonyFrameWork.System.SceneBlock", "SceneBlockEntry")]
        [TestCase(typeof(SuiteInfo), "SymphonyFrameWork.System.SceneBlock", "SceneBlockInfo")]
        [TestCase(typeof(SuiteLoadStateEnum), "SymphonyFrameWork.System.SceneBlock", "SceneBlockLoadStateEnum")]
        [TestCase(typeof(SuitePlanException), "SymphonyFrameWork.System.SceneBlock", "SceneBlockPlanException")]
        [TestCase(typeof(ServiceComponent), "SymphonyFrameWork.System.ServiceLocate", "ServiceLocateComponent")]
        public void RenamedType_HasMovedFromWithClassName(Type type, string sourceNamespace, string sourceClassName)
        {
            AssertMovedFrom(type, sourceNamespace, "SymphonyFrameWork", sourceClassName);
        }

        /// <summary> [MovedFrom] はすべて 6.x の名前空間とアセンブリを指す。 </summary>
        /// <remarks> 7.0.0 の名前を誤って書くと、API Updater が何も書き換えない。 </remarks>
        [Test]
        public void AllMovedFrom_PointToLegacyNames()
        {
            string[] legacyAssemblies = { "SymphonyFrameWork", "SymphonyFrameWork.Core", "SymphonyFrameWork.Editor" };

            List<string> wrong = new();
            foreach (Type type in FrameworkAssemblies().SelectMany(assembly => assembly.GetTypes()))
            {
                CustomAttributeData attribute = type
                    .GetCustomAttributesData()
                    .SingleOrDefault(data => data.AttributeType == typeof(MovedFromAttribute));
                if (attribute == null) { continue; }

                string sourceNamespace = attribute.ConstructorArguments[1].Value as string;
                string sourceAssembly = attribute.ConstructorArguments[2].Value as string;
                bool isLegacy = sourceNamespace != null
                    && sourceNamespace.StartsWith("SymphonyFrameWork", StringComparison.Ordinal)
                    && Array.IndexOf(legacyAssemblies, sourceAssembly) >= 0;
                if (!isLegacy) { wrong.Add($"{type.FullName}: {sourceNamespace} / {sourceAssembly}"); }
            }

            Assert.That(wrong, Is.Empty, string.Join(Environment.NewLine, wrong));
        }

        #endregion

        #region ServiceLocator / ServiceInjector / SymphonyLocateObject

        /// <summary> 旧 GetInstance は、6.x と同じく未登録で null を返す。 </summary>
        [Test]
        public void ServiceLocator_GetInstance_Unregistered_ReturnsNull()
        {
            using PartituraScope scope = new();

            Assert.That(LegacyServices.ServiceLocator.GetInstance<ServiceA>(), Is.Null);
        }

        /// <summary> 旧 GetInstance は、6.x と同じく未初期化で null を返す。 </summary>
        [Test]
        public void ServiceLocator_GetInstance_NotInitialized_ReturnsNull()
        {
            Partitura.ResetRuntimeState();

            Assert.That(LegacyServices.ServiceLocator.GetInstance<ServiceA>(), Is.Null);
        }

        /// <summary> 旧 RegisterInstance の登録は、新しい Partitura から取得できる。 </summary>
        [Test]
        public void ServiceLocator_RegisterInstance_IsVisibleFromPartitura()
        {
            using PartituraScope scope = new();
            ServiceA service = new();

            LegacyServices.ServiceLocator.RegisterInstance(service);

            Assert.That(Partitura.Get<ServiceA>(), Is.SameAs(service));
            Assert.That(LegacyServices.ServiceLocator.GetInstance<ServiceA>(), Is.SameAs(service));
        }

        /// <summary> 旧 LocateTypeEnum は、同じ順序の ServiceLifetimeEnum へ変換される。 </summary>
        [TestCase(LegacyServices.LocateTypeEnum.Singleton, ServiceLifetimeEnum.Persistent)]
        [TestCase(LegacyServices.LocateTypeEnum.Locator, ServiceLifetimeEnum.Scene)]
        public void ServiceLocator_LocateType_ConvertsToLifetime(
            LegacyServices.LocateTypeEnum locateType,
            ServiceLifetimeEnum expected)
        {
            using PartituraScope scope = new();

            LegacyServices.ServiceLocator.RegisterInstance(new ServiceA(), locateType);

            Assert.That(Partitura.TryGetRegistrationInfo(typeof(ServiceA), out ServiceRegistrationInfo info), Is.True);
            Assert.That(info.Lifetime, Is.EqualTo(expected));
        }

        /// <summary> 旧 DestroyInstance(instance) は、6.x と同じく渡したインスタンスを見ずに型の登録を破棄する。 </summary>
        [Test]
        public void ServiceLocator_DestroyInstance_WithOtherInstance_DestroysByType()
        {
            using PartituraScope scope = new();
            Partitura.Register(new ServiceA());

            bool destroyed = LegacyServices.ServiceLocator.DestroyInstance(new ServiceA());

            Assert.That(destroyed, Is.True);
            Assert.That(Partitura.IsRegistered<ServiceA>(), Is.False);
        }

        /// <summary> 旧 IsExistInstance(instance) は、6.x と同じく型の登録だけを確かめる。 </summary>
        [Test]
        public void ServiceLocator_IsExistInstance_WithOtherInstance_ChecksType()
        {
            using PartituraScope scope = new();
            Partitura.Register(new ServiceA());

            Assert.That(LegacyServices.ServiceLocator.IsExistInstance(new ServiceA()), Is.True);
        }

        /// <summary> 引数なしの旧 RegisterAfterLocate は、登録時に呼ばれる。 </summary>
        [Test]
        public void ServiceLocator_RegisterAfterLocate_Action_RunsOnRegister()
        {
            using PartituraScope scope = new();
            int calls = 0;

            LegacyServices.ServiceLocator.RegisterAfterLocate<ServiceA>(() => calls++);
            Partitura.Register(new ServiceA());

            Assert.That(calls, Is.EqualTo(1));
        }

        /// <summary> 引数なしの旧 RegisterAfterLocate は、6.x と同じく null を拒む。 </summary>
        [Test]
        public void ServiceLocator_RegisterAfterLocate_NullAction_Throws()
        {
            using PartituraScope scope = new();

            Assert.That(
                () => LegacyServices.ServiceLocator.RegisterAfterLocate<ServiceA>((Action)null),
                Throws.TypeOf<ArgumentNullException>());
        }

        /// <summary> 旧 IInjectable を実装した型へ、旧 ServiceInjector で注入できる。 </summary>
        [Test]
        public void ServiceInjector_LegacyInjectable_IsInjected()
        {
            using PartituraScope scope = new();
            ServiceA service = new();
            Partitura.Register(service);
            LegacyInjectable target = new();

            LegacyServices.ServiceInjector.Inject(target);

            Assert.That(target.Injected, Is.SameAs(service));
        }

        /// <summary> 旧 IInjectable を実装した型へ、新しい Partitura でも注入できる。 </summary>
        [Test]
        public void Partitura_LegacyInjectable_IsInjected()
        {
            using PartituraScope scope = new();
            ServiceA service = new();
            Partitura.Register(service);
            LegacyInjectable target = new();

            Partitura.Inject(target);

            Assert.That(target.Injected, Is.SameAs(service));
        }

        /// <summary> 旧 SymphonyLocateObject.GetInstance は、6.x と同じく未登録で null を返す。 </summary>
        [Test]
        public void SymphonyLocateObject_GetInstance_Unregistered_ReturnsNull()
        {
            using PartituraScope scope = new();
            LegacyServices.SymphonyLocateObject<ServiceA> reference = new();

            Assert.That(reference.GetInstance(), Is.Null);
        }

        /// <summary> 旧 SymphonyLocateObject は、登録後に取得してキャッシュする。 </summary>
        [Test]
        public void SymphonyLocateObject_TryGetInstance_Registered_ReturnsInstance()
        {
            using PartituraScope scope = new();
            ServiceA service = new();
            Partitura.Register(service);
            LegacyServices.SymphonyLocateObject<ServiceA> reference = new();

            Assert.That(reference.TryGetInstance(out ServiceA result), Is.True);
            Assert.That(result, Is.SameAs(service));
        }

        #endregion

        #region PauseManager

        /// <summary> 旧 Pause プロパティは、全カテゴリーを止めて状態を読む。 </summary>
        [Test]
        public void PauseManager_PauseProperty_PausesAll()
        {
            using FermataScope scope = new();

            LegacyPause.PauseManager.Pause = true;

            Assert.That(Fermata.IsPausedAny(), Is.True);
            Assert.That(LegacyPause.PauseManager.Pause, Is.True);
        }

        /// <summary> 旧 OnPauseChanged は、カテゴリーを指定しない購読として届く。 </summary>
        [Test]
        public void PauseManager_OnPauseChanged_ReceivesOverallChanges()
        {
            using FermataScope scope = new();
            List<bool> received = new();
            Action<bool> handler = received.Add;

            LegacyPause.PauseManager.OnPauseChanged += handler;
            Fermata.SetPauseAll(true);
            LegacyPause.PauseManager.OnPauseChanged -= handler;
            Fermata.SetPauseAll(false);

            Assert.That(received, Is.EqualTo(new[] { true }));
        }

        /// <summary> 6.x の生成物と同じ形（旧 IPausable を継承したカテゴリー）を、新しい Fermata で操作できる。 </summary>
        [Test]
        public void LegacyCategory_IsOperableFromFermata()
        {
            using FermataScope scope = new();
            LegacyCategoryPausable pausable = new();
            LegacyPause.PauseManager.IPausable.RegisterPauseManager(pausable);

            Fermata.SetPause<ILegacyCategory>(true);

            Assert.That(pausable.PauseCount, Is.EqualTo(1));
            Assert.That(LegacyPause.PauseManager.IsPaused<ILegacyCategory>(), Is.True);
        }

        /// <summary> 旧 IPausable だけを実装した対象は、6.x と同じく既定カテゴリーに属する。 </summary>
        [Test]
        public void LegacyPausable_WithoutCategory_BelongsToDefaultCategory()
        {
            IReadOnlyList<Type> categories = PauseCategoryResolver.Resolve(typeof(LegacyPausable));

            Assert.That(categories, Is.EqualTo(new[] { PauseCategoryResolver.DefaultCategory }));
        }

        /// <summary> 旧 IPausable をカテゴリーに指定すると、既定カテゴリーとして扱う。 </summary>
        [Test]
        public void PauseManager_SetPause_LegacyIPausable_PausesDefaultCategory()
        {
            using FermataScope scope = new();
            LegacyPausable pausable = new();
            LegacyPause.PauseManager.IPausable.RegisterPauseManager(pausable);

            LegacyPause.PauseManager.SetPause<LegacyPause.PauseManager.IPausable>(true);

            Assert.That(pausable.PauseCount, Is.EqualTo(1));
            Assert.That(Fermata.IsPaused<IPausable>(), Is.True);
        }

        #endregion

        #region SaveStore / SaveDataLoaderStrategy

        /// <summary> 旧 SaveDataContent の派生型は、新しい基底型として扱える。 </summary>
        [Test]
        public void LegacySaveDataContent_IsNewSaveDataContent()
        {
            Assert.That(typeof(SaveDataContent).IsAssignableFrom(typeof(LegacySaveData)), Is.True);
        }

        /// <summary> 旧ローダーの、旧型を受け取る override へ橋渡しされる。 </summary>
        [Test]
        public void LegacyLoader_SerializeToJson_BridgesToLegacyOverride()
        {
            LegacyLoader loader = new();

            string json = loader.Serialize(new LegacySaveData());

            Assert.That(json, Is.EqualTo(LegacyLoader.JSON));
        }

        /// <summary> 旧ローダーへ、旧型を継承していないデータを渡すと例外になる。 </summary>
        [Test]
        public void LegacyLoader_SerializeToJson_NewOnlyData_Throws()
        {
            LegacyLoader loader = new();

            Assert.That(
                () => loader.Serialize(new NewOnlySaveData()),
                Throws.TypeOf<InvalidOperationException>());
        }

        /// <summary> 旧 SaveStore.GetEntries は、6.19.0 以降の動きどおり未初期化で例外を投げる。 </summary>
        [Test]
        public void SaveStore_GetEntries_NotInitialized_Throws()
        {
            Manuscript.ResetRuntimeState();

            Assert.That(
                () => LegacySave.SaveStore.GetEntries(),
                Throws.TypeOf<SymphonyNotInitializedException>());
        }

        #endregion

        #region 内部処理

        /// <summary> フレームワークの3アセンブリ。 </summary>
        private static IEnumerable<Assembly> FrameworkAssemblies() => new[]
        {
            typeof(Partitura).Assembly,
            typeof(SymphonyConstant).Assembly,
            typeof(SymphonyConfigManager).Assembly,
        };

        /// <summary> [MovedFrom] の値を確かめる。 </summary>
        private static void AssertMovedFrom(
            Type type,
            string sourceNamespace,
            string sourceAssembly,
            string sourceClassName)
        {
            CustomAttributeData attribute = type
                .GetCustomAttributesData()
                .SingleOrDefault(data => data.AttributeType == typeof(MovedFromAttribute));

            Assert.That(attribute, Is.Not.Null, $"{type.Name} に [MovedFrom] がありません。");

            IList<CustomAttributeTypedArgument> arguments = attribute.ConstructorArguments;
            Assert.That(arguments[0].Value, Is.True, "autoUpdateAPI が true ではありません。");
            Assert.That(arguments[1].Value, Is.EqualTo(sourceNamespace));
            Assert.That(arguments[2].Value, Is.EqualTo(sourceAssembly));
            Assert.That(arguments[3].Value, Is.EqualTo(sourceClassName));
        }

        /// <summary> テストの間だけ Partitura を初期化する。 </summary>
        private sealed class PartituraScope : IDisposable
        {
            public PartituraScope()
            {
                Partitura.ResetRuntimeState();
                _hostObject = new GameObject("Version7 Compatibility Host");
                Partitura.Initialize(_hostObject.AddComponent<ServiceHostComponent>());
            }

            public void Dispose()
            {
                Partitura.ResetRuntimeState();
                if (_hostObject != null) { Object.DestroyImmediate(_hostObject); }
            }

            private readonly GameObject _hostObject;
        }

        /// <summary> テストの間だけ Fermata を初期化する。 </summary>
        private sealed class FermataScope : IDisposable
        {
            public FermataScope() => Fermata.Initialize();

            public void Dispose() => Fermata.ResetRuntimeState();
        }

        private sealed class ServiceA { }

        /// <summary> 6.x の書き方のまま、旧 IInjectable を実装した型。 </summary>
        private sealed class LegacyInjectable : LegacyServices.IInjectable<ServiceA>
        {
            public ServiceA Injected { get; private set; }

            public void Inject(ServiceA arg0) => Injected = arg0;
        }

        /// <summary> 6.x の生成物と同じ形のカテゴリー。 </summary>
        private interface ILegacyCategory : LegacyPause.PauseManager.IPausable { }

        /// <summary> カテゴリーを持たない、6.x の書き方のポーズ対象。 </summary>
        private sealed class LegacyPausable : LegacyPause.PauseManager.IPausable
        {
            public int PauseCount { get; private set; }

            public void Pause() => PauseCount++;
            public void Resume() { }
        }

        /// <summary> 6.x のカテゴリーに属するポーズ対象。 </summary>
        private sealed class LegacyCategoryPausable : ILegacyCategory
        {
            public int PauseCount { get; private set; }

            public void Pause() => PauseCount++;
            public void Resume() { }
        }

        /// <summary> 6.x の書き方のままのセーブデータ。 </summary>
        [Serializable]
        private sealed class LegacySaveData : LegacySave.SaveDataContent { }

        /// <summary> 7.0.0 の書き方のセーブデータ。 </summary>
        [Serializable]
        private sealed class NewOnlySaveData : SaveDataContent { }

        /// <summary> 6.x の書き方のまま、旧型を受け取るメソッドを override したローダー。 </summary>
        private sealed class LegacyLoader : LegacySave.SaveDataLoaderStrategy
        {
            public const string JSON = "legacy";

            /// <summary> 新しい基底型の引数で呼び、橋渡しを通す。 </summary>
            public string Serialize(SaveDataContent data) => SerializeToJson(data.GetType(), data);

            protected override string SerializeToJson(Type dataType, LegacySave.SaveDataContent data) => JSON;

            protected override void OverwriteFromJson(Type dataType, string json, LegacySave.SaveDataContent data) { }

            protected override bool ExistsCore(Type dataType) => false;

            protected override Awaitable<string> LoadJsonAsync(Type dataType, System.Threading.CancellationToken token) =>
                throw new NotSupportedException();

            protected override Awaitable SaveJsonAsync(Type dataType, string json, System.Threading.CancellationToken token) =>
                throw new NotSupportedException();

            protected override Awaitable DeleteCoreAsync(Type dataType, System.Threading.CancellationToken token) =>
                throw new NotSupportedException();
        }

        #endregion
    }
}
