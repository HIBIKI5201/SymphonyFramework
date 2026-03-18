using System.Threading.Tasks;
using UnityEngine;

namespace SymphonyFrameWork.System.SaveSystem
{
    public interface ISaveDataLoader<T>
        where T : class,new()
    {
        public ValueTask<SaveData<T>> Load();
        public ValueTask<SaveData<T>> Save(T data);
    }
}
