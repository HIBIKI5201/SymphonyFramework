#if UNITY_6000_3_OR_NEWER
using System.Reflection;

using NUnit.Framework;

using SymphonyFrameWork.Config;
using SymphonyFrameWork.Editor;

using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace SymphonyFrameWork.Tests
{
    /// <summary> Symphony Frameworkのメインツールバー登録と設定変更を検証する。 </summary>
    public sealed class SymphonyMainToolbarTests
    {
        /// <summary> 初期シーントグルはSymphony Frameworkグループの右側要素として登録される。 </summary>
        [Test]
        public void CreateSceneInitializationToggle_HasMainToolbarRegistration_UsesSymphonyGroup()
        {
            MethodInfo factory = typeof(SymphonyMainToolbar).GetMethod(
                nameof(SymphonyMainToolbar.CreateSceneInitializationToggle),
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(factory, Is.Not.Null);
            MainToolbarElementAttribute attribute =
                factory.GetCustomAttribute<MainToolbarElementAttribute>();

            Assert.That(attribute, Is.Not.Null);
            Assert.That(
                attribute.path,
                Is.EqualTo(SymphonyMainToolbar.SceneInitializationElementPath));
            Assert.That(
                attribute.defaultDockPosition,
                Is.EqualTo(MainToolbarDockPosition.Right));
        }

        /// <summary> Configへの適用は公開getterとシリアライズ済みフィールドを同時に更新する。 </summary>
        [Test]
        public void ApplySceneInitializationValue_FalseToTrue_UpdatesSerializedConfig()
        {
            SceneLoadConfig config = ScriptableObject.CreateInstance<SceneLoadConfig>();

            try
            {
                bool applied = SymphonyMainToolbar.ApplySceneInitializationValue(
                    config,
                    true);
                SerializedObject serializedConfig = new(config);
                serializedConfig.Update();

                Assert.That(applied, Is.True);
                Assert.That(config.IsResetAndLoadOnPlay, Is.True);
                Assert.That(
                    serializedConfig.FindProperty("_isResetAndLoadOnPlay").boolValue,
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        /// <summary> Configが未生成でも設定変更helperは例外を出さず失敗を返す。 </summary>
        [Test]
        public void ApplySceneInitializationValue_NullConfig_ReturnsFalse()
        {
            bool applied = SymphonyMainToolbar.ApplySceneInitializationValue(
                null,
                true);

            Assert.That(applied, Is.False);
        }
    }
}
#endif
