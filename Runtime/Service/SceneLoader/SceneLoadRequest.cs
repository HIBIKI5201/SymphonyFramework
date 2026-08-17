using System;

namespace SymphonyFrameWork.System.SceneLoad
{
    /// <summary>
    ///     ロードするシーン名とActive Scene選択用の優先度を表す。
    /// </summary>
    public readonly struct SceneLoadRequest : IEquatable<SceneLoadRequest>
    {
        #region 外部向けAPI

        /// <summary>
        ///     ロード対象と優先度を指定してリクエストを生成する。
        /// </summary>
        /// <param name="sceneName"> ロードするシーン名。 </param>
        /// <param name="priority"> Active Scene選択に使用する優先度。 </param>
        /// <exception cref="ArgumentException"> シーン名がnull、空、空白の場合。 </exception>
        public SceneLoadRequest(string sceneName, int priority = 0)
        {
            // 空の名前ではロード対象を識別できないため生成を拒否する。
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                throw new ArgumentException("シーン名を指定してください。", nameof(sceneName));
            }

            // 検証済みの名前とActive Scene選択用の優先度を不変値として保持する。
            SceneName = sceneName;
            Priority = priority;
        }

        /// <summary> ロードするシーン名。 </summary>
        public string SceneName { get; }

        /// <summary> Active Scene選択に使用する優先度。 </summary>
        public int Priority { get; }

        /// <summary>
        ///     2つのリクエストが同じシーン名と優先度を持つか判定する。
        /// </summary>
        /// <param name="left"> 左辺のリクエスト。 </param>
        /// <param name="right"> 右辺のリクエスト。 </param>
        /// <returns> 同値の場合はtrue。 </returns>
        public static bool operator ==(SceneLoadRequest left, SceneLoadRequest right) =>
            left.Equals(right);

        /// <summary>
        ///     2つのリクエストが異なるシーン名または優先度を持つか判定する。
        /// </summary>
        /// <param name="left"> 左辺のリクエスト。 </param>
        /// <param name="right"> 右辺のリクエスト。 </param>
        /// <returns> 異なる場合はtrue。 </returns>
        public static bool operator !=(SceneLoadRequest left, SceneLoadRequest right) =>
            !left.Equals(right);

        /// <summary>
        ///     指定したリクエストと同値か判定する。
        /// </summary>
        /// <param name="other"> 比較対象。 </param>
        /// <returns> シーン名と優先度が一致する場合はtrue。 </returns>
        public bool Equals(SceneLoadRequest other) =>
            string.Equals(SceneName, other.SceneName, StringComparison.Ordinal)
            && Priority == other.Priority;

        /// <summary>
        ///     指定したオブジェクトと同値か判定する。
        /// </summary>
        /// <param name="obj"> 比較対象。 </param>
        /// <returns> 同値のSceneLoadRequestの場合はtrue。 </returns>
        public override bool Equals(object obj) =>
            obj is SceneLoadRequest other && Equals(other);

        /// <summary>
        ///     シーン名と優先度に基づくハッシュコードを返す。
        /// </summary>
        /// <returns> ハッシュコード。 </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                // Equalsで比較するシーン名と優先度を同じ順序で合成する。
                return ((SceneName != null ? StringComparer.Ordinal.GetHashCode(SceneName) : 0) * 397)
                    ^ Priority;
            }
        }

        #endregion
    }
}
