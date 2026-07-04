using System.Collections.Generic;
using UnityEngine;

namespace SymphonyFrameWork.Config
{
    /// <summary>
    ///     シーンマネージャーのコンフィグを格納する
    /// </summary>
    public class SceneManagerConfig : ScriptableObject
    {
        public bool IsResetAndLoadOnPlay => _isResetAndLoadOnPlay;
        public string[] InitializeSceneList => _initializeSceneList;
        public string[] ResetIgnoreSceneList => _resetIgnoreSceneList;

        [SerializeField, Tooltip("エディタでの再生時にシーンをリセットしてロードを実行するか")]
        private bool _isResetAndLoadOnPlay;

        [SerializeField, Tooltip("初期化時にロードするシーン")]
        private string[] _initializeSceneList;
        [SerializeField, Tooltip("リセットしてもアンロードされないシーン")]
        private string[] _resetIgnoreSceneList;
    }
}