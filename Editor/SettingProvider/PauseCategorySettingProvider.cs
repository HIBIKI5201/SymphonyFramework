using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

namespace SymphonyFrameWork.Editor.SettingProvider
{
    /// <summary>
    ///     ポーズカテゴリーのProject Settings画面を提供する。
    /// </summary>
    /// <remarks>
    ///     **`internal` にしている。** 画面の描画は自動で検証できず、テストを置けない。
    ///     公開型にすると、テスト未実装の公開型の一覧を増やすことになる。
    ///     生成のロジックは <see cref="PauseCategoryGenerator" /> 側にあり、そちらは検証している。
    /// </remarks>
    internal sealed class PauseCategorySettingProvider
    {
        #region 外部向けAPI

        /// <summary> Project Settingsに表示する設定項目名。 </summary>
        internal const string LABEL = "Pause Category";

        /// <summary> SettingsProviderの完全な設定パス。 </summary>
        internal const string SELF_PATH = SymphonySettingProvider.PROVIDER_PATH + LABEL;

        /// <summary>
        ///     ポーズカテゴリー設定用のSettingsProviderを生成する。
        /// </summary>
        /// <returns> Project Settingsへ登録する設定画面。 </returns>
        [SettingsProvider]
        internal static SettingsProvider CreateCustomSettingsProvider()
        {
            SettingsProvider provider = new(SELF_PATH, SettingsScope.Project)
            {
                label = LABEL,
                guiHandler = IMGUI,
                activateHandler = (_, _) => ReloadCategoryNames(),
                keywords = new HashSet<string>(new[]
                {
                    "pause", "category", "pausable", "generate", "interface",
                }),
            };

            return provider;
        }

        #endregion

        #region 内部処理

        /// <summary> 編集中のカテゴリー名。保存するまでConfigへ書き戻さない。 </summary>
        private static List<string> _categoryNames = new();

        /// <summary>
        ///     Configから編集中の値を読み直す。
        /// </summary>
        private static void ReloadCategoryNames()
        {
            _categoryNames = new List<string>(PauseCategoryConfig.instance.CategoryNames);
        }

        /// <summary>
        ///     カテゴリー名の一覧と生成ボタンを描画する。
        /// </summary>
        /// <param name="searchContext"> Project Settingsの検索文字列。 </param>
        private static void IMGUI(string searchContext)
        {
            SymphonyDocumentationGUI.DrawOpenButton(SymphonyDocumentPageEnum.EditorTools);

            EditorGUILayout.HelpBox(
                "カテゴリーごとにポーズ状態を分けられます。"
                + $" 設定した名前から、{PauseCategoryGenerator.GeneratePath} へ"
                + " PauseManager.IPausable を継承した空のinterfaceを生成します。",
                MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Categories", EditorStyles.boldLabel);

            DrawCategoryNames();
            DrawListButtons();

            EditorGUILayout.Space();
            DrawSaveAndGenerate();
        }

        /// <summary>
        ///     カテゴリー名の入力欄と、生成されるinterface名の予告を描画する。
        /// </summary>
        /// <remarks>
        ///     **生成される名前をその場で見せる。** 入力した名前がそのまま
        ///     <c>I</c> と <c>Pausable</c> で挟まれるため、結果を見ないと想定と違う名前になりやすい。
        /// </remarks>
        private static void DrawCategoryNames()
        {
            for (int index = 0; index < _categoryNames.Count; index++)
            {
                EditorGUILayout.BeginHorizontal();
                _categoryNames[index] = EditorGUILayout.TextField(
                    (index + 1).ToString("00"),
                    _categoryNames[index]);

                string interfaceName = PauseCategoryGenerator.ResolveInterfaceName(_categoryNames[index]);
                EditorGUILayout.LabelField(
                    interfaceName ?? "（識別子として使えません）",
                    EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }

            if (_categoryNames.Count == 0)
            {
                EditorGUILayout.HelpBox("カテゴリーがまだ登録されていません。", MessageType.None);
            }
        }

        /// <summary>
        ///     カテゴリーの追加と削除のボタンを描画する。
        /// </summary>
        private static void DrawListButtons()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("＋ カテゴリーを追加")) { _categoryNames.Add(string.Empty); }

            using (new EditorGUI.DisabledScope(_categoryNames.Count == 0))
            {
                if (GUILayout.Button("－ 最後のカテゴリーを削除"))
                {
                    _categoryNames.RemoveAt(_categoryNames.Count - 1);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        ///     保存と生成のボタンを描画する。
        /// </summary>
        /// <remarks>
        ///     **設定から消したカテゴリーのファイルは削除しない。**
        ///     実装しているコードがある場合、黙って消すとコンパイルが壊れるためである。
        /// </remarks>
        private static void DrawSaveAndGenerate()
        {
            if (!GUILayout.Button("設定を保存してinterfaceを生成")) { return; }

            PauseCategoryConfig.instance.SetCategoryNames(_categoryNames);
            PauseCategoryGenerator.Generate();
        }

        #endregion
    }
}
