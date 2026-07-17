using System.Threading;
using System.Threading.Tasks;

namespace SymphonyFrameWork.System.SaveSystem
{
    public interface ISaveDataLoader
    {
        bool Exists<T>() where T : class, new();

        ValueTask<SaveData<T>> LoadAsync<T>(CancellationToken token = default) where T : class, new();

        ValueTask<SaveData<T>> SaveAsync<T>(T data, CancellationToken token = default) where T : class, new();

        ValueTask DeleteAsync<T>(CancellationToken token = default) where T : class, new();
    }
}
