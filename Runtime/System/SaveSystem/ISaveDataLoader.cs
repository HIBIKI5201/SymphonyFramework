using System;
using System.Threading;
using System.Threading.Tasks;

namespace SymphonyFrameWork.System.SaveSystem
{
    public interface ISaveDataLoader
    {
        bool Exists(Type dataType);

        ValueTask<SaveData<object>> LoadAsync(Type dataType, CancellationToken token = default);

        ValueTask<SaveData<object>> SaveAsync(Type dataType, object data, CancellationToken token = default);

        ValueTask DeleteAsync(Type dataType, CancellationToken token = default);
    }

    public interface ISaveDataLoader<T>
        where T : class, new()
    {
        ValueTask<SaveData<T>> Load();
        ValueTask<SaveData<T>> Save(T data);
    }
}
