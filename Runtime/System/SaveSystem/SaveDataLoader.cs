using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     セーブデータのライフサイクルを保証し、派生クラスをJSONの変換と永続化処理に限定します。
    /// </summary>
    [Serializable]
    public abstract class SaveDataLoader
    {
        public bool Exists(Type dataType)
        {
            ValidateDataType(dataType);
            return ExistsCore(dataType);
        }

        public async ValueTask LoadAsync(
            Type dataType,
            SaveDataContent data,
            CancellationToken token = default)
        {
            Validate(dataType, data, token);
            string json = await LoadJsonAsync(dataType, token);

            if (string.IsNullOrEmpty(json))
            {
                ResetToDefault(dataType, data);
                Debug.Log($"[{GetType().Name}]\n{dataType.Name} のデータが見つからないので生成しました。");
                return;
            }

            try
            {
                ResetToDefault(dataType, data);
                OverwriteFromJson(dataType, json, data);
            }
            catch (Exception ex)
            {
                ResetToDefault(dataType, data);
                Debug.LogWarning(
                    $"[{GetType().Name}]\n{dataType.Name} の本体データ復元に失敗しました。新たなインスタンス状態へ戻します。\n{ex.Message}");
            }
        }

        public async ValueTask SaveAsync(
            Type dataType,
            SaveDataContent data,
            CancellationToken token = default)
        {
            Validate(dataType, data, token);

            string previousSaveDate = data.SaveDate;
            data.UpdateSaveDate();

            try
            {
                string json = SerializeToJson(dataType, data);
                await SaveJsonAsync(dataType, json, token);
            }
            catch
            {
                data.SaveDate = previousSaveDate;
                throw;
            }

            Debug.Log($"[{GetType().Name}]\nデータをセーブしました。 date : {data.SaveDate}\n{data}");
        }

        public ValueTask DeleteAsync(Type dataType, CancellationToken token = default)
        {
            ValidateDataType(dataType);
            token.ThrowIfCancellationRequested();
            return DeleteCoreAsync(dataType, token);
        }

        protected abstract bool ExistsCore(Type dataType);

        protected abstract ValueTask<string> LoadJsonAsync(Type dataType, CancellationToken token);

        protected abstract ValueTask SaveJsonAsync(Type dataType, string json, CancellationToken token);

        protected abstract ValueTask DeleteCoreAsync(Type dataType, CancellationToken token);

        protected abstract string SerializeToJson(Type dataType, SaveDataContent data);

        protected abstract void OverwriteFromJson(Type dataType, string json, SaveDataContent data);

        private void ResetToDefault(Type dataType, SaveDataContent target)
        {
            SaveDataContent defaultData = (SaveDataContent)Activator.CreateInstance(dataType);

            try
            {
                string defaultJson = SerializeToJson(dataType, defaultData);
                OverwriteFromJson(dataType, defaultJson, target);
                target.ClearSaveDate();
            }
            finally
            {
                defaultData.Dispose();
            }
        }

        private static void Validate(Type dataType, SaveDataContent data, CancellationToken token)
        {
            ValidateDataType(dataType);

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (!dataType.IsInstanceOfType(data))
            {
                throw new ArgumentException($"{dataType.Name} のインスタンスを指定してください。", nameof(data));
            }

            token.ThrowIfCancellationRequested();
        }

        private static void ValidateDataType(Type dataType)
        {
            if (dataType == null)
            {
                throw new ArgumentNullException(nameof(dataType));
            }

            if (!dataType.IsClass
                || dataType.IsAbstract
                || dataType.IsGenericTypeDefinition
                || dataType.GetConstructor(Type.EmptyTypes) == null
                || !typeof(SaveDataContent).IsAssignableFrom(dataType))
            {
                throw new ArgumentException(
                    $"セーブ対象は {nameof(SaveDataContent)} を継承したデフォルトコンストラクタ付き具象クラスにしてください。",
                    nameof(dataType));
            }
        }
    }
}
