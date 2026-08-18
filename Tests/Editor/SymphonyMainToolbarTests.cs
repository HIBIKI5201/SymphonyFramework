#if UNITY_6000_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

using SymphonyFrameWork.Config;
using SymphonyFrameWork.Core;
using SymphonyFrameWork.Editor;

using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace SymphonyFrameWork.Tests
{
    /// <summary> Symphony Frameworkのメインツールバーメニューを検証する。 </summary>
    public sealed class SymphonyMainToolbarTests
    {
        /// <summary> プルダウンは右側の固定パスへ登録される。 </summary>
        [Test]
        public void CreateToolbarMenu_HasMainToolbarRegistration_UsesRightDock()
        {
            MethodInfo factory = typeof(SymphonyMainToolbar).GetMethod(
                nameof(SymphonyMainToolbar.CreateToolbarMenu),
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(factory, Is.Not.Null);
            MainToolbarElementAttribute attribute =
                factory.GetCustomAttribute<MainToolbarElementAttribute>();

            Assert.That(attribute, Is.Not.Null);
            Assert.That(
                attribute.path,
                Is.EqualTo(SymphonyMainToolbar.ToolbarMenuElementPath));
            Assert.That(
                attribute.defaultDockPosition,
                Is.EqualTo(MainToolbarDockPosition.Right));
        }

        /// <summary> プルダウンはFramework名と現在テーマ用アイコンを表示する。 </summary>
        [Test]
        public void CreateToolbarMenu_ReturnsDropdownWithFrameworkLabelAndIcon()
        {
            MainToolbarDropdown dropdown =
                SymphonyMainToolbar.CreateToolbarMenu();
            Texture2D expectedIcon = SymphonyMainToolbar.LoadToolbarIcon(
                EditorGUIUtility.isProSkin);

            Assert.That(dropdown.content.text, Is.EqualTo("Symphony Framework"));
            Assert.That(dropdown.content.image, Is.SameAs(expectedIcon));
        }

        /// <summary> 登録済み項目からScene Initを発見できる。 </summary>
        [Test]
        public void CreateItems_RegisteredSceneItem_DiscoversSceneInitializationItem()
        {
            IReadOnlyList<ISymphonyToolbarMenuItem> items =
                SymphonyToolbarMenuCatalog.CreateItems();

            Assert.That(
                items.Any(item => item is SceneInitializationToolbarMenuItem),
                Is.True);
        }

        /// <summary> 項目は優先度、同値の場合はパスのordinal順に並ぶ。 </summary>
        [Test]
        public void OrderItems_MixedPriority_SortsByPriorityThenPath()
        {
            ISymphonyToolbarMenuItem[] items =
            {
                new TestToolbarMenuItem("Zulu", 20),
                new TestToolbarMenuItem("Beta", 10),
                new TestToolbarMenuItem("Alpha", 10),
            };

            IReadOnlyList<ISymphonyToolbarMenuItem> ordered =
                SymphonyToolbarMenuCatalog.OrderItems(items);

            Assert.That(
                ordered.Select(item => item.Path),
                Is.EqualTo(new[] { "Alpha", "Beta", "Zulu" }));
        }

        /// <summary> 有効なConfigはチェック付きかつ操作可能になる。 </summary>
        [Test]
        public void SceneInitializationItem_ConfigEnabled_IsCheckedAndEnabled()
        {
            SceneLoadConfig config = ScriptableObject.CreateInstance<SceneLoadConfig>();

            try
            {
                SceneInitializationToolbarMenuItem.ApplySceneInitializationValue(
                    config,
                    true);
                SceneInitializationToolbarMenuItem item = new(config, () => { });

                Assert.That(item.IsChecked, Is.True);
                Assert.That(item.IsEnabled, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        /// <summary> Configが無い項目は未チェックかつ操作不能になる。 </summary>
        [Test]
        public void SceneInitializationItem_NullConfig_IsUncheckedAndDisabled()
        {
            SceneInitializationToolbarMenuItem item = new(null, () => { });

            Assert.That(item.IsChecked, Is.False);
            Assert.That(item.IsEnabled, Is.False);
        }

        /// <summary> 操作すると無効なConfigを有効にし、保存処理を1回呼ぶ。 </summary>
        [Test]
        public void Execute_SceneInitializationDisabled_EnablesSerializedConfig()
        {
            SceneLoadConfig config = ScriptableObject.CreateInstance<SceneLoadConfig>();
            int saveCount = 0;

            try
            {
                SceneInitializationToolbarMenuItem item = new(
                    config,
                    () => saveCount++);

                item.Execute();

                SerializedObject serializedConfig = new(config);
                serializedConfig.Update();
                Assert.That(config.IsResetAndLoadOnPlay, Is.True);
                Assert.That(
                    serializedConfig.FindProperty(
                        "_isResetAndLoadOnPlay").boolValue,
                    Is.True);
                Assert.That(saveCount, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        /// <summary> 明暗両テーマ用のアイコンをTextureとして読み込める。 </summary>
        [Test]
        public void ToolbarIcons_PackageAssets_ExistAndAreReadable()
        {
            Assert.That(SymphonyMainToolbar.LoadToolbarIcon(false), Is.Not.Null);
            Assert.That(SymphonyMainToolbar.LoadToolbarIcon(true), Is.Not.Null);
        }

        /// <summary> Lucideの再配布に必要な著作権表示と許諾文を同梱する。 </summary>
        [Test]
        public void ThirdPartyNotices_LucideLicense_ContainsRequiredNotices()
        {
            string path = EditorSymphonyConstant.FRAMEWORK_PATH
                + "/Third Party Notices.md";
            TextAsset notices = AssetDatabase.LoadAssetAtPath<TextAsset>(path);

            Assert.That(notices, Is.Not.Null);
            Assert.That(notices.text, Does.Contain("Lucide Icons and Contributors"));
            Assert.That(notices.text, Does.Contain("ISC License"));
            Assert.That(notices.text, Does.Contain("The MIT License (MIT)"));
        }

        /// <summary> テスト用の未登録メニュー項目。 </summary>
        private sealed class TestToolbarMenuItem : ISymphonyToolbarMenuItem
        {
            /// <summary> 指定したパスと優先度を保持する。 </summary>
            internal TestToolbarMenuItem(string path, int priority)
            {
                Path = path;
                Priority = priority;
            }

            /// <summary> テスト用の表示パス。 </summary>
            public string Path { get; }

            /// <summary> テスト用の表示優先度。 </summary>
            public int Priority { get; }

            /// <summary> テストではチェックを表示しない。 </summary>
            public bool IsChecked => false;

            /// <summary> テストでは常に操作可能。 </summary>
            public bool IsEnabled => true;

            /// <summary> テストでは操作しない。 </summary>
            public void Execute()
            {
            }
        }
    }
}
#endif
