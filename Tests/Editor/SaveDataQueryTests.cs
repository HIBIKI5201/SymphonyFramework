using System;
using System.Collections.Generic;

using NUnit.Framework;

using SymphonyFrameWork.System.SaveSystem;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     SaveDataQueryがEntityを公開Infoと表示用Dtoへ変換する契約を検証する。
    ///     Unity APIと外部I/Oに依存しないため、EditModeで直接検証できる。
    /// </summary>
    public sealed class SaveDataQueryTests
    {
        /// <summary> 型名がordinal順で先に来る検証用セーブデータ型。 </summary>
        private sealed class AlphaSaveData : SaveDataContent
        {
        }

        /// <summary> 型名がordinal順で後に来る検証用セーブデータ型。 </summary>
        private sealed class BetaSaveData : SaveDataContent
        {
        }

        /// <summary> レジストリを指定しない生成は拒否される。 </summary>
        [Test]
        public void Constructor_NullRegistry_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new SaveDataQuery(null));
        }

        /// <summary> 登録順に関係なく型の完全名でordinal昇順に並ぶ。 </summary>
        [Test]
        public void GetInfos_SortsByTypeFullName()
        {
            var registry = new SaveDataEntryRegistry();
            registry.GetOrCreate(typeof(BetaSaveData));
            registry.GetOrCreate(typeof(AlphaSaveData));
            var query = new SaveDataQuery(registry);

            IReadOnlyList<SaveDataRegistryEntryInfo> infos = query.GetInfos();

            Assert.That(
                new[] { infos[0].DataType, infos[1].DataType },
                Is.EqualTo(new[] { typeof(AlphaSaveData), typeof(BetaSaveData) }));
        }

        /// <summary> Dtoも公開Infoと同じ順序で並ぶ。 </summary>
        [Test]
        public void GetDtos_SortsByTypeFullName()
        {
            var registry = new SaveDataEntryRegistry();
            registry.GetOrCreate(typeof(BetaSaveData));
            registry.GetOrCreate(typeof(AlphaSaveData));
            var query = new SaveDataQuery(registry);

            IReadOnlyList<SaveDataDto> dtos = query.GetDtos();

            Assert.That(
                new[] { dtos[0].DataType, dtos[1].DataType },
                Is.EqualTo(new[] { typeof(AlphaSaveData), typeof(BetaSaveData) }));
            Assert.That(dtos[0].DataTypeName, Is.EqualTo(typeof(AlphaSaveData).FullName));
        }

        /// <summary>
        ///     キャッシュが存在することと読み込み済みであることは別の状態である。
        ///     生成直後はインスタンスがあっても未読み込みとして返す。
        /// </summary>
        [Test]
        public void GetInfos_DistinguishesCachedFromLoaded()
        {
            var registry = new SaveDataEntryRegistry();
            SaveDataEntryEntity entry = registry.GetOrCreate(typeof(AlphaSaveData));
            var query = new SaveDataQuery(registry);

            SaveDataRegistryEntryInfo created = query.GetInfos()[0];
            Assert.That(created.Data, Is.Not.Null);
            Assert.That(created.IsLoaded, Is.False);

            registry.MarkLoaded(typeof(AlphaSaveData), entry.Content);

            Assert.That(query.GetInfos()[0].IsLoaded, Is.True);
            Assert.That(query.GetDtos()[0].IsLoaded, Is.True);
        }

        /// <summary>
        ///     取得結果をキャッシュしない。
        ///     版番号では保存日時の変化を検出できないため、毎回組み立て直す。
        /// </summary>
        [Test]
        public void GetInfos_IsNotCached()
        {
            var registry = new SaveDataEntryRegistry();
            registry.GetOrCreate(typeof(AlphaSaveData));
            var query = new SaveDataQuery(registry);

            Assert.That(query.GetInfos().Count, Is.EqualTo(1));

            registry.GetOrCreate(typeof(BetaSaveData));

            Assert.That(query.GetInfos().Count, Is.EqualTo(2));
        }

        /// <summary> 返す一覧は変更できない。 </summary>
        [Test]
        public void GetInfos_ReturnsReadOnlyList()
        {
            var registry = new SaveDataEntryRegistry();
            registry.GetOrCreate(typeof(AlphaSaveData));
            var query = new SaveDataQuery(registry);

            Assert.That(
                ((IList<SaveDataRegistryEntryInfo>)query.GetInfos()).IsReadOnly,
                Is.True);
            Assert.That(
                ((IList<SaveDataDto>)query.GetDtos()).IsReadOnly,
                Is.True);
        }

        /// <summary> エントリが無ければ空一覧を返す。 </summary>
        [Test]
        public void GetInfos_EmptyRegistry_ReturnsEmpty()
        {
            var query = new SaveDataQuery(new SaveDataEntryRegistry());

            Assert.That(query.GetInfos(), Is.Empty);
            Assert.That(query.GetDtos(), Is.Empty);
        }
    }
}
