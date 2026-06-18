using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
namespace SymphonyFrameWork.System.SceneLoad
{
    public class SceneLoadData
    {
        public SceneLoadData() { }

        public (string Name, int Priority) ActiveScene => _activeScene;
        internal ReadOnlyDictionary<string, SceneInfo> SceneDict => new(_sceneDict);

        public void LoadStart(string name, int priority = 0)
        {
            _sceneDict.TryAdd(name, new(default, priority));
        }

        public void LoadComplete(string name, Scene scene)
        {
            if (!_sceneDict.TryGetValue(name, out SceneInfo info)) { return; }
            info.RegisterScene(scene);
            info.StateChange(SceneLoadState.Complete);
            _sceneDict[name] = info;
        }

        public void LoadFail(string name)
        {
            _sceneDict.Remove(name);
        }

        public void UnloadStart(string name)
        {
            if (!_sceneDict.TryGetValue(name, out SceneInfo info)) { return; }
            info.StateChange(SceneLoadState.Unloading);
            _sceneDict[name] = info;
        }

        public void UnloadComplete(string name)
        {
            _sceneDict.Remove(name);
        }

        public void SetActiveScene(string name, int priority) => _activeScene = (name, priority);

        public void Reset(params KeyValuePair<string, Scene>[] newList)
        {
            _sceneDict.Clear();
            foreach (var pair in newList)
            {
                _sceneDict.Add(pair.Key, new(pair.Value));
            }
        }

        public void AddLoadedAction(string name, Action action)
        {
            // ロード済みなら即座に実行して終了。
            if (_sceneDict.TryGetValue(name, out SceneInfo info))
            {
                if (SceneLoadState.Complete <= info.State)
                {
                    action?.Invoke();
                    return;
                }
            }

            if (!_loadedAction.TryAdd(name, action))
            {
                _loadedAction[name] += action;
            }
        }

        public void InvokeLoadedAction(string name)
        {
            if (!_loadedAction.TryGetValue(name, out Action action)) { return; }
            action?.Invoke();
            _loadedAction.Remove(name);
        }

        public bool IsExistScene(string name) => _sceneDict.ContainsKey(name);

        public bool TryGetSceneState(string name, out SceneLoadState state)
        {
            if (!_sceneDict.TryGetValue(name, out SceneInfo info))
            {
                state = SceneLoadState.None;
                return false;
            }

            state = info.State;
            return true;
        }

        internal bool TryGetSceneInfo(string name, out SceneInfo info) => _sceneDict.TryGetValue(name, out info);

        internal struct SceneInfo
        {
            public SceneInfo(Scene scene, int priority = 0)
            {
                _scene = scene;
                _state = SceneLoadState.Loading;
                _priority = priority;
            }

            public Scene Scene => _scene;

            public SceneLoadState State => _state;
            public int Priority => _priority;

            public void RegisterScene(Scene scene) => _scene = scene;

            public void StateChange(SceneLoadState state) => _state = state;

            private Scene _scene;
            private SceneLoadState _state;
            private int _priority;
        }

        private readonly Dictionary<string, SceneInfo> _sceneDict = new();
        private readonly Dictionary<string, Action> _loadedAction = new();

        private (string Name, int Priority) _activeScene;
    }
}
