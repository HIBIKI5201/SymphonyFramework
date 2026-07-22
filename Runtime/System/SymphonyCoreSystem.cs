using SymphonyFrameWork.Debugger.HUD;
using SymphonyFrameWork.Config;
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
    ///     SymphonyFrameWorkの管理シーンを持つ、パッケージ全体のComposition Rootです。
    /// </summary>
    internal static class SymphonyCoreSystem
    {
        /// <summary>
        ///     オブジェクトをSymphonySystemシーンに移動する
        /// </summary>
        /// <param name="go"> システムシーンへ移動するGameObject。 </param>
        internal static async void MoveObjectToSymphonySystem(GameObject go)
        {
            //シーンが制作されているか、対象がnullになったら進む
            await SymphonyTask.WaitUntil(() => _systemScene != null || go == null);

            if (go) SceneManager.MoveGameObjectToScene(go, _systemScene.Value);
        }

        /// <summary> SymphonySystemシーンで管理するコンポーネントを生成する。 </summary>
        /// <typeparam name="T"> 生成するコンポーネントの型。 </typeparam>
        /// <returns> 生成したコンポーネント。 </returns>
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
            SaveSystem.SaveSystem.Initialize(
                _systemObject.destroyCancellationToken,
                ResolveSaveDataLoader);

            //各クラスの初期化
            PauseManager.Initialize();
            ServiceLocator.Initialize(_systemObject.destroyCancellationToken);
            SceneLoader.Initialize(_systemObject.destroyCancellationToken);
            AudioManager.Initialize(
                SymphonyConfigLocator.GetConfig<AudioManagerConfig>());

            SymphonyDebugHUD.Initialize();

            GC.Collect();
        }

        /// <summary> 初期シーンのロード後にシーン管理を開始する。 </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void GameAfterSceneLoaded()
        {
            SceneManagerConfig config =
                SymphonyConfigLocator.GetConfig<SceneManagerConfig>();
            _ = SceneLoader.AfterSceneLoad(config);
        }

        /// <summary>
        ///     現在のConfigからセーブデータローダーを解決する。
        /// </summary>
        /// <returns> Configで選択されたローダー。未設定の場合は既定のローダー。 </returns>
        private static SaveDataLoader ResolveSaveDataLoader()
        {
            SaveSystemConfig config =
                SymphonyConfigLocator.GetConfig<SaveSystemConfig>();
            return config?.Loader ?? new JsonUtilitySaveDataLoader();
        }
    }
}
