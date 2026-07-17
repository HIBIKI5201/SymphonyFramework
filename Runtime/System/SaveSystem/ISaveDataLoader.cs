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
}
