using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace SymphonyFramework.System.SaveSystem
{
    /// <summary>
    ///     JsonUtilityとPlayerPrefsを使用するsealedな具象I/Oを提供する。
    /// </summary>
    [Serializable]
    // 型の名前空間とアセンブリ名はSaveDataConfigの[SerializeReference]へ焼かれている。
    // これが無いと、6.x以前に保存したConfigアセットでローダーが解決できなくなる。
    // [MovedFrom]は1つしか付けられないため、2.x以前の旧型名（JsonUtilitySaveDataLoader）での保存は対象外とした。
    [MovedFrom(
        true,
        sourceNamespace: "SymphonyFrameWork.System.SaveSystem",
        sourceAssembly: "SymphonyFrameWork")]
    internal sealed class JsonUtilitySaveDataLoaderStrategy : PlayerPrefsSaveDataLoaderStrategy
    {
        #region 内部処理

        /// <summary>
        ///     セーブデータをJsonUtility形式へ変換する。
        /// </summary>
        protected override string SerializeToJson(Type dataType, SaveDataContent data)
        {
            // Inspectorで確認しやすい保存形式を維持するため、整形済みJSONを生成する。
            return JsonUtility.ToJson(data, true);
        }

        /// <summary>
        ///     JsonUtility形式を既存インスタンスへ上書きする。
        /// </summary>
        protected override void OverwriteFromJson(Type dataType, string json, SaveDataContent data)
        {
            // Registryが保持する参照を維持するため、新規生成せず既存インスタンスへ復元する。
            JsonUtility.FromJsonOverwrite(json, data);
        }

        #endregion
    }
}
