using System;
using Newtonsoft.Json;
using SymphonyFrameWork.Attribute;
using UnityEngine;

namespace SymphonyFrameWork.System.SaveSystem
{
    [Serializable]
    public abstract class SaveDataContent : IDisposable
    {
        [ReadOnly]
        public string SaveDate;

        public void UpdateSaveDate(DateTime saveDate = default)
        {
            if (saveDate == default)
            {
                saveDate = DateTime.Now;
            }

            SaveDate = saveDate.ToString("O");
        }

        public void ClearSaveDate()
        {
            SaveDate = null;
        }

        public virtual void Dispose()
        {
            SaveDate = null;
        }
    }
}
