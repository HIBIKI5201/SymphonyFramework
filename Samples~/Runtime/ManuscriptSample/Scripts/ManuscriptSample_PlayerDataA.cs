using SymphonyFramework.SaveData;
using System;

namespace SymphonyFramework.Samples.ManuscriptSample
{
    /// <summary> プレイヤー名、レベル、所持金を保存するサンプルデータ。 </summary>
    [Serializable]
    public sealed class ManuscriptSample_PlayerDataA : SaveDataContent
    {
        /// <summary> 編集可能なプレイヤー名。 </summary>
        public string PlayerName = "Symphony";

        /// <summary> 編集可能なプレイヤーレベル。 </summary>
        public int Level = 1;

        /// <summary> 編集可能な所持金。 </summary>
        public int Gold = 100;
    }
}
