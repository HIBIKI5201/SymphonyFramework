using UnityEngine;

namespace SymphonyFrameWork.System.SaveSystem
{
    public interface SaveDataLoader<T>
        where T : class,new()
    {
        public SaveData<T> Load();
        public SaveData<T> Save(T data);
    }
}
