using Newtonsoft.Json;
using System;
using UnityEngine.Scripting.APIUpdating;

namespace SymphonyFramework.System.SaveSystem
{
    /// <summary>
    ///     Newtonsoft.JsonとPlayerPrefsを使用するsealedな具象I/Oを提供する。
    /// </summary>
    [Serializable]
    // 型の名前空間とアセンブリ名はSaveDataConfigの[SerializeReference]へ焼かれている。
    // これが無いと、6.x以前に保存したConfigアセットでローダーが解決できなくなる。
    // [MovedFrom]は1つしか付けられないため、2.x以前の旧型名（NewtonsoftSaveDataLoader）での保存は対象外とした。
    [MovedFrom(
        true,
        sourceNamespace: "SymphonyFrameWork.System.SaveSystem",
        sourceAssembly: "SymphonyFrameWork")]
    internal sealed class NewtonsoftSaveDataLoaderStrategy : PlayerPrefsSaveDataLoaderStrategy
    {
        #region 内部処理

        private static readonly JsonSerializerSettings _settings = new()
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace
        };

        /// <summary>
        ///     セーブデータをNewtonsoft.Json形式へ変換する。
        /// </summary>
        protected override string SerializeToJson(Type dataType, SaveDataContent data)
        {
            // 保存内容を確認しやすい形式に保つため、インデント付きJSONを生成する。
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }

        /// <summary>
        ///     Newtonsoft.Json形式を既存インスタンスへ上書きする。
        /// </summary>
        protected override void OverwriteFromJson(Type dataType, string json, SaveDataContent data)
        {
            // 保存されていないコレクション要素を残さないよう、既存値を置換して復元する。
            JsonConvert.PopulateObject(json, data, _settings);
        }

        #endregion
    }
}
