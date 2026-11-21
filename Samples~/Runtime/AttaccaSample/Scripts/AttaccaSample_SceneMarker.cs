using UnityEngine;

namespace SymphonyFramework.Samples.AttaccaSample
{
    /// <summary> 追加ロードされるサブシーンの生存確認用マーカー。 </summary>
    public sealed class AttaccaSample_SceneMarker : MonoBehaviour
    {
        [SerializeField, Tooltip("実況ログに表示するシーンの表示名。")]
        private string _sceneLabel = "Scene";

        /// <summary> シーンロードによってこのオブジェクトが有効化されたことをログへ出力する。 </summary>
        private void OnEnable()
        {
            Debug.Log($"[AttaccaSample] {_sceneLabel} is now active in hierarchy.");
        }

        /// <summary> シーンアンロードによってこのオブジェクトが破棄される直前にログへ出力する。 </summary>
        private void OnDisable()
        {
            Debug.Log($"[AttaccaSample] {_sceneLabel} is being removed from hierarchy.");
        }
    }
}
