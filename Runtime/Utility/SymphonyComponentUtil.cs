using UnityEngine;

namespace SymphonyFrameWork
{
    /// <summary>
    ///     コンポーネントに関するユーティリティクラス。
    /// </summary>
    public static class SymphonyComponentUtil
    {
        #region 外部向けAPI

        /// <summary>
        ///     Componentを取得し、存在しない場合は追加する。
        /// </summary>
        /// <typeparam name="T"> 取得または追加するComponentの型。 </typeparam>
        /// <param name="gameObject"> Componentを検索するGameObject。 </param>
        /// <returns> 既存または新たに追加したComponent。 </returns>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();

            // 既存Componentを再利用できない場合だけ追加し、重複生成を避ける。
            if (component == null) { component = gameObject.AddComponent<T>(); }

            return component;
        }

        /// <summary>
        ///     自身を除く子オブジェクトからComponentを取得する。
        /// </summary>
        /// <typeparam name="T"> 検索するComponentの型。 </typeparam>
        /// <param name="self"> 検索起点から除外するTransform。 </param>
        /// <param name="includeInactive"> 非アクティブな子も検索する場合はtrue。 </param>
        /// <returns> 子階層で最初に見つかったComponent。 </returns>
        public static T GetComponentInChildrenExcludeSelf<T>(this Transform self,
            bool includeInactive = false) 
            where T : Component
        {
            // 起点自身を検索対象へ含めないため、直下の子ごとに子孫検索を行う。
            foreach (Transform child in self)
            {
                // Unityは検索起点自身をincludeInactiveに関わらず含めるため、非アクティブな起点を除外する。
                if (!includeInactive && !child.gameObject.activeInHierarchy) { continue; }

                T component = child.GetComponentInChildren<T>(includeInactive);

                // Transformの列挙順で最初に見つかったComponentを返す。
                if (component != null) { return component; }
            }

            // 全ての子階層に対象が無い場合は、未検出をnullで通知する。
            return null;
        }

        /// <summary>
        ///     親を辿ってComponentを取得する。
        /// </summary>
        /// <typeparam name="T"> 検索するComponentの型。 </typeparam>
        /// <param name="transform"> 親方向への検索を開始するTransform。 </param>
        /// <returns> 親階層で最初に見つかったComponent。 </returns>
        public static T GetComponentInParents<T>(this Transform transform)
            where T : Component
        {
            Transform parent = transform.parent;

            // 起点自身を除き、近い親から順に検索する。
            while (parent != null)
            {
                T component = parent.GetComponent<T>();

                // 最も近い親で見つかったComponentを優先する。
                if (component != null) { return component; }

                parent = parent.parent;
            }

            // ルートまで対象が無い場合は、未検出をnullで通知する。
            return null;
        }

        #endregion
    }
}
