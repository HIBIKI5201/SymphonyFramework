using System;
using System.Collections.Generic;
using System.Threading;

using SymphonyFrameWork.Core;
using SymphonyFrameWork.Exceptions;
using SymphonyFrameWork.System;

using UnityEngine;
using UnityEngine.InputSystem;

namespace SymphonyFrameWork.Debugger.HUD
{
    /// <summary>
    ///     画面上にデバッグ用のHUDを表示する。
    /// </summary>
    public static class SymphonyDebugHUD
    {
        #region 外部向けAPI

        /// <summary>
        ///     HUDを表示する。
        /// </summary>
        public static void Show()
        {
            EnsureInitialized();
            if (!_isAvailable) { return; }

            // 明示操作またはShortcutが届いた時点でだけ、遅延Drawerを生成する。
            _ = _debugHUD.Value;
        }

        /// <summary>
        ///     HUDを非表示にする。
        /// </summary>
        public static void Hide()
        {
            EnsureInitialized();
            if (!_isAvailable) { return; }

            // 登録内容はFacadeへ残し、描画用GameObjectだけを破棄する。
            _debugHUD.Destroy();
        }

        /// <summary>
        ///     HUDへ追加のテキストを登録する。
        /// </summary>
        /// <param name="textFunc"> 毎フレーム表示文字列を返す処理。 </param>
        public static void AddText(Func<string> textFunc)
        {
            EnsureInitialized();
            if (!_isAvailable) { return; }

            if (textFunc == null) { throw new ArgumentNullException(nameof(textFunc)); }

            RegisterText(textFunc);
        }

        /// <summary>
        ///     HUDから追加のテキストを解除する。
        /// </summary>
        /// <param name="textFunc"> 解除する文字列生成処理。 </param>
        public static void RemoveText(Func<string> textFunc)
        {
            EnsureInitialized();
            if (!_isAvailable) { return; }

            if (textFunc == null) { throw new ArgumentNullException(nameof(textFunc)); }

            UnregisterText(textFunc);
        }

        /// <summary>
        ///     HUDへ追加のテキストを一時表示する。
        /// </summary>
        /// <param name="text"> 一時表示する文字列。 </param>
        /// <param name="duration"> 表示を継続する秒数。 </param>
        /// <param name="color"> 文字へ適用する色。既定値の場合は色指定なし。 </param>
        /// <param name="token"> 表示待機を中断するためのトークン。 </param>
        public static async Awaitable AddText(
            string text,
            float duration = 3,
            Color color = default,
            CancellationToken token = default)
        {
            EnsureInitialized();
            if (!_isAvailable) { return; }

            if (text == null) { throw new ArgumentNullException(nameof(text)); }

            if (duration < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(duration),
                    duration,
                    "表示時間は0秒以上で指定してください。");
            }

            if (color != default)
            {
                string colorHex = ColorUtility.ToHtmlStringRGB(color);
                text = $"<color=#{colorHex}>{text}</color>";
            }

            Func<string> textFunc = () => text;

            // 待機中だけ表示対象に含める。登録だけではHUDを自動表示しない。
            RegisterText(textFunc);

            try
            {
                await Awaitable.WaitForSecondsAsync(duration, token);
            }
            finally
            {
                // Shutdownと競合しても、解除処理から遅延Drawerを再生成しない。
                UnregisterText(textFunc);
            }
        }

        #endregion

        #region 内部処理

        private static readonly List<Func<string>> _extraTexts = new();

        private static SymphonyLazyObject<SymphonyHUDDrawer> _debugHUD;
        private static bool _isAvailable;
        private static bool _isInitialized;
        private static SymphonyHUDShortcutListener _shortcutListener;
        private static ISystemObjectFactory _systemObjectFactory;

        /// <summary> 現在HUDが表示されている場合はtrue。 </summary>
        internal static bool IsVisible => _debugHUD != null && _debugHUD.IsAlive;

        /// <summary> 現在保持している追加テキストの数。 </summary>
        internal static int RegisteredTextCount => _extraTexts.Count;

        /// <summary> Shortcut Listenerが生成済みの場合はtrue。 </summary>
        internal static bool HasShortcutListener => _shortcutListener;

        /// <summary>
        ///     SymphonyのシステムオブジェクトとしてHUD描画コンポーネントを生成する。
        /// </summary>
        /// <returns> 生成したHUD描画コンポーネント。 </returns>
        private static SymphonyHUDDrawer CreateDebugHUD()
        {
            if (_systemObjectFactory == null) { throw new SymphonyNotInitializedException(typeof(SymphonyDebugHUD)); }

            SymphonyHUDDrawer drawer =
                _systemObjectFactory.CreateComponent<SymphonyHUDDrawer>(nameof(SymphonyHUDDrawer));

            // 非表示中に登録された内容も、再表示時のDrawerへ同じ順序で復元する。
            foreach (Func<string> textFunc in _extraTexts) { drawer.Add(textFunc); }

            return drawer;
        }

        /// <summary>
        ///     Debug HUDが初期化済みか検証する。
        /// </summary>
        private static void EnsureInitialized()
        {
            if (!_isInitialized) { throw new SymphonyNotInitializedException(typeof(SymphonyDebugHUD)); }
        }

        /// <summary>
        ///     既存HUDを破棄し、入力監視と遅延生成状態を初期化する。
        /// </summary>
        /// <param name="systemObjectFactory"> HUD用GameObjectの生成契約。 </param>
        /// <param name="toggleAction"> HUD表示を切り替えるInput Action。 </param>
        /// <param name="isDebugBuild"> EditorまたはDevelopment Buildの場合はtrue。 </param>
        internal static void Initialize(
            ISystemObjectFactory systemObjectFactory,
            InputAction toggleAction,
            bool isDebugBuild)
        {
            ResetRuntimeState();

            if (isDebugBuild && systemObjectFactory == null)
            {
                throw new ArgumentNullException(nameof(systemObjectFactory));
            }

            _isInitialized = true;
            _isAvailable = isDebugBuild;
            if (!_isAvailable) { return; }

            try
            {
                _systemObjectFactory = systemObjectFactory;
                _debugHUD = new SymphonyLazyObject<SymphonyHUDDrawer>(
                    CreateDebugHUD,
                    drawer => DestroyGameObject(drawer.gameObject));

                // Shortcutの監視だけは常駐させ、Drawerは表示要求まで生成しない。
                _shortcutListener = _systemObjectFactory.CreateComponent<SymphonyHUDShortcutListener>(
                    nameof(SymphonyHUDShortcutListener));
                _shortcutListener.Configure(toggleAction, Toggle);
            }
            catch
            {
                ResetRuntimeState();
                throw;
            }
        }

        /// <summary> 現在の表示状態を反転する。 </summary>
        internal static void Toggle()
        {
            EnsureInitialized();
            if (!_isAvailable) { return; }

            if (IsVisible) { Hide(); }
            else { Show(); }
        }

        /// <summary>
        ///     生成済みHUD、入力監視、登録内容を解放する。
        /// </summary>
        internal static void ResetRuntimeState()
        {
            // Input Actionを先に停止し、遅延破棄の完了前に古いShortcutが発火することを防ぐ。
            if (_shortcutListener)
            {
                _shortcutListener.Shutdown();
                DestroyGameObject(_shortcutListener.gameObject);
            }

            _debugHUD?.Destroy();
            _extraTexts.Clear();

            _debugHUD = null;
            _isAvailable = false;
            _isInitialized = false;
            _shortcutListener = null;
            _systemObjectFactory = null;
        }

        /// <summary> 追加テキストをFacadeと生成済みDrawerへ登録する。 </summary>
        /// <param name="textFunc"> 毎フレーム表示文字列を返す処理。 </param>
        private static void RegisterText(Func<string> textFunc)
        {
            _extraTexts.Add(textFunc);
            if (_debugHUD.TryGetValue(out SymphonyHUDDrawer drawer)) { drawer.Add(textFunc); }
        }

        /// <summary> 追加テキストをFacadeと生成済みDrawerから解除する。 </summary>
        /// <param name="textFunc"> 解除する文字列生成処理。 </param>
        private static void UnregisterText(Func<string> textFunc)
        {
            _extraTexts.Remove(textFunc);
            if (_debugHUD != null && _debugHUD.TryGetValue(out SymphonyHUDDrawer drawer))
            {
                drawer.Remove(textFunc);
            }
        }

        /// <summary> 実行環境に適した方法でGameObjectを破棄する。 </summary>
        /// <param name="target"> 破棄するGameObject。 </param>
        private static void DestroyGameObject(GameObject target)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(target);
                return;
            }

            UnityEngine.Object.DestroyImmediate(target);
        }

        #endregion
    }
}
