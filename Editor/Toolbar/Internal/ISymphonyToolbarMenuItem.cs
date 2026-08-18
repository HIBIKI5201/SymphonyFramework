#if UNITY_6000_3_OR_NEWER
namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     Symphony Frameworkのツールバープルダウンへ表示する1項目を表す。
    /// </summary>
    internal interface ISymphonyToolbarMenuItem
    {
        /// <summary> メニュー内の表示パス。 </summary>
        string Path { get; }

        /// <summary> 小さい項目を先に表示する並び順。 </summary>
        int Priority { get; }

        /// <summary> チェック付きで表示する場合はtrue。 </summary>
        bool IsChecked { get; }

        /// <summary> 操作可能な場合はtrue。 </summary>
        bool IsEnabled { get; }

        /// <summary> 項目の操作を実行する。 </summary>
        void Execute();
    }
}
#endif
