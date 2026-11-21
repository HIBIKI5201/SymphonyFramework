using SymphonyFramework.Scenes;
using SymphonyFramework.Services;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SymphonyFramework.Samples.PartituraSample
{
    /// <summary> Partituraの取得、待機、シーン遷移時の保持を順に実演する。 </summary>
    public sealed class PartituraSample_Sequences : MonoBehaviour
    {
        /// <summary> Partituraのサンプルシーケンスを開始する。 </summary>
        private void Start()
        {
            _ = Sequence();
        }

        /// <summary> 登録取得、Single相当のシーン再読込、遅延登録待機を実行する。 </summary>
        private async ValueTask Sequence()
        {
            StringBuilder logBuilder = new StringBuilder();
            string currentSceneName = SceneManager.GetActiveScene().name;

            Camera camera = Partitura.Get<Camera>();
            PartituraSample_1 partituraSample_1 = Partitura.Get<PartituraSample_1>();

            logBuilder.AppendLine($"Camera instance retrieved from Partitura | name: {camera.name}, id: {camera.GetInstanceID()}");
            logBuilder.AppendLine($"PartituraSample_1 instance retrieved from Partitura | name: {partituraSample_1.name}, id: {partituraSample_1.GetInstanceID()}");
            Debug.Log(logBuilder.ToString());
            logBuilder.Clear();

            await Awaitable.WaitForSecondsAsync(3f, destroyCancellationToken);

            Debug.Log("Reloading the current scene...");
            await Attacca.UnloadAsync(currentSceneName);
            await Attacca.LoadAsync(currentSceneName, mode: LoadSceneMode.Single, priority: 1);
            Debug.Log("Reloading done.");

            camera = Partitura.Get<Camera>();

            logBuilder.AppendLine($"Camera instance retrieved from Partitura | name: {camera.name}, id: {camera.GetInstanceID()}");
            logBuilder.AppendLine($"PartituraSample_1 still exists because it is a singleton. | name: {partituraSample_1.name}, id: {partituraSample_1.GetInstanceID()}");
            Debug.Log(logBuilder.ToString());
            logBuilder.Clear();

            Debug.Log($"wait for register PartituraSample_2");
            PartituraSample_2 partituraSample_2 = await Partitura.GetAsync<PartituraSample_2>(10, destroyCancellationToken);
            Debug.Log($"PartituraSample_2 was registered | {partituraSample_2.name}, id: {partituraSample_2.GetInstanceID()}");

            // コンストラクタ注入では、依存を渡す側のコードを型ごとに書かなくてよい。
            PartituraSample_Consumer consumer = Partitura.CreateInstance<PartituraSample_Consumer>();
            Debug.Log($"Constructor injection | {consumer.Describe()}");
        }
    }
}
