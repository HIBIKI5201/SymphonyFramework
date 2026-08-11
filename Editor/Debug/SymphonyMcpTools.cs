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
        #region 外部向けAPI

        /// <summary>
        ///     Service Locatorの初期化状態と登録済みインスタンスをJSONで取得する。
        /// </summary>
        /// <returns> 必ず有効なJSON文字列。 </returns>
        public static string GetServiceLocatorJson()
        {
            bool initialized = false;

            try
            {
                // Edit ModeではRuntime状態が存在しないため、未初期化の固定形式を返す。
                initialized = EditorApplication.isPlaying && ServiceLocator.IsInitialized;
                // Edit ModeまたはComposition初期化前は、Runtime APIへ進まず空の状態を返す。
                if (!initialized)
                {
                    return Serialize(new
                    {
                        initialized = false,
                        registrationCount = 0,
                        registrations = Array.Empty<object>()
                    });
                }

                IReadOnlyList<ServiceRegistrationInfo> registrationInfos =
                    ServiceLocator.GetRegistrationInfos();
                // 外部自動化へ公開してよい診断情報だけを匿名型へ射影する。
                var registrations = registrationInfos
                    .Select(registrationInfo => new
                    {
                        typeName = GetTypeName(registrationInfo.ServiceType),
                        effectiveLocateType = GetLocateType(registrationInfo).ToString(),
                        instanceName = GetInstanceName(registrationInfo.Instance)
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
                // 状態取得の失敗もMCP側で解析できるJSONへ変換し、例外を境界外へ漏らさない。
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
                // Edit ModeではRuntime状態が存在しないため、未初期化の固定形式を返す。
                initialized = EditorApplication.isPlaying && SceneLoader.IsInitialized;
                // Edit ModeまたはComposition初期化前は、Runtime APIへ進まず空の状態を返す。
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
                // 追跡中シーンの公開スナップショットをJSON用の値へ射影する。
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
                // 追跡一覧内でアクティブとされたシーン名を同じスナップショットから求める。
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
                // 状態取得の失敗もMCP側で解析できるJSONへ変換し、例外を境界外へ漏らさない。
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
                // Edit ModeではRuntime状態を検査しないため、未初期化の固定形式を返す。
                initialized = EditorApplication.isPlaying && SaveStore.IsInitialized;
                // Edit ModeまたはComposition初期化前は、セーブ情報へ進まず空の状態を返す。
                if (!initialized)
                {
                    return Serialize(new
                    {
                        initialized = false,
                        entries = Array.Empty<object>()
                    });
                }

                // セーブ内容そのものを除外し、型名と管理状態だけを匿名型へ射影する。
                var entries = SaveStore.GetEntries()
                    .Select(entry => new
                    {
                        typeName = GetTypeName(entry.DataType),
                        saveDate = entry.SaveDate,
                        loaded = entry.IsLoaded
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
                // 状態取得の失敗もMCP側で解析できるJSONへ変換し、例外を境界外へ漏らさない。
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
                // Edit ModeではRuntime状態が存在しないため、未初期化の固定形式を返す。
                initialized = EditorApplication.isPlaying && PauseManager.IsInitialized;
                // Edit ModeまたはComposition初期化前は、Runtime APIへ進まず空の状態を返す。
                if (!initialized)
                {
                    return Serialize(new
                    {
                        initialized = false,
                        paused = false,
                        pausableSubscriberCount = 0
                    });
                }

                PauseInfo pauseInfo = PauseManager.GetPauseInfo();

                // 初期化済みの場合だけ、現在のポーズ状態と購読件数を公開する。
                return Serialize(new
                {
                    initialized = true,
                    paused = pauseInfo.IsPaused,
                    pausableSubscriberCount = pauseInfo.PausableSubscriberCount
                });
            }
            catch (Exception exception)
            {
                // 状態取得の失敗もMCP側で解析できるJSONへ変換し、例外を境界外へ漏らさない。
                return SerializeError(initialized, exception);
            }
        }

        #endregion

        #region 内部処理

        /// <summary>
        ///     型の診断用表示名を取得する。
        /// </summary>
        /// <param name="type"> 表示する型。 </param>
        /// <returns> 型の完全名または短い型名。 </returns>
        private static string GetTypeName(Type type)
        {
            return type?.FullName ?? type?.Name;
        }

        /// <summary>
        ///     登録情報から診断上の実効登録方式を判定する。
        /// </summary>
        /// <param name="registrationInfo"> 登録方式とpayloadを持つ公開スナップショット。 </param>
        /// <returns> Componentでは記録された登録方式、それ以外はLocator。 </returns>
        private static LocateTypeEnum GetLocateType(
            ServiceRegistrationInfo registrationInfo)
        {
            // Componentは階層移動の有無が挙動へ影響するため、記録された登録方式を返す。
            if (registrationInfo.Instance is Component) { return registrationInfo.LocateType; }

            // Component以外はSingleton指定でも階層移動が起きず、Locatorと同じ挙動になる。
            return LocateTypeEnum.Locator;
        }

        /// <summary>
        ///     登録済みインスタンスの診断用表示名を取得する。
        /// </summary>
        /// <param name="instance"> 登録済みインスタンス。 </param>
        /// <returns> ComponentではGameObject名、それ以外ではUnity Object名または型名。 </returns>
        private static string GetInstanceName(object instance)
        {
            // Componentは破棄済み判定をUnityのnull規則で行い、所属GameObject名を表示する。
            if (instance is Component component)
            {
                return component == null ? "(Destroyed)" : component.gameObject.name;
            }

            // その他のUnity Objectも破棄済み状態を区別し、利用可能ならObject名を表示する。
            if (instance is UnityEngine.Object unityObject)
            {
                return unityObject == null ? "(Destroyed)" : unityObject.name;
            }

            // 通常のCLRオブジェクトは名前を持たないため、型名を診断用表示へ使う。
            return instance?.GetType().Name;
        }

        /// <summary>
        ///     指定した読み取り結果をJSONへ変換する。
        /// </summary>
        /// <param name="value"> JSONへ変換する読み取り結果。 </param>
        /// <returns> インデント済みのJSON文字列。 </returns>
        private static string Serialize(object value)
        {
            // 自動化スクリプトから目視確認もしやすいインデント形式で統一する。
            return JsonConvert.SerializeObject(value, Formatting.Indented);
        }

        /// <summary>
        ///     状態読み取り中の失敗を例外送出せずJSONへ変換する。
        /// </summary>
        /// <param name="initialized"> 失敗前に確認できた初期化状態。 </param>
        /// <param name="exception"> 読み取り中に発生した例外。 </param>
        /// <returns> エラーメッセージを含む有効なJSON文字列。 </returns>
        private static string SerializeError(bool initialized, Exception exception)
        {
            try
            {
                // 取得済みの初期化状態と根本例外だけを安定したエラー形式へ変換する。
                return Serialize(new
                {
                    initialized,
                    error = exception.GetBaseException().Message
                });
            }
            catch
            {
                // 例外情報自体をシリアライズできない場合も、JSON契約だけは維持する。
                return "{\"initialized\":false,\"error\":\"State inspection failed.\"}";
            }
        }

        #endregion
    }
}
