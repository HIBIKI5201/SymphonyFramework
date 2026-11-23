using SymphonyFramework.SaveData;
using System;

namespace SymphonyFramework.Samples.ManuscriptSample
{
    /// <summary> 所持アイテムID一覧を保存するサンプルデータ。 </summary>
    [Serializable]
    public sealed class ManuscriptSample_PlayerDataB : SaveDataContent
    {
        /// <summary> 保存対象となる所持アイテムID一覧。 </summary>
        public int[] ItemIDs = Array.Empty<int>();
    }
}
