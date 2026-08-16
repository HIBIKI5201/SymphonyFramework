using System;

using NUnit.Framework;

using SymphonyFrameWork.Editor;
using SymphonyFrameWork.System.SaveSystem;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     セーブデータ型カタログのAssembly判定と型判定を検証する。
    /// </summary>
    public sealed class SaveDataTypeCatalogTests
    {
        /// <summary>
        ///     NUnit参照を持つAssembly名一覧をテスト用と判定する。
        /// </summary>
        [Test]
        public void IsTestAssembly_ReferencesNUnit_ReturnsTrue()
        {
            bool result = SaveDataTypeCatalog.IsTestAssembly(new[] { "System", "nunit.framework" });

            Assert.That(result, Is.True);
        }

        /// <summary>
        ///     Unity Test Runner参照を持つAssembly名一覧をテスト用と判定する。
        /// </summary>
        [Test]
        public void IsTestAssembly_ReferencesTestRunner_ReturnsTrue()
        {
            bool result = SaveDataTypeCatalog.IsTestAssembly(new[] { "System", "UnityEngine.TestRunner" });

            Assert.That(result, Is.True);
        }

        /// <summary>
        ///     製品側の参照だけを持つAssembly名一覧を通常用と判定する。
        /// </summary>
        [Test]
        public void IsTestAssembly_ProductAssembly_ReturnsFalse()
        {
            bool result = SaveDataTypeCatalog.IsTestAssembly(new[] { "System", "UnityEngine.CoreModule" });

            Assert.That(result, Is.False);
        }

        /// <summary>
        ///     自身のテストAssemblyを参照メタデータからテスト用と判定する。
        /// </summary>
        [Test]
        public void IsTestAssembly_OwnTestAssembly_ReturnsTrue()
        {
            bool result = SaveDataTypeCatalog.IsTestAssembly(typeof(SaveDataTypeCatalogTests).Assembly);

            Assert.That(result, Is.True);
        }

        /// <summary>
        ///     FrameworkのEditor Assemblyを通常用と判定する。
        /// </summary>
        [Test]
        public void IsTestAssembly_FrameworkEditorAssembly_ReturnsFalse()
        {
            bool result = SaveDataTypeCatalog.IsTestAssembly(typeof(SaveDataWindow).Assembly);

            Assert.That(result, Is.False);
        }

        /// <summary>
        ///     Serializableな具象セーブデータ型を受け入れる。
        /// </summary>
        [Test]
        public void IsSupportedSaveDataType_SerializableConcreteType_ReturnsTrue()
        {
            bool result = SaveDataTypeCatalog.IsSupportedSaveDataType(typeof(SerializableSaveData));

            Assert.That(result, Is.True);
        }

        /// <summary>
        ///     抽象セーブデータ型を除外する。
        /// </summary>
        [Test]
        public void IsSupportedSaveDataType_AbstractType_ReturnsFalse()
        {
            bool result = SaveDataTypeCatalog.IsSupportedSaveDataType(typeof(AbstractSaveData));

            Assert.That(result, Is.False);
        }

        /// <summary>
        ///     Serializable指定の無いセーブデータ型を除外する。
        /// </summary>
        [Test]
        public void IsSupportedSaveDataType_TypeWithoutSerializable_ReturnsFalse()
        {
            bool result = SaveDataTypeCatalog.IsSupportedSaveDataType(typeof(NonSerializableSaveData));

            Assert.That(result, Is.False);
        }

        /// <summary>
        ///     引数なしコンストラクタの無いセーブデータ型を除外する。
        /// </summary>
        [Test]
        public void IsSupportedSaveDataType_TypeWithoutDefaultConstructor_ReturnsFalse()
        {
            bool result = SaveDataTypeCatalog.IsSupportedSaveDataType(typeof(SaveDataWithoutDefaultConstructor));

            Assert.That(result, Is.False);
        }

        /// <summary>
        ///     SaveDataContentを継承しない型を除外する。
        /// </summary>
        [Test]
        public void IsSupportedSaveDataType_NonSaveDataType_ReturnsFalse()
        {
            bool result = SaveDataTypeCatalog.IsSupportedSaveDataType(typeof(SerializablePlainClass));

            Assert.That(result, Is.False);
        }

        [Serializable]
        private sealed class SerializableSaveData : SaveDataContent { }

        [Serializable]
        private abstract class AbstractSaveData : SaveDataContent { }

        private sealed class NonSerializableSaveData : SaveDataContent { }

        [Serializable]
        private sealed class SaveDataWithoutDefaultConstructor : SaveDataContent
        {
            /// <summary>
            ///     引数を要求して暗黙の引数なしコンストラクタを持たせない。
            /// </summary>
            /// <param name="value"> 検証用の値。 </param>
            internal SaveDataWithoutDefaultConstructor(int value) { }
        }

        [Serializable]
        private sealed class SerializablePlainClass { }
    }
}
