using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SymphonyFrameWork.Editor")]

// テストアセンブリはパッケージ同梱（Tests/）であり、UNITY_INCLUDE_TESTS が
// 定義された環境でのみコンパイルされる。内部実装の単体テストのために公開する。
[assembly: InternalsVisibleTo("SymphonyFrameWork.Tests.Editor")]
[assembly: InternalsVisibleTo("SymphonyFrameWork.Tests.Runtime")]
