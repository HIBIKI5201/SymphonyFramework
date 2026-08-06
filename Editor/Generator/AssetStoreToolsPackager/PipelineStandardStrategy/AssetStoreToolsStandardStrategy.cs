namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     フレームワークが標準で用意する手順の基底型。
    /// </summary>
    /// <remarks>
    ///     サブクラスセレクターは、中間クラスを持つ派生型を「基底型名/型名」の階層で表示する。
    ///     標準の手順をこの型の下へまとめることで、利用側が追加した手順と選択肢の上で区別できる。
    ///     利用側が自作する手順は<see cref="AssetStoreToolsPackageStepStrategy" />を直接継承する。
    /// </remarks>
    public abstract class AssetStoreToolsStandardStrategy : AssetStoreToolsPackageStepStrategy
    {
    }
}
