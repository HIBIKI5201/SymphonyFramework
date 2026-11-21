using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using SymphonyFramework.Core;
using SymphonyFramework.Debugger.Logger;

using UnityEditor;

namespace SymphonyFramework.Editor
{
    /// <summary>
    ///     6.x以前の表記（SymphonyFrameWork）で作られた利用側の生成物を、現在の表記へ移す。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Windows と macOS では大文字小文字だけが違うフォルダを同じものとして開けるため、移さなくても動いて見える。
    ///         Linux では別のフォルダになり、旧フォルダの設定が読まれず既定値の設定が新しく作られる。
    ///         **エラーもログも出ないため、設定アセットを作る前に必ず移す。**
    ///     </para>
    ///     <para>
    ///         大文字小文字だけの改名は1回では反映されない環境があるため、一時名を経由する2段階で行う。
    ///         Assets配下は <see cref="AssetDatabase.MoveAsset" /> を使い、.metaとGUIDを保つ。
    ///     </para>
    ///     <para>
    ///         移動の計画は <see cref="PlanFolderMoves" /> と <see cref="PlanFileRenames" /> の純粋な関数で組み立て、
    ///         Unity APIを呼ぶ実行部分から切り離してテストする。
    ///     </para>
    /// </remarks>
    internal static class RootFolderCaseMigration
    {
        #region 外部向けAPI

        /// <summary> 6.x以前のフォルダ名・アセンブリ名の表記。 </summary>
        internal const string LEGACY_NAME = "SymphonyFrameWork";

        /// <summary> 大文字小文字だけの改名で経由する一時名の接尾辞。 </summary>
        internal const string TEMPORARY_SUFFIX = "__case_migration";

        /// <summary>
        ///     旧表記のフォルダと生成物を現在の表記へ移す。
        /// </summary>
        /// <returns> Assets配下を変更した場合はtrue。 </returns>
        /// <exception cref="InvalidOperationException">
        ///     旧表記と現在の表記が別のフォルダとして両方ある場合、または移動に失敗した場合。
        ///     半端な状態のまま設定アセットを新規作成させないため、初期化を止める。
        /// </exception>
        internal static bool Run()
        {
            bool hasAssetChanges = false;

            // AssetDatabase管理下の生成物フォルダを先に移し、続けて中の生成物の表記を揃える。
            foreach (string parent in AssetParents)
            {
                hasAssetChanges |= MoveAssetFolder(parent);
            }

            hasAssetChanges |= RenameGeneratedFiles(EditorSymphonyConstant.ENUM_PATH);

            // UserSettingsはAssetDatabaseの管理外なので、ファイルシステムで同じ2段階の改名をする。
            MoveFileSystemFolder("UserSettings");

            return hasAssetChanges;
        }

        /// <summary>
        ///     1つの親フォルダについて、旧表記の子フォルダを現在の表記へ移す手順を組み立てる。
        /// </summary>
        /// <param name="parent"> 親フォルダのパス（例: <c>Assets/Resources</c>）。 </param>
        /// <param name="childNames"> 親フォルダ直下に実在するフォルダ名。ファイルシステムが返す表記のまま渡す。 </param>
        /// <returns> 移動の手順。移すものが無ければ空の手順。 </returns>
        internal static MigrationPlan PlanFolderMoves(string parent, IReadOnlyCollection<string> childNames)
        {
            if (parent == null) { throw new ArgumentNullException(nameof(parent)); }
            if (childNames == null) { throw new ArgumentNullException(nameof(childNames)); }

            // 旧表記がそのままの綴りで存在する場合だけ移す。現在の表記で返る環境では移行済みとみなす。
            if (!childNames.Contains(LEGACY_NAME, StringComparer.Ordinal)) { return MigrationPlan.Empty; }

            string legacyPath = $"{parent}/{LEGACY_NAME}";
            string currentPath = $"{parent}/{SymphonyConstant.SYMPHONY_FRAMEWORK}";

            // 両方が別のフォルダとして返るのは大文字小文字を区別する環境だけで、自動では統合しない。
            if (childNames.Contains(SymphonyConstant.SYMPHONY_FRAMEWORK, StringComparer.Ordinal))
            {
                return MigrationPlan.Conflict(legacyPath, currentPath);
            }

            return MigrationPlan.FromSteps(CreateTwoStepMove(legacyPath, currentPath));
        }

        /// <summary>
        ///     生成物フォルダ内で、ファイル名に旧表記を含むものを現在の表記へ改名する手順を組み立てる。
        /// </summary>
        /// <param name="filePaths"> 生成物フォルダ内のファイルパス。.metaは含めない。 </param>
        /// <returns> 改名の手順。 </returns>
        internal static MigrationPlan PlanFileRenames(IEnumerable<string> filePaths)
        {
            if (filePaths == null) { throw new ArgumentNullException(nameof(filePaths)); }

            List<MigrationStep> steps = new();
            foreach (string path in filePaths.OrderBy(path => path, StringComparer.Ordinal))
            {
                string fileName = Path.GetFileName(path);

                // 旧表記を含まないファイルは、生成物でも触らない。
                if (fileName.IndexOf(LEGACY_NAME, StringComparison.Ordinal) < 0) { continue; }

                string directory = Path.GetDirectoryName(path)?.Replace('\\', '/');
                string newName = ReplaceLegacyName(fileName);
                string newPath = string.IsNullOrEmpty(directory) ? newName : $"{directory}/{newName}";
                steps.AddRange(CreateTwoStepMove(path, newPath));
            }

            return MigrationPlan.FromSteps(steps);
        }

        /// <summary>
        ///     生成物の本文に含まれる旧表記を現在の表記へ置き換える。
        /// </summary>
        /// <param name="text"> 生成物の本文。 </param>
        /// <returns> 置き換えた本文。 </returns>
        /// <remarks>
        ///     生成asmdefの <c>name</c> と、ポーズカテゴリーの生成コードの <c>using</c> が対象になる。
        /// </remarks>
        internal static string ReplaceLegacyName(string text)
        {
            if (text == null) { throw new ArgumentNullException(nameof(text)); }

            return text.Replace(LEGACY_NAME, SymphonyConstant.SYMPHONY_FRAMEWORK, StringComparison.Ordinal);
        }

        #endregion

        #region 内部処理

        /// <summary> 旧表記のフォルダを持ちうるAssets配下の親フォルダ。 </summary>
        private static readonly string[] AssetParents =
        {
            "Assets/Resources",
            "Assets/Scripts",
            "Assets/Editor",
        };

        /// <summary> 本文を置き換える生成物の拡張子。 </summary>
        private static readonly string[] GeneratedTextExtensions = { ".asmdef", ".cs" };

        /// <summary>
        ///     一時名を経由する2段階の移動を作る。
        /// </summary>
        /// <param name="from"> 移動元。 </param>
        /// <param name="to"> 移動先。 </param>
        /// <returns> 移動元→一時名→移動先の2手順。 </returns>
        private static MigrationStep[] CreateTwoStepMove(string from, string to)
        {
            string temporary = to + TEMPORARY_SUFFIX;
            return new[] { new MigrationStep(from, temporary), new MigrationStep(temporary, to) };
        }

        /// <summary>
        ///     Assets配下の旧表記フォルダを移す。
        /// </summary>
        /// <param name="parent"> 親フォルダのパス。 </param>
        /// <returns> 移動した場合はtrue。 </returns>
        private static bool MoveAssetFolder(string parent)
        {
            // 親フォルダが無いプロジェクトでは、生成物もまだ作られていない。
            if (!Directory.Exists(parent)) { return false; }

            MigrationPlan plan = PlanFolderMoves(parent, GetChildDirectoryNames(parent));
            ThrowIfConflict(plan);
            if (plan.Steps.Count == 0) { return false; }

            ExecuteAssetSteps(plan);
            return true;
        }

        /// <summary>
        ///     生成物フォルダ内の旧表記のファイル名と本文を、現在の表記へ揃える。
        /// </summary>
        /// <param name="folder"> 生成物フォルダのパス。 </param>
        /// <returns> 変更した場合はtrue。 </returns>
        private static bool RenameGeneratedFiles(string folder)
        {
            if (!Directory.Exists(folder)) { return false; }

            bool hasAssetChanges = false;
            string[] files = Directory
                .GetFiles(folder, "*", SearchOption.AllDirectories)
                .Select(path => path.Replace('\\', '/'))
                .Where(path => !path.EndsWith(".meta", StringComparison.Ordinal))
                .ToArray();

            // 本文を先に書き換え、改名後のasmdefが旧アセンブリ名を名乗らないようにする。
            foreach (string path in files.Where(IsGeneratedTextFile))
            {
                string text = File.ReadAllText(path);
                string replaced = ReplaceLegacyName(text);
                if (replaced == text) { continue; }

                File.WriteAllText(path, replaced);
                AssetDatabase.ImportAsset(path);
                hasAssetChanges = true;
                SymphonyDebugLogger.LogDirect($"[{nameof(RootFolderCaseMigration)}] '{path}' の旧表記を置き換えました。");
            }

            MigrationPlan plan = PlanFileRenames(files);
            if (plan.Steps.Count == 0) { return hasAssetChanges; }

            ExecuteAssetSteps(plan);
            return true;
        }

        /// <summary>
        ///     UserSettings配下の旧表記フォルダを移す。
        /// </summary>
        /// <param name="parent"> 親フォルダのパス。 </param>
        private static void MoveFileSystemFolder(string parent)
        {
            if (!Directory.Exists(parent)) { return; }

            MigrationPlan plan = PlanFolderMoves(parent, GetChildDirectoryNames(parent));
            ThrowIfConflict(plan);

            foreach (MigrationStep step in plan.Steps)
            {
                Directory.Move(step.From, step.To);
            }

            if (plan.Steps.Count > 0)
            {
                SymphonyDebugLogger.LogDirect(
                    $"[{nameof(RootFolderCaseMigration)}] '{plan.Steps[0].From}' を '{plan.Steps[^1].To}' へ移しました。");
            }
        }

        /// <summary>
        ///     AssetDatabaseで移動の手順を順に実行する。
        /// </summary>
        /// <param name="plan"> 実行する手順。 </param>
        /// <exception cref="InvalidOperationException"> 移動に失敗した場合。 </exception>
        private static void ExecuteAssetSteps(MigrationPlan plan)
        {
            foreach (MigrationStep step in plan.Steps)
            {
                // MoveAssetは.metaを伴って移すため、GUIDとそれを参照するアセットのリンクが保たれる。
                string error = AssetDatabase.MoveAsset(step.From, step.To);
                if (!string.IsNullOrEmpty(error))
                {
                    throw new InvalidOperationException(
                        $"'{step.From}' を '{step.To}' へ移せませんでした: {error}"
                        + " 旧表記のまま残っているフォルダを手で改名してから、Unityを開き直してください。");
                }
            }

            SymphonyDebugLogger.LogDirect(
                $"[{nameof(RootFolderCaseMigration)}] '{plan.Steps[0].From}' を '{plan.Steps[^1].To}' へ移しました。");
        }

        /// <summary>
        ///     衝突がある計画なら例外で初期化を止める。
        /// </summary>
        /// <param name="plan"> 確認する計画。 </param>
        /// <exception cref="InvalidOperationException"> 衝突がある場合。 </exception>
        private static void ThrowIfConflict(MigrationPlan plan)
        {
            if (!plan.HasConflict) { return; }

            throw new InvalidOperationException(
                $"'{plan.ConflictLegacyPath}' と '{plan.ConflictCurrentPath}' が別のフォルダとして両方あります。"
                + " 中身を '" + plan.ConflictCurrentPath + "' へまとめて旧フォルダを消してから、Unityを開き直してください。");
        }

        /// <summary>
        ///     親フォルダ直下のフォルダ名を、ファイルシステムが返す表記のまま取得する。
        /// </summary>
        /// <param name="parent"> 親フォルダのパス。 </param>
        /// <returns> フォルダ名の一覧。 </returns>
        private static string[] GetChildDirectoryNames(string parent)
        {
            return Directory.GetDirectories(parent).Select(Path.GetFileName).ToArray();
        }

        /// <summary>
        ///     本文を置き換える生成物か判定する。
        /// </summary>
        /// <param name="path"> ファイルパス。 </param>
        /// <returns> 対象の拡張子ならtrue。 </returns>
        private static bool IsGeneratedTextFile(string path)
        {
            return GeneratedTextExtensions.Any(
                extension => path.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        /// <summary>
        ///     移動の1手順。
        /// </summary>
        internal readonly struct MigrationStep
        {
            /// <summary>
            ///     移動元と移動先を指定して生成する。
            /// </summary>
            /// <param name="from"> 移動元。 </param>
            /// <param name="to"> 移動先。 </param>
            internal MigrationStep(string from, string to)
            {
                From = from;
                To = to;
            }

            /// <summary> 移動元。 </summary>
            internal string From { get; }

            /// <summary> 移動先。 </summary>
            internal string To { get; }
        }

        /// <summary>
        ///     移動の手順、または自動では移せない衝突。
        /// </summary>
        internal sealed class MigrationPlan
        {
            /// <summary> 移すものが無い計画。 </summary>
            internal static MigrationPlan Empty { get; } = new(Array.Empty<MigrationStep>(), null, null);

            /// <summary> 実行する手順。 </summary>
            internal IReadOnlyList<MigrationStep> Steps { get; }

            /// <summary> 衝突があるか。 </summary>
            internal bool HasConflict => ConflictLegacyPath != null;

            /// <summary> 衝突した旧表記のパス。 </summary>
            internal string ConflictLegacyPath { get; }

            /// <summary> 衝突した現在の表記のパス。 </summary>
            internal string ConflictCurrentPath { get; }

            /// <summary>
            ///     手順から計画を作る。
            /// </summary>
            /// <param name="steps"> 実行する手順。 </param>
            /// <returns> 計画。 </returns>
            internal static MigrationPlan FromSteps(IEnumerable<MigrationStep> steps) =>
                new(steps.ToArray(), null, null);

            /// <summary>
            ///     衝突の計画を作る。
            /// </summary>
            /// <param name="legacyPath"> 旧表記のパス。 </param>
            /// <param name="currentPath"> 現在の表記のパス。 </param>
            /// <returns> 手順を持たない衝突の計画。 </returns>
            internal static MigrationPlan Conflict(string legacyPath, string currentPath) =>
                new(Array.Empty<MigrationStep>(), legacyPath, currentPath);

            private MigrationPlan(MigrationStep[] steps, string conflictLegacyPath, string conflictCurrentPath)
            {
                Steps = steps;
                ConflictLegacyPath = conflictLegacyPath;
                ConflictCurrentPath = conflictCurrentPath;
            }
        }
    }
}
