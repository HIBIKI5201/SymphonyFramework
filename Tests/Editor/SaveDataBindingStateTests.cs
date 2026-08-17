using System;
using System.Collections.Generic;

using NUnit.Framework;

using SymphonyFrameWork.Editor;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     Save Data Inspectorのバインド状態と表示値の対応を検証する。
    /// </summary>
    public sealed class SaveDataBindingStateTests
    {
        /// <summary>
        ///     未選択なら読み込み状態に関係なくNoneを返す。
        /// </summary>
        [Test]
        public void Resolve_NoSelection_ReturnsNone()
        {
            SaveDataBindingSourceEnum source = SaveDataBindingState.Resolve(false, true, true);

            Assert.That(source, Is.EqualTo(SaveDataBindingSourceEnum.None));
        }

        /// <summary>
        ///     Registryが読み込み済みならRegistryを返す。
        /// </summary>
        [Test]
        public void Resolve_RegistryLoaded_ReturnsRegistry()
        {
            SaveDataBindingSourceEnum source = SaveDataBindingState.Resolve(true, true, false);

            Assert.That(source, Is.EqualTo(SaveDataBindingSourceEnum.Registry));
        }

        /// <summary>
        ///     非接続でWindow専用インスタンスが読み込み済みならLoadedを返す。
        /// </summary>
        [Test]
        public void Resolve_LocalContentLoaded_ReturnsLoaded()
        {
            SaveDataBindingSourceEnum source = SaveDataBindingState.Resolve(true, false, true);

            Assert.That(source, Is.EqualTo(SaveDataBindingSourceEnum.Loaded));
        }

        /// <summary>
        ///     非接続でWindow専用インスタンスが未読み込みならInstanceを返す。
        /// </summary>
        [Test]
        public void Resolve_LocalContentNotLoaded_ReturnsInstance()
        {
            SaveDataBindingSourceEnum source = SaveDataBindingState.Resolve(true, false, false);

            Assert.That(source, Is.EqualTo(SaveDataBindingSourceEnum.Instance));
        }

        /// <summary>
        ///     RegistryとWindow専用インスタンスが読み込み済みならRegistryを優先する。
        /// </summary>
        [Test]
        public void Resolve_RegistryLoaded_TakesPrecedenceOverLocalContent()
        {
            SaveDataBindingSourceEnum source = SaveDataBindingState.Resolve(true, true, true);

            Assert.That(source, Is.EqualTo(SaveDataBindingSourceEnum.Registry));
        }

        /// <summary>
        ///     全状態が互いに異なるUSSクラス名を返す。
        /// </summary>
        [Test]
        public void GetLampUssClassName_EverySource_ReturnsDistinctName()
        {
            Array sources = Enum.GetValues(typeof(SaveDataBindingSourceEnum));
            HashSet<string> classNames = new();

            foreach (SaveDataBindingSourceEnum source in sources)
            {
                classNames.Add(SaveDataBindingState.GetLampUssClassName(source));
            }

            Assert.That(classNames.Count, Is.EqualTo(sources.Length));
        }

        /// <summary>
        ///     全状態のUSSクラス名が取り外し対象一覧に含まれる。
        /// </summary>
        [Test]
        public void AllLampUssClassNames_ContainsEveryClassName()
        {
            foreach (SaveDataBindingSourceEnum source in Enum.GetValues(typeof(SaveDataBindingSourceEnum)))
            {
                string className = SaveDataBindingState.GetLampUssClassName(source);
                Assert.That(SaveDataBindingState.AllLampUssClassNames, Does.Contain(className));
            }
        }

        /// <summary>
        ///     全状態が空でない表示文言を返す。
        /// </summary>
        [Test]
        public void GetDisplayText_EverySource_ReturnsNonEmpty()
        {
            foreach (SaveDataBindingSourceEnum source in Enum.GetValues(typeof(SaveDataBindingSourceEnum)))
            {
                Assert.That(SaveDataBindingState.GetDisplayText(source), Is.Not.Empty);
            }
        }

        /// <summary>
        ///     ロード中は進行中の文言を返す。
        /// </summary>
        [Test]
        public void BuildStatusSuffix_Loading_ReportsLoading()
        {
            string suffix = SaveDataBindingState.BuildStatusSuffix(true, false);

            Assert.That(suffix, Does.Contain("Loading"));
        }

        /// <summary>
        ///     Dirty状態は未保存の変更を示す文言を返す。
        /// </summary>
        [Test]
        public void BuildStatusSuffix_Dirty_ReportsUnsavedChange()
        {
            string suffix = SaveDataBindingState.BuildStatusSuffix(false, true);

            Assert.That(suffix, Does.Contain("未保存"));
        }

        /// <summary>
        ///     ロード中でもDirtyでもなければ空文字列を返す。
        /// </summary>
        [Test]
        public void BuildStatusSuffix_Idle_ReturnsEmpty()
        {
            string suffix = SaveDataBindingState.BuildStatusSuffix(false, false);

            Assert.That(suffix, Is.Empty);
        }
    }
}
