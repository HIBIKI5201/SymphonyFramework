using UnityEditor;

using UnityEngine;
using UnityEngine.UIElements;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     Project Settings画面と管理パネルへ、対応するドキュメントを開くボタンを配置する。
    /// </summary>
    internal static class SymphonyDocumentationGUI
    {
        #region 外部向けAPI

        /// <summary> 管理パネルのUXMLが持つ、ドキュメントを開くボタンの要素名。 </summary>
        internal const string DOCUMENT_BUTTON_NAME = "button-document";

        /// <summary>
        ///     UXMLから生成された管理パネルのボタンへ、ドキュメントを開く操作を結び付ける。
        /// </summary>
        /// <param name="container"> UXMLから生成されたルート要素。 </param>
        /// <param name="page"> ボタンから開くドキュメントページ。 </param>
        /// <remarks>
        ///     購読先のButtonはパネルと寿命を共にするため、匿名ラムダで購読してよい。
        ///     UXMLが差し替わってボタンが無くなっても、パネルの初期化を落とさない。
        /// </remarks>
        internal static void BindOpenButton(VisualElement container, SymphonyDocumentPageEnum page)
        {
            Button button = container?.Q<Button>(DOCUMENT_BUTTON_NAME);

            // UXMLが差し替わってボタンが無い場合も、管理パネル自体の初期化は継続する。
            if (button == null) { return; }

            // ボタンはパネルと寿命を共にするため、匿名ラムダの購読解除は不要とする。
            button.clicked += () => SymphonyDocumentation.Open(page);
        }

        /// <summary>
        ///     ドキュメントを開くボタンを右寄せで描画する。
        /// </summary>
        /// <param name="page"> ボタンから開くドキュメントページ。 </param>
        /// <remarks>
        ///     設定画面の描画は途中で早期returnすることがあるため、呼び出しは各画面の先頭へ置く。
        /// </remarks>
        internal static void DrawOpenButton(SymphonyDocumentPageEnum page)
        {
            // 各設定画面の内容量に左右されず、ドキュメントへの導線を右端へ揃える。
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(BUTTON_LABEL, GUILayout.Width(BUTTON_WIDTH))) { SymphonyDocumentation.Open(page); }
            }

            EditorGUILayout.Space();
        }

        #endregion

        #region 内部処理

        private const string BUTTON_LABEL = "ドキュメントを開く";
        private const float BUTTON_WIDTH = 160f;

        #endregion
    }
}
