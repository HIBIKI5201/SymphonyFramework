using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using SymphonyFrameWork.System.SaveSystem;
using SymphonyFrameWork.Utility;

using UnityEngine;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     SaveDataViewStoreが問い合わせと操作をQuery／Serviceへ委譲する契約を検証する。
    /// </summary>
    public sealed class SaveDataViewStoreTests
    {
        #region 内部処理

        /// <summary> 検証用のセーブデータ型。 </summary>
        [Serializable]
        private sealed class TestSaveData : SaveDataContent
        {
            public int Value;
        }

        /// <summary> 保存先をメモリ上のDictionaryに置き換えたローダー。 </summary>
        private sealed class MemorySaveDataLoader : SaveDataLoaderStrategy
        {
            private readonly Dictionary<Type, string> _storage = new();

            /// <summary>
            ///     指定型の保存値が存在するか確認する。
            /// </summary>
            protected override bool ExistsCore(Type dataType) => _storage.ContainsKey(dataType);

            /// <summary>
            ///     指定型のJSONをメモリから読み込む。
            /// </summary>
            protected override Awaitable<string> LoadJsonAsync(Type dataType, CancellationToken token)
            {
                _storage.TryGetValue(dataType, out string json);
                return SymphonyAwaitable.FromResult(json);
            }

            /// <summary>
            ///     指定型のJSONをメモリへ保存する。
            /// </summary>
            protected override Awaitable SaveJsonAsync(Type dataType, string json, CancellationToken token)
            {
                _storage[dataType] = json;
                return SymphonyAwaitable.Completed();
            }

            /// <summary>
            ///     指定型のJSONをメモリから削除する。
            /// </summary>
            protected override Awaitable DeleteCoreAsync(Type dataType, CancellationToken token)
            {
                _storage.Remove(dataType);
                return SymphonyAwaitable.Completed();
            }

            /// <summary>
            ///     セーブデータをJSONへ変換する。
            /// </summary>
            protected override string SerializeToJson(Type dataType, SaveDataContent data) =>
                JsonUtility.ToJson(data);

            /// <summary>
            ///     JSONを既存のセーブデータへ上書きする。
            /// </summary>
            protected override void OverwriteFromJson(Type dataType, string json, SaveDataContent data) =>
                JsonUtility.FromJsonOverwrite(json, data);
        }

        private SaveDataEntryRegistry _registry;
        private SaveDataService _service;
        private SaveDataQuery _query;
        private SaveDataViewModel _viewModel;
        private SaveDataViewStore _viewStore;

        /// <summary>
        ///     各テスト用のQuery、Service、ViewModel、ViewStoreを構築する。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _registry = new SaveDataEntryRegistry();
            MemorySaveDataLoader loader = new();
            _service = new SaveDataService(_registry, () => loader);
            _query = new SaveDataQuery(_registry);
            _viewModel = new SaveDataViewModel(_query, _service);
            _viewStore = new SaveDataViewStore(_query, _service);
        }

        /// <summary>
        ///     ViewModelを破棄して次のテストへ購読を持ち越さない。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            _viewModel?.Dispose();
            _viewModel = null;
        }

        /// <summary>
        ///     Queryがnullの生成を拒否する。
        /// </summary>
        [Test]
        public void Constructor_NullQuery_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new SaveDataViewStore(null, _service));
        }

        /// <summary>
        ///     Serviceがnullの生成を拒否する。
        /// </summary>
        [Test]
        public void Constructor_NullService_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new SaveDataViewStore(_query, null));
        }

        /// <summary>
        ///     未ロードの型をfalseとして返す。
        /// </summary>
        [Test]
        public void IsLoaded_BeforeLoad_ReturnsFalse()
        {
            Assert.That(_viewStore.IsLoaded(typeof(TestSaveData)), Is.False);
        }

        /// <summary>
        ///     未ロードの型を例外にせずnullとして返す。
        /// </summary>
        [Test]
        public void GetLoadedContent_NotLoaded_ReturnsNull()
        {
            Assert.That(_viewStore.GetLoadedContent(typeof(TestSaveData)), Is.Null);
        }

        /// <summary>
        ///     ロード後はRegistry正本と同じインスタンスを返す。
        /// </summary>
        [Test]
        public async Task GetLoadedContent_AfterLoad_ReturnsRegistryInstance()
        {
            await _viewStore.LoadAsync(typeof(TestSaveData));

            SaveDataContent registryInstance = _service.Get(typeof(TestSaveData));
            Assert.That(
                _viewStore.GetLoadedContent(typeof(TestSaveData)),
                Is.SameAs(registryInstance));
        }

        /// <summary>
        ///     保存後は永続化データの存在を返す。
        /// </summary>
        [Test]
        public async Task Exists_AfterSave_ReturnsTrue()
        {
            await _viewStore.SaveAsync(typeof(TestSaveData));

            Assert.That(_viewStore.Exists(typeof(TestSaveData)), Is.True);
        }

        /// <summary>
        ///     注入したローダーの型名を返す。
        /// </summary>
        [Test]
        public void CurrentLoaderName_ReturnsLoaderTypeName()
        {
            Assert.That(
                _viewStore.CurrentLoaderName,
                Is.EqualTo(nameof(MemorySaveDataLoader)));
        }

        /// <summary>
        ///     Registry経由のロードでViewModelの表示値を更新する。
        /// </summary>
        [Test]
        public async Task LoadAsync_RaisesViewModelUpdate()
        {
            int notifiedCount = 0;
            using IDisposable subscription = _viewModel.Entries.Subscribe(_ => notifiedCount++);
            int notifiedCountBeforeLoad = notifiedCount;

            await _viewStore.LoadAsync(typeof(TestSaveData));

            Assert.That(notifiedCount - notifiedCountBeforeLoad, Is.EqualTo(1));
        }

        /// <summary>
        ///     DetachedロードではViewModelの表示値を更新しない。
        /// </summary>
        [Test]
        public async Task LoadDetachedAsync_DoesNotRaiseViewModelUpdate()
        {
            // Service経由で保存値を作り、Registryをリセットして非接続状態を準備する。
            await _service.SaveAsync(typeof(TestSaveData));
            _service.Reset();

            TestSaveData target = new();
            int notifiedCount = 0;
            using IDisposable subscription = _viewModel.Entries.Subscribe(_ => notifiedCount++);
            int notifiedCountBeforeLoad = notifiedCount;

            await _viewStore.LoadDetachedAsync(typeof(TestSaveData), target);

            Assert.That(notifiedCount - notifiedCountBeforeLoad, Is.EqualTo(0));
        }

        #endregion
    }
}
