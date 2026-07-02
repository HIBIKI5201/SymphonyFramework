using SymphonyFrameWork.System.SceneLoad;
using SymphonyFrameWork.System.ServiceLocate;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SymphonyFrameWork.Samples.ServiceLocatorSample
{
    public class ServiceLocatorSample_Sequences : MonoBehaviour
    {
        private void Start()
        {
            _ = Sequence();
        }

        private async ValueTask Sequence()
        {
            StringBuilder logBuilder = new StringBuilder();
            string currentSceneName = SceneManager.GetActiveScene().name;

            Camera camera = ServiceLocator.GetInstance<Camera>();
            ServiceLocatorSample_1 serviceLocatorSample_1 = ServiceLocator.GetInstance<ServiceLocatorSample_1>();

            logBuilder.AppendLine($"Camera instance retrieved from ServiceLocator | name: {camera.name}, id: {camera.GetInstanceID()}");
            logBuilder.AppendLine($"ServiceLocatorSample_1 instance retrieved from ServiceLocator | name: {serviceLocatorSample_1.name}, id: {serviceLocatorSample_1.GetInstanceID()}");
            Debug.Log(logBuilder.ToString());
            logBuilder.Clear();

            await Awaitable.WaitForSecondsAsync(3f, destroyCancellationToken);

            Debug.Log("Reloading the current scene...");
            await SceneLoader.UnloadScene(currentSceneName);
            await SceneLoader.LoadScene(currentSceneName, mode: LoadSceneMode.Single, priority: 1);
            Debug.Log("Reloading done.");

            camera = ServiceLocator.GetInstance<Camera>();

            logBuilder.AppendLine($"Camera instance retrieved from ServiceLocator | name: {camera.name}, id: {camera.GetInstanceID()}");
            logBuilder.AppendLine($"ServiceLocatorSample_1 still exists because it is a singleton. | name: {serviceLocatorSample_1.name}, id: {serviceLocatorSample_1.GetInstanceID()}");
            Debug.Log(logBuilder.ToString());
            logBuilder.Clear();
        }
    }
}
