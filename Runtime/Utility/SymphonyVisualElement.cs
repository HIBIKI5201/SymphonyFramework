using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
        public enum InitializeType
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
        public enum LoadType
        {
            Resources = 0,
            Addressable = 1,
            AssetDataBase = 2
        }

        public SymphonyVisualElement(string path, InitializeType initializeType = InitializeType.All,
            LoadType loadType = LoadType.Resources)
        {
            InitializeTask = Initialize(path, initializeType, loadType);
        }

        /// <summary>
        ///     初期化処理のタスク
        /// </summary>
        public Task InitializeTask { get; private set; }

        /// <summary>
        ///     初期化処理
        /// </summary>
        /// <param name="path">UXMLのパス</param>
        /// <param name="type">初期化のタイプ</param>
        /// <returns></returns>
        private async Task Initialize(string path, InitializeType type, LoadType loadType)
        {
            VisualTreeAsset treeAsset = default;
            if (!string.IsNullOrEmpty(path))
            {
                switch (loadType)
                {
                    case LoadType.Resources:
                        treeAsset = Resources.Load<VisualTreeAsset>(path);
                        break;

                    case LoadType.Addressable:
                        Debug.LogWarning("Addressableは現在使用できません");
                        break;

                    case LoadType.AssetDataBase:
#if UNITY_EDITOR
                        treeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
#else
                        Debug.Log("AssetDataBaseを使用したロードはエディタ専用です");
#endif
                        break;
                }
            }
            else
            {
                Debug.LogError($"{name} failed initialize");
                return;
            }


            if (treeAsset != null)
            {
                #region 親エレメントの初期化

                var container = treeAsset.Instantiate();
                container.style.width = Length.Percent(100);
                container.style.height = Length.Percent(100);

                if ((type & InitializeType.PickModeIgnore) != 0)
                {
                    RegisterCallback<KeyDownEvent>(e => e.StopPropagation());
                    pickingMode = PickingMode.Ignore;

                    container.RegisterCallback<KeyDownEvent>(e => e.StopPropagation());
                    container.pickingMode = PickingMode.Ignore;
                }

                if ((type & InitializeType.Absolute) != 0) style.position = Position.Absolute;

                if ((type & InitializeType.FullRangth) != 0)
                {
                    style.height = Length.Percent(100);
                    style.width = Length.Percent(100);
                }


                hierarchy.Add(container);

                #endregion

                // UI要素の取得
                await Initialize_S(container);
            }
            else
            {
                Debug.LogError($"Failed to load UXML file \nfrom : {path}");
            }
        }

        /// <summary>
        ///     サブクラス固有の初期化処理
        /// </summary>
        /// <param name="container">ロードしたUXMLのコンテナ</param>
        /// <returns></returns>
        protected abstract Task Initialize_S(TemplateContainer container);
    }
}