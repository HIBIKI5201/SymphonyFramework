// 6.x（名前空間 SymphonyFrameWork.*）から 7.0.0 へ上げた利用側の `using SymphonyFrameWork.X;` を、コンパイルエラーにしないための目印。
// この SymphonyFramework アセンブリに、6.x の名前空間を中身の無い internal 型で残す。型そのものは新しい名前空間へ移っており、
// 6.x の名前を指す [MovedFrom] で Unity の API Updater が利用側スクリプトを書き換える。
// 7.0.0 の移行期間だけ残す。8.0.0 で削除する（Deprecations.md）。

namespace SymphonyFrameWork
{
    /// <summary> 6.x の名前空間 <c>SymphonyFrameWork</c> を残すための目印。 </summary>
    internal static class LegacyNamespaceMarker
    {
    }
}

namespace SymphonyFrameWork.Attribute
{
    /// <summary> 6.x の名前空間 <c>SymphonyFrameWork.Attribute</c> を残すための目印。 </summary>
    internal static class LegacyNamespaceMarker
    {
    }
}

namespace SymphonyFrameWork.Config
{
    /// <summary> 6.x の名前空間 <c>SymphonyFrameWork.Config</c> を残すための目印。 </summary>
    internal static class LegacyNamespaceMarker
    {
    }
}

namespace SymphonyFrameWork.Debugger
{
    /// <summary> 6.x の名前空間 <c>SymphonyFrameWork.Debugger</c> を残すための目印。 </summary>
    internal static class LegacyNamespaceMarker
    {
    }
}

namespace SymphonyFrameWork.Debugger.HUD
{
    /// <summary> 6.x の名前空間 <c>SymphonyFrameWork.Debugger.HUD</c> を残すための目印。 </summary>
    internal static class LegacyNamespaceMarker
    {
    }
}

namespace SymphonyFrameWork.Debugger.Logger
{
    /// <summary> 6.x の名前空間 <c>SymphonyFrameWork.Debugger.Logger</c> を残すための目印。 </summary>
    internal static class LegacyNamespaceMarker
    {
    }
}

namespace SymphonyFrameWork.Exceptions
{
    /// <summary> 6.x の名前空間 <c>SymphonyFrameWork.Exceptions</c> を残すための目印。 </summary>
    internal static class LegacyNamespaceMarker
    {
    }
}

namespace SymphonyFrameWork.Utility
{
    /// <summary> 6.x の名前空間 <c>SymphonyFrameWork.Utility</c> を残すための目印。 </summary>
    internal static class LegacyNamespaceMarker
    {
    }
}
