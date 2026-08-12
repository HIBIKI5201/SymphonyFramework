using SymphonyFrameWork.Attribute;
using SymphonyFrameWork.System.SaveSystem;
using System;
using UnityEngine;

namespace SymphonyFrameWork
{
    /// <summary>
    ///     セーブデータのシリアライズ方式と保存先を選択する。
    /// </summary>
    /// <remarks>
    ///     利用側コードへ設定型を公開せず、InspectorとProject Settingsだけを編集経路とする。
    /// </remarks>
    [Serializable]
    internal sealed class SaveDataConfig : ScriptableObject
    {
        #region 外部向けAPI

        /// <summary> 現在選択されているセーブデータローダー。 </summary>
        public SaveDataLoaderStrategy Loader => _loader;

        #endregion

        #region 内部処理

        [SerializeReference, SubclassSelector, Tooltip("セーブデータの変換と永続化を担当するローダー。")]
        private SaveDataLoaderStrategy _loader = new JsonUtilitySaveDataLoaderStrategy();

        #endregion
    }
}
