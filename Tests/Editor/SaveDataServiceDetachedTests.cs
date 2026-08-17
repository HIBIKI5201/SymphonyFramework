using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using SymphonyFrameWork.System.SaveSystem;
using SymphonyFrameWork.Utility;

using UnityEngine;
using UnityEngine.TestTools;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     SaveDataServiceのdetached I/OがRegistryから独立する契約を検証する。
    /// </summary>
    public sealed class SaveDataServiceDetachedTests
    {
        #region 内部処理

        /// <summary> 検証用のセーブデータ型。 </summary>
        [Serializable]
        private sealed class TestSaveData : SaveDataContent
        {
            public int Value;
        }

        /// <summary> 型不一致の検証に使用する別のセーブデータ型。 </summary>
        [Serializable]
        private sealed class OtherSaveData : SaveDataContent
        { }

        /// <summary> 保存先をメモリ上のDictionaryに置き換えたローダー。 </summary>
        private sealed class MemorySaveDataLoader : SaveDataLoaderStrategy
        {
            private readonly Dictionary<Type, string> _storage = new();

            /// <summary>
            ///     指定型の保存値がメモリ上に存在するか確認する。
            /// </summary>
            protected override bool ExistsCore(Type dataType) => _storage.ContainsKey(dataType);

            /// <summary>
            ///     指定型のJSONをメモリ上から読み込む。
            /// </summary>
            protected override Awaitable<string> LoadJsonAsync(Type dataType, CancellationToken token)
            {
                _storage.TryGetValue(dataType, out string json);
                return SymphonyAwaitable.FromResult(json);
            }

            /// <summary>
            ///     指定型のJSONをメモリ上へ保存する。
            /// </summary>
            protected override Awaitable SaveJsonAsync(Type dataType, string json, CancellationToken token)
            {
                _storage[dataType] = json;
                return SymphonyAwaitable.Completed();
            }

            /// <summary>
            ///     指定型のJSONをメモリ上から削除する。
            /// </summary>
            protected override Awaitable DeleteCoreAsync(Type dataType, CancellationToken token)
            {
                _storage.Remove(dataType);
                return SymphonyAwaitable.Completed();
            }

            /// <summary>
            ///     検証用データをJSONへ変換する。
            /// </summary>
            protected override string SerializeToJson(Type dataType, SaveDataContent data) =>
                JsonUtility.ToJson(data);

            /// <summary>
            ///     JSONを既存の検証用データへ上書きする。
            /// </summary>
            protected override void OverwriteFromJson(Type dataType, string json, SaveDataContent data) =>
                JsonUtility.FromJsonOverwrite(json, data);
        }

        private SaveDataEntryRegistry _registry;
        private SaveDataService _service;

        /// <summary>
        ///     各テスト用に独立したRegistryとメモリローダーを構築する。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _registry = new SaveDataEntryRegistry();
            MemorySaveDataLoader loader = new();
            _service = new SaveDataService(_registry, () => loader);
        }

        /// <summary>
        ///     detachedロードはRegistryへエントリを追加しない。
        /// </summary>
        [Test]
        public async Task LoadDetachedAsync_DoesNotRegisterEntry()
        {
            await _service.SaveAsync(typeof(TestSaveData));
            _service.Reset();
            int entryCountBefore = _registry.GetEntities().Count;
            TestSaveData target = new();

            await _service.LoadDetachedAsync(typeof(TestSaveData), target);

            Assert.That(_registry.GetEntities().Count, Is.EqualTo(entryCountBefore));
            target.Dispose();
        }

        /// <summary>
        ///     detachedロードは保存済みの値を指定インスタンスへ復元する。
        /// </summary>
        [Test]
        public async Task LoadDetachedAsync_RestoresPersistedValues()
        {
            await _service.LoadAsync(typeof(TestSaveData));
            TestSaveData saved = (TestSaveData)_service.Get(typeof(TestSaveData));
            saved.Value = 27;
            await _service.SaveAsync(typeof(TestSaveData));
            _service.Reset();
            TestSaveData target = new();

            await _service.LoadDetachedAsync(typeof(TestSaveData), target);

            Assert.That(target.Value, Is.EqualTo(27));
            target.Dispose();
        }

        /// <summary>
        ///     保存値が無いdetachedロードは指定インスタンスを既定値へ戻す。
        /// </summary>
        [Test]
        public async Task LoadDetachedAsync_NoPersistedData_ResetsToDefault()
        {
            LogAssert.Expect(LogType.Log, new Regex("データが見つからないので生成しました"));
            TestSaveData target = new() { Value = 42 };

            await _service.LoadDetachedAsync(typeof(TestSaveData), target);

            Assert.That(target.Value, Is.EqualTo(0));
            target.Dispose();
        }

        /// <summary>
        ///     detachedロードは状態変更eventを発行しない。
        /// </summary>
        [Test]
        public async Task LoadDetachedAsync_DoesNotRaiseStateChanged()
        {
            await _service.SaveAsync(typeof(TestSaveData));
            _service.Reset();
            int notifiedCount = 0;
            _service.OnStateChanged += () => notifiedCount++;
            int notifiedCountBefore = notifiedCount;
            TestSaveData target = new();

            await _service.LoadDetachedAsync(typeof(TestSaveData), target);

            Assert.That(notifiedCount - notifiedCountBefore, Is.EqualTo(0));
            target.Dispose();
        }

        /// <summary>
        ///     detached保存はRegistryへ登録せず永続化する。
        /// </summary>
        [Test]
        public async Task SaveDetachedAsync_PersistsWithoutRegistering()
        {
            int entryCountBefore = _registry.GetEntities().Count;
            TestSaveData source = new() { Value = 8 };

            await _service.SaveDetachedAsync(typeof(TestSaveData), source);

            Assert.That(_service.Exists(typeof(TestSaveData)), Is.True);
            Assert.That(_registry.GetEntities().Count, Is.EqualTo(entryCountBefore));
            source.Dispose();
        }

        /// <summary>
        ///     detached保存は保存日時を更新する。
        /// </summary>
        [Test]
        public async Task SaveDetachedAsync_UpdatesSaveDate()
        {
            TestSaveData source = new();
            Assert.That(source.SaveDate, Is.Null);

            await _service.SaveDetachedAsync(typeof(TestSaveData), source);

            Assert.That(source.SaveDate, Is.Not.Null.And.Not.Empty);
            source.Dispose();
        }

        /// <summary>
        ///     同期完了するローダーではdetached保存Taskも同期完了する。
        /// </summary>
        [Test]
        public void SaveDetachedAsync_CompletesSynchronously()
        {
            TestSaveData source = new();

            Task saveTask = _service.SaveDetachedAsync(typeof(TestSaveData), source);

            Assert.That(saveTask.IsCompleted, Is.True);
            source.Dispose();
        }

        /// <summary>
        ///     detached削除は保存先だけを削除する。
        /// </summary>
        [Test]
        public async Task DeleteDetachedAsync_RemovesPersistedDataOnly()
        {
            await _service.SaveAsync(typeof(TestSaveData));
            _service.Reset();
            int entryCountBefore = _registry.GetEntities().Count;

            await _service.DeleteDetachedAsync(typeof(TestSaveData));

            Assert.That(_service.Exists(typeof(TestSaveData)), Is.False);
            Assert.That(_registry.GetEntities().Count, Is.EqualTo(entryCountBefore));
        }

        /// <summary>
        ///     detached保存はnullの保存元を拒否する。
        /// </summary>
        [Test]
        public void SaveDetachedAsync_NullSource_Throws()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _service.SaveDetachedAsync(typeof(TestSaveData), null));
        }

        /// <summary>
        ///     detachedロードは指定型と異なるインスタンスを拒否する。
        /// </summary>
        [Test]
        public void LoadDetachedAsync_MismatchedType_Throws()
        {
            OtherSaveData target = new();

            Assert.ThrowsAsync<ArgumentException>(async () =>
                await _service.LoadDetachedAsync(typeof(TestSaveData), target));
            target.Dispose();
        }

        #endregion
    }
}
