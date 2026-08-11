using SymphonyFrameWork.Attribute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     SerializeReferenceの具象型を選択する。
    /// </summary>
    /// <remarks>
    ///     宣言型の継承関係とUnityのmanaged reference型名を対応付け、既存の多態的な値を維持する。
    ///     選択候補の具象型はActivator.CreateInstanceで生成できる必要がある。
    /// </remarks>
    [CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
    public sealed class SubclassSelectorDrawer : PropertyDrawer
    {
        #region 外部向けAPI

        /// <summary>
        ///     プロパティのGUIを描画する。
        /// </summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // SerializeReference以外のフィールドでは多態的な型情報を扱えないため、標準描画へ戻す。
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            // 先頭行をラベルと型選択Popupに分割し、参照先のフィールド描画領域を残す。
            Rect firstLineRect = position;
            firstLineRect.height = EditorGUIUtility.singleLineHeight;
            Rect controlRect = EditorGUI.PrefixLabel(firstLineRect, label);

            // SerializedPropertyのパスから宣言型を復元できない場合は、既存値を壊さないよう標準描画へ戻す。
            Type baseType = GetType(property);
            if (baseType == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            // 宣言型とMonoBehaviourの許可条件が同じ場合だけ候補一覧を再利用する。
            (Type baseType, bool) cacheKey =
                (baseType, ((SubclassSelectorAttribute)attribute).IsIncludeMono());
            if (!s_TypeCache.TryGetValue(
                cacheKey,
                out (Type[] types, string[] names, string[] fullNames) cachedData))
            {
                cachedData = CreateInheritedTypesCache(baseType, cacheKey.Item2);
                s_TypeCache.Add(cacheKey, cachedData);
            }
            (Type[] inheritedTypes, string[] typePopupNameArray, string[] typeFullNameArray) = cachedData;

            // 保存済みの型が候補から外れている場合は、先頭のnull選択へフォールバックする。
            int currentTypeIndex = Array.IndexOf(typeFullNameArray, property.managedReferenceFullTypename);
            if (currentTypeIndex < 0) { currentTypeIndex = 0; }

            // 現在の具象型と継承階層から作った候補をPopupへ表示する。
            int selectedTypeIndex = EditorGUI.Popup(controlRect, currentTypeIndex, typePopupNameArray);

            // 選択が変わった場合だけインスタンスを作り直し、既存のシリアライズ済みフィールド値を不用意に失わない。
            if (currentTypeIndex != selectedTypeIndex)
            {
                Type selectedType = inheritedTypes[selectedTypeIndex];
                property.managedReferenceValue =
                    selectedType == null ? null : Activator.CreateInstance(selectedType);
            }

            // 参照先がある場合だけ具象型のフィールドを次の行へ描画し、null時の空領域を作らない。
            if (property.managedReferenceValue != null)
            {
                Rect fieldPosition = new(
                    position.x,
                    position.y + EditorGUIUtility.singleLineHeight,
                    position.width,
                    position.height - EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(fieldPosition, property, GUIContent.none, true);
            }
        }

        /// <summary>
        ///     プロパティの高さに基づいてGUIの高さを計算する。
        /// </summary>
        /// <param name="property">高さ計算の対象となるプロパティ。</param>
        /// <param name="label">プロパティの表示ラベル。</param>
        /// <returns>プロパティの高さ。</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // SerializeReference以外のフィールドではPopupを追加しないため、標準の高さを返す。
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            // 宣言型を復元できない場合は標準描画へ戻るため、標準の高さを返す。
            Type baseType = GetType(property);
            if (baseType == null) { return EditorGUI.GetPropertyHeight(property, label, true); }

            // 型選択Popupの1行分を常に確保する。
            float totalHeight = EditorGUIUtility.singleLineHeight;

            // 参照先がある場合は、ラベル無しでもFoldoutを含む標準PropertyFieldの高さを加える。
            if (property.managedReferenceValue != null)
            {
                totalHeight += EditorGUI.GetPropertyHeight(property, GUIContent.none, true);
            }

            return totalHeight;
        }

        #endregion

        #region 内部処理

        /// <summary> 宣言型と候補条件ごとの派生型情報。 </summary>
        private static readonly Dictionary<
            (Type, bool),
            (Type[] types, string[] names, string[] fullNames)> s_TypeCache = new();

        /// <summary>
        ///     指定された基底クラスを継承する全ての型情報を収集し、キャッシュデータを作成する。
        /// </summary>
        /// <param name="baseType"> 基底クラスの型。 </param>
        /// <param name="includeMono"> MonoBehaviourを継承する型を含めるかどうか。 </param>
        /// <returns> 派生型情報のキャッシュデータ。 </returns>
        private static (Type[], string[], string[]) CreateInheritedTypesCache(Type baseType, bool includeMono)
        {
            Type monoType = typeof(MonoBehaviour);

            // 表示カテゴリの判定には中間のabstract型も必要なため、全派生型を先に収集する。
            Type[] allInheritedTypesWithAbstract = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => baseType.IsAssignableFrom(p) && p.IsClass && (!monoType.IsAssignableFrom(p) || includeMono))
                .ToArray();

            // Activatorで生成できないabstract型を候補から除外する。
            Type[] selectableTypesArray = allInheritedTypesWithAbstract
                .Where(t => !t.IsAbstract)
                .ToArray();

            // SerializeReferenceを未設定へ戻せるよう、nullを先頭の候補にする。
            Type[] finalSelectableTypes = selectableTypesArray.Prepend(null).ToArray();

            // 型候補と同じ順序でPopup表示名を作成する。
            string[] typePopupNameArray = GetPopupArray(finalSelectableTypes, baseType, allInheritedTypesWithAbstract);

            // Unityが保持するmanaged reference型名と照合できる形式で完全修飾名を作成する。
            string[] typeFullNameArray = finalSelectableTypes
                .Select(type => type == null ? string.Empty : $"{type.Assembly.GetName().Name} {type.FullName}")
                .ToArray();

            // 3配列の添字対応を保ったままキャッシュへ返す。
            return (finalSelectableTypes, typePopupNameArray, typeFullNameArray);
        }

        /// <summary>
        ///     ポップアップ表示用の型名配列を取得する。
        /// </summary>
        /// <param name="finalSelectableTypes"> nullを含む選択可能な型一覧。 </param>
        /// <param name="baseType"> SerializeReferenceフィールドの宣言型。 </param>
        /// <param name="allInheritedTypesWithAbstract"> カテゴリ判定に使用する全派生型。 </param>
        /// <returns> 継承階層をカテゴリとして表現した表示名一覧。 </returns>
        private static string[] GetPopupArray(
            Type[] finalSelectableTypes,
            Type baseType,
            Type[] allInheritedTypesWithAbstract)
        {
            return finalSelectableTypes.Select(type =>
            {
                // 先頭へ追加した未設定候補には型名が無いため、専用表示を返す。
                if (type == null) { return "<null>"; }

                string displayName;
                Type parent = type.BaseType;

                // 継承階層に応じて親型のカテゴリ、自身のカテゴリ、または型名だけの表示へ分ける。
                if (parent != null && parent != baseType && allInheritedTypesWithAbstract.Contains(parent))
                {
                    displayName = $"{parent.Name}/{type.Name}";
                }
                else if (allInheritedTypesWithAbstract.Any(t => t.BaseType == type))
                {
                    displayName = $"{type.Name}/{type.Name}";
                }
                else { displayName = type.Name; }

                // ネスト型の区切りをPopupの階層区切りへ変換する。
                if (displayName.Contains('+')) { displayName = displayName.Replace('+', '/'); }

                return displayName;
            }).ToArray();
        }

        /// <summary>
        ///     ポップアップGUIの描画範囲を取得する。
        /// </summary>
        /// <param name="currentPosition">現在のプロパティの描画範囲。</param>
        /// <returns>ポップアップの描画範囲。</returns>
        private Rect GetPopupPosition(Rect currentPosition)
        {
            // Unity標準ラベルの右側だけをPopup領域として使用する。
            Rect popupPosition = new Rect(currentPosition);
            popupPosition.width -= EditorGUIUtility.labelWidth;
            popupPosition.x += EditorGUIUtility.labelWidth;
            popupPosition.height = EditorGUIUtility.singleLineHeight;
            return popupPosition;
        }

        /// <summary>
        ///     SerializedPropertyから、そのプロパティの基底となる型を取得する。
        /// </summary>
        /// <param name="property">対象のプロパティ。</param>
        /// <returns>プロパティの基底型。</returns>
        private static Type GetType(SerializedProperty property)
        {
            FieldInfo fieldInfo = GetFieldInfo(property);
            if (fieldInfo == null)
            {
                Debug.LogWarning($"Could not find field for property {property.propertyPath}");
                return null;
            }

            Type fieldType = fieldInfo.FieldType;

            // 配列ではフィールド型ではなく、SerializeReferenceが付く要素型を候補の基底型とする。
            if (fieldType.IsArray) { return fieldType.GetElementType(); }

            // Listではフィールド型ではなく、SerializeReferenceが付く要素型を候補の基底型とする。
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                return fieldType.GetGenericArguments()[0];
            }

            return fieldType;
        }

        /// <summary>
        ///     SerializedPropertyのパスを解析し、対応するFieldInfoを取得する。
        /// </summary>
        /// <param name="property">対象のプロパティ。</param>
        /// <returns>対応するFieldInfo。</returns>
        private static FieldInfo GetFieldInfo(SerializedProperty property)
        {
            // Unityの配列パスを要素単位で解析できる短縮形式へ変換する。
            string[] pathElements = property.propertyPath.Replace(".Array.data[", "[").Split('.');
            Type currentContainingType = property.serializedObject.targetObject.GetType();
            FieldInfo fieldAtLastElement = null;

            // ルート型から順にパスを辿り、各区間を保持する型へ探索起点を進める。
            for (int i = 0; i < pathElements.Length; i++)
            {
                string element = pathElements[i];

                if (element.Contains("["))
                {
                    // 配列またはListの要素パスでは、継承元も含めてコレクションフィールドを探索する。
                    string fieldName = element.Substring(0, element.IndexOf("["));

                    FieldInfo collectionField = null;
                    for (Type t = currentContainingType; collectionField == null && t != null; t = t.BaseType)
                    {
                        collectionField = t.GetField(
                            fieldName,
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    }

                    // フィールドを解決できないパスは推測せず、標準描画へフォールバックさせる。
                    if (collectionField == null) { return null; }

                    Type elementType = null;
                    // 要素型を復元できる配列とListだけを扱い、それ以外のコレクションは標準描画へ戻す。
                    if (collectionField.FieldType.IsArray) { elementType = collectionField.FieldType.GetElementType(); }
                    else if (
                        collectionField.FieldType.IsGenericType &&
                        collectionField.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        elementType = collectionField.FieldType.GetGenericArguments()[0];
                    }
                    else { return null; }

                    // 次のパス要素を正しい宣言型から探索するため、コンテナ型を要素型へ進める。
                    currentContainingType = elementType;

                    // 最終要素ではGetType側が要素型を抽出できるよう、コレクション自体のFieldInfoを保持する。
                    if (i == pathElements.Length - 1) { fieldAtLastElement = collectionField; }
                }
                else
                {
                    // 通常フィールドも基底型まで遡り、privateなシリアライズ対象を解決する。
                    FieldInfo actualField = null;
                    for (Type t = currentContainingType; actualField == null && t != null; t = t.BaseType)
                    {
                        actualField = t.GetField(
                            element,
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    }

                    // フィールドを解決できないパスは推測せず、標準描画へフォールバックさせる。
                    if (actualField == null) { return null; }

                    if (i == pathElements.Length - 1) { fieldAtLastElement = actualField; }
                    else
                    {
                        // 中間のmanaged referenceが持つ実型を辿るため、Unity形式へ戻したパスで値を取得する。
                        string intermediatePath = BuildPropertyPath(pathElements, i + 1);
                        SerializedProperty intermediateProperty =
                            property.serializedObject.FindProperty(intermediatePath);

                        if (
                            intermediateProperty != null &&
                            intermediateProperty.propertyType == SerializedPropertyType.ManagedReference)
                        {
                            // 実体がある場合は具象型を使い、nullの場合だけ宣言型へフォールバックする。
                            currentContainingType =
                                intermediateProperty.managedReferenceValue?.GetType() ?? actualField.FieldType;
                        }
                        else { currentContainingType = actualField.FieldType; }
                    }
                }
            }
            return fieldAtLastElement;
        }

        /// <summary>
        ///     簡略化されたパス要素からUnity標準のプロパティパスを再構築する。
        /// </summary>
        /// <param name="pathElements"> 簡略化済みのパス要素配列。 </param>
        /// <param name="count"> 先頭から何要素を対象に含めるか。 </param>
        /// <returns> Unity標準形式のプロパティパス。 </returns>
        private static string BuildPropertyPath(string[] pathElements, int count)
        {
            // 中間フィールドまでの区間だけを、元の並びを維持した配列へ再構築する。
            string[] segments = new string[count];
            for (int i = 0; i < count; i++)
            {
                string element = pathElements[i];
                int bracketIndex = element.IndexOf('[');
                // 配列要素でない区間はUnity形式への変換が不要なため、そのまま次へ進む。
                if (bracketIndex < 0)
                {
                    segments[i] = element;
                    continue;
                }

                // 短縮した配列添字をFindPropertyが解決できるArray.data形式へ戻す。
                string fieldName = element.Substring(0, bracketIndex);
                string indexPart = element.Substring(bracketIndex);
                segments[i] = fieldName + ".Array.data" + indexPart;
            }

            return string.Join(".", segments);
        }

        #endregion
    }
}
