using System;
using System.Collections.Generic;

using UnityEngine;

namespace SymphonyFrameWork.Core
{
    /// <summary>
    ///     ViewModelが所有する値を保持し、値が変化したときに購読者へ通知する。
    ///     一時的な表示状態の通知だけに使用し、Unityのシリアライズ、永続化、
    ///     グローバルなEvent Busとして使用しない。
    ///     配列やListは既定のEqualityComparerでは参照比較になるため、利用側が
    ///     内容比較用のcomparerと通知後に変化しないスナップショットを用意する。
    /// </summary>
    /// <typeparam name="T"> 保持する値の型。 </typeparam>
    internal sealed class ReactiveProperty<T> : IReadOnlyReactiveProperty<T>, IDisposable
    {
        /// <summary>
        ///     初期値と値の比較方法を指定して初期化する。
        /// </summary>
        /// <param name="initialValue"> 初期値。 </param>
        /// <param name="comparer">
        ///     値の比較方法。省略した場合はEqualityComparer&lt;T&gt;.Defaultを使用する。
        /// </param>
        public ReactiveProperty(T initialValue, IEqualityComparer<T> comparer = null)
        {
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
            EnsureMainThread();
            ThrowIfDisposed();

            if (_comparer.Equals(_value, value))
            {
                return false;
            }

            _value = value;
            Subscription[] subscriptions = _subscriptions.ToArray();
            List<Exception> exceptions = null;

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
            EnsureMainThread();
            ThrowIfDisposed();

            if (observer == null)
            {
                throw new ArgumentNullException(nameof(observer));
            }

            var subscription = new Subscription(this, observer);
            _subscriptions.Add(subscription);

            if (notifyCurrent)
            {
                try
                {
                    observer(_value);
                }
                catch (Exception exception)
                {
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
            EnsureMainThread();

            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            foreach (Subscription subscription in _subscriptions)
            {
                subscription.Detach();
            }

            _subscriptions.Clear();
        }

        private readonly IEqualityComparer<T> _comparer;
        private readonly List<Subscription> _subscriptions = new();

        private T _value;
        private bool _isDisposed;

        /// <summary>
        ///     破棄済みの場合は例外を送出する。
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ReactiveProperty<T>));
            }
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
            try
            {
                _ = Time.frameCount;
            }
            catch (UnityException exception)
            {
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
            if (exceptions == null)
            {
                return;
            }

            Debug.LogException(new AggregateException(
                $"[{nameof(ReactiveProperty<T>)}] 購読者への通知で例外が発生しました。",
                exceptions));
        }

        /// <summary>
        ///     指定した購読を解除する。
        /// </summary>
        /// <param name="subscription"> 解除する購読。 </param>
        private void Unsubscribe(Subscription subscription)
        {
            EnsureMainThread();
            _subscriptions.Remove(subscription);
            subscription.Detach();
        }

        /// <summary>
        ///     ReactivePropertyから単一の購読を解除するためのハンドル。
        /// </summary>
        private sealed class Subscription : IDisposable
        {
            /// <summary>
            ///     所有者と購読者を指定して初期化する。
            /// </summary>
            /// <param name="owner"> 購読を所有するReactiveProperty。 </param>
            /// <param name="observer"> 値を受け取る購読者。 </param>
            public Subscription(ReactiveProperty<T> owner, Action<T> observer)
            {
                _owner = owner;
                Observer = observer;
            }

            /// <summary> 値を受け取る購読者。 </summary>
            public Action<T> Observer { get; }

            /// <summary>
            ///     購読を解除する。解除済みの場合は何もしない。
            /// </summary>
            public void Dispose()
            {
                if (_owner == null)
                {
                    return;
                }

                _owner.Unsubscribe(this);
            }

            /// <summary>
            ///     所有者との関連を解除する。
            /// </summary>
            public void Detach()
            {
                _owner = null;
            }

            private ReactiveProperty<T> _owner;
        }
    }
}
