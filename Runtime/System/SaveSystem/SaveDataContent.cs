using System;

namespace SymphonyFrameWork.System.SaveSystem
{
    [Serializable]
    public abstract class SaveDataContent : IDisposable
    {
        public string SaveDate;

        public void UpdateSaveDate(DateTime saveDate = default)
        {
            if (saveDate == default)
            {
                saveDate = DateTime.Now;
            }

            SaveDate = saveDate.ToString("O");
        }

        public virtual void Dispose()
        {
            SaveDate = null;
        }
    }
}
