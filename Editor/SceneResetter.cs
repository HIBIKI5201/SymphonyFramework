using SymphonyFrameWork.Config;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SymphonyFrameWork.Editor
{
    public static class SceneResetter
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetAndLoadSceneOnPlay()
        {
#if UNITY_EDITOR
            // EditorConfigLocator を通じて SceneManagerConfig を取得
            var config = SymphonyEditorConfigLocator.GetConfig<SceneManagerConfig>();

            // コンフィグが存在しない、または機能が有効でない場合は何もしない
            if (config == null || !config.IsResetAndLoadOnPlay)
            {
                return;
            }

            // ロードすべきシーンのリストを取得
            var initializeSceneList = config.InitializeSceneList;
            if (initializeSceneList == null || initializeSceneList.Count == 0 || string.IsNullOrEmpty(initializeSceneList.First()))
            {
                return;
            }

            // 最初のシーンをシングルモードでロード（現在のシーンを置き換える）
            string firstScenePath = initializeSceneList.First();
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(firstScenePath, new LoadSceneParameters(LoadSceneMode.Single));

            // 2つ目以降のシーンを追加モードでロード
            foreach (var scenePath in initializeSceneList.Skip(1))
            {
                if (!string.IsNullOrEmpty(scenePath))
                {
                    UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Additive));
                }
            }
#endif
        }
    }
}
