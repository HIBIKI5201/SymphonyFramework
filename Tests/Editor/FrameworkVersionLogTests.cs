using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using SymphonyFrameWork.Core;
using SymphonyFrameWork.Debugger.Logger;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.TestTools;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     エラーログがFrameworkのバージョンを伴い、その値がpackage.jsonと一致する契約を検証する。
    /// </summary>
    /// <remarks>
    ///     報告を受ける側が最初に必要とするのがバージョンである。
    ///     **定数がpackage.jsonからずれると、報告されたログの版が嘘になる。**
    /// </remarks>
    public sealed class FrameworkVersionLogTests
    {
        /// <summary> テスト中に届いた通知。 </summary>
        private readonly List<(string text, LogKindEnum kind)> _received = new();

        /// <summary> 購読を開始する。 </summary>
        [SetUp]
        public void SetUp()
        {
            _received.Clear();
            SymphonyDebugLogger.OnLogDirect += Receive;
        }

        /// <summary> 購読を解除する。 </summary>
        [TearDown]
        public void TearDown() => SymphonyDebugLogger.OnLogDirect -= Receive;

        /// <summary> VERSION定数がpackage.jsonのversionと一致する。 </summary>
        [Test]
        public void Version_Constant_MatchesPackageManifest()
        {
            string manifestPath = Path.Combine(EditorSymphonyConstant.FRAMEWORK_PATH, "package.json");

            Assert.That(File.Exists(manifestPath), Is.True, $"'{manifestPath}' が見つかりません。");

            Match match = Regex.Match(
                File.ReadAllText(manifestPath), "\"version\"\\s*:\\s*\"(?<version>[^\"]+)\"");

            Assert.That(match.Success, Is.True, "package.jsonにversionが見つかりません。");
            Assert.That(SymphonyConstant.VERSION, Is.EqualTo(match.Groups["version"].Value));
        }

        /// <summary> エラーログの先頭へバージョン表記を付ける。 </summary>
        [Test]
        public void LogDirect_ErrorKind_PrependsVersionTag()
        {
            LogAssert.Expect(LogType.Error, new Regex(Regex.Escape(SymphonyDebugLogger.VersionTag)));

            SymphonyDebugLogger.LogDirect("失敗しました", LogKindEnum.Error);

            Assert.That(_received, Has.Count.EqualTo(1));
            Assert.That(_received[0].text, Does.StartWith(SymphonyDebugLogger.VersionTag));
            Assert.That(_received[0].text, Does.EndWith("失敗しました"));
        }

        /// <summary> バージョン表記は実際の版を含む。 </summary>
        [Test]
        public void VersionTag_Format_ContainsFrameworkNameAndVersion()
        {
            Assert.That(SymphonyDebugLogger.VersionTag, Does.Contain(SymphonyConstant.SYMPHONY_FRAMEWORK));
            Assert.That(SymphonyDebugLogger.VersionTag, Does.Contain(SymphonyConstant.VERSION));
        }

        /// <summary> 通常ログと警告ログにはバージョン表記を付けない。 </summary>
        [Test]
        public void LogDirect_NormalAndWarningKind_DoesNotPrependVersionTag()
        {
            LogAssert.Expect(LogType.Log, new Regex("通常の記録"));
            LogAssert.Expect(LogType.Warning, new Regex("注意の記録"));

            SymphonyDebugLogger.LogDirect("通常の記録");
            SymphonyDebugLogger.LogDirect("注意の記録", LogKindEnum.Warning);

            Assert.That(_received, Has.Count.EqualTo(2));
            Assert.That(_received[0].text, Is.EqualTo("通常の記録"));
            Assert.That(_received[1].text, Is.EqualTo("注意の記録"));
        }

        /// <summary> 例外の通知にもバージョン表記を付ける。 </summary>
        [Test]
        public void LogException_Exception_PrependsVersionTagToNotification()
        {
            LogAssert.Expect(LogType.Exception, new Regex(@"InvalidOperationException: 例外の記録"));

            SymphonyDebugLogger.LogException(new InvalidOperationException("例外の記録"));

            Assert.That(_received, Has.Count.EqualTo(1));
            Assert.That(_received[0].text, Does.StartWith(SymphonyDebugLogger.VersionTag));
            Assert.That(_received[0].text, Does.Contain("例外の記録"));
        }

        /// <summary> 通知の内容を記録する。 </summary>
        /// <param name="text"> 通知されたテキスト。 </param>
        /// <param name="kind"> 通知された重要度。 </param>
        private void Receive(string text, LogKindEnum kind) => _received.Add((text, kind));
    }
}
