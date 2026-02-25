using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace SymphonyFrameWork.System.SceneLoad
{
    public class SceneLoadData
    {
        public SceneLoadData() { }

        public void LoadStart(string name)
        {
            _sceneDict.TryAdd(name, new(default));
        }

        public void LoadComplete(string name, Scene scene)
        {
            if (!_sceneDict.TryGetValue(name, out SceneInfo info)) { return; }
            info.RegisterScene(scene);
            info.StateChange(SceneLoadState.Complete);
        }

        public void LoadFail(string name)
        {
            _sceneDict.Remove(name);
        }

        public void UnloadStart(string name)
        {
            if (!_sceneDict.TryGetValue(name, out SceneInfo info)) { return; }
            info.StateChange(SceneLoadState.Unloading);
        }

        public void UnloadComplete(string name)
        {
            _sceneDict.Remove(name);
        }

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
        public bool TryGetSceneInfo(string name, out SceneInfo info) => _sceneDict.TryGetValue(name, out info);

        public struct SceneInfo
        {
            public SceneInfo(Scene scene)
            {
                _scene = scene;
                _state = SceneLoadState.Loading;
            }

            public Scene Scene => _scene;

            public SceneLoadState State => _state;

            public void RegisterScene(Scene scene) => _scene = scene;

            public void StateChange(SceneLoadState state) => _state = state;

            private Scene _scene;
            private SceneLoadState _state;
        }

        public enum SceneLoadState
        {
            Loading,
            Complete,
            Unloading
        }

        private static readonly Dictionary<string, SceneInfo> _sceneDict = new();
        private static readonly Dictionary<string, Action> _loadedAction = new();


    }
}
