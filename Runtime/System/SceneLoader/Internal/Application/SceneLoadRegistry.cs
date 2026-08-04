using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SymphonyFrameWork.System.SceneLoad
{
    /// <summary> シーンEntityの所有、検索、Active Scene情報、ロード完了通知を管理する。 </summary>
    internal sealed class SceneLoadRegistry
    {
        /// <summary> 空のRegistryを生成する。 </summary>
        internal SceneLoadRegistry()
        {
            _readOnlyEntities = new ReadOnlyDictionary<string, SceneLoadEntity>(_entities);
        }

        /// <summary> 追跡中Entityを名前で参照する読み取り専用Dictionary。 </summary>
        internal IReadOnlyDictionary<string, SceneLoadEntity> Entities => _readOnlyEntities;

        /// <summary> 記録済みActive Scene名。 </summary>
        internal string ActiveSceneName => _activeScene.Name;

        /// <summary> 記録済みActive Sceneの優先度。 </summary>
        internal int ActiveScenePriority => _activeScene.Priority;

        private readonly Dictionary<string, SceneLoadEntity> _entities = new();
        private readonly IReadOnlyDictionary<string, SceneLoadEntity> _readOnlyEntities;
        private readonly Dictionary<string, Action> _loadedActions = new();

        private (string Name, int Priority) _activeScene;

        /// <summary> 指定したシーンをロード開始状態として登録または更新する。 </summary>
        /// <param name="request"> ロード対象と優先度。 </param>
        /// <returns> 登録または更新したEntity。 </returns>
        internal SceneLoadEntity StartLoading(SceneLoadRequest request)
        {
            if (_entities.TryGetValue(request.SceneName, out SceneLoadEntity entity))
            {
                entity.StartLoading(request.Priority);
                return entity;
            }

            entity = new SceneLoadEntity(request.SceneName, request.Priority);
            _entities.Add(request.SceneName, entity);
            return entity;
        }

        /// <summary> 指定したロード済みシーンを登録または更新する。 </summary>
        /// <param name="request"> シーン名と優先度。 </param>
        /// <returns> 登録または更新したEntity。 </returns>
        internal SceneLoadEntity RegisterLoaded(SceneLoadRequest request)
        {
            if (!_entities.TryGetValue(request.SceneName, out SceneLoadEntity entity))
            {
                entity = new SceneLoadEntity(
                    request.SceneName,
                    request.Priority,
                    SceneLoadStateEnum.Complete);
                _entities.Add(request.SceneName, entity);
                return entity;
            }

            entity.UpdatePriority(request.Priority);
            entity.CompleteLoading();
            return entity;
        }

        /// <summary> 指定名のEntityを取得する。 </summary>
        /// <param name="sceneName"> シーン名。 </param>
        /// <param name="entity"> 取得できたEntity。 </param>
        /// <returns> 取得できた場合はtrue。 </returns>
        internal bool TryGet(string sceneName, out SceneLoadEntity entity)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                entity = null;
                return false;
            }

            return _entities.TryGetValue(sceneName, out entity);
        }

        /// <summary> 指定名のEntityが追跡中か確認する。 </summary>
        /// <param name="sceneName"> シーン名。 </param>
        /// <returns> 追跡中の場合はtrue。 </returns>
        internal bool Contains(string sceneName) =>
            !string.IsNullOrWhiteSpace(sceneName) && _entities.ContainsKey(sceneName);

        /// <summary> 指定名のEntityを追跡から削除する。 </summary>
        /// <param name="sceneName"> シーン名。 </param>
        /// <returns> 削除できた場合はtrue。 </returns>
        internal bool Remove(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            return _entities.Remove(sceneName);
        }

        /// <summary> 指定したSceneをActive Sceneとして記録する。 </summary>
        /// <param name="sceneName"> シーン名。 </param>
        /// <returns> 記録値が変化した場合はtrue。 </returns>
        internal bool SetActiveScene(string sceneName)
        {
            if (!_entities.TryGetValue(sceneName, out SceneLoadEntity entity))
            {
                return false;
            }

            var next = (entity.Name, entity.Priority);
            if (_activeScene == next)
            {
                return false;
            }

            _activeScene = next;
            return true;
        }

        /// <summary> Active Sceneの記録を消去する。 </summary>
        /// <returns> 記録値が変化した場合はtrue。 </returns>
        internal bool ClearActiveScene()
        {
            if (string.IsNullOrEmpty(_activeScene.Name) && _activeScene.Priority == 0)
            {
                return false;
            }

            _activeScene = default;
            return true;
        }

        /// <summary> ロード完了済みEntityのうち最高優先度のものを取得する。 </summary>
        /// <param name="entity"> 取得できたEntity。 </param>
        /// <returns> 対象が存在する場合はtrue。 </returns>
        internal bool TryGetHighestPriorityLoaded(out SceneLoadEntity entity)
        {
            entity = null;

            foreach (KeyValuePair<string, SceneLoadEntity> pair in _entities)
            {
                SceneLoadEntity current = pair.Value;
                if (current.State != SceneLoadStateEnum.Complete)
                {
                    continue;
                }

                if (entity == null || entity.Priority <= current.Priority)
                {
                    entity = current;
                }
            }

            return entity != null;
        }

        /// <summary> Unityでロード済みのScene名に合わせて追跡Entityを再構築する。 </summary>
        /// <param name="loadedSceneNames"> Unityでロード済みのScene名一覧。 </param>
        /// <param name="activeSceneName"> UnityのActive Scene名。 </param>
        /// <returns> 追跡状態またはActive Scene情報が変化した場合はtrue。 </returns>
        internal bool Synchronize(
            IReadOnlyList<string> loadedSceneNames,
            string activeSceneName)
        {
            if (loadedSceneNames == null)
            {
                throw new ArgumentNullException(nameof(loadedSceneNames));
            }

            var nextEntities = new Dictionary<string, SceneLoadEntity>();

            for (int i = 0; i < loadedSceneNames.Count; i++)
            {
                string sceneName = loadedSceneNames[i];
                if (string.IsNullOrWhiteSpace(sceneName) || nextEntities.ContainsKey(sceneName))
                {
                    continue;
                }

                int priority = _entities.TryGetValue(sceneName, out SceneLoadEntity existing)
                    ? existing.Priority
                    : 0;

                nextEntities.Add(
                    sceneName,
                    new SceneLoadEntity(
                        sceneName,
                        priority,
                        SceneLoadStateEnum.Complete));
            }

            bool changed = !HasSameEntities(nextEntities);
            _entities.Clear();
            foreach (KeyValuePair<string, SceneLoadEntity> pair in nextEntities)
            {
                _entities.Add(pair.Key, pair.Value);
            }

            if (!string.IsNullOrWhiteSpace(activeSceneName)
                && _entities.ContainsKey(activeSceneName))
            {
                changed |= SetActiveScene(activeSceneName);
            }
            else
            {
                changed |= ClearActiveScene();
            }

            return changed;
        }

        /// <summary> ロード完了後に一度実行するcallbackを登録する。 </summary>
        /// <param name="sceneName"> 対象シーン名。 </param>
        /// <param name="action"> 実行するcallback。 </param>
        /// <returns> 既にロード済みで即時実行すべき場合はtrue。 </returns>
        internal bool RegisterLoadedAction(string sceneName, Action action)
        {
            if (TryGet(sceneName, out SceneLoadEntity entity)
                && entity.State == SceneLoadStateEnum.Complete)
            {
                return true;
            }

            if (!_loadedActions.TryAdd(sceneName, action))
            {
                _loadedActions[sceneName] += action;
            }

            return false;
        }

        /// <summary> 指定シーンのロード完了callbackを取り出して登録から削除する。 </summary>
        /// <param name="sceneName"> 対象シーン名。 </param>
        /// <returns> 登録済みcallback。存在しない場合はnull。 </returns>
        internal Action TakeLoadedAction(string sceneName)
        {
            if (!_loadedActions.TryGetValue(sceneName, out Action action))
            {
                return null;
            }

            _loadedActions.Remove(sceneName);
            return action;
        }

        /// <summary> Entity、Active Scene、完了callbackをすべて消去する。 </summary>
        internal void Clear()
        {
            _entities.Clear();
            _loadedActions.Clear();
            _activeScene = default;
        }

        /// <summary> 現在のEntity群が指定Dictionaryと同じ値を持つか確認する。 </summary>
        /// <param name="other"> 比較対象。 </param>
        /// <returns> 同じ場合はtrue。 </returns>
        private bool HasSameEntities(Dictionary<string, SceneLoadEntity> other)
        {
            if (_entities.Count != other.Count)
            {
                return false;
            }

            foreach (KeyValuePair<string, SceneLoadEntity> pair in other)
            {
                if (!_entities.TryGetValue(pair.Key, out SceneLoadEntity current)
                    || current.Priority != pair.Value.Priority
                    || current.State != pair.Value.State
                    || current.Progress != pair.Value.Progress)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
