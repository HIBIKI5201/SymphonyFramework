using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using UnityEngine.Scripting.APIUpdating;

namespace SymphonyFramework.Editor
{
    /// <summary>
    ///     6.x の名前（<c>SymphonyFrameWork.*</c>）と 7.0.0 の名前の対応表。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         **表は読み込み済みのアセンブリの <see cref="MovedFromAttribute" /> から作り、手書きの表を持たない。**
    ///         型に付けた移行属性と食い違わないためである。
    ///     </para>
    ///     <para>
    ///         旧名前空間に残した <see cref="ObsoleteAttribute" /> 付きの転送は、書き換えの対象から外すために別に持つ。
    ///         転送は名前もメンバーも変わるため、機械的に置き換えると別の API を呼ぶことになる。
    ///     </para>
    /// </remarks>
    internal sealed class LegacyNameMap
    {
        #region 外部向けAPI

        /// <summary> 6.x の名前空間の根。 </summary>
        internal const string LEGACY_ROOT = "SymphonyFrameWork";

        /// <summary>
        ///     対応表の項目から作る。
        /// </summary>
        /// <param name="entries"> 旧名から新しい名前への対応。 </param>
        /// <param name="forwarders"> 旧名前空間に残した転送の（名前空間, 型名）。 </param>
        /// <param name="publicTypes"> 新しい名前空間にある公開型の（名前空間, 型名）。曖昧さの判定に使う。 </param>
        /// <param name="extensions"> 旧名前空間から移った拡張メソッド。 </param>
        /// <remarks>
        ///     名前が <c>Attribute</c> で終わる型は、属性として書くときの短縮名も対応に加える。
        /// </remarks>
        internal LegacyNameMap(
            IEnumerable<Entry> entries,
            IEnumerable<(string Namespace, string Name)> forwarders,
            IEnumerable<(string Namespace, string Name)> publicTypes,
            IEnumerable<ExtensionEntry> extensions)
        {
            if (entries == null) { throw new ArgumentNullException(nameof(entries)); }
            if (forwarders == null) { throw new ArgumentNullException(nameof(forwarders)); }
            if (publicTypes == null) { throw new ArgumentNullException(nameof(publicTypes)); }
            if (extensions == null) { throw new ArgumentNullException(nameof(extensions)); }

            List<Entry> entryList = entries.ToList();
            foreach (Entry entry in entryList)
            {
                _entries.TryAdd((entry.OldNamespace, entry.OldName), entry);
            }

            // 短縮名は、正式名の対応をすべて入れた後で、空いている場合だけ加える。
            foreach (Entry entry in entryList)
            {
                if (!TryTrimAttributeSuffix(entry.OldName, out string oldShortName)
                    || !TryTrimAttributeSuffix(entry.NewName, out string newShortName))
                {
                    continue;
                }

                _entries.TryAdd(
                    (entry.OldNamespace, oldShortName),
                    new Entry(entry.OldNamespace, oldShortName, entry.NewNamespace, newShortName));
            }

            _forwarders = new HashSet<(string, string)>(forwarders);
            _publicTypes = new HashSet<(string, string)>(publicTypes);
            _extensions = extensions
                .GroupBy(extension => extension.OldNamespace, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<ExtensionEntry>)group.ToArray(),
                    StringComparer.Ordinal);
        }

        /// <summary>
        ///     読み込み済みの SymphonyFramework のアセンブリから対応表を作る。
        /// </summary>
        /// <returns> 対応表。 </returns>
        internal static LegacyNameMap CreateFromLoadedAssemblies()
        {
            return Create(AppDomain.CurrentDomain.GetAssemblies().Where(IsFrameworkAssembly));
        }

        /// <summary>
        ///     指定したアセンブリの型から対応表を作る。
        /// </summary>
        /// <param name="assemblies"> 読むアセンブリ。 </param>
        /// <returns> 対応表。 </returns>
        internal static LegacyNameMap Create(IEnumerable<Assembly> assemblies)
        {
            if (assemblies == null) { throw new ArgumentNullException(nameof(assemblies)); }

            List<Entry> entries = new();
            List<(string, string)> forwarders = new();
            List<(string, string)> publicTypes = new();
            List<ExtensionEntry> extensions = new();

            foreach (Type type in assemblies.SelectMany(GetLoadableTypes))
            {
                // 入れ子の型は、利用側が外側の型名から書くため対象にしない。
                if (type.IsNested || !type.IsPublic) { continue; }

                string typeNamespace = type.Namespace ?? string.Empty;
                string typeName = StripArity(type.Name);
                publicTypes.Add((typeNamespace, typeName));

                if (IsLegacyNamespace(typeNamespace))
                {
                    if (type.IsDefined(typeof(ObsoleteAttribute), false))
                    {
                        forwarders.Add((typeNamespace, typeName));
                    }

                    continue;
                }

                if (!TryGetMovedFrom(type, out string oldNamespace, out string oldName)) { continue; }

                Entry entry = new(oldNamespace, oldName ?? typeName, typeNamespace, typeName);
                entries.Add(entry);

                string[] extensionMethods = GetExtensionMethodNames(type);
                if (extensionMethods.Length > 0)
                {
                    extensions.Add(new ExtensionEntry(oldNamespace, typeNamespace, extensionMethods));
                }
            }

            return new LegacyNameMap(entries, forwarders, publicTypes, extensions);
        }

        /// <summary>
        ///     旧名前空間と旧型名から、新しい名前を引く。
        /// </summary>
        /// <param name="oldNamespace"> 旧名前空間。 </param>
        /// <param name="oldName"> 旧型名。属性の短縮名も引ける。 </param>
        /// <param name="entry"> 見つかった対応。 </param>
        /// <returns> 対応がある場合はtrue。 </returns>
        internal bool TryGetEntry(string oldNamespace, string oldName, out Entry entry)
        {
            return _entries.TryGetValue((oldNamespace, oldName), out entry);
        }

        /// <summary>
        ///     旧名前空間に残した転送か判定する。
        /// </summary>
        /// <param name="oldNamespace"> 旧名前空間。 </param>
        /// <param name="name"> 型名（ジェネリックの型引数の数を除く）。 </param>
        /// <returns> 転送ならtrue。 </returns>
        internal bool IsForwarder(string oldNamespace, string name)
        {
            return _forwarders.Contains((oldNamespace, name));
        }

        /// <summary>
        ///     名前空間に指定した名前の公開型があるか判定する。
        /// </summary>
        /// <param name="typeNamespace"> 名前空間。 </param>
        /// <param name="name"> 型名。 </param>
        /// <returns> ある場合はtrue。 </returns>
        internal bool HasType(string typeNamespace, string name)
        {
            return _publicTypes.Contains((typeNamespace, name));
        }

        /// <summary>
        ///     旧名前空間から移った拡張メソッドを取得する。
        /// </summary>
        /// <param name="oldNamespace"> 旧名前空間。 </param>
        /// <returns> 拡張メソッドの一覧。無ければ空。 </returns>
        internal IReadOnlyList<ExtensionEntry> GetExtensions(string oldNamespace)
        {
            return _extensions.TryGetValue(oldNamespace, out IReadOnlyList<ExtensionEntry> list)
                ? list
                : Array.Empty<ExtensionEntry>();
        }

        /// <summary>
        ///     6.x の名前空間か判定する。
        /// </summary>
        /// <param name="typeNamespace"> 名前空間。 </param>
        /// <returns> <c>SymphonyFrameWork</c> またはその下ならtrue。 </returns>
        internal static bool IsLegacyNamespace(string typeNamespace)
        {
            return typeNamespace != null
                && typeNamespace.StartsWith(LEGACY_ROOT, StringComparison.Ordinal)
                && (typeNamespace.Length == LEGACY_ROOT.Length || typeNamespace[LEGACY_ROOT.Length] == '.');
        }

        #endregion

        #region 内部処理

        /// <summary> 対象にするアセンブリ名の接頭辞。 </summary>
        private const string FRAMEWORK_ASSEMBLY_PREFIX = "SymphonyFramework";

        /// <summary> 属性型の名前の接尾辞。 </summary>
        private const string ATTRIBUTE_SUFFIX = "Attribute";

        private readonly Dictionary<(string, string), Entry> _entries = new();
        private readonly HashSet<(string, string)> _forwarders;
        private readonly HashSet<(string, string)> _publicTypes;
        private readonly Dictionary<string, IReadOnlyList<ExtensionEntry>> _extensions;

        /// <summary>
        ///     対応表の元にするアセンブリか判定する。テストのアセンブリは除く。
        /// </summary>
        /// <param name="assembly"> アセンブリ。 </param>
        /// <returns> 対象ならtrue。 </returns>
        private static bool IsFrameworkAssembly(Assembly assembly)
        {
            string name = assembly.GetName().Name;
            return name.StartsWith(FRAMEWORK_ASSEMBLY_PREFIX, StringComparison.Ordinal)
                && !name.Contains(".Tests", StringComparison.Ordinal);
        }

        /// <summary>
        ///     読み込める型だけを取得する。一部の型が読み込めないアセンブリでも、読める型は使う。
        /// </summary>
        /// <param name="assembly"> アセンブリ。 </param>
        /// <returns> 型の一覧。 </returns>
        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null);
            }
        }

        /// <summary>
        ///     型の <see cref="MovedFromAttribute" /> から旧名前空間と旧型名を読む。
        /// </summary>
        /// <param name="type"> 型。 </param>
        /// <param name="oldNamespace"> 旧名前空間。 </param>
        /// <param name="oldName"> 旧型名。型名が変わっていなければnull。 </param>
        /// <returns> 6.x の名前空間からの移行属性がある場合はtrue。 </returns>
        /// <remarks>
        ///     属性の値はpublicなプロパティで公開されていないため、コンストラクタの引数を名前で読む。
        /// </remarks>
        private static bool TryGetMovedFrom(Type type, out string oldNamespace, out string oldName)
        {
            oldNamespace = null;
            oldName = null;

            CustomAttributeData data = type
                .GetCustomAttributesData()
                .FirstOrDefault(attribute => attribute.AttributeType == typeof(MovedFromAttribute));
            if (data == null) { return false; }

            ParameterInfo[] parameters = data.Constructor.GetParameters();
            for (int i = 0; i < parameters.Length && i < data.ConstructorArguments.Count; i++)
            {
                string value = data.ConstructorArguments[i].Value as string;
                switch (parameters[i].Name)
                {
                    case "sourceNamespace":
                        oldNamespace = value;
                        break;
                    case "sourceClassName":
                        oldName = value;
                        break;
                }
            }

            return IsLegacyNamespace(oldNamespace);
        }

        /// <summary>
        ///     static クラスが持つ拡張メソッドの名前を取得する。
        /// </summary>
        /// <param name="type"> 型。 </param>
        /// <returns> 拡張メソッドの名前。static クラスでなければ空。 </returns>
        private static string[] GetExtensionMethodNames(Type type)
        {
            if (!type.IsAbstract || !type.IsSealed) { return Array.Empty<string>(); }

            return type
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(method => method.IsDefined(typeof(ExtensionAttribute), false))
                .Select(method => method.Name)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        ///     ジェネリック型の名前から型引数の数（<c>`1</c>）を除く。
        /// </summary>
        /// <param name="name"> 型名。 </param>
        /// <returns> 型引数の数を除いた名前。 </returns>
        private static string StripArity(string name)
        {
            int index = name.IndexOf('`');
            return index < 0 ? name : name.Substring(0, index);
        }

        /// <summary>
        ///     属性型の名前から <c>Attribute</c> を除く。
        /// </summary>
        /// <param name="name"> 型名。 </param>
        /// <param name="shortName"> 除いた名前。 </param>
        /// <returns> 除ける場合はtrue。 </returns>
        private static bool TryTrimAttributeSuffix(string name, out string shortName)
        {
            shortName = null;
            if (name.Length <= ATTRIBUTE_SUFFIX.Length
                || !name.EndsWith(ATTRIBUTE_SUFFIX, StringComparison.Ordinal))
            {
                return false;
            }

            shortName = name.Substring(0, name.Length - ATTRIBUTE_SUFFIX.Length);
            return true;
        }

        #endregion

        /// <summary>
        ///     旧名から新しい名前への対応。
        /// </summary>
        internal readonly struct Entry
        {
            /// <summary>
            ///     旧名と新しい名前を指定して生成する。
            /// </summary>
            /// <param name="oldNamespace"> 旧名前空間。 </param>
            /// <param name="oldName"> 旧型名。 </param>
            /// <param name="newNamespace"> 新しい名前空間。 </param>
            /// <param name="newName"> 新しい型名。 </param>
            internal Entry(string oldNamespace, string oldName, string newNamespace, string newName)
            {
                OldNamespace = oldNamespace;
                OldName = oldName;
                NewNamespace = newNamespace;
                NewName = newName;
            }

            /// <summary> 旧名前空間。 </summary>
            internal string OldNamespace { get; }

            /// <summary> 旧型名。 </summary>
            internal string OldName { get; }

            /// <summary> 新しい名前空間。 </summary>
            internal string NewNamespace { get; }

            /// <summary> 新しい型名。 </summary>
            internal string NewName { get; }
        }

        /// <summary>
        ///     旧名前空間から移った static クラスの拡張メソッド。
        /// </summary>
        /// <remarks>
        ///     拡張メソッドは型名が本文に現れないため、メソッド名から新しい名前空間の要否を判断する。
        /// </remarks>
        internal sealed class ExtensionEntry
        {
            /// <summary>
            ///     旧名前空間、新しい名前空間、メソッド名を指定して生成する。
            /// </summary>
            /// <param name="oldNamespace"> 旧名前空間。 </param>
            /// <param name="newNamespace"> 新しい名前空間。 </param>
            /// <param name="methodNames"> 拡張メソッドの名前。 </param>
            internal ExtensionEntry(string oldNamespace, string newNamespace, IEnumerable<string> methodNames)
            {
                OldNamespace = oldNamespace;
                NewNamespace = newNamespace;
                MethodNames = new HashSet<string>(methodNames ?? Array.Empty<string>(), StringComparer.Ordinal);
            }

            /// <summary> 旧名前空間。 </summary>
            internal string OldNamespace { get; }

            /// <summary> 新しい名前空間。 </summary>
            internal string NewNamespace { get; }

            /// <summary> 拡張メソッドの名前。 </summary>
            internal IReadOnlyCollection<string> MethodNames { get; }
        }
    }
}
