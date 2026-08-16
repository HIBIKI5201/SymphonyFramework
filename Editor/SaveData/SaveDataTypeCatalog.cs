using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using SymphonyFrameWork.System.SaveSystem;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     Editorで管理可能なセーブデータ型を探索する。
    /// </summary>
    internal static class SaveDataTypeCatalog
    {
        #region 外部向けAPI

        /// <summary>
        ///     AppDomain内から管理可能なセーブデータ型を収集する。
        /// </summary>
        /// <returns> 完全名の序数昇順に並んだ対応型。 </returns>
        internal static IReadOnlyList<Type> CollectSupportedTypes()
        {
            // ロード可能な全Assemblyから対応型だけを抽出し、表示順を完全名で固定する。
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(GetTypesSafe)
                .Where(IsSupportedSaveDataType)
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        ///     指定したAssemblyがテスト用の参照を持つかを判定する。
        /// </summary>
        /// <param name="assembly"> 判定するAssembly。 </param>
        /// <returns> テスト用Assemblyの場合はtrue。 </returns>
        internal static bool IsTestAssembly(Assembly assembly)
        {
            // 参照情報を取得できないAssemblyは、利用者の型を隠さない通常Assemblyとして扱う。
            if (assembly == null) { return false; }

            try
            {
                IEnumerable<string> referencedAssemblyNames = assembly
                    .GetReferencedAssemblies()
                    .Select(assemblyName => assemblyName.Name);
                return IsTestAssembly(referencedAssemblyNames);
            }
            catch (Exception)
            {
                // 参照メタデータを読めない場合は、見えなくなる方向へ倒さない。
                return false;
            }
        }

        /// <summary>
        ///     参照Assembly名の一覧がテスト用の参照を含むかを判定する。
        /// </summary>
        /// <param name="referencedAssemblyNames"> 参照Assemblyの単純名。 </param>
        /// <returns> NUnitまたはUnity Test Runnerを参照する場合はtrue。 </returns>
        internal static bool IsTestAssembly(IEnumerable<string> referencedAssemblyNames)
        {
            // 参照一覧が無い場合は通常Assemblyとして扱う。
            if (referencedAssemblyNames == null) { return false; }

            return referencedAssemblyNames.Any(name =>
                string.Equals(name, "nunit.framework", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "UnityEngine.TestRunner", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        ///     管理パネルで生成・編集できるセーブデータ具象型か検証する。
        /// </summary>
        /// <param name="type"> 判定する型。 </param>
        /// <returns> 管理可能なセーブデータ型の場合はtrue。 </returns>
        internal static bool IsSupportedSaveDataType(Type type)
        {
            // 抽象型と未構築の型は、SerializeReferenceの編集対象にしない。
            if (type == null
                || !type.IsClass
                || type.IsAbstract
                || type.IsGenericTypeDefinition)
            {
                return false;
            }

            // SaveStoreが生成できるよう、引数なしコンストラクタを持つ型だけを許可する。
            if (type.GetConstructor(Type.EmptyTypes) == null) { return false; }

            // セーブデータ契約を満たさない型は、同じ生成条件を持っていても対象外とする。
            if (!typeof(SaveDataContent).IsAssignableFrom(type)) { return false; }

            // UnityのSerializeReferenceで編集できるよう、Serializable指定を最後に確認する。
            return type.IsDefined(typeof(SerializableAttribute), false);
        }

        #endregion

        #region 内部処理

        /// <summary>
        ///     Assemblyから取得可能な型だけを列挙する。
        /// </summary>
        /// <param name="assembly"> 型を列挙するAssembly。 </param>
        /// <returns> 解決できた型の一覧。 </returns>
        private static IEnumerable<Type> GetTypesSafe(Assembly assembly)
        {
            try
            {
                // すべての型を解決できるAssemblyでは通常の型一覧を返す。
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // 一部解決失敗でも、取得できた非null型まで捨てずに列挙する。
                return ex.Types.Where(type => type != null);
            }
        }

        #endregion
    }
}
