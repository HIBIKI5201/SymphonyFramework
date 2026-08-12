using SymphonyFrameWork.Core;
using SymphonyFrameWork.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     必須パッケージの不足を検出して導入する。
    /// </summary>
    public static class SymphonyPackageLoader
    {
        #region 外部向けAPI

        /// <summary>
        ///     メニューから必須パッケージの確認を開始する。
        /// </summary>
        [MenuItem(SymphonyConstant.TOOL_MENU_PATH + nameof(SymphonyPackageLoader), priority = 100)]
        private static void MenuExecution()
        {
            // 利用者が明示的に実行した場合は、不足が無いこともダイアログで通知する。
            CheckAndInstallPackagesAsync(false);
        }

        #endregion

        #region 内部処理

        private static readonly string REQUIRE_PACKAGE_LIST_PATH =
            EditorSymphonyConstant.FRAMEWORK_PATH + "/Editor/PackageLoader/PackageList.txt";

        /// <summary>
        ///     必須パッケージ一覧とインストール済み一覧を比較し、不足分の導入を確認する。
        /// </summary>
        /// <param name="isEnterEditor"> Editor起動時の自動確認の場合はtrue。 </param>
        private static async void CheckAndInstallPackagesAsync(bool isEnterEditor)
        {
            // Package Managerが要求を受け付けられない初期化前なら、確認処理を開始しない。
            if (Client.List() == null) { return; }

            // 不足判定の基準にするインストール済み一覧を非同期で取得する。
            PackageCollection installedPackages = await GetInstalledPackagesAsync();

            // 一覧の取得に失敗した場合は、不正確な不足判定と追加要求を行わない。
            if (installedPackages == null) { return; }

            // 同梱リストの空行を除き、Package Managerへ渡せるパッケージ名へ正規化する。
            string[] requirePackageList = AssetDatabase.LoadAssetAtPath<TextAsset>(REQUIRE_PACKAGE_LIST_PATH)
                ?.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                ?.Select(line => line.Trim())
                ?.ToArray()
                ?? new string[0];

            string[] missingPackages = GetMissingPackages(requirePackageList, installedPackages);

            // 不足が無ければ手動実行時だけ完了を示し、不足があれば利用者の同意後に導入する。
            if (missingPackages.Length < 1)
            {
                if (!isEnterEditor && EditorUtility.DisplayDialog($"{nameof(SymphonyPackageLoader)}",
                        "全てのパッケージがインストールされています",
                        "OK"))
                {
                }
            }
            else
            {
                if (EditorUtility.DisplayDialog($"{nameof(SymphonyPackageLoader)}",
                        "以下のパッケージをインストールします\n" + string.Join('\n', missingPackages),
                        "OK", "Cancel"))
                {
                    // 利用者が同意した場合だけPackage Managerへ変更要求を送る。
                    await InstallPackageAsync(missingPackages);
                }
            }
        }

        /// <summary>
        ///     インストール済みパッケージを返す。
        /// </summary>
        /// <returns> Package Managerから取得したインストール済みパッケージ一覧。 </returns>
        private static async Task<PackageCollection> GetInstalledPackagesAsync()
        {
            // Package Managerの応答待ちでEditorが停止しないよう、進捗表示と非同期待機を組み合わせる。
            EditorUtility.DisplayProgressBar(nameof(SymphonyPackageLoader), "パッケージを確認中", 0);

            ListRequest listRequest = Client.List();

            float timer = Time.time;
            // 応答が無い要求で待ち続けないよう、60秒を上限に非同期で待機する。
            await SymphonyAwaitable.WaitWhile(
                () => !listRequest.IsCompleted && Time.time <= timer + 60);

            EditorUtility.ClearProgressBar();

            // 上限を超えた場合は、後続の結果確認に先立って利用者へ通知する。
            if (timer + 60 < Time.time)
            {
                EditorUtility.DisplayDialog(nameof(SymphonyPackageLoader), "タイムアウトしました", "OK");
            }

            // Package Managerが失敗を返した場合は、部分的な一覧を不足判定へ渡さない。
            if (listRequest.Status == StatusCode.Failure)
            {
                Debug.LogError("Failed to fetch package list: " + listRequest.Error.message);
                return null;
            }

            return listRequest.Result;
        }

        /// <summary>
        ///     必須パッケージのうち未導入のものを返す。
        /// </summary>
        private static string[] GetMissingPackages(string[] required, PackageCollection installedPackages)
        {
            ConcurrentBag<string> missingPackages = new();

            // 各候補を独立して照合し、並列処理から安全に不足一覧へ集約する。
            Parallel.ForEach(required, pkg =>
            {
                string fullPackageName = "com.unity." + pkg;

                // インストール済み一覧に完全名が無い候補だけを導入対象にする。
                if (!installedPackages.Any(installedPkg => installedPkg.name == fullPackageName))
                {
                    missingPackages.Add(fullPackageName);
                }
            });

            return missingPackages.ToArray();
        }

        /// <summary>
        ///     指定したパッケージを導入する。
        /// </summary>
        /// <param name="packageNames"> Package Managerへ追加する完全パッケージ名一覧。 </param>
        /// <returns> 全パッケージ追加処理を表すTask。 </returns>
        private static async Task InstallPackageAsync(string[] packageNames)
        {
            // すべての追加要求を先に開始し、個別の完了待ちを並行して進める。
            IEnumerable<Task> tasks = packageNames.Select(async name =>
            {
                AddRequest addRequest = Client.Add(name);

                // Package Managerの完了を待つ間もEditorスレッドへ制御を戻す。
                while (!addRequest.IsCompleted) { await Task.Yield(); }

                // 1件の失敗で他の追加要求を中断せず、各結果をConsoleへ残す。
                if (addRequest.Status == StatusCode.Failure)
                {
                    Debug.LogError("Failed to install package: " + addRequest.Error.message);
                }
                else { Debug.Log("Package installed: " + name); }
            });

            // 呼び出し元へ戻る前に、開始したすべての追加要求を完了させる。
            await Task.WhenAll(tasks);
        }

        #endregion
    }
}
