using SymphonyFrameWork.Debugger.HUD;
using SymphonyFrameWork.System.SceneLoad;
using SymphonyFrameWork.System.SaveSystem;
using SymphonyFrameWork.System.ServiceLocate;
using SymphonyFrameWork.Utility;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SymphonyFrameWork.System
{
    /// <summary>
    ///     SymphonyFrameWorkの管理シーンを持つ
    /// </summary>
    public static class SymphonyCoreSystem
    {
        /// <summary>
        ///     オブジェクトをSymphonySystemシーンに移動する
        /// </summary>
        /// <param name="go"></param>
        public static async void MoveObjectToSymphonySystem(GameObject go)
        {
            //シーンが制作されているか、対象がnullになったら進む
            await SymphonyTask.WaitUntil(() => _systemScene != null || go == null);

            if (go) SceneManager.MoveGameObjectToScene(go, _systemScene.Value);
        }

        internal static T CreateSystemObject<T>() where T : Component
        {
            var go = new GameObject(typeof(T).Name);
            T component = go.AddComponent<T>();
            MoveObjectToSymphonySystem(go);
            return component;
        }

        internal const string SYMPHONY_SCENE_NAME = "SymphonySystem";

        private static Scene? _systemScene;
        private static SymphonyCoreSystemObject _systemObject;

        /// <summary>
        ///     初期化でシステム用のシーンを作成
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void GameBeforeSceneLoaded()
        {
            //専用のシーン生成
            _systemScene = SceneManager.CreateScene(SYMPHONY_SCENE_NAME);

            var systemGameObject = new GameObject(nameof(SymphonyCoreSystem));
            _systemObject = systemGameObject.AddComponent<SymphonyCoreSystemObject>();
            SceneManager.MoveGameObjectToScene(systemGameObject, _systemScene.Value);
            SaveSystem.SaveSystem.Initialize(_systemObject.destroyCancellationToken);

            //各クラスの初期化
            PauseManager.Initialize();
            ServiceLocator.Initialize(_systemObject.destroyCancellationToken);
            SceneLoader.Initialize(_systemObject.destroyCancellationToken);
            AudioManager.Initialize();

            SymphonyDebugHUD.Initialize();

            GC.Collect();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void GameAfterSceneLoaded()
        {
            _ = SceneLoader.AfterSceneLoad();
        }
    }
}
