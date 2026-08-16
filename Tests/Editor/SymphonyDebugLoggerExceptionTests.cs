using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using SymphonyFrameWork.Core;
using SymphonyFrameWork.Debugger.Logger;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.TestTools;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     例外ログがConsoleと購読者の両方へ届き、Core層のログも同じ集約先へ載る契約を検証する。
    /// </summary>
    public sealed class SymphonyDebugLoggerExceptionTests
    {
        /// <summary> テスト中に届いた通知。 </summary>
        private readonly List<(string text, LogKindEnum kind)> _received = new();

        /// <summary> 復元用に、テスト開始時点のCore中継先を控える。 </summary>
        private Action<Exception> _previousRelay;

        /// <summary> 購読を開始し、Coreの中継先を退避する。 </summary>
        [SetUp]
        public void SetUp()
        {
            _received.Clear();
            _previousRelay = CoreLogRelay.ExceptionHandler;
            SymphonyDebugLogger.OnLogDirect += Receive;
        }

        /// <summary> 購読を解除し、Coreの中継先を戻す。 </summary>
        [TearDown]
        public void TearDown()
        {
            SymphonyDebugLogger.OnLogDirect -= Receive;
            CoreLogRelay.ExceptionHandler = _previousRelay;
        }

        /// <summary> 例外をConsoleへ例外として出し、購読者へはError種別で通知する。 </summary>
        [Test]
        public void LogException_Exception_NotifiesSubscribersAsError()
        {
            LogAssert.Expect(LogType.Exception, new Regex(@"InvalidOperationException: 失敗しました"));

            SymphonyDebugLogger.LogException(new InvalidOperationException("失敗しました"));

            Assert.That(_received, Has.Count.EqualTo(1));
            Assert.That(_received[0].kind, Is.EqualTo(LogKindEnum.Error));
            Assert.That(_received[0].text, Does.Contain("System.InvalidOperationException"));
            Assert.That(_received[0].text, Does.Contain("失敗しました"));
        }

        /// <summary> nullを渡しても例外にならず、診断としてError種別で通知する。 </summary>
        [Test]
        public void LogException_Null_LogsDiagnosticWithoutThrowing()
        {
            LogAssert.Expect(LogType.Error, new Regex(@"nullの例外がLogExceptionへ渡されました。"));

            Assert.That(() => SymphonyDebugLogger.LogException(null), Throws.Nothing);

            Assert.That(_received, Has.Count.EqualTo(1));
            Assert.That(_received[0].kind, Is.EqualTo(LogKindEnum.Error));
        }

        /// <summary> 要約は型の完全名と理由を1行で含み、スタックトレースを含めない。 </summary>
        [Test]
        public void DescribeException_Exception_ContainsTypeAndMessageOnOneLine()
        {
            string described = SymphonyDebugLogger.DescribeException(new ArgumentNullException("引数"));

            Assert.That(described, Does.StartWith("System.ArgumentNullException: "));
            Assert.That(described, Does.Not.Contain("\n"));
        }

        /// <summary> Core層の例外は、注入された中継先へ渡る。 </summary>
        [Test]
        public void CoreLogRelay_HandlerInjected_RoutesExceptionToHandler()
        {
            List<Exception> routed = new();
            CoreLogRelay.ExceptionHandler = routed.Add;

            InvalidOperationException exception = new("Coreからの例外");
            CoreLogRelay.LogException(exception);

            Assert.That(routed, Is.EqualTo(new[] { exception }));
        }

        /// <summary> 中継先が未注入でも、Unity標準の出力へ落ちて失われない。 </summary>
        [Test]
        public void CoreLogRelay_NoHandler_FallsBackToUnityDebug()
        {
            CoreLogRelay.ExceptionHandler = null;

            LogAssert.Expect(LogType.Exception, new Regex(@"InvalidOperationException: 未注入"));

            Assert.That(() => CoreLogRelay.LogException(new InvalidOperationException("未注入")), Throws.Nothing);
        }

        /// <summary> 解除すると、注入前と同じUnity標準の出力へ戻る。 </summary>
        [Test]
        public void CoreLogRelay_Reset_ClearsInjectedHandler()
        {
            CoreLogRelay.ExceptionHandler = _ => { };

            CoreLogRelay.Reset();

            Assert.That(CoreLogRelay.ExceptionHandler, Is.Null);
        }

        /// <summary> 通知の内容を記録する。 </summary>
        /// <param name="text"> 通知されたテキスト。 </param>
        /// <param name="kind"> 通知された重要度。 </param>
        private void Receive(string text, LogKindEnum kind) => _received.Add((text, kind));
    }
}
