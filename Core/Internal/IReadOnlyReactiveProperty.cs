using System;

namespace SymphonyFrameWork.Core
{
    /// <summary>
    ///     ViewModelの表示状態を読み取り、変更を購読する契約。
    /// </summary>
    /// <remarks>
    ///     一時的な表示状態の通知に限り、シリアライズ、永続化、Event Busには使用しない。
    ///     配列やListでは、利用側が内容比較と不変なスナップショットを用意する。
    /// </remarks>
    /// <typeparam name="T"> 保持する値の型。 </typeparam>
    internal interface IReadOnlyReactiveProperty<out T>
    {
        #region 外部向けAPI

        /// <summary> 現在の値。 </summary>
        T Value { get; }

        /// <summary>
        ///     値の変更通知を購読する。
        /// </summary>
        /// <param name="observer"> 値を受け取る購読者。 </param>
        /// <param name="notifyCurrent"> 購読時に現在値を通知する場合はtrue。 </param>
        /// <returns> 購読を解除するためのIDisposable。 </returns>
        IDisposable Subscribe(Action<T> observer, bool notifyCurrent = true);

        #endregion
    }
}
