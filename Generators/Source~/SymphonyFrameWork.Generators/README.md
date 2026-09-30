# SymphonyFrameWork.Generators

Unityへ配布するRoslyn Source Generatorの生成元です。`Source~`配下はUnityのインポート対象外で、
このプロジェクトはFrameworkのRuntime/Editorアセンブリとは独立してビルドします。

## ビルド

```powershell
dotnet build -c Release
Copy-Item -LiteralPath 'bin/Release/netstandard2.0/SymphonyFrameWork.Generators.dll' `
  -Destination '../../SymphonyFrameWork.Generators.dll'
```

ビルド後はUnityでDLLを再インポートし、コンパイルとEditModeテストを実行してください。
配布DLLの`.meta`は`RoslynAnalyzer`ラベルと全実行プラットフォーム無効の設定を保持します。
