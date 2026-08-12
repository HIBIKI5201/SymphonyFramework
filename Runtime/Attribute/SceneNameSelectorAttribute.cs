using System;
using UnityEngine;

namespace SymphonyFrameWork.Attribute
{
    /// <summary>
    ///     String変数をシーンのリストから選択できるようにする。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class SceneNameSelectorAttribute : PropertyAttribute
    {
        #region 外部向けAPI

        /// <summary>
        ///     シーン名選択欄を表示する属性を生成する。
        /// </summary>
        public SceneNameSelectorAttribute()
        {
        }

        #endregion
    }
}
