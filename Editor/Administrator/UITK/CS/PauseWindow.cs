using SymphonyFrameWork.System;
using SymphonyFrameWork.Utility;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace SymphonyFrameWork.Editor
{
    [UxmlElement]
    public partial class PauseWindow : SymphonyVisualElement
    {
        private FieldInfo _pauseInfo;
        private Label _pauseText;
        private VisualElement _pauseVisual;

        public PauseWindow() : base(
            SymphonyAdministrator.UITK_UXML_PATH + "PauseWindow.uxml",
            InitializeType.None,
            LoadType.AssetDataBase)
        { }

        protected override Task Initialize_S(TemplateContainer container)
        {
            // _pause フィールドを取得
            _pauseInfo = typeof(PauseManager).GetField("_pause", BindingFlags.Static | BindingFlags.NonPublic);

            _pauseVisual = container.Q<VisualElement>("pause");
            _pauseText = container.Q<Label>("pause-text");

            container.Q<Button>("button-pause").clicked += () => PauseManager.Pause = true;
            container.Q<Button>("button-resume").clicked += () => PauseManager.Pause = false;

            return Task.CompletedTask;
        }

        public void Update()
        {
            if (_pauseVisual != null && _pauseInfo != null)
            {
                var active = (bool)_pauseInfo.GetValue(null);
                _pauseVisual.style.backgroundColor = new StyleColor(active ? Color.green : Color.red);
                _pauseText.text = active ? "True" : "False";
            }
        }
    }
}