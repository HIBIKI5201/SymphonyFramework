using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;

namespace SymphonyFrameWork.Utility
{
    /// <summary>
    ///     VisualElementをC#から制御する基底クラスを提供する。
    /// </summary>
    [UxmlElement]
    public abstract partial class SymphonyVisualElement : VisualElement
    {
        #region 外部向けAPI

        /// <summary>
        ///     UXMLのパスと読込方法を指定して非同期初期化を開始する。
        /// </summary>
        /// <param name="path"> 読み込むUXMLアセットのパス。 </param>
        /// <param name="initializeType"> ルート要素へ適用する初期化設定。 </param>
        /// <param name="loadType"> UXMLアセットの読込方法。 </param>
        public SymphonyVisualElement(string path, InitializeTypeEnum initializeType = InitializeTypeEnum.All,
            LoadTypeEnum loadType = LoadTypeEnum.Addressable)
        {
            // コンストラクタをブロックせず、利用側が完了を待機できるTaskとして初期化を開始する。
            InitializeTask = Initialize(path, initializeType, loadType);
        }

        /// <summary> 初期化処理のTask。 </summary>
        public Task InitializeTask { get; private set; }

        /// <summary>
        ///     ルート要素へ適用する初期化設定を表す。
        /// </summary>
        [Flags]
        public enum InitializeTypeEnum
        {
            #region 外部向けAPI

            /// <summary> 初期化設定を適用しない。 </summary>
            None = 0,

            /// <summary> 絶対位置へ設定する。 </summary>
            Absolute = 1 << 0,

            /// <summary> 親要素の全領域へ広げる。 </summary>
            FullRangth = 1 << 1,

            /// <summary> ポインターの判定対象から除外する。 </summary>
            PickModeIgnore = 1 << 2,

            /// <summary> 全ての初期化設定を適用する。 </summary>
            All = Absolute | FullRangth | PickModeIgnore

            #endregion
        }

        /// <summary>
        ///     UXMLアセットの読込方法を表す。
        /// </summary>
        public enum LoadTypeEnum
        {
            #region 外部向けAPI

            /// <summary> Resourcesから読み込む。 </summary>
            Resources = 0,

            /// <summary> Addressablesから読み込む。 </summary>
            Addressable = 1,

            /// <summary> AssetDatabaseから読み込む。 </summary>
            AssetDataBase = 2

            #endregion
        }

        #endregion

        #region 内部処理

        /// <summary> Editor側から注入されたAssetDatabase用のUXMLローダー。 </summary>
        internal static Func<string, VisualTreeAsset> EditorAssetLoader { get; set; }

        /// <summary>
        ///     サブクラス固有の初期化処理を行う。
        /// </summary>
        /// <param name="root"> 読み込んだUXMLのコンテナ。 </param>
        /// <returns> サブクラス固有の初期化処理を表すAwaitable。 </returns>
        protected abstract Awaitable Initialize_S(VisualElement root);

        /// <summary>
        ///     UXMLを読み込み、ルート要素とサブクラスを初期化する。
        /// </summary>
        /// <param name="path"> UXMLのパス。 </param>
        /// <param name="type"> 初期化の種類。 </param>
        /// <param name="loadType"> UXMLアセットの読込方法。 </param>
        /// <returns> UXML読込とサブクラス初期化を表すTask。 </returns>
        private async Task Initialize(string path, InitializeTypeEnum type, LoadTypeEnum loadType)
        {
            VisualTreeAsset treeAsset = default;

            // 読込先を特定できない場合は、実行環境でも確認できるエラーを残して初期化を中止する。
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError($"{name} failed initialize");
                return;
            }

            // 指定された保存方式だけを使用し、異なるローダー間で暗黙のフォールバックを行わない。
            switch (loadType)
            {
                case LoadTypeEnum.Resources:
                    treeAsset = Resources.Load<VisualTreeAsset>(path);
                    break;
                    
                case LoadTypeEnum.Addressable:
                    AsyncOperationHandle<VisualTreeAsset> asyncOperation 
                        = Addressables.LoadAssetAsync<VisualTreeAsset>(path);
                    await asyncOperation.Task;

                    // Addressablesが成功した場合だけ結果を採用し、失敗は実行環境で確認できるエラーにする。
                    if (asyncOperation.Status == AsyncOperationStatus.Succeeded) { treeAsset = asyncOperation.Result; }
                    else
                    {
                        Debug.LogError($"Failed to load UXML file \nfrom : {path} \nusing Addressables");
                        return;
                    }
                    asyncOperation.Release();
                    break;

                case LoadTypeEnum.AssetDataBase:
#if UNITY_EDITOR
                    // RuntimeからUnityEditorを参照せず、Editor側が注入したローダー経由でAssetDatabaseを使う。
                    treeAsset = EditorAssetLoader?.Invoke(path);
#else
                    // PlayerではEditor専用方式を利用できないことを、既存契約どおり通常ログで通知する。
                    Debug.Log("AssetDataBaseを使用したロードはエディタ専用です");
#endif
                    break;
            }

            // 読み込めた場合だけツリーを複製し、ルート設定の完了後にサブクラスを初期化する。
            if (treeAsset != null)
            {
                treeAsset.CloneTree(this);

                // 入力を子へ伝播させず、ルート自身もポインター判定から除外する指定をまとめて適用する。
                if ((type & InitializeTypeEnum.PickModeIgnore) != 0)
                {
                    RegisterCallback<KeyDownEvent>(e => e.StopPropagation());
                    pickingMode = PickingMode.Ignore;
                }

                // 親のレイアウトフローから独立させる指定だけ、絶対位置へ切り替える。
                if ((type & InitializeTypeEnum.Absolute) != 0) { style.position = Position.Absolute; }

                // 親領域全体を使用する指定では、幅と高さを同じタイミングで100%へ揃える。
                if ((type & InitializeTypeEnum.FullRangth) != 0)
                {
                    style.height = Length.Percent(100);
                    style.width = Length.Percent(100);
                }

                // 共通のルート設定が整った後で、派生型にUI要素の取得と初期化を委ねる。
                await Initialize_S(this);
            }
            else
            {
                // 方式固有の処理が結果を返さなかった場合も、実行環境で追跡できるエラーを残す。
                Debug.LogError($"Failed to load UXML file \nfrom : {path}");
            }
        }

        #endregion
    }
}
