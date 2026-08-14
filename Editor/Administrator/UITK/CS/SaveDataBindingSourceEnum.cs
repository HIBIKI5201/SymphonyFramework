namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     Save Data Inspectorが参照するインスタンスの所有元を表す。
    /// </summary>
    internal enum SaveDataBindingSourceEnum
    {
        /// <summary> 対象型が選択されていない状態。 </summary>
        None,

        /// <summary> Window専用の新規インスタンスを参照する状態。 </summary>
        Instance,

        /// <summary> 永続化データを読み込んだWindow専用インスタンスを参照する状態。 </summary>
        Loaded,

        /// <summary> Registryが所有する正本インスタンスを参照する状態。 </summary>
        Registry
    }
}
