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
        /// <param name="filterMethodName">
        ///     候補を絞り込む <c>bool(string)</c> メソッドの名前。絞り込まない場合はnull。
        /// </param>
        /// <remarks>
        ///     メソッドはフィールドを持つUnityObjectの型か、その基底型へ定義する。
        ///     staticとinstanceのどちらでもよく、アクセス修飾子は問わない。
        /// </remarks>
        public SceneNameSelectorAttribute(string filterMethodName = null)
        {
            _filterMethodName = filterMethodName;
        }

        /// <summary> 候補を絞り込むメソッドの名前。指定が無い場合はnull。 </summary>
        public string FilterMethodName => _filterMethodName;

        #endregion

        #region 内部処理

        private readonly string _filterMethodName;

        #endregion
    }
}
