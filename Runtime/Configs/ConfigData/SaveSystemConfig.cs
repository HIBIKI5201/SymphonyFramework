using SymphonyFrameWork.Attribute;
using SymphonyFrameWork.System.SaveSystem;
using System;
using UnityEngine;

namespace SymphonyFrameWork
{
    [Serializable]
    public class SaveSystemConfig : ScriptableObject
    {
        public SaveDataLoader Loader => _loader;

        [SerializeReference, SubclassSelector]
        private SaveDataLoader _loader = new JsonUtilitySaveDataLoader();
    }
}
