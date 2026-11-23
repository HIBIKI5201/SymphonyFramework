using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace SymphonyFramework.Attribute
{
    /// <summary>
    ///     インスペクター上で編集不可のプロパティを生成する。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    // 6.x の名前（SymphonyFrameWork.Attribute）を使う利用側スクリプトを、Unity の API Updater が書き換えるための対応。
    [MovedFrom(
        true,
        sourceNamespace: "SymphonyFrameWork.Attribute",
        sourceAssembly: "SymphonyFrameWork")]
    public sealed class ReadOnlyAttribute : PropertyAttribute
    {
        #region 外部向けAPI

        /// <summary>
        ///     読み取り専用表示を指定する属性を生成する。
        /// </summary>
        public ReadOnlyAttribute()
        {
        }

        #endregion
    }
}
