using SymphonyFrameWork.Attribute;
using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    public class AutoEnumGeneratorConfig : ScriptableObject
    {
        [ReadOnly, SerializeField] private bool _autoSceneListUpdate = true;
        public bool AutoSceneListUpdate 
        {
            get => _autoSceneListUpdate;
            set
            {
                _autoSceneListUpdate = value;
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            } 
        }

        [ReadOnly, SerializeField] private bool _autoTagsUpdate = false;

        public bool AutoTagsUpdate
        {
            get => _autoTagsUpdate;
            set
            {
                _autoTagsUpdate = value;
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }
        }

        [ReadOnly, SerializeField] private bool _autoLayersUpdate = false;

        public bool AutoLayerUpdate
        {
            get => _autoLayersUpdate;
            set
            {
                _autoLayersUpdate = value;
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }
        }
    }
}