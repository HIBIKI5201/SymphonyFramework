using SymphonyFrameWork.Attribute;
using SymphonyFrameWork.System.SaveSystem;
using System;
using UnityEngine;

namespace SymphonyFrameWork
{
    [Serializable]
    public class SaveSystemConfig : ScriptableObject
    {
        public ISaveDataLoader Loader => _loader;

        [SerializeReference, SubclassSelector]
        private ISaveDataLoader _loader = new JsonUtilitySaveDataLoader();
    }
}
