using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using SymphonyFrameWork.System.SaveSystem;

using UnityEngine;
using UnityEngine.TestTools;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     SaveDataViewModelがServiceの状態変更を表示用一覧へ反映する契約を検証する。
    ///     保存先はメモリ上のテスト用ローダーへ差し替えるため、EditModeで完結する。
    /// </summary>
    public sealed class SaveDataViewModelTests
    {
        /// <summary> 検証用のセーブデータ型。 </summary>
        [Serializable]
        private sealed class TestSaveData : SaveDataContent
        {
            public int Value;
        }

        /// <summary> 保存先をメモリ上のDictionaryに置き換えたローダー。 </summary>
        private sealed class MemorySaveDataLoader : SaveDataLoader
        {
            private readonly Dictionary<Type, string> _storage = new();

            protected override bool ExistsCore(Type dataType) => _storage.ContainsKey(dataType);

            protected override ValueTask<string> LoadJsonAsync(Type dataType, CancellationToken token)
            {
                _storage.TryGetValue(dataType, out string json);
                return new ValueTask<string>(json);
            }

            protected override ValueTask SaveJsonAsync(Type dataType, string json, CancellationToken token)
            {
                _storage[dataType] = json;
                return default;
            }

            protected override ValueTask DeleteCoreAsync(Type dataType, CancellationToken token)
            {
                _storage.Remove(dataType);
                return default;
            }

            protected override string SerializeToJson(Type dataType, SaveDataContent data) =>
                JsonUtility.ToJson(data);

            protected override void OverwriteFromJson(Type dataType, string json, SaveDataContent data) =>
                JsonUtility.FromJsonOverwrite(json, data);
        }

        private SaveDataEntryRegistry _registry;
        private SaveDataService _service;
        private SaveDataQuery _query;
        private SaveDataViewModel _viewModel;

        /// <summary> 各テストごとに独立したレジストリとViewModelを構築する。 </summary>
        [SetUp]
        public void SetUp()
        {
            _registry = new SaveDataEntryRegistry();
            var loader = new MemorySaveDataLoader();
            _service = new SaveDataService(_registry, () => loader);
            _query = new SaveDataQuery(_registry);
            _viewModel = new SaveDataViewModel(_query, _service);
        }

        /// <summary> ViewModelを破棄して次のテストへ状態を持ち越さない。 </summary>
        [TearDown]
        public void TearDown()
        {
            _viewModel?.Dispose();
            _viewModel = null;
        }

        /// <summary> 引数を省略した生成は拒否される。 </summary>
        [Test]
        public void Constructor_NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => new SaveDataViewModel(null, _service));
            Assert.Throws<ArgumentNullException>(() => new SaveDataViewModel(_query, null));
        }

        /// <summary> 生成直後はQueryの現在値をそのまま公開する。 </summary>
        [Test]
        public void InitialValue_MatchesQuery()
        {
            Assert.That(_viewModel.Entries.Value, Is.Empty);

            _registry.GetOrCreate(typeof(TestSaveData));
            using var viewModel = new SaveDataViewModel(_query, _service);

            Assert.That(viewModel.Entries.Value.Count, Is.EqualTo(1));
        }

        /// <summary> ロード完了で一覧が更新される。 </summary>
        [Test]
        public async Task LoadAsync_UpdatesEntries()
        {
            await _service.LoadAsync(typeof(TestSaveData));

            IReadOnlyList<SaveDataDto> entries = _viewModel.Entries.Value;
            Assert.That(entries.Count, Is.EqualTo(1));
            Assert.That(entries[0].DataType, Is.EqualTo(typeof(TestSaveData)));
            Assert.That(entries[0].IsLoaded, Is.True);
        }

        /// <summary>
        ///     **保存で変わるのは保存日時だけであり、レジストリの構造は変わらない。**
        ///     版番号ベースの変更検出では取りこぼすため、保存は常に通知する。
        /// </summary>
        [Test]
        public async Task SaveAsync_NotifiesSaveDateChange()
        {
            await _service.LoadAsync(typeof(TestSaveData));

            int versionBeforeSave = _registry.Version;
            int notifiedCount = 0;
            using IDisposable subscription =
                _viewModel.Entries.Subscribe(_ => notifiedCount++, notifyCurrent: false);

            await _service.SaveAsync(typeof(TestSaveData));

            Assert.That(
                _registry.Version,
                Is.EqualTo(versionBeforeSave),
                "保存はレジストリの版番号を変えない。");
            Assert.That(notifiedCount, Is.EqualTo(1));
            Assert.That(_viewModel.Entries.Value[0].SaveDate, Is.Not.Null.And.Not.Empty);
        }

        /// <summary> 内容が変わらない操作では通知しない。 </summary>
        [Test]
        public async Task Reset_WithoutEntries_DoesNotNotify()
        {
            await _service.LoadAsync(typeof(TestSaveData));

            int notifiedCount = 0;
            using IDisposable subscription =
                _viewModel.Entries.Subscribe(_ => notifiedCount++, notifyCurrent: false);

            _service.Reset();
            Assert.That(notifiedCount, Is.EqualTo(1), "エントリの消去は通知する。");
            Assert.That(_viewModel.Entries.Value, Is.Empty);

            _service.Reset();
            Assert.That(notifiedCount, Is.EqualTo(1), "既に空なら通知しない。");
        }

        /// <summary> 破棄後はServiceの変更を反映せず、多重破棄も無害である。 </summary>
        [Test]
        public async Task Dispose_StopsUpdatesAndIsIdempotent()
        {
            _viewModel.Dispose();
            _viewModel.Dispose();

            await _service.LoadAsync(typeof(TestSaveData));

            Assert.That(_viewModel.Entries.Value, Is.Empty);
        }

        /// <summary>
        ///     購読側の例外を保存処理へ伝播させない。
        ///     デバッグ表示の失敗でセーブが失敗すると被害が大きいため、発行側で止める。
        /// </summary>
        [Test]
        public void SaveAsync_SubscriberException_DoesNotFailOperation()
        {
            LogAssert.Expect(LogType.Exception, new Regex("subscriber failure"));

            _service.OnStateChanged += () => throw new InvalidOperationException("subscriber failure");

            Assert.DoesNotThrowAsync(async () => await _service.SaveAsync(typeof(TestSaveData)));
        }
    }
}
