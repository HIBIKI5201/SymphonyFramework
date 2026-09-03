using SymphonyFrameWork.System;

using UnityEngine;

namespace SymphonyFrameWork.Samples.PauseManagerSample
{
    /// <summary>
    ///     Pause中は停止する往復移動オブジェクトの共通部分。
    /// </summary>
    /// <remarks>
    ///     **属するカテゴリーだけが派生クラスごとに違う。** 移動と購読の手順は同じであり、
    ///     カテゴリーを分けることで「片方だけ止める」が成立することを示す。
    /// </remarks>
    public abstract class PauseManagerSample_MoverBase : MonoBehaviour, PauseManager.IPausable
    {
        [SerializeField, Tooltip("往復移動の中心からの最大距離。")]
        private float _range = 3f;

        [SerializeField, Tooltip("往復移動の速さ。")]
        private float _speed = 2f;

        [SerializeField, Tooltip("往復移動を可視化するCubeの色。")]
        private Color _color = Color.white;

        private bool _paused;
        private float _elapsed;

        /// <summary> 往復移動を可視化するCubeを生成する。 </summary>
        protected virtual void Awake()
        {
            Transform visual = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            visual.SetParent(transform, worldPositionStays: false);
            visual.localPosition = Vector3.zero;

            // 2つのカテゴリーを見分けられるよう、Cubeの色を変える。
            if (visual.TryGetComponent(out Renderer renderer))
            {
                renderer.material.color = _color;
            }
        }

        /// <summary> PauseManagerへポーズ通知の購読を登録する。 </summary>
        private void OnEnable()
        {
            PauseManager.IPausable.RegisterPauseManager(this);
        }

        /// <summary> PauseManagerへの購読を解除する。 </summary>
        private void OnDisable()
        {
            PauseManager.IPausable.UnregisterPauseManager(this);
        }

        /// <summary> Pause中でなければ往復移動を進める。 </summary>
        private void Update()
        {
            if (_paused)
            {
                return;
            }

            _elapsed += Time.deltaTime * _speed;
            float x = Mathf.Sin(_elapsed) * _range;
            transform.position = new Vector3(x, transform.position.y, transform.position.z);
        }

        /// <summary> 移動を停止する。 </summary>
        public void Pause() => _paused = true;

        /// <summary> 移動を再開する。 </summary>
        public void Resume() => _paused = false;

        /// <summary> 見た目の色を設定する。 </summary>
        /// <param name="color"> 設定する色。 </param>
        /// <remarks> 実行時に生成する場合、Awakeより前に呼ぶ。 </remarks>
        public void SetColor(Color color) => _color = color;
    }
}
