using SymphonyFrameWork.Attribute;
using System.Collections.Generic;
using UnityEngine;

namespace SymphonyFrameWork.Editor
{
    /// <summary>
    ///     Asset Store Tools Packagerが実行する手順の並びを保持するアセット。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Project Settings > SymphonyFrameWork > Asset Store Tools Packager で
    ///         このアセットを配列としてアサインすると、Packagerウィンドウの出力形式の選択肢になる。
    ///         選択肢の表示名はアセット名。
    ///     </para>
    ///     <para>
    ///         手順は<see cref="AssetStoreToolsPackageStepStrategy" />を継承して自作でき、
    ///         サブクラスセレクターから選択できる。
    ///     </para>
    /// </remarks>
    [CreateAssetMenu(
        fileName = nameof(AssetStoreToolsPackagePipeline),
        menuName = "SymphonyFrameWork/Asset Store Tools Package Pipeline")]
    public sealed class AssetStoreToolsPackagePipeline : ScriptableObject
    {
        /// <summary> 実行する手順。 </summary>
        public IReadOnlyList<AssetStoreToolsPackageStepStrategy> Steps => _steps;

        [SerializeReference]
        [SubclassSelector]
        [Tooltip("実行する手順。Plan段階とExecute段階のそれぞれで、この順序どおりに実行される。")]
        private List<AssetStoreToolsPackageStepStrategy> _steps = new();

        /// <summary>
        ///     既定テンプレートの手順を持つインスタンスを生成する。
        /// </summary>
        /// <remarks>
        ///     <see cref="AssetStoreToolsCombinePackageStrategy" />は含めない。
        ///     統合パッケージが差分インポートの単位にならないため、
        ///     必要な利用者だけが自分でパイプラインへ追加する位置付けにしている。
        /// </remarks>
        /// <returns> Singles → Used Dependencies → Create ZIP を持つ未保存のインスタンス。 </returns>
        internal static AssetStoreToolsPackagePipeline CreateTemplate()
        {
            var pipeline = CreateInstance<AssetStoreToolsPackagePipeline>();
            pipeline._steps = new List<AssetStoreToolsPackageStepStrategy>
            {
                new AssetStoreToolsSinglePackageStrategy(),
                new AssetStoreToolsUsedDependenciesStrategy(),
                new AssetStoreToolsCreateZipStrategy(),
            };

            return pipeline;
        }
    }
}
