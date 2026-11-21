using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace SymphonyFramework.Attribute
{
    /// <summary>
    ///     インスペクターに文字を表示する。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    // 6.x の名前（SymphonyFrameWork.Attribute）を使う利用側スクリプトを、Unity の API Updater が書き換えるための対応。
    [MovedFrom(
        true,
        sourceNamespace: "SymphonyFrameWork.Attribute",
        sourceAssembly: "SymphonyFrameWork")]
    public sealed class DisplayTextAttribute : PropertyAttribute
    {
        #region 外部向けAPI

        /// <summary>
        ///     インスペクターに表示する文字列を指定して属性を生成する。
        /// </summary>
        /// <param name="text"> インスペクターに表示する文字列。 </param>
        public DisplayTextAttribute(string text)
        {
            Text = text;
        }

        /// <summary> インスペクターに表示する文字列。 </summary>
        public string Text { get; private set; }

        #endregion
    }
}
