using System;
using System.Collections.Generic;

namespace SymphonyFrameWork.System.SceneBlock
{
    /// <summary>
    ///     追跡中Scene Blockと、シーンごとの保持元を保持する。
    /// </summary>
    /// <remarks>
    ///     **シーンをアンロードしてよいかの判断材料は、すべてこの型が持つ。**
    ///     ブロックからシーンへの対応と、シーンから保持ブロックへの逆引きを同時に更新する。
    /// </remarks>
    internal sealed class SceneBlockRegistry
    {
        #region 外部向けAPI

        /// <summary> 追跡中の全ブロック。 </summary>
        internal IReadOnlyDictionary<string, SceneBlockLoadEntity> Entities => _entities;

        /// <summary>
        ///     ブロックを追跡対象へ登録する。
        /// </summary>
        /// <param name="entity"> 登録するブロック。 </param>
        /// <exception cref="ArgumentNullException"> entityがnullの場合。 </exception>
        internal void Register(SceneBlockLoadEntity entity)
        {
            if (entity == null) { throw new ArgumentNullException(nameof(entity)); }

            // 同名の再登録は呼び出し側が事前に弾く契約のため、ここでは上書きしない。
            _entities.Add(entity.BlockName, entity);
        }

        /// <summary>
        ///     ブロック名から追跡中のブロックを取得する。
        /// </summary>
        /// <param name="blockName"> 取得するブロック名。 </param>
        /// <param name="entity"> 取得できたブロック。 </param>
        /// <returns> 追跡中の場合はtrue。 </returns>
        internal bool TryGet(string blockName, out SceneBlockLoadEntity entity) =>
            _entities.TryGetValue(blockName ?? string.Empty, out entity);

        /// <summary>
        ///     ブロックを追跡対象から取り除く。
        /// </summary>
        /// <param name="blockName"> 取り除くブロック名。 </param>
        /// <returns> 取り除いた場合はtrue。 </returns>
        /// <remarks>
        ///     シーンの保持は取り除かない。**先に保持を解いてから呼ぶこと。**
        /// </remarks>
        internal bool Remove(string blockName) =>
            _entities.Remove(blockName ?? string.Empty);

        /// <summary>
        ///     指定したブロックがシーンを保持していることを記録する。
        /// </summary>
        /// <param name="sceneName"> 保持されるシーン名。 </param>
        /// <param name="blockName"> 保持するブロック名。 </param>
        internal void AddHolder(string sceneName, string blockName)
        {
            if (!_sceneHolders.TryGetValue(sceneName, out HashSet<string> holders))
            {
                holders = new HashSet<string>(StringComparer.Ordinal);
                _sceneHolders.Add(sceneName, holders);
            }

            holders.Add(blockName);
        }

        /// <summary>
        ///     指定したブロックによるシーンの保持を解く。
        /// </summary>
        /// <param name="sceneName"> 保持を解くシーン名。 </param>
        /// <param name="blockName"> 保持を解くブロック名。 </param>
        /// <returns> どのブロックも保持しなくなった場合はtrue。 </returns>
        internal bool ReleaseHolder(string sceneName, string blockName)
        {
            if (!_sceneHolders.TryGetValue(sceneName, out HashSet<string> holders)) { return true; }

            holders.Remove(blockName);
            if (holders.Count > 0) { return false; }

            // 保持元が0件になった対応は残さず、辞書を膨らませない。
            _sceneHolders.Remove(sceneName);
            return true;
        }

        /// <summary>
        ///     いずれかのブロックがシーンを保持しているか確認する。
        /// </summary>
        /// <param name="sceneName"> 確認するシーン名。 </param>
        /// <returns> 保持しているブロックがある場合はtrue。 </returns>
        internal bool IsHeldByAnyBlock(string sceneName) =>
            _sceneHolders.ContainsKey(sceneName);

        /// <summary>
        ///     ブロック以外の経路でロードされたシーンとして記録する。
        /// </summary>
        /// <param name="sceneName"> 記録するシーン名。 </param>
        /// <remarks>
        ///     **一度記録したシーンは、どのブロックのアンロードでもアンロードしない。**
        ///     利用側が自分でロードしたシーンを、フレームワークが勝手に落とさないための印である。
        /// </remarks>
        internal void MarkExternallyHeld(string sceneName) =>
            _externallyHeldScenes.Add(sceneName);

        /// <summary>
        ///     ブロック以外の経路でロードされたシーンか確認する。
        /// </summary>
        /// <param name="sceneName"> 確認するシーン名。 </param>
        /// <returns> ブロック外で保持されている場合はtrue。 </returns>
        internal bool IsExternallyHeld(string sceneName) =>
            _externallyHeldScenes.Contains(sceneName);

        /// <summary>
        ///     指定したブロックが保持しているシーン名を取得する。
        /// </summary>
        /// <param name="blockName"> 取得するブロック名。 </param>
        /// <returns> 保持しているシーン名。決定的な順序へ揃える。 </returns>
        internal IReadOnlyList<string> GetHeldSceneNames(string blockName)
        {
            List<string> sceneNames = new();
            foreach (KeyValuePair<string, HashSet<string>> entry in _sceneHolders)
            {
                if (entry.Value.Contains(blockName)) { sceneNames.Add(entry.Key); }
            }

            // 辞書の列挙順に依存しない表示と比較にするため、Ordinal順へ揃える。
            sceneNames.Sort(StringComparer.Ordinal);
            return sceneNames;
        }

        /// <summary>
        ///     追跡状態をすべて破棄する。
        /// </summary>
        internal void Clear()
        {
            // Domain Reloadが無効でも、Play Mode間で保持情報を持ち越さない。
            _entities.Clear();
            _sceneHolders.Clear();
            _externallyHeldScenes.Clear();
        }

        #endregion

        #region 内部処理

        private readonly Dictionary<string, SceneBlockLoadEntity> _entities =
            new(StringComparer.Ordinal);

        private readonly Dictionary<string, HashSet<string>> _sceneHolders =
            new(StringComparer.Ordinal);

        private readonly HashSet<string> _externallyHeldScenes =
            new(StringComparer.Ordinal);

        #endregion
    }
}
