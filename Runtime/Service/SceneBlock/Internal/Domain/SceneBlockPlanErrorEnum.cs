namespace SymphonyFrameWork.System.SceneBlock
{
    /// <summary>
    ///     Scene Blockの依存グラフで検出した異常の種類を表す。
    /// </summary>
    internal enum SceneBlockPlanErrorEnum
    {
        /// <summary> ノード識別子が重複している。 </summary>
        DuplicateNode,

        /// <summary> ノードが自身へ依存している。 </summary>
        SelfDependency,

        /// <summary> 依存辺が存在しないノードを参照している。 </summary>
        MissingReference,

        /// <summary> ノード間に循環依存がある。 </summary>
        CyclicDependency
    }
}
