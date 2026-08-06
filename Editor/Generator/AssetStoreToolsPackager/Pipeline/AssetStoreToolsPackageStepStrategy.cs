using System;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     パッケージ出力の1手順を表す拡張点。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         手順は<see cref="Plan" />段階と<see cref="Execute" />段階に分かれ、
    ///         パイプライン全体で「全手順の<see cref="Plan" />」→「全手順の<see cref="Execute" />」の順に走る。
    ///         各段階の中では<see cref="AssetStoreToolsPackagePipeline.Steps" />の順序どおりに実行される。
    ///     </para>
    ///     <para>
    ///         2段階に分けるのは、出力前に確認ウィンドウで内容を提示するため。
    ///         絞り込みが<see cref="Execute" />の中で起きると、提示した一覧と実物が食い違う。
    ///     </para>
    ///     <para>
    ///         別アセンブリでこのクラスを継承する場合、
    ///         <see cref="Plan" />と<see cref="Execute" />のオーバーライドは<c>protected</c>として宣言する。
    ///         C#の規則により、別アセンブリからは<c>protected internal</c>のままでは宣言できない。
    ///     </para>
    /// </remarks>
    [Serializable]
    public abstract class AssetStoreToolsPackageStepStrategy
    {
        /// <summary> ウィンドウと確認ウィンドウへ表示する名前。 </summary>
        public virtual string DisplayName => GetType().Name;

        /// <summary>
        ///     出力対象を絞り込む段階。既定では何もしない。
        /// </summary>
        /// <remarks>
        ///     ファイルを書き出さないこと。この段階の結果が確認ウィンドウへ提示される。
        /// </remarks>
        /// <param name="plan"> 組み立て中の出力計画。 </param>
        protected internal virtual void Plan(AssetStoreToolsPackagePlan plan)
        {
        }

        /// <summary>
        ///     出力を行う段階。既定では何もしない。
        /// </summary>
        /// <remarks>
        ///     この段階で例外を投げても、ランナーが記録して次の手順へ進む。
        /// </remarks>
        /// <param name="context"> 出力先と確定済みの計画を保持するコンテキスト。 </param>
        protected internal virtual void Execute(AssetStoreToolsPackageExportContext context)
        {
        }
    }
}
