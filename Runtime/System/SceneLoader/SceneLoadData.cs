using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace SymphonyFrameWork
{
    public class SceneLoadData
    {
        public SceneLoadData() { }

        private static readonly Dictionary<string, Scene> _sceneDict = new();
        private static readonly Dictionary<string, Action> _loadedActionDict = new();
    }
}
