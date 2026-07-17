using System;
using System.Threading;
using System.Threading.Tasks;

namespace SymphonyFrameWork.System.SaveSystem
{
    public interface ISaveDataLoader
    {
        bool Exists(Type dataType);

        ValueTask<SaveDataContent> LoadAsync(Type dataType, CancellationToken token = default);

        ValueTask<SaveDataContent> SaveAsync(Type dataType, SaveDataContent data, CancellationToken token = default);

        ValueTask DeleteAsync(Type dataType, CancellationToken token = default);
    }
}
