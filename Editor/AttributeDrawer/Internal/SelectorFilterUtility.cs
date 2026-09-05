using System;
using System.Collections.Generic;
using System.Reflection;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     セレクター属性が指定したフィルターメソッドを解決し、候補の採否を決める。
    /// </summary>
    /// <remarks>
    ///     Unity APIへ触れないため、EditModeテストから直接検証できる。
    ///     描画とメッセージの表示はDrawer側が行う。
    /// </remarks>
    internal static class SelectorFilterUtility
    {
        #region 外部向けAPI

        /// <summary>
        ///     候補ごとの採否を表すマスクを作る。
        /// </summary>
        /// <typeparam name="T"> 候補の型。フィルターメソッドの引数型と一致させる参照型。 </typeparam>
        /// <param name="candidates"> ドロップダウンへ並べる候補。 </param>
        /// <param name="filterMethodName"> フィルターメソッドの名前。絞り込まない場合はnullまたは空。 </param>
        /// <param name="target"> フィルターメソッドを探し、instanceメソッドを呼ぶ対象。 </param>
        /// <param name="mask"> 候補と同じ長さで、残す候補がtrueになる配列。 </param>
        /// <param name="errorMessage"> 失敗した理由。成功した場合はnull。 </param>
        /// <returns> マスクを作れた場合はtrue。 </returns>
        internal static bool TryCreateMask<T>(
            T[] candidates,
            string filterMethodName,
            object target,
            out bool[] mask,
            out string errorMessage)
            where T : class
        {
            errorMessage = null;

            // 候補が無い場合の空判定はDrawer側の責務であり、ここでは空のマスクを返して成功とする。
            if (candidates == null)
            {
                mask = Array.Empty<bool>();
                return true;
            }

            // フィルターの指定が無い属性は従来どおり全候補を残す。
            if (string.IsNullOrEmpty(filterMethodName))
            {
                mask = CreateFilledMask(candidates.Length);
                return true;
            }

            // 対象が無いとメソッドの宣言場所もinstanceの呼び出し先も決まらないため、絞り込みを諦める。
            if (target == null)
            {
                mask = null;
                errorMessage = "フィルターの対象を取得できません。";
                return false;
            }

            // 契約に合うメソッドが無いのは利用側の記述ミスであるため、期待するシグネチャを添えて知らせる。
            MethodInfo filterMethod = FindFilterMethod(target.GetType(), filterMethodName, typeof(T));
            if (filterMethod == null)
            {
                mask = null;
                errorMessage =
                    $"フィルターメソッド '{filterMethodName}' が見つかりません。"
                    + $" bool {filterMethodName}({DisplayNameOf(typeof(T))}) を"
                    + $" {target.GetType().Name} へ定義してください。";
                return false;
            }

            // staticメソッドは対象を渡さずに呼ぶ。
            object invocationTarget = filterMethod.IsStatic ? null : target;
            bool[] result = new bool[candidates.Length];

            for (int i = 0; i < candidates.Length; i++)
            {
                // 未設定を表すnullの候補は、利用側のフィルターへ渡さず常に残す。
                if (candidates[i] == null)
                {
                    result[i] = true;
                    continue;
                }

                // 利用側の例外をそのまま投げるとOnGUIの描画ごと落ちるため、メッセージへ変換する。
                try
                {
                    result[i] = (bool)filterMethod.Invoke(invocationTarget, new object[] { candidates[i] });
                }
                catch (Exception exception)
                {
                    Exception thrown = (exception as TargetInvocationException)?.InnerException ?? exception;
                    mask = null;
                    errorMessage = $"フィルターメソッド '{filterMethodName}' が例外を投げました: {thrown.Message}";
                    return false;
                }
            }

            mask = result;
            return true;
        }

        /// <summary>
        ///     保存済みの値が候補から外れている場合に使う、Popup表示用の既定indexを解決する。
        /// </summary>
        /// <param name="candidates"> Popupへ並べる候補。 </param>
        /// <param name="currentValue"> シリアライズ済みの値。 </param>
        /// <returns> 候補内に見つかった場合はそのindex、見つからない場合は0。 </returns>
        internal static int ResolveDisplayIndex(string[] candidates, string currentValue)
        {
            // 保存済みの値が候補外の場合も、Popupには先頭の候補を表示する。
            int index = Array.IndexOf(candidates, currentValue);
            if (index < 0) { return 0; }

            return index;
        }

        /// <summary>
        ///     マスクがtrueの要素だけを、元の順序を保って取り出す。
        /// </summary>
        /// <typeparam name="T"> 要素の型。 </typeparam>
        /// <param name="source"> 元の配列。 </param>
        /// <param name="mask"> sourceと同じ長さのマスク。 </param>
        /// <returns> 残った要素の配列。長さが食い違う場合はsource自身。 </returns>
        internal static T[] ApplyMask<T>(T[] source, bool[] mask)
        {
            // 添字の対応が取れないマスクで絞ると別の要素を落とすため、元の配列をそのまま返す。
            if (source == null || mask == null || source.Length != mask.Length) { return source; }

            List<T> kept = new(source.Length);
            for (int i = 0; i < source.Length; i++)
            {
                if (mask[i]) { kept.Add(source[i]); }
            }

            return kept.ToArray();
        }

        #endregion

        #region 内部処理

        /// <summary> 基底型を1段ずつ辿るため、宣言元だけを対象にする検索条件。 </summary>
        private const BindingFlags SEARCH_FLAGS =
            BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.DeclaredOnly;

        /// <summary>
        ///     指定した型とその基底型から、契約に合うフィルターメソッドを探す。
        /// </summary>
        /// <param name="targetType"> 探索を始める型。 </param>
        /// <param name="filterMethodName"> フィルターメソッドの名前。 </param>
        /// <param name="candidateType"> フィルターメソッドが受け取る候補の型。 </param>
        /// <returns> 契約に合うメソッド。見つからない場合はnull。 </returns>
        private static MethodInfo FindFilterMethod(Type targetType, string filterMethodName, Type candidateType)
        {
            Type[] parameterTypes = { candidateType };

            // privateなメソッドは宣言元でしか見つからないため、基底型を1段ずつ辿る。
            for (Type type = targetType; type != null; type = type.BaseType)
            {
                MethodInfo method = type.GetMethod(
                    filterMethodName,
                    SEARCH_FLAGS,
                    null,
                    parameterTypes,
                    null);

                // 同名で引数だけ合うメソッドは契約外のため、採用せず基底型の探索を続ける。
                if (method != null && method.ReturnType == typeof(bool)) { return method; }
            }

            return null;
        }

        /// <summary>
        ///     エラーメッセージへ載せる型名を作る。
        /// </summary>
        /// <param name="type"> 表示する型。 </param>
        /// <returns> 利用側がコードへ書くときの型名。 </returns>
        private static string DisplayNameOf(Type type)
        {
            // stringは利用側がキーワードで書くため、CLR名のStringでは対応が読み取りにくい。
            if (type == typeof(string)) { return "string"; }

            return type.Name;
        }

        /// <summary>
        ///     すべての候補を残すマスクを作る。
        /// </summary>
        /// <param name="length"> 候補の数。 </param>
        /// <returns> 全要素がtrueの配列。 </returns>
        private static bool[] CreateFilledMask(int length)
        {
            bool[] mask = new bool[length];
            for (int i = 0; i < length; i++) { mask[i] = true; }

            return mask;
        }

        #endregion
    }
}
