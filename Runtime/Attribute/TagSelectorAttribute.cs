using System;
using UnityEngine;

namespace SymphonyFrameWork.Attribute
{
    /// <summary>
    ///     String変数をタグのリストから選択できるようにする。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class TagSelectorAttribute : PropertyAttribute
    {
        #region 外部向けAPI

        /// <summary>
        ///     タグ選択欄を表示する属性を生成する。
        /// </summary>
        public TagSelectorAttribute()
        {
        }

        #endregion
    }
}
