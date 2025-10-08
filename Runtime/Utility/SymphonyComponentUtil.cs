using UnityEngine;

namespace SymphonyFrameWork
{
    /// <summary>
    ///     コンポーネントに関するユーティリティクラス。
    /// </summary>
    public static class SymphonyComponentUtil
    {
        /// <summary>
        ///     コンポーネントを取得、なければ追加します。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="gameObject"></param>
        /// <summary>
        /// Gets the component of type T from the GameObject, adding and returning a new one if none is present.
        /// </summary>
        /// <returns>The existing component of type T, or a newly added component if none was present.</returns>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }

        /// <summary>
        ///     自身を除く子オブジェクトからコンポーネントを取得します。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="includeInactive"></param>
        /// <summary>
        /// Finds a component of type <typeparamref name="T"/> in the descendants of this transform's immediate children, excluding the transform itself.
        /// </summary>
        /// <param name="self">The transform whose direct children will be searched (the transform itself is not inspected).</param>
        /// <param name="includeInactive">If true, include inactive GameObjects in the search; otherwise ignore inactive GameObjects.</param>
        /// <returns>The first matching component of type <typeparamref name="T"/> found in any descendant of a direct child, or <c>null</c> if none is found.</returns>
        public static T GetComponentInChildrenExcludeSelf<T>(this Transform self,
            bool includeInactive = false) 
            where T : Component
        {
            // Transformを直接たどる方が明確
            foreach (Transform child in self)
            {
                // 子オブジェクトからコンポーネントを検索する。
                T component = child.GetComponentInChildren<T>(includeInactive);

                if (component != null)
                {
                    return component;
                }
            }

            // 見つからなければnull。
            return null;
        }

        /// <summary>
        ///     親を辿ってコンポーネントを取得します。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="transform"></param>
        /// <summary>
        /// Searches upward from the specified transform through its parent hierarchy and returns the first component of type T found on a parent.
        /// </summary>
        /// <typeparam name="T">The component type to search for.</typeparam>
        /// <returns>The first component of type T found on any parent transform, or null if none is found.</returns>
        public static T GetComponentInParents<T>(this Transform transform)
            where T : Component
        {
            Transform parent = transform.parent;

            // 親を辿ってコンポーネントを探す。
            while (parent != null)
            {
                T component = parent.GetComponent<T>();
                if (component != null)
                {
                    return component; // 見つかったら返す。
                }

                parent = parent.parent; // 次の親へ移動。
            }

            return null;
        }
    }
}