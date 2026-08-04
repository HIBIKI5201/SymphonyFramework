using System.Runtime.CompilerServices;

// Editorツールの内部実装はフレームワーク専用のため internal のままにするが、
// 純粋なロジックを単体テストで検証できるようテストアセンブリへフレンド指定する。
// テストアセンブリはパッケージ同梱（Tests/）であり、UNITY_INCLUDE_TESTS が
// 定義された環境でのみコンパイルされる。
[assembly: InternalsVisibleTo("SymphonyFrameWork.Tests.Editor")]
