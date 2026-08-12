using System;
using System.Threading;
using System.Threading.Tasks;

using SymphonyFrameWork.Core;
using SymphonyFrameWork.Exceptions;
using SymphonyFrameWork.System;

using UnityEngine;

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
            // 遅延生成の契約が準備済みであることを確認してから、値へのアクセスでHUDを生成する。
            EnsureInitialized();
            _ = _debugHUD.Value;
        }

        /// <summary>
        ///     HUDを非表示にする。
        /// </summary>
        public static void Hide()
        {
            // 未初期化状態での呼び出しを隠さず、生成済みの場合だけ遅延オブジェクトを破棄する。
            EnsureInitialized();
            _debugHUD.Destroy();
        }

        /// <summary>
        ///     HUDへ追加のテキストを登録する。
        /// </summary>
        /// <param name="textFunc"> 毎フレーム表示文字列を返す処理。 </param>
        public static void AddText(Func<string> textFunc)
        {
            EnsureInitialized();

            // 毎フレーム呼び出す処理として保持するため、nullは登録前に拒否する。
            if (textFunc == null) { throw new ArgumentNullException(nameof(textFunc)); }

            _debugHUD.Value.Add(textFunc);
        }

        /// <summary>
        ///     HUDから追加のテキストを解除する。
        /// </summary>
        /// <param name="textFunc"> 解除する文字列生成処理。 </param>
        public static void RemoveText(Func<string> textFunc)
        {
            EnsureInitialized();

            // 登録一覧の照合に使えないnullは、解除処理へ渡さず呼び出し誤りとして扱う。
            if (textFunc == null) { throw new ArgumentNullException(nameof(textFunc)); }

            _debugHUD.Value.Remove(textFunc);
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

            // 表示処理が成立しないnull文字列は、HUDへ登録する前に拒否する。
            if (text == null) { throw new ArgumentNullException(nameof(text)); }

            // 待機APIが扱えない負の表示時間は、登録前に拒否して表示の残留を防ぐ。
            if (duration < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(duration),
                    duration,
                    "表示時間は0秒以上で指定してください。");
            }

            // 既定色では既存のHUDスタイルを使い、明示色だけRich Textタグへ変換する。
            if (color != default)
            {
                string colorHex = ColorUtility.ToHtmlStringRGB(color);
                text = $"<color=#{colorHex}>{text}</color>";
            }

            Func<string> textFunc = () => text;

            // 待機中だけ表示対象に含め、終了経路にかかわらずfinallyで必ず解除する。
            _debugHUD.Value.Add(textFunc);

            try
            {
                await Awaitable.WaitForSecondsAsync(duration, token);
            }
            finally
            {
                _debugHUD.Value.Remove(textFunc);
            }
        }

        #endregion

        #region 内部処理

        private static SymphonyLazyObject<SymphonyHUDDrawer> _debugHUD;
        private static ISystemObjectFactory _systemObjectFactory;

        /// <summary>
        ///     SymphonyのシステムオブジェクトとしてHUD描画コンポーネントを生成する。
        /// </summary>
        /// <returns> 生成したHUD描画コンポーネント。 </returns>
        private static SymphonyHUDDrawer CreateDebugHUD()
        {
            // Orchestratorによる生成契約の注入前は、通常のGameObjectとして生成しない。
            if (_systemObjectFactory == null) { throw new SymphonyNotInitializedException(typeof(SymphonyDebugHUD)); }

            return _systemObjectFactory.CreateComponent<SymphonyHUDDrawer>(nameof(SymphonyHUDDrawer));
        }

        /// <summary>
        ///     Debug HUDが利用可能な状態か検証する。
        /// </summary>
        private static void EnsureInitialized()
        {
            // Orchestratorから遅延生成契約が注入されていない状態では、全ての公開操作を拒否する。
            if (_debugHUD == null) { throw new SymphonyNotInitializedException(typeof(SymphonyDebugHUD)); }
        }

        /// <summary>
        ///     既存HUDを破棄し、遅延生成状態を初期化する。
        /// </summary>
        /// <param name="systemObjectFactory"> HUD描画用GameObjectの生成契約。 </param>
        internal static void Initialize(ISystemObjectFactory systemObjectFactory)
        {
            // Domain Reload無効環境でも前回のHUDを残さないよう、再構築前に状態を破棄する。
            ResetRuntimeState();
            _systemObjectFactory = systemObjectFactory;
            _debugHUD = new SymphonyLazyObject<SymphonyHUDDrawer>(
                CreateDebugHUD,
                drawer => UnityEngine.Object.Destroy(drawer.gameObject));
        }

        /// <summary>
        ///     生成済みHUDと遅延生成状態を解放する。
        /// </summary>
        internal static void ResetRuntimeState()
        {
            // 未生成時も同じ経路で破棄し、次回初期化が古いfactoryを再利用しない状態へ戻す。
            _debugHUD?.Destroy();
            _debugHUD = null;
            _systemObjectFactory = null;
        }

        #endregion
    }
}
