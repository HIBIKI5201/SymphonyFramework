using SymphonyFrameWork.Utility;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     enum自動更新設定と手動生成操作を提供する。
    /// </summary>
    [UxmlElement]
    public sealed partial class AutoEnumGeneratorWindow : SymphonyVisualElement
    {
        #region 外部向けAPI

        /// <summary>
        ///     管理パネル用UXMLの非同期初期化を開始する。
        /// </summary>
        /// <remarks>
        ///     UXMLの基準パスはパッケージ導入とAssets直置きの双方を解決する。
        /// </remarks>
        public AutoEnumGeneratorWindow() : base(
            SymphonyAdministrator.UITK_UXML_PATH + "AutoEnumGeneratorWindow.uxml",
            InitializeTypeEnum.None,
            LoadTypeEnum.AssetDataBase)
        { }

        #endregion

        #region 内部処理

        /// <summary>
        ///     自動生成設定のToggleと手動生成Buttonを構成する。
        /// </summary>
        protected override Awaitable Initialize_S(VisualElement container)
        {
            // パネルから対応する利用者向けドキュメントを開けるようにする。
            SymphonyDocumentationGUI.BindOpenButton(container, SymphonyDocumentPageEnum.AutoEnumGenerator);

            // すべてのToggleを同じ設定アセットへ接続し、変更を即時反映する。
            AutoEnumGeneratorConfig config = SymphonyEditorConfigLocator.GetConfig<AutoEnumGeneratorConfig>();

            (Toggle toggle, Button button) sceneList = GetElement("scene");
            sceneList.toggle.value = config.AutoSceneListUpdate;
            sceneList.toggle.RegisterValueChangedCallback(
                evt => config.AutoSceneListUpdate = evt.newValue);
            sceneList.button.clicked += () => AutoEnumGenerator.SceneListEnumGenerate();

            (Toggle toggle, Button button) tags = GetElement("tags");
            tags.toggle.value = config.AutoTagsUpdate;
            tags.toggle.RegisterValueChangedCallback(
                evt => config.AutoTagsUpdate = evt.newValue);
            tags.button.clicked += () => AutoEnumGenerator.TagsEnumGenerate();

            (Toggle toggle, Button button) layers = GetElement("layers");
            layers.toggle.value = config.AutoLayerUpdate;
            layers.toggle.RegisterValueChangedCallback(
                evt => config.AutoLayerUpdate = evt.newValue);
            layers.button.clicked += () => AutoEnumGenerator.LayersEnumGenerate();

            // VisualElementと同じ寿命の匿名ラムダは、Window破棄時に要素ごと解放される。
            return SymphonyAwaitable.Completed();

            // UXML内で同じ名前を持つ操作単位からToggleとButtonを取得する。
            (Toggle toggle, Button button) GetElement(string name) =>
                container.Q<VisualElement>(name) switch
                    { VisualElement ve => (ve.Q<Toggle>(), ve.Q<Button>()) };
        }

        #endregion
    }
}
