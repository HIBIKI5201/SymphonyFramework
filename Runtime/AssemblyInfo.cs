using System.Runtime.CompilerServices;

// Editor機能が設定アセットと内部実装を共有できるよう、配布アセンブリ内に可視性を限定して公開する。
[assembly: InternalsVisibleTo("SymphonyFrameWork.Editor")]

// テストアセンブリはパッケージ同梱（Tests/）であり、UNITY_INCLUDE_TESTS が
// 定義された環境でのみコンパイルされる。内部実装の単体テストのために公開する。
[assembly: InternalsVisibleTo("SymphonyFrameWork.Tests.Editor")]
[assembly: InternalsVisibleTo("SymphonyFrameWork.Tests.Runtime")]
