using System;
using System.Linq;

using NUnit.Framework;

using SymphonyFrameWork.Config;

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
