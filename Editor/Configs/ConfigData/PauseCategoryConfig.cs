using System;
using System.Collections.Generic;

using SymphonyFrameWork.Core;

using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     自動生成するポーズカテゴリーの名前を保持する。
    /// </summary>
    /// <remarks>
    ///     **Project Settings からだけ編集する。** 生成物と同じく利用側プロジェクトの設定であり、
    ///     コードから書き換える経路は持たせない。
    /// </remarks>
    [FilePath(
        EditorSymphonyConstant.PROJCET_SETTING_FILE_PATH + nameof(PauseCategoryConfig) + ".asset",
        FilePathAttribute.Location.ProjectFolder)]
    internal sealed class PauseCategoryConfig : ScriptableSingleton<PauseCategoryConfig>
    {
        #region 外部向けAPI

        /// <summary> 設定されているカテゴリー名。 </summary>
        internal IReadOnlyList<string> CategoryNames =>
            _categoryNames ?? Array.Empty<string>();

        /// <summary>
        ///     カテゴリー名を設定して保存する。
        /// </summary>
        /// <param name="categoryNames"> 設定するカテゴリー名。 </param>
        /// <exception cref="ArgumentNullException"> categoryNamesがnullの場合。 </exception>
        internal void SetCategoryNames(IReadOnlyList<string> categoryNames)
        {
            if (categoryNames == null) { throw new ArgumentNullException(nameof(categoryNames)); }

            // 呼び出し側の配列を後から書き換えられても設定が変わらないよう写す。
            string[] snapshot = new string[categoryNames.Count];
            for (int index = 0; index < categoryNames.Count; index++)
            {
                snapshot[index] = categoryNames[index];
            }

            _categoryNames = snapshot;

            // ScriptableSingletonはSaveを呼ぶまでProjectSettings配下へ書き出さない。
            Save(true);
        }

        #endregion

        #region 内部処理

        [SerializeField, Tooltip("自動生成するポーズカテゴリーの名前。")]
        private string[] _categoryNames = Array.Empty<string>();

        #endregion
    }
}
