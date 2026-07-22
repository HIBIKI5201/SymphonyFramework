using System;
using UnityEngine;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     JsonUtility と PlayerPrefs を利用するセーブデータローダーです。
    /// </summary>
    [Serializable]
    public class JsonUtilitySaveDataLoader : PlayerPrefsSaveDataLoader
    {
        protected override string SerializeToJson(Type dataType, SaveDataContent data)
        {
            return JsonUtility.ToJson(data, true);
        }

        protected override void OverwriteFromJson(Type dataType, string json, SaveDataContent data)
        {
            JsonUtility.FromJsonOverwrite(json, data);
        }
    }
}
