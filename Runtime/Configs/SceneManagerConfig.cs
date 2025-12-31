using System.Collections.Generic;
using UnityEngine;

namespace SymphonyFrameWork.Config
{
    /// <summary>
    ///     シーンマネージャーのコンフィグを格納する
    /// </summary>
    public class SceneManagerConfig : ScriptableObject
    {
        [SerializeField, Tooltip("エディタでの再生時にシーンをリセットしてロードを実行するか")]
        private bool _isResetAndLoadOnPlay;

        [SerializeField, Tooltip("初期化時にロードするシーン")]
        private List<string> _initializeSceneList;

        public bool IsResetAndLoadOnPlay => _isResetAndLoadOnPlay;
        public List<string> InitializeSceneList => _initializeSceneList;
    }
}