using Newtonsoft.Json;
using System;
using UnityEngine.Scripting.APIUpdating;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     Newtonsoft.Json と PlayerPrefs を利用するセーブデータローダーです。
    /// </summary>
    [Serializable]
    // 旧型名はSaveDataConfigの[SerializeReference]へ焼かれている。
    // これが無いと既存のConfigアセットでローダーが解決できなくなる。
    [MovedFrom(true, null, null, "NewtonsoftSaveDataLoader")]
    internal sealed class NewtonsoftSaveDataLoaderStrategy : PlayerPrefsSaveDataLoaderStrategy
    {
        /// <summary> Newtonsoft.JsonでセーブデータをJSONへ変換する。 </summary>
        protected override string SerializeToJson(Type dataType, SaveDataContent data)
        {
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }

        /// <summary> Newtonsoft.JsonでJSONを既存インスタンスへ上書きする。 </summary>
        protected override void OverwriteFromJson(Type dataType, string json, SaveDataContent data)
        {
            JsonConvert.PopulateObject(json, data, _settings);
        }

        private static readonly JsonSerializerSettings _settings = new()
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace
        };
    }
}
