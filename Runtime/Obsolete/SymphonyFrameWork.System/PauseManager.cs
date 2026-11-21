using System.Collections;
using System.Threading;

using SymphonyFramework.Pause;

using UnityEngine;

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     6.x の <c>PauseManager</c> を <see cref="Fermata" /> へ転送する。
    /// </summary>
    /// <remarks>
    ///     7.0.0 の移行期間だけ残す。8.0.0 で削除する。
    ///     入れ子の <see cref="IPausable" /> は、6.x のポーズカテゴリーの生成物をコンパイルさせ、
    ///     Editor の起動時の移行（<c>PauseCategoryGenerator.MigrateLegacyAssembly</c>）を走らせるためにも要る。
    /// </remarks>
    [global::System.Obsolete("PauseManager は Fermata（名前空間 SymphonyFramework.Pause）へ改名しました。Fermata を使用してください。8.0.0 で削除します。")]
    public static class PauseManager
    {
        #region 外部向けAPI

        /// <summary>
        ///     6.x のポーズ通知の契約とカテゴリーの基底。新しい <see cref="SymphonyFramework.Pause.IPausable" /> を継承する。
        /// </summary>
        public interface IPausable : SymphonyFramework.Pause.IPausable
        {
            #region 外部向けAPI

            /// <summary> 旧 <c>RegisterPauseManager</c>。<see cref="Fermata.Register" /> へ転送する。 </summary>
            /// <param name="pausable"> ポーズ通知を受け取る対象。 </param>
            static void RegisterPauseManager(IPausable pausable) => Fermata.Register(pausable);

            /// <summary> 旧 <c>UnregisterPauseManager</c>。<see cref="Fermata.Unregister" /> へ転送する。 </summary>
            /// <param name="pausable"> ポーズ通知を解除する対象。 </param>
            static void UnregisterPauseManager(IPausable pausable) => Fermata.Unregister(pausable);

            #endregion
        }

        /// <summary> 旧 <c>OnPauseChanged</c>。<see cref="Fermata.AddPauseChangedHandler(global::System.Action{bool})" /> へ転送する。 </summary>
        public static event global::System.Action<bool> OnPauseChanged
        {
            add => Fermata.AddPauseChangedHandler(value);
            remove => Fermata.RemovePauseChangedHandler(value);
        }

        /// <summary> 旧 <c>Pause</c>。取得は <see cref="Fermata.IsPausedAny" />、設定は <see cref="Fermata.SetPauseAll" /> へ転送する。 </summary>
        public static bool Pause
        {
            get => Fermata.IsPausedAny();
            set => Fermata.SetPauseAll(value);
        }

        /// <summary> <see cref="Fermata.GetPauseInfo()" /> へ転送する。 </summary>
        public static PauseInfo GetPauseInfo() => Fermata.GetPauseInfo();

        /// <summary> <see cref="Fermata.SetPause{TCategory}" /> へ転送する。 </summary>
        public static void SetPause<TCategory>(bool isPaused) where TCategory : SymphonyFramework.Pause.IPausable
        {
            // 6.x の PauseManager.IPausable は既定カテゴリーを表していたため、新しい IPausable へ読み替える。
            if (IsLegacyDefault<TCategory>()) { Fermata.SetPause<SymphonyFramework.Pause.IPausable>(isPaused); }
            else { Fermata.SetPause<TCategory>(isPaused); }
        }

        /// <summary> <see cref="Fermata.IsPaused{TCategory}" /> へ転送する。 </summary>
        public static bool IsPaused<TCategory>() where TCategory : SymphonyFramework.Pause.IPausable =>
            IsLegacyDefault<TCategory>()
                ? Fermata.IsPaused<SymphonyFramework.Pause.IPausable>()
                : Fermata.IsPaused<TCategory>();

        /// <summary> <see cref="Fermata.SetPauseAll" /> へ転送する。 </summary>
        public static void SetPauseAll(bool isPaused) => Fermata.SetPauseAll(isPaused);

        /// <summary> <see cref="Fermata.IsPausedAny" /> へ転送する。 </summary>
        public static bool IsPausedAny() => Fermata.IsPausedAny();

        /// <summary> <see cref="Fermata.AddPauseChangedHandler{TCategory}" /> へ転送する。 </summary>
        public static void AddPauseChangedHandler<TCategory>(global::System.Action<bool> handler)
            where TCategory : SymphonyFramework.Pause.IPausable
        {
            // 6.x の PauseManager.IPausable は既定カテゴリーを表していたため、新しい IPausable へ読み替える。
            if (IsLegacyDefault<TCategory>()) { Fermata.AddPauseChangedHandler<SymphonyFramework.Pause.IPausable>(handler); }
            else { Fermata.AddPauseChangedHandler<TCategory>(handler); }
        }

        /// <summary> <see cref="Fermata.RemovePauseChangedHandler{TCategory}" /> へ転送する。 </summary>
        public static void RemovePauseChangedHandler<TCategory>(global::System.Action<bool> handler)
            where TCategory : SymphonyFramework.Pause.IPausable
        {
            // 6.x の PauseManager.IPausable は既定カテゴリーを表していたため、新しい IPausable へ読み替える。
            if (IsLegacyDefault<TCategory>()) { Fermata.RemovePauseChangedHandler<SymphonyFramework.Pause.IPausable>(handler); }
            else { Fermata.RemovePauseChangedHandler<TCategory>(handler); }
        }

        /// <summary> <see cref="Fermata.GetPauseInfo{TCategory}" /> へ転送する。 </summary>
        public static PauseInfo GetPauseInfo<TCategory>() where TCategory : SymphonyFramework.Pause.IPausable =>
            IsLegacyDefault<TCategory>()
                ? Fermata.GetPauseInfo<SymphonyFramework.Pause.IPausable>()
                : Fermata.GetPauseInfo<TCategory>();

        /// <summary> 旧 <c>PausableNextFrameAsync</c>。<see cref="Fermata.NextFrameAsync(CancellationToken)" /> へ転送する。 </summary>
        public static Awaitable PausableNextFrameAsync(CancellationToken token = default) =>
            Fermata.NextFrameAsync(token);

        /// <summary> 旧 <c>PausableNextFrameAsync</c>。<see cref="Fermata.NextFrameAsync{TCategory}" /> へ転送する。 </summary>
        public static Awaitable PausableNextFrameAsync<TCategory>(CancellationToken token = default)
            where TCategory : SymphonyFramework.Pause.IPausable =>
            IsLegacyDefault<TCategory>()
                ? Fermata.NextFrameAsync<SymphonyFramework.Pause.IPausable>(token)
                : Fermata.NextFrameAsync<TCategory>(token);

        /// <summary> 旧 <c>PausableWaitForSecond</c>。<see cref="Fermata.WaitForSeconds(float)" /> へ転送する。 </summary>
        public static IEnumerator PausableWaitForSecond(float time) => Fermata.WaitForSeconds(time);

        /// <summary> 旧 <c>PausableWaitForSecond</c>。<see cref="Fermata.WaitForSeconds{TCategory}" /> へ転送する。 </summary>
        public static IEnumerator PausableWaitForSecond<TCategory>(float time)
            where TCategory : SymphonyFramework.Pause.IPausable =>
            IsLegacyDefault<TCategory>()
                ? Fermata.WaitForSeconds<SymphonyFramework.Pause.IPausable>(time)
                : Fermata.WaitForSeconds<TCategory>(time);

        /// <summary> 旧 <c>PausableWaitForSecondAsync</c>。<see cref="Fermata.WaitForSecondsAsync(float, CancellationToken)" /> へ転送する。 </summary>
        public static Awaitable PausableWaitForSecondAsync(float time, CancellationToken token = default) =>
            Fermata.WaitForSecondsAsync(time, token);

        /// <summary> 旧 <c>PausableWaitForSecondAsync</c>。<see cref="Fermata.WaitForSecondsAsync{TCategory}" /> へ転送する。 </summary>
        public static Awaitable PausableWaitForSecondAsync<TCategory>(float time, CancellationToken token = default)
            where TCategory : SymphonyFramework.Pause.IPausable =>
            IsLegacyDefault<TCategory>()
                ? Fermata.WaitForSecondsAsync<SymphonyFramework.Pause.IPausable>(time, token)
                : Fermata.WaitForSecondsAsync<TCategory>(time, token);

        /// <summary> 旧 <c>PausableWaitUntil</c>。<see cref="Fermata.WaitUntilAsync(global::System.Func{bool}, CancellationToken)" /> へ転送する。 </summary>
        public static Awaitable PausableWaitUntil(global::System.Func<bool> action, CancellationToken token = default) =>
            Fermata.WaitUntilAsync(action, token);

        /// <summary> 旧 <c>PausableWaitUntil</c>。<see cref="Fermata.WaitUntilAsync{TCategory}" /> へ転送する。 </summary>
        public static Awaitable PausableWaitUntil<TCategory>(
            global::System.Func<bool> action,
            CancellationToken token = default)
            where TCategory : SymphonyFramework.Pause.IPausable =>
            IsLegacyDefault<TCategory>()
                ? Fermata.WaitUntilAsync<SymphonyFramework.Pause.IPausable>(action, token)
                : Fermata.WaitUntilAsync<TCategory>(action, token);

        /// <summary> 旧 <c>PausableDestroy</c>。<see cref="Fermata.DestroyAsync(GameObject, float, CancellationToken)" /> へ転送する。 </summary>
        public static Awaitable PausableDestroy(GameObject obj, float t, CancellationToken token = default) =>
            Fermata.DestroyAsync(obj, t, token);

        /// <summary> 旧 <c>PausableDestroy</c>。<see cref="Fermata.DestroyAsync{TCategory}" /> へ転送する。 </summary>
        public static Awaitable PausableDestroy<TCategory>(GameObject obj, float t, CancellationToken token = default)
            where TCategory : SymphonyFramework.Pause.IPausable =>
            IsLegacyDefault<TCategory>()
                ? Fermata.DestroyAsync<SymphonyFramework.Pause.IPausable>(obj, t, token)
                : Fermata.DestroyAsync<TCategory>(obj, t, token);

        /// <summary> 旧 <c>PausableInvoke</c>。<see cref="Fermata.InvokeAsync(global::System.Action, float, CancellationToken)" /> へ転送する。 </summary>
        public static Awaitable PausableInvoke(global::System.Action action, float t, CancellationToken token = default) =>
            Fermata.InvokeAsync(action, t, token);

        /// <summary> 旧 <c>PausableInvoke</c>。<see cref="Fermata.InvokeAsync{TCategory}" /> へ転送する。 </summary>
        public static Awaitable PausableInvoke<TCategory>(
            global::System.Action action,
            float t,
            CancellationToken token = default)
            where TCategory : SymphonyFramework.Pause.IPausable =>
            IsLegacyDefault<TCategory>()
                ? Fermata.InvokeAsync<SymphonyFramework.Pause.IPausable>(action, t, token)
                : Fermata.InvokeAsync<TCategory>(action, t, token);

        #endregion

        #region 内部処理

        /// <summary>
        ///     カテゴリーとして 6.x の <see cref="IPausable" /> が指定されたかを判定する。
        /// </summary>
        /// <typeparam name="TCategory"> 指定されたカテゴリー。 </typeparam>
        /// <returns> 旧型そのものが指定された場合は true。既定カテゴリーへ読み替える。 </returns>
        private static bool IsLegacyDefault<TCategory>() => typeof(TCategory) == typeof(IPausable);

        #endregion
    }
}
