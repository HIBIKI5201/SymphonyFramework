using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SymphonyFrameWork.Utility
{
    /// <summary>
    ///     Taskの機能を拡張するクラス
    /// </summary>
    public static class SymphonyTask
    {
        /// <summary>
        ///     バッググラウンドで処理する
        /// </summary>
        /// <param name="action"> バックグラウンドスレッドで実行する処理。 </param>
        public static async void BackGroundThreadAction(Action action)
        {
            await Awaitable.BackgroundThreadAsync();
            action.Invoke();
            Debug.Log($"{action.Method} is done");
        }

        /// <summary>
        ///     バッググラウンドで処理する
        /// </summary>
        /// <param name="action"> バックグラウンドスレッドで実行する処理。 </param>
        public static async Task BackGroundThreadActionAsync(Action action)
        {
            await Awaitable.BackgroundThreadAsync();
            action.Invoke();
            Debug.Log($"{action.Method} is done");
        }

        /// <summary>
        ///     条件がtrueになるまで待機する
        /// </summary>
        /// <param name="action">条件の結果を返すメソッド</param>
        /// <param name="token"> 待機を中断するためのトークン。 </param>
        /// <returns> 条件成立までの待機処理を表すTask。 </returns>
        public static async Task WaitUntil(Func<bool> action, CancellationToken token = default)
        {
            while (!action.Invoke()) await Awaitable.NextFrameAsync(token);
        }

        /// <summary>
        /// 親のタスクが終了した時に実行される
        /// </summary>
        /// <param name="task"> 完了を監視するTask。 </param>
        /// <param name="action"> Task完了後に実行する処理。 </param>
        /// <param name="token"> 後続処理を中止するためのトークン。 </param>
        /// <returns>親のタスク</returns>
        public static Task OnComplete(this Task task, Action action, CancellationToken token = default)
        {
            OnCompleteInvoke();
            return task;

            async void OnCompleteInvoke()
            {
                await task;

                if (token.IsCancellationRequested) return;

                action?.Invoke();
            }
        }
    }
}
