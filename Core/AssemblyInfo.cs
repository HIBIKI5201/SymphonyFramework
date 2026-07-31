using System.Runtime.CompilerServices;

// Core/Internal/ のヘルパーはフレームワーク専用のため internal のままにするが、
// アセンブリが分かれているRuntimeとEditorから利用できるようフレンド指定する。
[assembly: InternalsVisibleTo("SymphonyFrameWork")]
[assembly: InternalsVisibleTo("SymphonyFrameWork.Editor")]

// テストアセンブリはパッケージ同梱（Tests/）であり、UNITY_INCLUDE_TESTS が
// 定義された環境でのみコンパイルされる。内部実装の単体テストのために公開する。
[assembly: InternalsVisibleTo("SymphonyFrameWork.Tests.Editor")]
[assembly: InternalsVisibleTo("SymphonyFrameWork.Tests.Runtime")]
