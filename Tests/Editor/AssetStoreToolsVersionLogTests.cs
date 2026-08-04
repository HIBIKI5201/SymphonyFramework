using NUnit.Framework;

using SymphonyFrameWork.Editor;

using System.Collections.Generic;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     バージョンログのリビジョン操作と、変更パスからのディレクトリ名解決を検証する。
    /// </summary>
    public sealed class AssetStoreToolsVersionLogTests
    {
        /// <summary> ディレクトリ配下のアセットからディレクトリ名を解決する。 </summary>
        [Test]
        public void TryResolveDirectoryName_AssetUnderDirectory_ReturnsDirectoryName()
        {
            bool result = AssetStoreToolsVersionPathResolver.TryResolveDirectoryName(
                "Assets/AssetStoreTools/Demigiant/DOTween/DOTween.dll",
                ROOT_PATH,
                out string directoryName);

            Assert.That(result, Is.True);
            Assert.That(directoryName, Is.EqualTo("Demigiant"));
        }

        /// <summary> 対象フォルダ直下のファイルはどのパッケージにも属さない。 </summary>
        [Test]
        public void TryResolveDirectoryName_FileAtRoot_ReturnsFalse()
        {
            bool result = AssetStoreToolsVersionPathResolver.TryResolveDirectoryName(
                "Assets/AssetStoreTools/PackagerConfig.json",
                ROOT_PATH,
                out string directoryName);

            Assert.That(result, Is.False);
            Assert.That(directoryName, Is.Null);
        }

        /// <summary> ディレクトリ自身のパスでは加算しない。 </summary>
        [Test]
        public void TryResolveDirectoryName_DirectoryItself_ReturnsFalse()
        {
            bool result = AssetStoreToolsVersionPathResolver.TryResolveDirectoryName(
                "Assets/AssetStoreTools/Demigiant",
                ROOT_PATH,
                out _);

            Assert.That(result, Is.False);
        }

        /// <summary>
        ///     出力時バージョンファイル自身では加算しない。
        ///     加算するとパッケージ出力が次の加算を呼び、リビジョンが際限なく増える。
        /// </summary>
        [Test]
        public void TryResolveDirectoryName_ExportedVersionFile_ReturnsFalse()
        {
            bool result = AssetStoreToolsVersionPathResolver.TryResolveDirectoryName(
                "Assets/AssetStoreTools/Demigiant/ExportedVersion.json",
                ROOT_PATH,
                out _);

            Assert.That(result, Is.False);
        }

        /// <summary> 出力時バージョンファイルの.metaでも加算しない。 </summary>
        [Test]
        public void TryResolveDirectoryName_ExportedVersionMetaFile_ReturnsFalse()
        {
            bool result = AssetStoreToolsVersionPathResolver.TryResolveDirectoryName(
                "Assets/AssetStoreTools/Demigiant/ExportedVersion.json.meta",
                ROOT_PATH,
                out _);

            Assert.That(result, Is.False);
        }

        /// <summary> 対象フォルダの外は解決しない。 </summary>
        [Test]
        public void TryResolveDirectoryName_OutsideRoot_ReturnsFalse()
        {
            bool result = AssetStoreToolsVersionPathResolver.TryResolveDirectoryName(
                "Assets/Scripts/Foo.cs",
                ROOT_PATH,
                out _);

            Assert.That(result, Is.False);
        }

        /// <summary> Windowsのパス区切りでも解決する。 </summary>
        [Test]
        public void TryResolveDirectoryName_BackslashPath_ReturnsDirectoryName()
        {
            bool result = AssetStoreToolsVersionPathResolver.TryResolveDirectoryName(
                @"Assets\AssetStoreTools\Demigiant\Foo.cs",
                ROOT_PATH,
                out string directoryName);

            Assert.That(result, Is.True);
            Assert.That(directoryName, Is.EqualTo("Demigiant"));
        }

        /// <summary> ルートパスの末尾スラッシュを許容する。 </summary>
        [Test]
        public void TryResolveDirectoryName_RootWithTrailingSlash_ReturnsDirectoryName()
        {
            bool result = AssetStoreToolsVersionPathResolver.TryResolveDirectoryName(
                "Assets/AssetStoreTools/Demigiant/Foo.cs",
                "Assets/AssetStoreTools/",
                out string directoryName);

            Assert.That(result, Is.True);
            Assert.That(directoryName, Is.EqualTo("Demigiant"));
        }

        /// <summary> 未登録のディレクトリのリビジョンは0として扱う。 </summary>
        [Test]
        public void GetVersion_UnknownName_ReturnsZero()
        {
            var log = new AssetStoreToolsVersionLog();

            Assert.That(log.GetVersion("Foo"), Is.Zero);
        }

        /// <summary> 未登録のディレクトリを加算するとリビジョン1で登録される。 </summary>
        [Test]
        public void IncrementVersion_UnknownName_BecomesOne()
        {
            var log = new AssetStoreToolsVersionLog();

            log.IncrementVersion("Foo");

            Assert.That(log.GetVersion("Foo"), Is.EqualTo(1));
        }

        /// <summary> 登録済みのリビジョンは1つだけ進む。 </summary>
        [Test]
        public void IncrementVersion_KnownName_AddsOne()
        {
            AssetStoreToolsVersionLog log = CreateLog("Foo", 3);

            log.IncrementVersion("Foo");

            Assert.That(log.GetVersion("Foo"), Is.EqualTo(4));
        }

        /// <summary> 加算すると更新日時が書き換わる。 </summary>
        [Test]
        public void IncrementVersion_UpdatesTimestamp()
        {
            AssetStoreToolsVersionLog log = CreateLog("Foo", 1);

            // 絶対時刻ではなく、加算前の値からの変化で判定する。
            string beforeTimestamp = log.Directories[0].UpdatedAt;
            log.IncrementVersion("Foo");

            Assert.That(log.Directories[0].UpdatedAt, Is.Not.EqualTo(beforeTimestamp));
        }

        /// <summary> フィールドが欠落したJSONを読んでも空一覧として扱う。 </summary>
        [Test]
        public void Normalize_NullEntries_BecomesEmptyList()
        {
            var log = new AssetStoreToolsVersionLog { Directories = null };

            AssetStoreToolsVersionLog normalized = log.Normalize();

            Assert.That(normalized.Directories, Is.Empty);
        }

        /// <summary> 名前が空白のみのエントリは捨てる。 </summary>
        [Test]
        public void Normalize_EntryWithEmptyName_IsRemoved()
        {
            var log = new AssetStoreToolsVersionLog
            {
                Directories = new List<AssetStoreToolsVersionEntry>
                {
                    new() { Name = "Foo", Version = 1 },
                    new() { Name = "  ", Version = 2 },
                },
            };

            AssetStoreToolsVersionLog normalized = log.Normalize();

            Assert.That(normalized.Directories.Count, Is.EqualTo(1));
            Assert.That(normalized.Directories[0].Name, Is.EqualTo("Foo"));
        }

        /// <summary> 手で編集して負値になったリビジョンは0へ丸める。 </summary>
        [Test]
        public void Normalize_NegativeVersion_BecomesZero()
        {
            AssetStoreToolsVersionLog log = CreateLog("Foo", -5);

            AssetStoreToolsVersionLog normalized = log.Normalize();

            Assert.That(normalized.GetVersion("Foo"), Is.Zero);
        }

        /// <summary> テストで使うパッケージ対象フォルダのルートパス。 </summary>
        private const string ROOT_PATH = "Assets/AssetStoreTools";

        /// <summary> 1件だけ登録した状態のバージョンログを生成する。 </summary>
        /// <param name="name"> 登録するディレクトリ名。 </param>
        /// <param name="version"> 登録するリビジョン。 </param>
        /// <returns> 指定した1件を持つバージョンログ。 </returns>
        private static AssetStoreToolsVersionLog CreateLog(string name, int version) => new()
        {
            Directories = new List<AssetStoreToolsVersionEntry>
            {
                new()
                {
                    Name = name,
                    Version = version,
                    UpdatedAt = "2000-01-01T00:00:00.0000000Z",
                },
            },
        };
    }
}
