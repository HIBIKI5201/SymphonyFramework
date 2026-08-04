using System;
using System.Collections.Generic;

using NUnit.Framework;

using SymphonyFrameWork.System.SaveSystem;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     SaveDataEntryEntityとSaveDataEntryRegistryの契約を検証する。
    ///     どちらもUnity APIと外部I/Oに依存しないため、EditModeで直接検証できる。
    /// </summary>
    public sealed class SaveDataEntryRegistryTests
    {
        /// <summary> 検証用のセーブデータ型。 </summary>
        private sealed class TestSaveData : SaveDataContent
        {
            public int Value;
        }

        /// <summary> もう1つの検証用セーブデータ型。 </summary>
        private sealed class OtherSaveData : SaveDataContent
        {
        }

        #region Entity

        /// <summary> Entityの同一性はセーブデータ型で決まる。 </summary>
        [Test]
        public void Entity_ExposesDataTypeAndContent()
        {
            var content = new TestSaveData();
            var entity = new SaveDataEntryEntity(typeof(TestSaveData), content);

            Assert.That(entity.DataType, Is.EqualTo(typeof(TestSaveData)));
            Assert.That(entity.Content, Is.SameAs(content));
        }

        /// <summary> 生成直後は未読み込みである。 </summary>
        [Test]
        public void Entity_InitialState_IsNotLoaded()
        {
            var entity = new SaveDataEntryEntity(typeof(TestSaveData), new TestSaveData());

            Assert.That(entity.IsLoaded, Is.False);
        }

        /// <summary> 現在のキャッシュと同一インスタンスなら読み込み済みになる。 </summary>
        [Test]
        public void Entity_MarkLoadedIfCurrent_SameInstance_MarksLoaded()
        {
            var content = new TestSaveData();
            var entity = new SaveDataEntryEntity(typeof(TestSaveData), content);

            bool marked = entity.MarkLoadedIfCurrent(content);

            Assert.That(marked, Is.True);
            Assert.That(entity.IsLoaded, Is.True);
        }

        /// <summary>
        ///     別インスタンスでは読み込み済みにしない。
        ///     読み込み中にキャッシュが差し替わった場合へ古い結果を反映させないため。
        /// </summary>
        [Test]
        public void Entity_MarkLoadedIfCurrent_DifferentInstance_DoesNotMark()
        {
            var entity = new SaveDataEntryEntity(typeof(TestSaveData), new TestSaveData());

            bool marked = entity.MarkLoadedIfCurrent(new TestSaveData());

            Assert.That(marked, Is.False);
            Assert.That(entity.IsLoaded, Is.False);
        }

        /// <summary> 読み込み済み状態を解除できる。 </summary>
        [Test]
        public void Entity_MarkUnloaded_ClearsLoadedState()
        {
            var content = new TestSaveData();
            var entity = new SaveDataEntryEntity(typeof(TestSaveData), content);
            entity.MarkLoadedIfCurrent(content);

            entity.MarkUnloaded();

            Assert.That(entity.IsLoaded, Is.False);
        }

        /// <summary> nullを渡した生成は拒否される。 </summary>
        [Test]
        public void Entity_NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(
                () => new SaveDataEntryEntity(null, new TestSaveData()));
            Assert.Throws<ArgumentNullException>(
                () => new SaveDataEntryEntity(typeof(TestSaveData), null));
        }

        #endregion

        #region Registry

        /// <summary> 未登録の型は既定値のインスタンスで作成される。 </summary>
        [Test]
        public void Registry_GetOrCreate_CreatesEntry()
        {
            var registry = new SaveDataEntryRegistry();

            SaveDataEntryEntity entry = registry.GetOrCreate(typeof(TestSaveData));

            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.DataType, Is.EqualTo(typeof(TestSaveData)));
            Assert.That(entry.Content, Is.TypeOf<TestSaveData>());
        }

        /// <summary> 同じ型を要求すると同一のEntityが返る。 </summary>
        [Test]
        public void Registry_GetOrCreate_SameType_ReturnsSameEntry()
        {
            var registry = new SaveDataEntryRegistry();

            SaveDataEntryEntity first = registry.GetOrCreate(typeof(TestSaveData));
            SaveDataEntryEntity second = registry.GetOrCreate(typeof(TestSaveData));

            Assert.That(second, Is.SameAs(first));
        }

        /// <summary> 型ごとに別のEntityを保持する。 </summary>
        [Test]
        public void Registry_GetOrCreate_DifferentTypes_AreIndependent()
        {
            var registry = new SaveDataEntryRegistry();

            SaveDataEntryEntity first = registry.GetOrCreate(typeof(TestSaveData));
            SaveDataEntryEntity second = registry.GetOrCreate(typeof(OtherSaveData));

            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(registry.GetEntities().Count, Is.EqualTo(2));
        }

        /// <summary> 未登録の型は未読み込みとして扱う。 </summary>
        [Test]
        public void Registry_IsLoaded_UnknownType_ReturnsFalse()
        {
            var registry = new SaveDataEntryRegistry();

            Assert.That(registry.IsLoaded(typeof(TestSaveData)), Is.False);
        }

        /// <summary> 現在のキャッシュと同一インスタンスなら読み込み済みになる。 </summary>
        [Test]
        public void Registry_MarkLoaded_SameInstance_MarksLoaded()
        {
            var registry = new SaveDataEntryRegistry();
            SaveDataEntryEntity entry = registry.GetOrCreate(typeof(TestSaveData));

            registry.MarkLoaded(typeof(TestSaveData), entry.Content);

            Assert.That(registry.IsLoaded(typeof(TestSaveData)), Is.True);
        }

        /// <summary> 別インスタンスでは読み込み済みにしない。 </summary>
        [Test]
        public void Registry_MarkLoaded_DifferentInstance_DoesNotMark()
        {
            var registry = new SaveDataEntryRegistry();
            registry.GetOrCreate(typeof(TestSaveData));

            registry.MarkLoaded(typeof(TestSaveData), new TestSaveData());

            Assert.That(registry.IsLoaded(typeof(TestSaveData)), Is.False);
        }

        /// <summary> 読み込み済み状態を解除できる。 </summary>
        [Test]
        public void Registry_MarkUnloaded_ClearsLoadedState()
        {
            var registry = new SaveDataEntryRegistry();
            SaveDataEntryEntity entry = registry.GetOrCreate(typeof(TestSaveData));
            registry.MarkLoaded(typeof(TestSaveData), entry.Content);

            registry.MarkUnloaded(typeof(TestSaveData));

            Assert.That(registry.IsLoaded(typeof(TestSaveData)), Is.False);
        }

        /// <summary> 列挙用に返すのは複製であり、変更してもレジストリへ影響しない。 </summary>
        [Test]
        public void Registry_GetEntities_ReturnsCopy()
        {
            var registry = new SaveDataEntryRegistry();
            registry.GetOrCreate(typeof(TestSaveData));

            IReadOnlyList<SaveDataEntryEntity> first = registry.GetEntities();
            registry.GetOrCreate(typeof(OtherSaveData));

            Assert.That(first.Count, Is.EqualTo(1), "取得済みの一覧は後から追加されたエントリを含まない。");
            Assert.That(registry.GetEntities().Count, Is.EqualTo(2));
        }

        /// <summary> 全消去でエントリと読み込み済み状態が空になる。 </summary>
        [Test]
        public void Registry_Clear_RemovesAllEntries()
        {
            var registry = new SaveDataEntryRegistry();
            SaveDataEntryEntity entry = registry.GetOrCreate(typeof(TestSaveData));
            registry.MarkLoaded(typeof(TestSaveData), entry.Content);

            registry.Clear();

            Assert.That(registry.GetEntities(), Is.Empty);
            Assert.That(registry.IsLoaded(typeof(TestSaveData)), Is.False);
        }

        #endregion

        #region 版番号

        /// <summary>
        ///     版番号は状態が実際に変わったときだけ増える。
        ///     Serviceが「通知すべき変化があったか」を判定するために使う。
        /// </summary>
        [Test]
        public void Registry_Version_IncrementsOnlyOnActualChange()
        {
            var registry = new SaveDataEntryRegistry();
            int initial = registry.Version;

            SaveDataEntryEntity entry = registry.GetOrCreate(typeof(TestSaveData));
            int afterCreate = registry.Version;
            Assert.That(afterCreate, Is.GreaterThan(initial), "新規作成で増える。");

            registry.GetOrCreate(typeof(TestSaveData));
            Assert.That(registry.Version, Is.EqualTo(afterCreate), "既存エントリの取得では増えない。");

            registry.MarkLoaded(typeof(TestSaveData), entry.Content);
            int afterLoad = registry.Version;
            Assert.That(afterLoad, Is.GreaterThan(afterCreate), "読み込み済みへの遷移で増える。");

            registry.MarkLoaded(typeof(TestSaveData), entry.Content);
            Assert.That(registry.Version, Is.EqualTo(afterLoad), "同じ状態の再設定では増えない。");

            registry.MarkUnloaded(typeof(TestSaveData));
            int afterUnload = registry.Version;
            Assert.That(afterUnload, Is.GreaterThan(afterLoad), "読み込み済みの解除で増える。");

            registry.MarkUnloaded(typeof(TestSaveData));
            Assert.That(registry.Version, Is.EqualTo(afterUnload), "未読み込みの再解除では増えない。");

            registry.Clear();
            int afterClear = registry.Version;
            Assert.That(afterClear, Is.GreaterThan(afterUnload), "全消去で増える。");

            registry.Clear();
            Assert.That(registry.Version, Is.EqualTo(afterClear), "空の状態の再消去では増えない。");
        }

        #endregion

        #region 進行中ロードの重複排除

        /// <summary> 進行中の処理が無ければ取得できない。 </summary>
        [Test]
        public void Registry_TryGetLoadingTask_NotRegistered_ReturnsFalse()
        {
            var registry = new SaveDataEntryRegistry();

            Assert.That(registry.TryGetLoadingTask(typeof(TestSaveData), out _), Is.False);
        }

        /// <summary>
        ///     **完了済みのタスクは登録しない。**
        ///     同期的に完了するローダーで完了済みタスクが残ると、以降のロードが
        ///     すべて重複扱いになって実行されなくなるため。
        /// </summary>
        [Test]
        public void Registry_RegisterLoadingTaskIfPending_CompletedTask_IsNotRegistered()
        {
            var registry = new SaveDataEntryRegistry();

            registry.RegisterLoadingTaskIfPending(
                typeof(TestSaveData),
                global::System.Threading.Tasks.Task.CompletedTask);

            Assert.That(
                registry.TryGetLoadingTask(typeof(TestSaveData), out _),
                Is.False,
                "完了済みタスクを登録すると以降のロードが二度と走らなくなる。");
        }

        /// <summary> 未完了のタスクは重複排除用に登録される。 </summary>
        [Test]
        public void Registry_RegisterLoadingTaskIfPending_PendingTask_IsRegistered()
        {
            var registry = new SaveDataEntryRegistry();
            var completionSource = new global::System.Threading.Tasks.TaskCompletionSource<bool>();

            registry.RegisterLoadingTaskIfPending(typeof(TestSaveData), completionSource.Task);

            Assert.That(registry.TryGetLoadingTask(typeof(TestSaveData), out _), Is.True);

            registry.RemoveLoadingTask(typeof(TestSaveData));
            Assert.That(registry.TryGetLoadingTask(typeof(TestSaveData), out _), Is.False);

            completionSource.SetResult(true);
        }

        #endregion
    }
}
