using UnityEngine;

namespace SymphonyFrameWork.Samples.ServiceLocatorSample
{
    /// <summary> コンストラクタで依存を受け取るピュアC#の型。ServiceInjector.CreateInstanceで生成される。 </summary>
    public sealed class ServiceLocatorSample_Consumer
    {
        private readonly Camera _camera;
        private readonly ServiceLocatorSample_1 _sample1;
        private readonly int _retryCount;

        /// <summary> 依存2件と、登録しない設定値1件を受け取る。 </summary>
        /// <param name="camera"> Service Locatorから解決されるCamera。 </param>
        /// <param name="sample1"> Service Locatorから解決されるサンプルComponent。 </param>
        /// <param name="retryCount"> Service Locatorに登録が無いため既定値が使われる設定値。 </param>
        public ServiceLocatorSample_Consumer(
            Camera camera,
            ServiceLocatorSample_1 sample1,
            int retryCount = 3)
        {
            // コンストラクタで受け取るため、未注入の状態が存在しない。
            _camera = camera;
            _sample1 = sample1;
            _retryCount = retryCount;
        }

        /// <summary> 受け取った依存を1行の説明文にする。 </summary>
        /// <returns> 実況ログへ出す説明文。 </returns>
        public string Describe() =>
            $"Camera: {_camera.name}, Sample1: {_sample1.name}, RetryCount: {_retryCount} (default value)";
    }
}
