using System.Threading.Tasks;
using System;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     旧来の型ごとローダーAPIです。
    /// </summary>
    [Obsolete("Use ISaveDataLoader with generic methods instead.")]
    public interface ISaveDataLoader<T>
        where T : class, new()
    {
        ValueTask<SaveData<T>> Load();
        ValueTask<SaveData<T>> Save(T data);
    }
}
