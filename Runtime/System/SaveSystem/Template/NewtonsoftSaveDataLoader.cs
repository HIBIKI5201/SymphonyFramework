using Newtonsoft.Json;
using System;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     Newtonsoft.Json と PlayerPrefs を利用するセーブデータローダーです。
    /// </summary>
    [Serializable]
    public class NewtonsoftSaveDataLoader : PlayerPrefsSaveDataLoader
    {
        protected override string SerializeToJson(Type dataType, SaveDataContent data)
        {
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }

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
