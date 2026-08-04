using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;

namespace SymphonyFrameWork.Utility
{
    /// <summary>
    ///     VisualElementをCSで制御するベースクラス
    /// </summary>
    [UxmlElement]
    public abstract partial class SymphonyVisualElement : VisualElement
    {
        /// <summary>
        ///     初期化のタイプ
        /// </summary>
        [Flags]
        public enum InitializeTypeEnum
        {
            None = 0,
            Absolute = 1 << 0,
            FullRangth = 1 << 1,
            PickModeIgnore = 1 << 2,
            All = Absolute | FullRangth | PickModeIgnore
        }

        /// <summary>
        ///     ロードの方法
        /// </summary>
        public enum LoadTypeEnum
        {
            Resources = 0,
            Addressable = 1,
            AssetDataBase = 2
        }

        /// <summary> UXMLのパスと読込方法を指定して非同期初期化を開始する。 </summary>
        /// <param name="path"> 読み込むUXMLアセットのパス。 </param>
        /// <param name="initializeType"> ルート要素へ適用する初期化設定。 </param>
        /// <param name="loadType"> UXMLアセットの読込方法。 </param>
        public SymphonyVisualElement(string path, InitializeTypeEnum initializeType = InitializeTypeEnum.All,
            LoadTypeEnum loadType = LoadTypeEnum.Addressable)
        {
            InitializeTask = Initialize(path, initializeType, loadType);
        }

        /// <summary>
        ///     初期化処理のタスク
        /// </summary>
        public Task InitializeTask { get; private set; }

        /// <summary> Editor側から注入されたAssetDatabase用のUXMLローダー。 </summary>
        internal static Func<string, VisualTreeAsset> EditorAssetLoader { get; set; }

        /// <summary>
        ///     初期化処理
        /// </summary>
        /// <param name="path">UXMLのパス</param>
        /// <param name="type">初期化のタイプ</param>
        /// <param name="loadType"> UXMLアセットの読込方法。 </param>
        /// <returns> UXML読込とサブクラス初期化を表すTask。 </returns>
        private async Task Initialize(string path, InitializeTypeEnum type, LoadTypeEnum loadType)
        {
            VisualTreeAsset treeAsset = default;
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError($"{name} failed initialize");
                return;
            }

            switch (loadType)
            {
                case LoadTypeEnum.Resources:
                    treeAsset = Resources.Load<VisualTreeAsset>(path);
                    break;
                    
                case LoadTypeEnum.Addressable:
                    AsyncOperationHandle<VisualTreeAsset> asyncOperation 
                        = Addressables.LoadAssetAsync<VisualTreeAsset>(path);
                    await asyncOperation.Task;

                    if (asyncOperation.Status == AsyncOperationStatus.Succeeded)
                    {
                        treeAsset = asyncOperation.Result;
                    }
                    else
                    {
                        Debug.LogError($"Failed to load UXML file \nfrom : {path} \nusing Addressables");
                        return;
                    }
                    asyncOperation.Release();
                    break;

                case LoadTypeEnum.AssetDataBase:
#if UNITY_EDITOR
                    treeAsset = EditorAssetLoader?.Invoke(path);
#else
                        Debug.Log("AssetDataBaseを使用したロードはエディタ専用です");
#endif
                    break;
            }


            if (treeAsset != null)
            {
                #region 親エレメントの初期化

                treeAsset.CloneTree(this);

                if ((type & InitializeTypeEnum.PickModeIgnore) != 0)
                {
                    RegisterCallback<KeyDownEvent>(e => e.StopPropagation());
                    pickingMode = PickingMode.Ignore;
                }

                if ((type & InitializeTypeEnum.Absolute) != 0) { style.position = Position.Absolute; }

                if ((type & InitializeTypeEnum.FullRangth) != 0)
                {
                    style.height = Length.Percent(100);
                    style.width = Length.Percent(100);
                }

                #endregion

                // UI要素の取得
                await Initialize_S(this);
            }
            else
            {
                Debug.LogError($"Failed to load UXML file \nfrom : {path}");
            }
        }

        /// <summary>
        ///     サブクラス固有の初期化処理
        /// </summary>
        /// <param name="root">ロードしたUXMLのコンテナ</param>
        /// <returns> サブクラス固有の初期化処理を表すAwaitable。 </returns>
        protected abstract Awaitable Initialize_S(VisualElement root);
    }
}
