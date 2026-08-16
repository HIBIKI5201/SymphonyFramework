using System;
using System.Collections.Generic;

using UnityEngine;

namespace SymphonyFrameWork.Core
{
    /// <summary>
    ///     ViewModelの値を保持し、変更を購読者へ通知する。
    /// </summary>
    /// <remarks>
    ///     一時的な表示状態の通知に限り、シリアライズ、永続化、Event Busには使用しない。
    ///     配列やListでは、利用側が内容比較と通知後も不変なスナップショットを用意する。
    /// </remarks>
    /// <typeparam name="T"> 保持する値の型。 </typeparam>
    internal sealed class ReactiveProperty<T> : IReadOnlyReactiveProperty<T>, IDisposable
    {
        #region 外部向けAPI

        /// <summary>
        ///     初期値と値の比較方法を指定して初期化する。
        /// </summary>
        /// <param name="initialValue"> 初期値。 </param>
        /// <param name="comparer">
        ///     値の比較方法。省略した場合はEqualityComparer&lt;T&gt;.Defaultを使用する。
        /// </param>
        public ReactiveProperty(T initialValue, IEqualityComparer<T> comparer = null)
        {
            // 初期値を保持し、比較方法が未指定の場合は型の標準比較へ統一する。
            _value = initialValue;
            _comparer = comparer ?? EqualityComparer<T>.Default;
        }

        /// <summary> 現在の値。 </summary>
        public T Value => _value;

        /// <summary>
        ///     値が変化した場合に更新し、購読者へ通知する。
        /// </summary>
        /// <param name="value"> 新しい値。 </param>
        /// <returns> 値が変化した場合はtrue。 </returns>
        /// <exception cref="InvalidOperationException">
        ///     メインスレッド以外から呼び出された場合に送出される。
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     破棄後に呼び出された場合に送出される。
        /// </exception>
        public bool SetValue(T value)
        {
            // Unityの状態と購読一覧を安全に扱えるスレッドとライフサイクルに限定する。
            EnsureMainThread();
            ThrowIfDisposed();

            // 比較上同じ値なら、不要な通知と購読者側の副作用を発生させない。
            if (_comparer.Equals(_value, value)) { return false; }

            // 通知中の購読解除で列挙状態が変わらないよう、現在の購読一覧を固定する。
            _value = value;
            Subscription[] subscriptions = _subscriptions.ToArray();
            List<Exception> exceptions = null;

            // 一部の購読者が失敗しても、残りの購読者への通知を継続する。
            foreach (Subscription subscription in subscriptions)
            {
                try
                {
                    subscription.Observer(value);
                }
                catch (Exception exception)
                {
                    exceptions ??= new List<Exception>();
                    exceptions.Add(exception);
                }
            }

            // 購読者の例外は全通知の完了後にまとめ、更新自体は成功として扱う。
            LogObserverExceptions(exceptions);
            return true;
        }

        /// <summary>
        ///     値の変更通知を購読する。
        /// </summary>
        /// <param name="observer"> 値を受け取る購読者。 </param>
        /// <param name="notifyCurrent"> 購読時に現在値を通知する場合はtrue。 </param>
        /// <returns> 購読を解除するためのIDisposable。 </returns>
        /// <exception cref="ArgumentNullException">
        ///     observerがnullの場合に送出される。
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     メインスレッド以外から呼び出された場合に送出される。
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     破棄後に呼び出された場合に送出される。
        /// </exception>
        public IDisposable Subscribe(Action<T> observer, bool notifyCurrent = true)
        {
            // 購読一覧を安全に扱えるスレッドとライフサイクルに限定する。
            EnsureMainThread();
            ThrowIfDisposed();

            // 通知時まで失敗を遅らせないよう、nullの購読者は登録前に拒否する。
            if (observer == null) { throw new ArgumentNullException(nameof(observer)); }

            // 現在値の通知中でも購読者自身が解除できるよう、通知前に一覧へ追加する。
            Subscription subscription = new(this, observer);
            _subscriptions.Add(subscription);

            // 購読開始時点の状態も必要な購読者にだけ、現在値を即時通知する。
            if (notifyCurrent)
            {
                try
                {
                    observer(_value);
                }
                catch (Exception exception)
                {
                    // 初回通知の失敗でも購読自体は維持し、他の通知と同じ方法で記録する。
                    LogObserverExceptions(new List<Exception> { exception });
                }
            }

            return subscription;
        }

        /// <summary>
        ///     すべての購読を解除し、更新と新規購読を拒否する。
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     メインスレッド以外から呼び出された場合に送出される。
        /// </exception>
        public void Dispose()
        {
            // 購読一覧を安全に破棄できるスレッドに限定する。
            EnsureMainThread();

            // 破棄済みの場合は、複数回の破棄を同じ結果にする。
            if (_isDisposed) { return; }

            // 再入された操作を拒否してから、各ハンドルの所有者参照を切り離す。
            _isDisposed = true;

            foreach (Subscription subscription in _subscriptions) { subscription.Detach(); }

            _subscriptions.Clear();
        }

        #endregion

        #region 内部処理

        private readonly IEqualityComparer<T> _comparer;
        private readonly List<Subscription> _subscriptions = new();

        private T _value;
        private bool _isDisposed;

        /// <summary>
        ///     破棄済みの場合は例外を送出する。
        /// </summary>
        private void ThrowIfDisposed()
        {
            // 破棄後は値と購読一覧の整合性を保証できないため、操作を拒否する。
            if (_isDisposed) { throw new ObjectDisposedException(nameof(ReactiveProperty<T>)); }
        }

        /// <summary>
        ///     現在のスレッドがUnityのメインスレッドでない場合は例外を送出する。
        ///     Unity APIはメインスレッド以外から呼ぶとUnityExceptionになるため、
        ///     副作用のないプロパティ読み取りで判定する。
        ///     Awaitableはプールされた型であり、awaitせずに生成すると
        ///     プールへ返却されないため、判定には使用しない。
        /// </summary>
        private static void EnsureMainThread()
        {
            // awaitされずプールへ戻らないAwaitableを生成せず、Unity APIへのアクセス可否で判定する。
            try
            {
                _ = Time.frameCount;
            }
            catch (UnityException exception)
            {
                // Unity APIが拒否した場合は、呼び出し側へスレッド制約を明示する。
                throw new InvalidOperationException(
                    $"[{nameof(ReactiveProperty<T>)}] 操作はメインスレッドで実行してください。",
                    exception);
            }
        }

        /// <summary>
        ///     購読者が送出した例外をまとめて記録する。
        /// </summary>
        /// <param name="exceptions"> 記録する例外。 </param>
        private static void LogObserverExceptions(List<Exception> exceptions)
        {
            // 例外が発生していない通知ではログを出さない。
            if (exceptions == null) { return; }

            // 複数の購読者例外を1件に集約し、通知失敗の全体像を保持する。
            // CoreはRuntimeのロガーを直接参照できないため、中継口へ渡して集約先を揃える。
            CoreLogRelay.LogException(new AggregateException(
                $"[{nameof(ReactiveProperty<T>)}] 購読者への通知で例外が発生しました。",
                exceptions));
        }

        /// <summary>
        ///     指定した購読を解除する。
        /// </summary>
        /// <param name="subscription"> 解除する購読。 </param>
        private void Unsubscribe(Subscription subscription)
        {
            // 購読一覧とハンドルの状態変更をメインスレッドへ限定する。
            EnsureMainThread();

            // 一覧から先に除外し、以後の通知対象にならない状態で所有者参照を切る。
            _subscriptions.Remove(subscription);
            subscription.Detach();
        }

        /// <summary>
        ///     ReactivePropertyから単一の購読を解除するためのハンドル。
        /// </summary>
        private sealed class Subscription : IDisposable
        {
            #region 外部向けAPI

            /// <summary>
            ///     所有者と購読者を指定して初期化する。
            /// </summary>
            /// <param name="owner"> 購読を所有するReactiveProperty。 </param>
            /// <param name="observer"> 値を受け取る購読者。 </param>
            public Subscription(ReactiveProperty<T> owner, Action<T> observer)
            {
                // 解除先と通知先を同じ購読単位へ保持する。
                _owner = owner;
                Observer = observer;
            }

            /// <summary> 値を受け取る購読者。 </summary>
            public Action<T> Observer { get; }

            /// <summary>
            ///     購読を解除する。
            /// </summary>
            /// <remarks> 解除済みの場合は何もしない。 </remarks>
            public void Dispose()
            {
                // 所有者との関連が既に切れている場合は、重複解除を避ける。
                if (_owner == null) { return; }

                _owner.Unsubscribe(this);
            }

            /// <summary>
            ///     所有者との関連を解除する。
            /// </summary>
            public void Detach()
            {
                _owner = null;
            }

            #endregion

            #region 内部処理

            private ReactiveProperty<T> _owner;

            #endregion
        }

        #endregion
    }
}
