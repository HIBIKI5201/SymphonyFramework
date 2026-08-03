using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     JsonUtility と PlayerPrefs を利用するセーブデータローダーです。
    /// </summary>
    [Serializable]
    // 旧型名はSaveSystemConfigの[SerializeReference]へ焼かれている。
    // これが無いと既存のConfigアセットでローダーが解決できなくなる。
    [MovedFrom(true, null, null, "JsonUtilitySaveDataLoader")]
    internal sealed class JsonUtilitySaveDataLoaderStrategy : PlayerPrefsSaveDataLoaderStrategy
    {
        /// <summary> Unity JsonUtilityでセーブデータをJSONへ変換する。 </summary>
        protected override string SerializeToJson(Type dataType, SaveDataContent data)
        {
            return JsonUtility.ToJson(data, true);
        }

        /// <summary> Unity JsonUtilityでJSONを既存インスタンスへ上書きする。 </summary>
        protected override void OverwriteFromJson(Type dataType, string json, SaveDataContent data)
        {
            JsonUtility.FromJsonOverwrite(json, data);
        }
    }
}
