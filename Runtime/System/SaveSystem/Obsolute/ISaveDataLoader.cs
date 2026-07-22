using System.Threading.Tasks;
using System;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     旧来の型ごとローダーAPIです。
    /// </summary>
    [Obsolete("Use SaveDataLoader instead.")]
    public interface ISaveDataLoader<T>
        where T : SaveDataContent, new()
    {
        ValueTask<T> Load();
        ValueTask<T> Save(T data);
    }
}
