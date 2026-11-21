// 6.x（名前空間 SymphonyFrameWork.*）から 7.0.0 へ上げた利用側の `using SymphonyFrameWork.X;` を、コンパイルエラーにしないための目印。
// この SymphonyFramework.Editor アセンブリに、6.x の名前空間を中身の無い internal 型で残す。型そのものは新しい名前空間へ移っており、
// 6.x の名前を指す [MovedFrom] で Unity の API Updater が利用側スクリプトを書き換える。
// 7.0.0 の移行期間だけ残す。8.0.0 で削除する（Deprecations.md）。

namespace SymphonyFrameWork.Editor
{
    /// <summary> 6.x の名前空間 <c>SymphonyFrameWork.Editor</c> を残すための目印。 </summary>
    internal static class LegacyNamespaceMarker
    {
    }
}

namespace SymphonyFrameWork.Editor.Debugger
{
    /// <summary> 6.x の名前空間 <c>SymphonyFrameWork.Editor.Debugger</c> を残すための目印。 </summary>
    internal static class LegacyNamespaceMarker
    {
    }
}

namespace SymphonyFrameWork.Editor.SettingProvider
{
    /// <summary> 6.x の名前空間 <c>SymphonyFrameWork.Editor.SettingProvider</c> を残すための目印。 </summary>
    internal static class LegacyNamespaceMarker
    {
    }
}
