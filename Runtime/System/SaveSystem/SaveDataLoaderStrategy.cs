using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     利用側がJSON変換と永続化処理を差し替える抽象Strategyを提供する。
    /// </summary>
    /// <remarks>
    ///     セーブデータのライフサイクルは基底型が保証し、操作は<see cref="SaveStore"/>が呼び出す。
    /// </remarks>
    [Serializable]
    public abstract class SaveDataLoaderStrategy
    {
        #region 内部処理

        /// <summary>
        ///     指定した型の永続化データが存在するか確認する。
        /// </summary>
        /// <param name="dataType"> 確認するセーブデータ型。 </param>
        /// <returns> 永続化データが存在する場合はtrue。 </returns>
        internal bool Exists(Type dataType)
        {
            // 保存先へ不正な型を渡さないよう、I/Oより先に共通契約を検証する。
            ValidateDataType(dataType);
            return ExistsCore(dataType);
        }

        /// <summary>
        ///     永続化されたJSONを指定インスタンスへ復元する。
        /// </summary>
        /// <param name="dataType"> 復元するセーブデータ型。 </param>
        /// <param name="data"> 復元結果を上書きするインスタンス。 </param>
        /// <param name="token"> 処理を中断するためのトークン。 </param>
        internal async Task LoadAsync(
            Type dataType,
            SaveDataContent data,
            CancellationToken token = default)
        {
            // I/O開始前に型、復元先、キャンセル状態を検証する。
            Validate(dataType, data, token);
            string json = await LoadJsonAsync(dataType, token);

            // 保存値が無い場合は新規データとして既定状態を生成し、正常なロード完了として扱う。
            if (string.IsNullOrEmpty(json))
            {
                ResetToDefault(dataType, data);
                Debug.Log($"[{GetType().Name}]\n{dataType.Name} のデータが見つからないので生成しました。");
                return;
            }

            try
            {
                // 保存時に存在しなかったフィールドへ型の既定値を適用してから、保存内容を上書きする。
                ResetToDefault(dataType, data);
                OverwriteFromJson(dataType, json, data);
            }
            catch (Exception ex)
            {
                // 本体データの破損や形式不一致ではロード全体を失敗させず、安全な既定状態へ戻す。
                ResetToDefault(dataType, data);
                Debug.LogWarning(
                    $"[{GetType().Name}]\n{dataType.Name} の本体データ復元に失敗しました。新たなインスタンス状態へ戻します。\n{ex.Message}");
            }
        }

        /// <summary>
        ///     指定インスタンスをJSONへ変換して永続化する。
        /// </summary>
        /// <param name="dataType"> 保存するセーブデータ型。 </param>
        /// <param name="data"> 保存するインスタンス。 </param>
        /// <param name="token"> 処理を中断するためのトークン。 </param>
        internal async Task SaveAsync(
            Type dataType,
            SaveDataContent data,
            CancellationToken token = default)
        {
            // シリアライズやI/Oより先に型、保存内容、キャンセル状態を検証する。
            Validate(dataType, data, token);

            // 書き込み失敗時にメモリ上の保存日時だけが進まないよう、更新前の値を退避する。
            string previousSaveDate = data.SaveDate;
            data.UpdateSaveDate();

            try
            {
                // 保存日時を含む現在状態を派生形式へ変換し、派生先のI/Oへ渡す。
                string json = SerializeToJson(dataType, data);
                await SaveJsonAsync(dataType, json, token);
            }
            catch
            {
                // 永続化に失敗した場合は、保存済み状態を示す日時を操作前へ戻して再送出する。
                data.SaveDate = previousSaveDate;
                throw;
            }

            Debug.Log($"[{GetType().Name}]\nデータをセーブしました。 date : {data.SaveDate}\n{data}");
        }

        /// <summary>
        ///     指定した型の永続化データを削除する。
        /// </summary>
        /// <param name="dataType"> 削除するセーブデータ型。 </param>
        /// <param name="token"> 処理を中断するためのトークン。 </param>
        internal async Task DeleteAsync(Type dataType, CancellationToken token = default)
        {
            // 無効な型や開始前のキャンセルでは保存先へ副作用を発生させない。
            ValidateDataType(dataType);
            token.ThrowIfCancellationRequested();
            await DeleteCoreAsync(dataType, token);
        }

        /// <summary>
        ///     派生ローダー固有の保存先にデータが存在するか確認する。
        /// </summary>
        protected abstract bool ExistsCore(Type dataType);

        /// <summary>
        ///     派生ローダー固有の保存先からJSONを読み込む。
        /// </summary>
        protected abstract Awaitable<string> LoadJsonAsync(Type dataType, CancellationToken token);

        /// <summary>
        ///     派生ローダー固有の保存先へJSONを書き込む。
        /// </summary>
        protected abstract Awaitable SaveJsonAsync(Type dataType, string json, CancellationToken token);

        /// <summary>
        ///     派生ローダー固有の保存先からデータを削除する。
        /// </summary>
        protected abstract Awaitable DeleteCoreAsync(Type dataType, CancellationToken token);

        /// <summary>
        ///     指定インスタンスを派生ローダーのJSON形式へ変換する。
        /// </summary>
        protected abstract string SerializeToJson(Type dataType, SaveDataContent data);

        /// <summary>
        ///     JSONの内容を既存のセーブデータへ上書きする。
        /// </summary>
        protected abstract void OverwriteFromJson(Type dataType, string json, SaveDataContent data);

        /// <summary>
        ///     対象インスタンスを指定型の既定状態へ戻す。
        /// </summary>
        private void ResetToDefault(Type dataType, SaveDataContent target)
        {
            // 利用側のデフォルトコンストラクタを既定値の正本として一時インスタンスを生成する。
            SaveDataContent defaultData = (SaveDataContent)Activator.CreateInstance(dataType);

            try
            {
                // Serializer固有の規則を揃えるため、既定値も同じJSON経路で既存キャッシュへ転写する。
                string defaultJson = SerializeToJson(dataType, defaultData);
                OverwriteFromJson(dataType, defaultJson, target);
                target.ClearSaveDate();
            }
            finally
            {
                // 一時インスタンスが利用側資源を保持していても、成功失敗に関係なく解放する。
                defaultData.Dispose();
            }
        }

        /// <summary>
        ///     セーブデータ型、インスタンス、キャンセル状態を検証する。
        /// </summary>
        private static void Validate(Type dataType, SaveDataContent data, CancellationToken token)
        {
            // 型契約を先に確定し、以降のインスタンス判定を安全に行う。
            ValidateDataType(dataType);

            // 復元先または保存元が無い要求は、Serializerへ渡す前に拒否する。
            if (data == null) { throw new ArgumentNullException(nameof(data)); }

            // 指定型と実体が異なると保存形式の型契約が崩れるため拒否する。
            if (!dataType.IsInstanceOfType(data))
            {
                throw new ArgumentException($"{dataType.Name} のインスタンスを指定してください。", nameof(data));
            }

            // 検証中に届いたキャンセルでもI/Oを開始しない。
            token.ThrowIfCancellationRequested();
        }

        /// <summary>
        ///     セーブ対象として生成可能な具象型であることを検証する。
        /// </summary>
        private static void ValidateDataType(Type dataType)
        {
            // 型情報が無い要求は後続のリフレクションへ渡さず、引数違反として通知する。
            if (dataType == null) { throw new ArgumentNullException(nameof(dataType)); }

            // 既定値生成と基底ライフサイクルの両方を保証できる具象クラスだけを受け入れる。
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

        #endregion
    }
}
