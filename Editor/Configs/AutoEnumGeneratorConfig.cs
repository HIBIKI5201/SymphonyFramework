using SymphonyFrameWork.Core;
using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    [FilePath(EditorSymphonyConstant.PROJCET_SETTING_FILE_PATH + nameof(AutoEnumGeneratorConfig) + ".asset", FilePathAttribute.Location.ProjectFolder)]
    public class AutoEnumGeneratorConfig : ScriptableSingleton<AutoEnumGeneratorConfig>
    {
        public bool AutoSceneListUpdate
        {
            get => _autoSceneListUpdate;
            set
            {
                _autoSceneListUpdate = value;
                Save();
            }
        }

        public bool AutoTagsUpdate
        {
            get => _autoTagsUpdate;
            set
            {
                _autoTagsUpdate = value;
                Save();
            }
        }

        public bool AutoLayerUpdate
        {
            get => _autoLayersUpdate;
            set
            {
                _autoLayersUpdate = value;
                Save();
            }
        }

        [SerializeField] private bool _autoSceneListUpdate = true;
        [SerializeField] private bool _autoTagsUpdate = false;
        [SerializeField] private bool _autoLayersUpdate = false;

        private void Save() => Save(true);
    }
}