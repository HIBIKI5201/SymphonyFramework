using System.Collections.Generic;
using SymphonyFrameWork.Attribute;
using UnityEngine;

namespace SymphonyFrameWork.Config
{
    /// <summary>
    ///     シーンマネージャーのコンフィグを格納する
    /// </summary>
    public class SceneManagerConfig : ScriptableObject
    {
        [DisplayText("開発中の機能です")] [Space] [SerializeField] [Tooltip("ロードシーンを有効化するかどうか")]
        private bool _isActiveLoadScene;

        [SerializeField] [Tooltip("ロード中に表示されるシーン")]
        private string _loadScene;

        [SerializeField] [Tooltip("初期化時にロードするシーン")]
        private List<string> _initializeSceneList;

        public bool IsActiveLoadScene => _isActiveLoadScene;
        public string LoadScene => _loadScene;
        public List<string> InitializeSceneList => _initializeSceneList;
    }
}