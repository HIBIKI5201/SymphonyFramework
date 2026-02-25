using UnityEngine;

namespace SymphonyFrameWork.System.SceneLoad
{
    public enum SceneLoadState : int
    {
        None = -1,
        Loading = 0,
        Complete = 1,
        Unloading = 2
    }
}
