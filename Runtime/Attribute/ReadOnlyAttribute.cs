using System;
using UnityEngine;

namespace SymphonyFrameWork.Attribute
{
    /// <summary>
    ///     インスペクター上で編集不可のプロパティを生成する
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class ReadOnlyAttribute : PropertyAttribute
    {
        /// <summary> 読み取り専用表示を指定する属性を生成する。 </summary>
        public ReadOnlyAttribute()
        {
        }
    }
}
