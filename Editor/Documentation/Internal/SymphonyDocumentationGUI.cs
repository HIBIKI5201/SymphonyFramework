using UnityEditor;

using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     Project Settings画面へ、対応するドキュメントを開くボタンを描画する。
    /// </summary>
    internal static class SymphonyDocumentationGUI
    {
        private const string BUTTON_LABEL = "ドキュメントを開く";
        private const float BUTTON_WIDTH = 160f;

        /// <summary>
        ///     ドキュメントを開くボタンを右寄せで描画する。
        /// </summary>
        /// <param name="page"> ボタンから開くドキュメントページ。 </param>
        /// <remarks>
        ///     設定画面の描画は途中で早期returnすることがあるため、呼び出しは各画面の先頭へ置く。
        /// </remarks>
        internal static void DrawOpenButton(SymphonyDocumentPageEnum page)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(BUTTON_LABEL, GUILayout.Width(BUTTON_WIDTH)))
                {
                    SymphonyDocumentation.Open(page);
                }
            }

            EditorGUILayout.Space();
        }
    }
}
