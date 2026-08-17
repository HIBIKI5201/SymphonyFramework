using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

using SymphonyFrameWork.Config;
using SymphonyFrameWork.Core;
using SymphonyFrameWork.Debugger.HUD;

using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace SymphonyFrameWork.Tests
{
    /// <summary>
    ///     Debug HUDのDevelopment Build限定化、遅延表示、登録内容の維持を検証する。
    /// </summary>
    public sealed class SymphonyDebugHUDTests
    {
        private RecordingSystemObjectFactory _factory;

        /// <summary> 各テスト用のGameObject生成記録を初期化する。 </summary>
        [SetUp]
        public void SetUp()
        {
            SymphonyDebugHUD.ResetRuntimeState();
            _factory = new RecordingSystemObjectFactory();
        }

        /// <summary> static状態とテスト用GameObjectを残さない。 </summary>
        [TearDown]
        public void TearDown()
        {
            SymphonyDebugHUD.ResetRuntimeState();
            _factory.Dispose();
        }

        /// <summary> 通常ビルド条件ではHUD用Componentを生成しない。 </summary>
        [Test]
        public void Initialize_NonDevelopmentBuild_DoesNotCreateComponents()
        {
            using InputAction action = DebugHUDConfig.CreateDefaultToggleAction();

            SymphonyDebugHUD.Initialize(_factory, action, isDebugBuild: false);

            Assert.That(_factory.ComponentCount, Is.Zero);
            Assert.That(SymphonyDebugHUD.HasShortcutListener, Is.False);
            Assert.That(SymphonyDebugHUD.IsVisible, Is.False);
        }

        /// <summary> 通常ビルド条件では追加テキストを保持しない。 </summary>
        [Test]
        public void AddText_NonDevelopmentBuild_DoesNotRetainCallback()
        {
            using InputAction action = DebugHUDConfig.CreateDefaultToggleAction();
            SymphonyDebugHUD.Initialize(_factory, action, isDebugBuild: false);

            SymphonyDebugHUD.AddText(() => "release");
            SymphonyDebugHUD.Show();

            Assert.That(SymphonyDebugHUD.RegisteredTextCount, Is.Zero);
            Assert.That(_factory.ComponentCount, Is.Zero);
        }

        /// <summary> Development Build条件の初期化ではShortcut Listenerだけを生成する。 </summary>
        [Test]
        public void Initialize_DevelopmentBuild_CreatesShortcutListenerOnly()
        {
            InitializeDevelopmentBuild();

            Assert.That(_factory.Count<SymphonyHUDShortcutListener>(), Is.EqualTo(1));
            Assert.That(_factory.Count<SymphonyHUDDrawer>(), Is.Zero);
            Assert.That(SymphonyDebugHUD.HasShortcutListener, Is.True);
            Assert.That(SymphonyDebugHUD.IsVisible, Is.False);
        }

        /// <summary> 非表示中のAddTextはDrawerを自動生成しない。 </summary>
        [Test]
        public void AddText_Hidden_DoesNotCreateDrawer()
        {
            InitializeDevelopmentBuild();

            SymphonyDebugHUD.AddText(() => "hidden");

            Assert.That(SymphonyDebugHUD.RegisteredTextCount, Is.EqualTo(1));
            Assert.That(_factory.Count<SymphonyHUDDrawer>(), Is.Zero);
            Assert.That(SymphonyDebugHUD.IsVisible, Is.False);
        }

        /// <summary> Show時に非表示中の登録内容を生成したDrawerへ反映する。 </summary>
        [Test]
        public void Show_AfterAddText_CreatesDrawerWithRegisteredText()
        {
            InitializeDevelopmentBuild();
            SymphonyDebugHUD.AddText(() => "registered");

            SymphonyDebugHUD.Show();

            SymphonyHUDDrawer drawer = _factory.Last<SymphonyHUDDrawer>();
            Assert.That(drawer.RegisteredTextCount, Is.EqualTo(1));
            Assert.That(SymphonyDebugHUD.IsVisible, Is.True);
        }

        /// <summary> Hide後に再表示しても継続テキストを維持する。 </summary>
        [Test]
        public void Hide_ThenShow_PreservesRegisteredText()
        {
            InitializeDevelopmentBuild();
            SymphonyDebugHUD.AddText(() => "persistent");
            SymphonyDebugHUD.Show();

            SymphonyDebugHUD.Hide();
            SymphonyDebugHUD.Show();

            Assert.That(_factory.Count<SymphonyHUDDrawer>(), Is.EqualTo(2));
            Assert.That(_factory.Last<SymphonyHUDDrawer>().RegisteredTextCount, Is.EqualTo(1));
        }

        /// <summary> Toggleを2回呼ぶと表示から非表示へ戻る。 </summary>
        [Test]
        public void Toggle_VisibleState_SwitchesDrawer()
        {
            InitializeDevelopmentBuild();

            SymphonyDebugHUD.Toggle();
            Assert.That(SymphonyDebugHUD.IsVisible, Is.True);

            SymphonyDebugHUD.Toggle();
            Assert.That(SymphonyDebugHUD.IsVisible, Is.False);
        }

        /// <summary> ResetはListener、Drawer、登録内容をすべて解放する。 </summary>
        [Test]
        public void ResetRuntimeState_AfterInitialization_ReleasesOwnedState()
        {
            InitializeDevelopmentBuild();
            SymphonyDebugHUD.AddText(() => "reset");
            SymphonyDebugHUD.Show();

            SymphonyDebugHUD.ResetRuntimeState();

            Assert.That(SymphonyDebugHUD.HasShortcutListener, Is.False);
            Assert.That(SymphonyDebugHUD.IsVisible, Is.False);
            Assert.That(SymphonyDebugHUD.RegisteredTextCount, Is.Zero);
            Assert.That(_factory.AliveObjectCount, Is.Zero);
        }

        /// <summary> 既定ActionでDevelopment Build条件を初期化する。 </summary>
        private void InitializeDevelopmentBuild()
        {
            using InputAction action = DebugHUDConfig.CreateDefaultToggleAction();
            SymphonyDebugHUD.Initialize(_factory, action, isDebugBuild: true);
        }

        /// <summary> 生成したComponentとGameObjectを記録するテスト用Factory。 </summary>
        private sealed class RecordingSystemObjectFactory : ISystemObjectFactory, IDisposable
        {
            private readonly List<Component> _components = new();
            private readonly List<GameObject> _objects = new();

            /// <summary> 生成したComponentの総数。 </summary>
            internal int ComponentCount => _components.Count;

            /// <summary> 現在生存しているGameObjectの数。 </summary>
            internal int AliveObjectCount => _objects.Count(gameObject => gameObject);

            /// <inheritdoc />
            public GameObject CreateObject(string name)
            {
                GameObject gameObject = new(name);
                _objects.Add(gameObject);
                return gameObject;
            }

            /// <inheritdoc />
            public T CreateComponent<T>(string name) where T : Component
            {
                T component = CreateObject(name).AddComponent<T>();
                _components.Add(component);
                return component;
            }

            /// <summary> 指定Component型の生成数を返す。 </summary>
            internal int Count<T>() where T : Component => _components.Count(component => component is T);

            /// <summary> 最後に生成した指定型Componentを返す。 </summary>
            internal T Last<T>() where T : Component => _components.OfType<T>().Last();

            /// <summary> 残っているテスト用GameObjectを破棄する。 </summary>
            public void Dispose()
            {
                foreach (GameObject gameObject in _objects)
                {
                    if (gameObject) { Object.DestroyImmediate(gameObject); }
                }

                _components.Clear();
                _objects.Clear();
            }
        }
    }
}
