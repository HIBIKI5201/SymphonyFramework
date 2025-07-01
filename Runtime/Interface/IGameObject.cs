using System.Threading;
using UnityEngine;

namespace SymphonyFrameWork
{
    public interface IGameObject
    {
        GameObject gameObject { get; }
        Transform transform { get; }

        CancellationToken destroyCancellationToken { get; }
    }
}
