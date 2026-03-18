using UnityEngine;

namespace SymphonyFrameWork.System.SaveSystem
{
    public interface ISaveDataLoader<T>
        where T : class,new()
    {
        public SaveData<T> Load();
        public SaveData<T> Save(T data);
    }
}
