using SymphonyFrameWork.Config;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SymphonyFrameWork.System.SceneLoad
{
    public static class SceneResetter
    {
        public static ValueTask ResetScene(SceneLoadManager manager, SceneManagerConfig config, ReadOnlySpan<string> ignores)
        {
            int sceneCount = SceneManager.sceneCount;

            Span<Scene> allScenes = stackalloc Scene[sceneCount];
            Span<Scene> unloadScenes = stackalloc Scene[sceneCount];

            for (int i = 0; i < sceneCount; i++)
            {
                allScenes[i] = SceneManager.GetSceneAt(i);
            }

            int index = 0;
            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = allScenes[i];
                if (ignores.Contain(scene.name)) { continue; }

                unloadScenes[index++] = scene;
            }

            string[] unloadSceneNames = new string[index];
            for (int i = 0; i < index; i++)
            {
                unloadSceneNames[i] = unloadScenes[i].name;
            }
            
            return ConvertTask(manager.UnloadScenes(unloadSceneNames));
        }


        public static ValueTask LoadScene(SceneLoadManager manager, SceneManagerConfig config)
        {
            return ConvertTask(manager.LoadScenes(config.InitializeSceneList));
        }

        /// <summary>
        ///     入れられたシーンを全てアンロードする。
        /// </summary>
        /// <param name="scenes"></param>
        /// <returns></returns>
        private static async ValueTask UnloadScenes(Scene[] scenes)
        {
            AsyncOperation[] tasks = new AsyncOperation[scenes.Length];

            for (int i = 0; i < scenes.Length; i++)
            {
                Scene scene = scenes[i];
                tasks[i] = SceneManager.UnloadSceneAsync(scene);
            }

            foreach (var task in tasks)
            {
                await task;
            }
        }

        /// <summary>
        ///     文字列スパンに特定の文字が含まれているか。
        /// </summary>
        /// <param name="span"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        private static bool Contain(this ReadOnlySpan<string> span, string element)
        {
            for (int i = 0; i < span.Length; i++)
            {
                string s = span[i];
                if (s == element) { return true; }
            }

            return false;
        }

        private static async ValueTask ConvertTask<T>(ValueTask<T> task) => await task;
    }
}
