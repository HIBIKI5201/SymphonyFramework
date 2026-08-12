using System;

namespace SymphonyFrameWork.System.SceneLoad
{
    /// <summary>
    ///     シーン名を同一性としてロード状態、進捗、優先度を保持する。
    /// </summary>
    internal sealed class SceneLoadEntity
    {
        #region 外部向けAPI

        /// <summary>
        ///     初期状態を指定してシーンEntityを生成する。
        /// </summary>
        /// <param name="name"> シーン名。 </param>
        /// <param name="priority"> Active Scene選択用の優先度。 </param>
        /// <param name="state"> 初期ロード状態。 </param>
        internal SceneLoadEntity(
            string name,
            int priority = 0,
            SceneLoadStateEnum state = SceneLoadStateEnum.Loading)
        {
            // 空の名前ではEntityの同一性を確立できないため生成を拒否する。
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("シーン名を指定してください。", nameof(name));
            }

            Name = name;
            Priority = priority;
            State = state;

            // 完了状態で生成された場合は、初期進捗も完了値へ揃える。
            Progress = state == SceneLoadStateEnum.Complete ? 1f : 0f;
        }

        /// <summary> シーン名。 </summary>
        internal string Name { get; }

        /// <summary> 現在のロード状態。 </summary>
        internal SceneLoadStateEnum State { get; private set; }

        /// <summary> Active Scene選択に使用する優先度。 </summary>
        internal int Priority { get; private set; }

        /// <summary> 現在の処理進捗。 </summary>
        internal float Progress { get; private set; }

        /// <summary>
        ///     ロード開始状態へ遷移する。
        /// </summary>
        /// <param name="priority"> Active Scene選択用の優先度。 </param>
        /// <returns> 状態または値が変化した場合はtrue。 </returns>
        internal bool StartLoading(int priority)
        {
            // 更新前の値から、観測可能な状態が変わるかを先に確定する。
            bool changed = State != SceneLoadStateEnum.Loading
                || Priority != priority
                || Progress != 0f;

            State = SceneLoadStateEnum.Loading;
            Priority = priority;
            Progress = 0f;
            return changed;
        }

        /// <summary>
        ///     進捗を0から1の範囲へ正規化して更新する。
        /// </summary>
        /// <param name="progress"> 更新する進捗。 </param>
        /// <returns> 値が変化した場合はtrue。 </returns>
        internal bool ReportProgress(float progress)
        {
            // 外部の進捗実装が範囲外の値を返してもEntityの契約を維持する。
            float normalized = Math.Max(0f, Math.Min(1f, progress));
            if (Progress == normalized) { return false; }

            Progress = normalized;
            return true;
        }

        /// <summary>
        ///     ロード完了状態へ遷移する。
        /// </summary>
        /// <returns> 状態または値が変化した場合はtrue。 </returns>
        internal bool CompleteLoading()
        {
            // 状態と進捗のどちらかが完了値でなければ変更として扱う。
            bool changed = State != SceneLoadStateEnum.Complete || Progress != 1f;
            State = SceneLoadStateEnum.Complete;
            Progress = 1f;
            return changed;
        }

        /// <summary>
        ///     アンロード開始状態へ遷移する。
        /// </summary>
        /// <returns> 状態または値が変化した場合はtrue。 </returns>
        internal bool StartUnloading()
        {
            // 状態と進捗のどちらかが開始値でなければ変更として扱う。
            bool changed = State != SceneLoadStateEnum.Unloading || Progress != 0f;
            State = SceneLoadStateEnum.Unloading;
            Progress = 0f;
            return changed;
        }

        /// <summary>
        ///     Active Scene選択用の優先度を更新する。
        /// </summary>
        /// <param name="priority"> 新しい優先度。 </param>
        /// <returns> 値が変化した場合はtrue。 </returns>
        internal bool UpdatePriority(int priority)
        {
            // 同じ優先度なら、不要な状態変更通知を発生させない。
            if (Priority == priority) { return false; }

            Priority = priority;
            return true;
        }

        #endregion
    }
}
