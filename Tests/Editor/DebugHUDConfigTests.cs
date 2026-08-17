using System;
using System.Linq;

using NUnit.Framework;

using SymphonyFrameWork.Config;

using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SymphonyFrameWork.Tests
{
    /// <summary> DebugHUDConfigの既定Input Actionを検証する。 </summary>
    public sealed class DebugHUDConfigTests
    {
        /// <summary> 新規ConfigはShift + D + PのTwo Modifier Compositeを持つ。 </summary>
        [Test]
        public void DefaultToggleAction_NewConfig_UsesShiftDPTwoModifierComposite()
        {
            DebugHUDConfig config = ScriptableObject.CreateInstance<DebugHUDConfig>();

            try
            {
                InputAction action = config.ToggleAction;
                InputBinding composite = action.bindings.Single(binding => binding.isComposite);

                Assert.That(composite.path, Is.EqualTo("ButtonWithTwoModifiers"));
                AssertPartPath(action, "Modifier1", "<Keyboard>/shift");
                AssertPartPath(action, "Modifier2", "<Keyboard>/d");
                AssertPartPath(action, "Button", "<Keyboard>/p");
            }
            finally
            {
                config.ToggleAction?.Dispose();
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        /// <summary> Runtime読取後のSerializedProperty変更を再構築してBindingとIDを維持する。 </summary>
        [Test]
        public void RebuildToggleAction_AfterSerializedBindingChange_PreservesBindingsAndId()
        {
            DebugHUDConfig config = ScriptableObject.CreateInstance<DebugHUDConfig>();

            try
            {
                InputAction originalAction = config.ToggleAction;
                Guid originalId = originalAction.id;

                // Runtime Listenerと同じくbindingsを読み、singleton用ActionMapを生成済みにする。
                Assert.That(originalAction.bindings.Count, Is.EqualTo(4));

                SerializedObject serializedObject = new(config);
                SerializedProperty bindings = serializedObject
                    .FindProperty("_toggleAction")
                    .FindPropertyRelative("m_SingletonActionBindings");
                bindings.GetArrayElementAtIndex(3)
                    .FindPropertyRelative("m_Path")
                    .stringValue = "<Gamepad>/buttonSouth";
                serializedObject.ApplyModifiedPropertiesWithoutUndo();

                config.RebuildToggleActionSerializationState();

                Assert.That(config.ToggleAction, Is.Not.SameAs(originalAction));
                Assert.That(config.ToggleAction.id, Is.EqualTo(originalId));
                Assert.That(config.ToggleAction.bindings.Count, Is.EqualTo(4));
                Assert.That(config.ToggleAction.bindings[3].path, Is.EqualTo("<Gamepad>/buttonSouth"));
            }
            finally
            {
                config.ToggleAction?.Dispose();
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        /// <summary> 再構築後も既定Compositeのpartを維持する。 </summary>
        [Test]
        public void RebuildToggleAction_DefaultComposite_PreservesCompositeParts()
        {
            DebugHUDConfig config = ScriptableObject.CreateInstance<DebugHUDConfig>();

            try
            {
                config.RebuildToggleActionSerializationState();

                InputAction action = config.ToggleAction;
                Assert.That(action.bindings.Single(binding => binding.isComposite).path,
                    Is.EqualTo("ButtonWithTwoModifiers"));
                AssertPartPath(action, "Modifier1", "<Keyboard>/shift");
                AssertPartPath(action, "Modifier2", "<Keyboard>/d");
                AssertPartPath(action, "Button", "<Keyboard>/p");
            }
            finally
            {
                config.ToggleAction?.Dispose();
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        /// <summary> Compositeの指定partが期待するControl pathを持つことを確認する。 </summary>
        private static void AssertPartPath(InputAction action, string partName, string expectedPath)
        {
            InputBinding binding = action.bindings.Single(candidate =>
                candidate.isPartOfComposite &&
                string.Equals(candidate.name, partName, StringComparison.OrdinalIgnoreCase));

            Assert.That(binding.path, Is.EqualTo(expectedPath));
        }
    }
}
