using System;
using System.Collections.Generic;
using System.IO;

using SymphonyFrameWork.Core;
using SymphonyFrameWork.System.SceneLoad;
using SymphonyFrameWork.System.ServiceLocate;

using NUnit.Framework;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     ComponentとInterfaceが、所属するモジュールの名前空間と置き場に居る契約を検証する。
    /// </summary>
    /// <remarks>
    ///     名前空間は利用側のすべての <c>using</c> に現れるため、戻ると破壊的変更が2回起きる。
    ///     置き場と名前空間の両方を固定する。
    /// </remarks>
    public sealed class TypeModuleOwnershipTests
    {
        /// <summary> 型と、その型が属するべき名前空間。 </summary>
        private static readonly (Type type, string expectedNamespace)[] ModuleOwnedTypes =
        {
            (typeof(ServiceLocateComponent), "SymphonyFrameWork.System.ServiceLocate"),
            (typeof(SymphonyLocateObject<object>), "SymphonyFrameWork.System.ServiceLocate"),
            (typeof(IInjectable), "SymphonyFrameWork.System.ServiceLocate"),
            (typeof(IInjectable<object>), "SymphonyFrameWork.System.ServiceLocate"),
            (typeof(IInitializeAsync), "SymphonyFrameWork.System.SceneLoad"),
        };

        /// <summary> 型と、その型が置かれるべきパッケージ内の相対パス。 </summary>
        private static readonly (string fileName, string directory)[] ModuleOwnedFiles =
        {
            ("ServiceLocateComponent.cs", "Runtime/Service/ServiceLocator"),
            ("SymphonyLocateObject.cs", "Runtime/Service/ServiceLocator"),
            ("IInjectable.cs", "Runtime/Service/ServiceLocator"),
            ("IInitializeAsync.cs", "Runtime/Service/SceneLoader"),
        };

        /// <summary> モジュールに属する型が、そのモジュールの名前空間にある。 </summary>
        [Test]
        public void ModuleOwnedTypes_Namespace_MatchesOwningModule()
        {
            foreach ((Type type, string expectedNamespace) in ModuleOwnedTypes)
            {
                Assert.That(
                    type.Namespace,
                    Is.EqualTo(expectedNamespace),
                    $"{type.Name} の名前空間が所属モジュールと一致しません。");
            }
        }

        /// <summary> モジュールに属する型のファイルが、そのモジュールのフォルダにある。 </summary>
        [Test]
        public void ModuleOwnedTypes_FileLocation_MatchesOwningModule()
        {
            foreach ((string fileName, string directory) in ModuleOwnedFiles)
            {
                string path = Path.Combine(
                    EditorSymphonyConstant.FRAMEWORK_PATH, directory.Replace("/", Path.DirectorySeparatorChar.ToString()), fileName);

                Assert.That(File.Exists(path), Is.True, $"'{directory}/{fileName}' が見つかりません。");
            }
        }

        /// <summary> どのモジュールにも属さない型は、共通の置き場に残っている。 </summary>
        [Test]
        public void SharedTypes_NotOwnedByModule_RemainInCommonLocation()
        {
            Assert.That(typeof(IGameObject).Namespace, Is.EqualTo("SymphonyFrameWork"));

            Assert.That(
                File.Exists(Path.Combine(
                    EditorSymphonyConstant.FRAMEWORK_PATH, "Runtime", "Interface", "IGameObject.cs")),
                Is.True);
        }

        /// <summary> 移動元のフォルダは空のまま残さない。 </summary>
        [Test]
        public void RuntimeComponent_EmptiedFolder_IsRemoved()
        {
            Assert.That(
                Directory.Exists(Path.Combine(EditorSymphonyConstant.FRAMEWORK_PATH, "Runtime", "Component")),
                Is.False,
                "Runtime/Component/ は中身をモジュールへ移したため削除しました（Issue #105）。");
        }

        /// <summary> 移動した型が旧名前空間へ残っていない。 </summary>
        [Test]
        public void ModuleOwnedTypes_OldNamespaces_AreNotResolvable()
        {
            List<string> remaining = new();

            foreach (string fullName in new[]
            {
                "SymphonyFrameWork.Utility.ServiceLocateComponent",
                "SymphonyFrameWork.Utility.SymphonyLocateObject`1",
                "SymphonyFrameWork.IInjectable",
                "SymphonyFrameWork.IInitializeAsync",
            })
            {
                if (typeof(ServiceLocateComponent).Assembly.GetType(fullName) != null)
                {
                    remaining.Add(fullName);
                }
            }

            Assert.That(
                remaining,
                Is.Empty,
                "旧名前空間の型が残っています。移行は完了させ、互換シムを残さない方針です。"
                + $"\n{string.Join("\n", remaining)}");
        }
    }
}
