using System;
using System.Threading;
using System.Threading.Tasks;

namespace SymphonyFrameWork.System.SaveSystem
{
    public interface ISaveDataLoader
    {
        bool Exists(Type dataType);

        ValueTask<SaveData> LoadAsync(Type dataType, CancellationToken token = default);

        ValueTask<SaveData> SaveAsync(Type dataType, SaveDataContent data, CancellationToken token = default);

        ValueTask DeleteAsync(Type dataType, CancellationToken token = default);
    }
}
