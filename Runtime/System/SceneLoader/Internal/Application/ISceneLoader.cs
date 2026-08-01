using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine.SceneManagement;

namespace SymphonyFrameWork.System.SceneLoad
{
    /// <summary> ApplicationからUnityのScene操作を利用するための最小契約。 </summary>
    internal interface ISceneLoader
    {
        /// <summary> Unityが現在選択しているActive Scene名。 </summary>
        string ActiveSceneName { get; }

        /// <summary> Unityで現在ロード済みのシーン名一覧を返す。 </summary>
        /// <returns> ロード済みシーン名のスナップショット。 </returns>
        IReadOnlyList<string> GetLoadedSceneNames();

        /// <summary> 指定したロード済みSceneを取得する。 </summary>
        /// <param name="sceneName"> シーン名。 </param>
        /// <param name="scene"> 取得できたScene。 </param>
        /// <returns> 有効なロード済みSceneを取得できた場合はtrue。 </returns>
        bool TryGetLoadedScene(string sceneName, out Scene scene);

        /// <summary> 指定したロード済みSceneをActive Sceneにする。 </summary>
        /// <param name="sceneName"> シーン名。 </param>
        /// <returns> 切り替えられた場合はtrue。 </returns>
        bool TrySetActiveScene(string sceneName);

        /// <summary> 指定したSceneをAdditiveでロードする。 </summary>
        /// <param name="sceneName"> シーン名。 </param>
        /// <param name="progress"> 進捗の通知先。 </param>
        /// <param name="token"> 待機を中断するトークン。 </param>
        /// <returns> ロードに成功した場合はtrue。 </returns>
        ValueTask<bool> LoadSceneAsync(
            string sceneName,
            IProgress<float> progress,
            CancellationToken token);

        /// <summary> 指定したSceneをアンロードする。 </summary>
        /// <param name="sceneName"> シーン名。 </param>
        /// <param name="progress"> 進捗の通知先。 </param>
        /// <param name="token"> 待機を中断するトークン。 </param>
        /// <returns> アンロードに成功した場合はtrue。 </returns>
        ValueTask<bool> UnloadSceneAsync(
            string sceneName,
            IProgress<float> progress,
            CancellationToken token);

        /// <summary> ロード済みSceneのルートObjectへ依存注入と非同期初期化を行う。 </summary>
        /// <param name="sceneName"> シーン名。 </param>
        /// <returns> 初期化処理を表すValueTask。 </returns>
        ValueTask InitializeRootObjectsAsync(string sceneName);
    }
}
