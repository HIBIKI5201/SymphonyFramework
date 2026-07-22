using SymphonyFrameWork.System.SaveSystem;
using System;

namespace SymphonyFrameWork.Samples.SaveDataSystemSample
{
    [Serializable]
    public class SaveDataSystemSample_PlayerDataA : SaveDataContent
    {
        public string PlayerName = "Symphony";
        public int Level = 1;
        public int Gold = 100;
    }
}
