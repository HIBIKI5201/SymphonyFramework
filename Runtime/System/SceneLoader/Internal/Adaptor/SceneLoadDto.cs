using System;

namespace SymphonyFrameWork.System.SceneLoad
{
    /// <summary> Scene Loadの表示に必要な不変の更新値。 </summary>
    internal readonly struct SceneLoadDto : IEquatable<SceneLoadDto>
    {
        /// <summary> 表示に必要な値を指定して更新値を生成する。 </summary>
        /// <param name="sceneName"> シーン名。 </param>
        /// <param name="state"> ロード状態。 </param>
        /// <param name="priority"> Active Scene選択に使用する優先度。 </param>
        /// <param name="progress"> 0から1の範囲に正規化された処理進捗。 </param>
        /// <param name="isActive"> Active Sceneの場合はtrue。 </param>
        internal SceneLoadDto(
            string sceneName,
            SceneLoadState state,
            int priority,
            float progress,
            bool isActive)
        {
            SceneName = sceneName;
            State = state;
            Priority = priority;
            Progress = progress;
            IsActive = isActive;
        }

        /// <summary> シーン名。 </summary>
        internal string SceneName { get; }

        /// <summary> 現在のロード状態。 </summary>
        internal SceneLoadState State { get; }

        /// <summary> Active Scene選択に使用する優先度。 </summary>
        internal int Priority { get; }

        /// <summary> 0から1の範囲に正規化された処理進捗。 </summary>
        internal float Progress { get; }

        /// <summary> Active Sceneの場合はtrue。 </summary>
        internal bool IsActive { get; }

        /// <summary> 指定した更新値と同値か判定する。 </summary>
        /// <param name="other"> 比較対象。 </param>
        /// <returns> すべての値が一致する場合はtrue。 </returns>
        public bool Equals(SceneLoadDto other) =>
            string.Equals(SceneName, other.SceneName, StringComparison.Ordinal)
            && State == other.State
            && Priority == other.Priority
            && Progress.Equals(other.Progress)
            && IsActive == other.IsActive;

        /// <summary> 指定したオブジェクトと同値か判定する。 </summary>
        /// <param name="obj"> 比較対象。 </param>
        /// <returns> 同値のSceneLoadDtoの場合はtrue。 </returns>
        public override bool Equals(object obj) =>
            obj is SceneLoadDto other && Equals(other);

        /// <summary> 更新値の全値に基づくハッシュコードを返す。 </summary>
        /// <returns> ハッシュコード。 </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = SceneName != null
                    ? StringComparer.Ordinal.GetHashCode(SceneName)
                    : 0;
                hashCode = (hashCode * 397) ^ (int)State;
                hashCode = (hashCode * 397) ^ Priority;
                hashCode = (hashCode * 397) ^ Progress.GetHashCode();
                hashCode = (hashCode * 397) ^ IsActive.GetHashCode();
                return hashCode;
            }
        }
    }
}
