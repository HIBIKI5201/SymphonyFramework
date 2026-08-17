using NUnit.Framework;

using SymphonyFrameWork.Config;
using SymphonyFrameWork.Editor.SettingProvider;

using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Tests
{
    /// <summary> SymphonySettingProviderのDebug HUD設定編集状態を検証する。 </summary>
    public sealed class SymphonySettingProviderTests
    {
        private DebugHUDConfig _config;

        /// <summary> 各テスト用ConfigとSerializedObjectキャッシュを初期化する。 </summary>
        [SetUp]
        public void SetUp()
        {
            SymphonySettingProvider.ReleaseDebugHUDSerializedObject();
            _config = ScriptableObject.CreateInstance<DebugHUDConfig>();
        }

        /// <summary> SerializedObjectとConfigを解放する。 </summary>
        [TearDown]
        public void TearDown()
        {
            SymphonySettingProvider.ReleaseDebugHUDSerializedObject();
            _config.ToggleAction?.Dispose();
            Object.DestroyImmediate(_config);
        }

        /// <summary> 同じConfigの表示中はPropertyDrawer用SerializedObjectを再利用する。 </summary>
        [Test]
        public void GetDebugHUDSerializedObject_SameConfig_ReusesInstance()
        {
            SerializedObject first = SymphonySettingProvider.GetDebugHUDSerializedObject(_config);
            SerializedObject second = SymphonySettingProvider.GetDebugHUDSerializedObject(_config);

            Assert.That(second, Is.SameAs(first));
        }

        /// <summary> 設定画面の終了後は次回表示用SerializedObjectを作り直す。 </summary>
        [Test]
        public void ReleaseDebugHUDSerializedObject_NextRequestCreatesNewInstance()
        {
            SerializedObject first = SymphonySettingProvider.GetDebugHUDSerializedObject(_config);

            SymphonySettingProvider.ReleaseDebugHUDSerializedObject();
            SerializedObject second = SymphonySettingProvider.GetDebugHUDSerializedObject(_config);

            Assert.That(second, Is.Not.SameAs(first));
        }
    }
}
