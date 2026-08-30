using System;
using UnityEngine;

namespace SymphonyFrameWork.Attribute
{
    /// <summary>
    ///     シリアライズ参照へ代入できる派生型をインスペクターから選択可能にする。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class SubclassSelectorAttribute : PropertyAttribute
    {
        #region 外部向けAPI

        /// <summary>
        ///     MonoBehaviour派生型を候補に含めるか指定して属性を生成する。
        /// </summary>
        /// <param name="includeMono"> MonoBehaviour派生型を候補に含める場合はtrue。 </param>
        /// <param name="filterMethodName">
        ///     候補を絞り込む <c>bool(Type)</c> メソッドの名前。絞り込まない場合はnull。
        /// </param>
        /// <remarks>
        ///     メソッドはフィールドを持つUnityObjectの型か、その基底型へ定義する。
        ///     staticとinstanceのどちらでもよく、アクセス修飾子は問わない。
        ///     未設定へ戻すための先頭の候補はフィルターへ渡さず、常に残る。
        /// </remarks>
        public SubclassSelectorAttribute(bool includeMono = false, string filterMethodName = null)
        {
            _includeMono = includeMono;
            _filterMethodName = filterMethodName;
        }

        /// <summary> 候補を絞り込むメソッドの名前。指定が無い場合はnull。 </summary>
        public string FilterMethodName => _filterMethodName;

        /// <summary>
        ///     MonoBehaviour派生型を候補に含めるかを取得する。
        /// </summary>
        /// <returns> 候補に含める場合はtrue。 </returns>
        public bool IsIncludeMono() => _includeMono;

        #endregion

        #region 内部処理

        private readonly bool _includeMono;

        private readonly string _filterMethodName;

        #endregion
    }
}
