using System.Threading.Tasks;

namespace SymphonyFrameWork
{
    /// <summary>
    ///     非同期で初期化するインターフェース。
    /// </summary>
    public interface IInitializeAsync
    {
        #region 外部向けAPI

        /// <summary> 実行中または完了済みの初期化処理。 </summary>
        public Task InitializeTask { get; protected set; }

        /// <summary> 初期化処理が完了しているかを示す。 </summary>
        public bool IsDone => InitializeTask != null ? InitializeTask.IsCompleted : false;

        /// <summary>
        ///     初期化を開始する。
        /// </summary>
        /// <returns> 初期化処理を表すTask。 </returns>
        public async Task DoInitialize()
        {
            // 完了済みの初期化は再実行せず、同じ状態を維持する。
            if (IsDone) { return; }

            // 実行中のTaskを公開してから待機し、並行する呼び出しが同じ進行状態を参照できるようにする。
            InitializeTask = InitializeAsync();
            await InitializeTask;

            // 完了後は実装固有のTaskを保持せず、完了状態だけを軽量なTaskで表す。
            InitializeTask = Task.CompletedTask;
        }

        #endregion

        #region 内部処理

        /// <summary>
        ///     初期化のフローを実装する。
        /// </summary>
        /// <returns> 実装固有の初期化処理を表すTask。 </returns>
        protected Task InitializeAsync();

        #endregion
    }
}
