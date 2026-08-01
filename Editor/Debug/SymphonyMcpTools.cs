using System;
using System.Collections.Generic;
using System.Linq;

using SymphonyFrameWork.System;
using SymphonyFrameWork.System.SaveSystem;
using SymphonyFrameWork.System.SceneLoad;
using SymphonyFrameWork.System.ServiceLocate;

using UnityEditor;
using UnityEngine;

using Newtonsoft.Json;

namespace SymphonyFrameWork.Editor.Debugger
{
    /// <summary>
    ///     MCPや自動化スクリプトからSymphonyのランタイム状態をJSONで読み取るためのツール群。
    /// </summary>
    public static class SymphonyMcpTools
    {
        /// <summary>
        ///     Service Locatorの初期化状態と登録済みインスタンスをJSONで取得する。
        /// </summary>
        /// <returns> 必ず有効なJSON文字列。 </returns>
        public static string GetServiceLocatorJson()
        {
            bool initialized = false;

            try
            {
                initialized = EditorApplication.isPlaying && ServiceLocator.IsInitialized;
                if (!initialized)
                {
                    return Serialize(new
                    {
                        initialized = false,
                        registrationCount = 0,
                        registrations = Array.Empty<object>()
                    });
                }

                IReadOnlyDictionary<Type, object> registeredInstances = ServiceLocator.RegisteredInstances;
                var registrations = registeredInstances
                    .Select(pair => new
                    {
                        typeName = GetTypeName(pair.Key),
                        effectiveLocateType = GetLocateType(pair.Value).ToString(),
                        instanceName = GetInstanceName(pair.Value)
                    })
                    .ToArray();

                return Serialize(new
                {
                    initialized = true,
                    registrationCount = registrations.Length,
                    registrations
                });
            }
            catch (Exception exception)
            {
                return SerializeError(initialized, exception);
            }
        }

        /// <summary>
        ///     Scene Loaderの初期化状態、追跡中シーン、アクティブシーンをJSONで取得する。
        /// </summary>
        /// <returns> 必ず有効なJSON文字列。 </returns>
        public static string GetSceneLoaderJson()
        {
            bool initialized = false;

            try
            {
                initialized = EditorApplication.isPlaying && SceneLoader.IsInitialized;
                if (!initialized)
                {
                    return Serialize(new
                    {
                        initialized = false,
                        scenes = Array.Empty<object>(),
                        activeSceneName = (string)null
                    });
                }

                IReadOnlyList<SceneLoadInfo> sceneInfos = SceneLoader.GetSceneInfos();
                var scenes = sceneInfos
                    .Select(sceneInfo => new
                    {
                        name = sceneInfo.SceneName,
                        state = sceneInfo.State.ToString(),
                        priority = sceneInfo.Priority,
                        progress = sceneInfo.Progress,
                        isActive = sceneInfo.IsActive
                    })
                    .ToArray();
                string activeSceneName = sceneInfos
                    .FirstOrDefault(sceneInfo => sceneInfo.IsActive)
                    .SceneName;

                return Serialize(new
                {
                    initialized = true,
                    scenes,
                    activeSceneName
                });
            }
            catch (Exception exception)
            {
                return SerializeError(initialized, exception);
            }
        }

        /// <summary>
        ///     Save Data Registryの初期化状態と機微な内容を除いた登録情報をJSONで取得する。
        /// </summary>
        /// <returns> 必ず有効なJSON文字列。 </returns>
        public static string GetSaveDataJson()
        {
            bool initialized = false;

            try
            {
                initialized = EditorApplication.isPlaying && SaveDataRegistry.IsInitialized;
                if (!initialized)
                {
                    return Serialize(new
                    {
                        initialized = false,
                        entries = Array.Empty<object>()
                    });
                }

                HashSet<Type> loadedTypes = new(SaveDataRegistry.LoadedTypes);
                var entries = SaveDataRegistry.GetEntries()
                    .Select(entry => new
                    {
                        typeName = GetTypeName(entry.DataType),
                        saveDate = entry.SaveDate,
                        loaded = loadedTypes.Contains(entry.DataType)
                    })
                    .ToArray();

                return Serialize(new
                {
                    initialized = true,
                    entries
                });
            }
            catch (Exception exception)
            {
                return SerializeError(initialized, exception);
            }
        }

        /// <summary>
        ///     Pause Managerの初期化状態、ポーズ状態、IPausable購読件数をJSONで取得する。
        /// </summary>
        /// <returns> 必ず有効なJSON文字列。 </returns>
        public static string GetPauseJson()
        {
            bool initialized = false;

            try
            {
                initialized = EditorApplication.isPlaying && PauseManager.IsInitialized;
                if (!initialized)
                {
                    return Serialize(new
                    {
                        initialized = false,
                        paused = false,
                        pausableSubscriberCount = 0
                    });
                }

                return Serialize(new
                {
                    initialized = true,
                    paused = PauseManager.IsPaused,
                    pausableSubscriberCount = PauseManager.PausableSubscriberCount
                });
            }
            catch (Exception exception)
            {
                return SerializeError(initialized, exception);
            }
        }

        /// <summary> 型の完全名を取得し、完全名が無い型では短い型名を返す。 </summary>
        /// <param name="type"> 表示する型。 </param>
        /// <returns> 型の完全名または短い型名。 </returns>
        private static string GetTypeName(Type type)
        {
            return type?.FullName ?? type?.Name;
        }

        /// <summary>
        ///     登録済みインスタンスに実際に適用されている登録方式を判定する。
        ///     Service Locatorは登録時に渡された<see cref="LocateType"/>を保持しないため、
        ///     これは記録値ではなく現在の状態から導いた**実効値**である。
        ///     Componentでない登録は<see cref="LocateType.Singleton"/>を指定しても
        ///     階層移動が起きず両者の挙動が同じになるため、常にLocatorとして扱う。
        /// </summary>
        /// <param name="instance"> 登録済みインスタンス。 </param>
        /// <returns> ComponentがLocator所有階層にある場合はSingleton、それ以外はLocator。 </returns>
        private static LocateType GetLocateType(object instance)
        {
            Transform singletonRoot = ServiceLocator.SingletonRoot;
            if (instance is Component component
                && component != null
                && singletonRoot != null
                && component.transform.parent == singletonRoot)
            {
                return LocateType.Singleton;
            }

            return LocateType.Locator;
        }

        /// <summary> 登録済みインスタンスの診断用表示名を取得する。 </summary>
        /// <param name="instance"> 登録済みインスタンス。 </param>
        /// <returns> ComponentではGameObject名、それ以外ではUnity Object名または型名。 </returns>
        private static string GetInstanceName(object instance)
        {
            if (instance is Component component)
            {
                return component == null ? "(Destroyed)" : component.gameObject.name;
            }

            if (instance is UnityEngine.Object unityObject)
            {
                return unityObject == null ? "(Destroyed)" : unityObject.name;
            }

            return instance?.GetType().Name;
        }

        /// <summary> 指定した読み取り結果をJSONへ変換する。 </summary>
        /// <param name="value"> JSONへ変換する読み取り結果。 </param>
        /// <returns> インデント済みのJSON文字列。 </returns>
        private static string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value, Formatting.Indented);
        }

        /// <summary> 状態読み取り中の失敗を例外送出せずJSONへ変換する。 </summary>
        /// <param name="initialized"> 失敗前に確認できた初期化状態。 </param>
        /// <param name="exception"> 読み取り中に発生した例外。 </param>
        /// <returns> エラーメッセージを含む有効なJSON文字列。 </returns>
        private static string SerializeError(bool initialized, Exception exception)
        {
            try
            {
                return Serialize(new
                {
                    initialized,
                    error = exception.GetBaseException().Message
                });
            }
            catch
            {
                return "{\"initialized\":false,\"error\":\"State inspection failed.\"}";
            }
        }
    }
}
