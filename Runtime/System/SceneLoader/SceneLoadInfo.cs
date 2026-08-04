using System;

namespace SymphonyFrameWork.System.SceneLoad
{
    /// <summary> 追跡中シーンの状態を表す不変なスナップショット。 </summary>
    public readonly struct SceneLoadInfo : IEquatable<SceneLoadInfo>
    {
        /// <summary> Queryが抽出した追跡状態からスナップショットを生成する。 </summary>
        /// <param name="sceneName"> シーン名。 </param>
        /// <param name="state"> ロード状態。 </param>
        /// <param name="priority"> Active Scene選択に使用する優先度。 </param>
        /// <param name="progress"> 0から1の範囲に正規化された処理進捗。 </param>
        /// <param name="isActive"> Active Sceneの場合はtrue。 </param>
        internal SceneLoadInfo(
            string sceneName,
            SceneLoadStateEnum state,
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
        public string SceneName { get; }

        /// <summary> 現在のロード状態。 </summary>
        public SceneLoadStateEnum State { get; }

        /// <summary> Active Scene選択に使用する優先度。 </summary>
        public int Priority { get; }

        /// <summary> 0から1の範囲に正規化された処理進捗。 </summary>
        public float Progress { get; }

        /// <summary> Active Sceneの場合はtrue。 </summary>
        public bool IsActive { get; }

        /// <summary> 2つのスナップショットが同じ値を持つか判定する。 </summary>
        /// <param name="left"> 左辺のスナップショット。 </param>
        /// <param name="right"> 右辺のスナップショット。 </param>
        /// <returns> 同値の場合はtrue。 </returns>
        public static bool operator ==(SceneLoadInfo left, SceneLoadInfo right) =>
            left.Equals(right);

        /// <summary> 2つのスナップショットが異なる値を持つか判定する。 </summary>
        /// <param name="left"> 左辺のスナップショット。 </param>
        /// <param name="right"> 右辺のスナップショット。 </param>
        /// <returns> 異なる場合はtrue。 </returns>
        public static bool operator !=(SceneLoadInfo left, SceneLoadInfo right) =>
            !left.Equals(right);

        /// <summary> 指定したスナップショットと同値か判定する。 </summary>
        /// <param name="other"> 比較対象。 </param>
        /// <returns> すべての値が一致する場合はtrue。 </returns>
        public bool Equals(SceneLoadInfo other) =>
            string.Equals(SceneName, other.SceneName, StringComparison.Ordinal)
            && State == other.State
            && Priority == other.Priority
            && Progress.Equals(other.Progress)
            && IsActive == other.IsActive;

        /// <summary> 指定したオブジェクトと同値か判定する。 </summary>
        /// <param name="obj"> 比較対象。 </param>
        /// <returns> 同値のSceneLoadInfoの場合はtrue。 </returns>
        public override bool Equals(object obj) =>
            obj is SceneLoadInfo other && Equals(other);

        /// <summary> スナップショットの全値に基づくハッシュコードを返す。 </summary>
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
