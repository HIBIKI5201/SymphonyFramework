using SymphonyFrameWork.System.SaveSystem;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    public sealed class SaveDataDebugState : ScriptableObject
    {
        [SerializeReference]
        private SaveDataContent _data;

        public SaveDataContent GetData() => _data;

        public void SetData(SaveDataContent data)
        {
            _data = data;
        }
    }
}
