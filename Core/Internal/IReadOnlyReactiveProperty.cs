using System;

namespace SymphonyFrameWork.Core
{
    /// <summary>
    ///     ViewModelが保持する表示状態を読み取り、変更を購読するための契約。
    ///     一時的な表示状態の通知だけに使用し、Unityのシリアライズ、永続化、
    ///     グローバルなEvent Busとして使用しない。
    ///     配列やListを値にする場合、ReactivePropertyの既定比較は参照比較になるため、
    ///     利用側が内容比較用のcomparerと不変なスナップショットを用意する。
    /// </summary>
    /// <typeparam name="T"> 保持する値の型。 </typeparam>
    internal interface IReadOnlyReactiveProperty<out T>
    {
        /// <summary> 現在の値。 </summary>
        T Value { get; }

        /// <summary>
        ///     値の変更通知を購読する。
        /// </summary>
        /// <param name="observer"> 値を受け取る購読者。 </param>
        /// <param name="notifyCurrent"> 購読時に現在値を通知する場合はtrue。 </param>
        /// <returns> 購読を解除するためのIDisposable。 </returns>
        IDisposable Subscribe(Action<T> observer, bool notifyCurrent = true);
    }
}
