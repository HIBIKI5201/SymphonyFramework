using System.Runtime.CompilerServices;

// フレームワーク専用の内部実装を、別アセンブリのRuntimeとEditorから利用できるようにする。
[assembly: InternalsVisibleTo("SymphonyFrameWork")]
[assembly: InternalsVisibleTo("SymphonyFrameWork.Editor")]

// UNITY_INCLUDE_TESTSが定義された環境に限り、内部実装をパッケージ同梱テストから検証できるようにする。
[assembly: InternalsVisibleTo("SymphonyFrameWork.Tests.Editor")]
[assembly: InternalsVisibleTo("SymphonyFrameWork.Tests.Runtime")]
