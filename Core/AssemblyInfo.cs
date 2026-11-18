using System.Runtime.CompilerServices;

// フレームワーク専用の内部実装を、別アセンブリのRuntimeとEditorから利用できるようにする。
[assembly: InternalsVisibleTo("SymphonyFramework")]
[assembly: InternalsVisibleTo("SymphonyFramework.Editor")]

// UNITY_INCLUDE_TESTSが定義された環境に限り、内部実装をパッケージ同梱テストから検証できるようにする。
[assembly: InternalsVisibleTo("SymphonyFramework.Tests.Editor")]
[assembly: InternalsVisibleTo("SymphonyFramework.Tests.Runtime")]
