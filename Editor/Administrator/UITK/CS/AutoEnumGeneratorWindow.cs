using SymphonyFrameWork.Utility;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace SymphonyFrameWork.Editor
{
    [UxmlElement]
    public partial class AutoEnumGeneratorWindow : SymphonyVisualElement
    {
        public AutoEnumGeneratorWindow() : base(
            SymphonyAdministrator.UITK_UXML_PATH + "AutoEnumGeneratorWindow.uxml",
            InitializeType.None,
            LoadType.AssetDataBase)
        { }
        protected override Task Initialize_S(TemplateContainer container)
        {
            //コンフィグデータを取得
            var config = SymphonyEditorConfigLocator.GetConfig<AutoEnumGeneratorConfig>();

            var sceneList = GetElement("scene");
            sceneList.toggle.value = config.AutoSceneListUpdate;
            sceneList.toggle.RegisterValueChangedCallback(
                evt => config.AutoSceneListUpdate = evt.newValue);
            sceneList.button.clicked += () => AutoEnumGenerator.SceneListEnumGenerate();

            var tags = GetElement("tags");
            tags.toggle.value = config.AutoTagsUpdate;
            tags.toggle.RegisterValueChangedCallback(
                evt => config.AutoTagsUpdate = evt.newValue);
            tags.button.clicked += () => AutoEnumGenerator.TagsEnumGenerate();

            var layers = GetElement("layers");
            layers.toggle.value = config.AutoLayerUpdate;
            layers.toggle.RegisterValueChangedCallback(
                evt => config.AutoLayerUpdate = evt.newValue);
            layers.button.clicked += () => AutoEnumGenerator.LayersEnumGenerate();

            return Task.CompletedTask;

            (Toggle toggle, Button button) GetElement(string name) =>
                container.Q<VisualElement>(name) switch
                    { VisualElement ve => (ve.Q<Toggle>(), ve.Q<Button>()) };
        }
    }
}
