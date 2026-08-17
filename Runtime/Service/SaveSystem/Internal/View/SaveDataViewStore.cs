using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     Save DataのView操作と問い合わせを集約する。
    /// </summary>
    internal sealed class SaveDataViewStore
    {
        #region 外部向けAPI

        /// <summary>
        ///     問い合わせ先と操作先を指定して生成する。
        /// </summary>
        /// <param name="query"> 表示に必要な状態の問い合わせ先。 </param>
        /// <param name="service"> Save Data操作の委譲先。 </param>
        internal SaveDataViewStore(SaveDataQuery query, SaveDataService service)
        {
            // 問い合わせと操作のどちらが欠けてもViewの窓口として成立しないため、生成時に拒否する。
            _query = query ?? throw new ArgumentNullException(nameof(query));
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary> 現在選択されているローダーの型名。 </summary>
        internal string CurrentLoaderName => _service.GetCurrentLoader().GetType().Name;

        /// <summary>
        ///     指定型が読み込み済みか確認する。
        /// </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        /// <returns> 読み込み済みの場合はtrue。 </returns>
        internal bool IsLoaded(Type dataType) =>
            TryGetEntry(dataType, out SaveDataEntryInfo entry) && entry.IsLoaded;

        /// <summary>
        ///     指定型の永続化データが存在するか確認する。
        /// </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        /// <returns> 永続化データが存在する場合はtrue。 </returns>
        internal bool Exists(Type dataType) => _service.Exists(dataType);

        /// <summary>
        ///     指定型の読み込み済みインスタンスを取得する。
        /// </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        /// <returns> 読み込み済みのRegistry正本。未読み込みの場合はnull。 </returns>
        internal SaveDataContent GetLoadedContent(Type dataType) =>
            TryGetEntry(dataType, out SaveDataEntryInfo entry) && entry.IsLoaded
                ? entry.Data
                : null;

        /// <summary>
        ///     指定型の永続化データをRegistryへ読み込む。
        /// </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        /// <param name="token"> 処理を中断するためのトークン。 </param>
        /// <returns> 読み込みの完了を表すTask。 </returns>
        internal Task LoadAsync(Type dataType, CancellationToken token = default) =>
            _service.LoadAsync(dataType, token);

        /// <summary>
        ///     指定型のRegistry正本を保存先へ書き込む。
        /// </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        /// <param name="token"> 処理を中断するためのトークン。 </param>
        /// <returns> 保存の完了を表すTask。 </returns>
        internal Task SaveAsync(Type dataType, CancellationToken token = default) =>
            _service.SaveAsync(dataType, token);

        /// <summary>
        ///     指定型の永続化データを削除し、Registry正本を既定値へ戻す。
        /// </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        /// <param name="token"> 処理を中断するためのトークン。 </param>
        /// <returns> 削除の完了を表すTask。 </returns>
        internal Task DeleteAsync(Type dataType, CancellationToken token = default) =>
            _service.DeleteAsync(dataType, token);

        /// <summary>
        ///     Registryを経由せず指定インスタンスへ永続化データを読み込む。
        /// </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        /// <param name="target"> 読み込み先のインスタンス。 </param>
        /// <param name="token"> 処理を中断するためのトークン。 </param>
        /// <returns> 読み込みの完了を表すTask。 </returns>
        internal Task LoadDetachedAsync(
            Type dataType,
            SaveDataContent target,
            CancellationToken token = default) =>
            _service.LoadDetachedAsync(dataType, target, token);

        /// <summary>
        ///     Registryを経由せず指定インスタンスを保存先へ書き込む。
        /// </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        /// <param name="source"> 保存するインスタンス。 </param>
        /// <param name="token"> 処理を中断するためのトークン。 </param>
        /// <returns> 保存の完了を表すTask。 </returns>
        internal Task SaveDetachedAsync(
            Type dataType,
            SaveDataContent source,
            CancellationToken token = default) =>
            _service.SaveDetachedAsync(dataType, source, token);

        /// <summary>
        ///     Registryを経由せず指定型の永続化データを削除する。
        /// </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        /// <param name="token"> 処理を中断するためのトークン。 </param>
        /// <returns> 削除の完了を表すTask。 </returns>
        internal Task DeleteDetachedAsync(Type dataType, CancellationToken token = default) =>
            _service.DeleteDetachedAsync(dataType, token);

        #endregion

        #region 内部処理

        private readonly SaveDataQuery _query;
        private readonly SaveDataService _service;

        /// <summary>
        ///     Queryの現在スナップショットから指定型のエントリを取得する。
        /// </summary>
        /// <param name="dataType"> 対象のセーブデータ型。 </param>
        /// <param name="entry"> 見つかったエントリ。 </param>
        /// <returns> 指定型のエントリが存在する場合はtrue。 </returns>
        private bool TryGetEntry(Type dataType, out SaveDataEntryInfo entry)
        {
            // Queryが同一時点で抽出した型、正本参照、ロード状態を一組のまま検索する。
            IReadOnlyList<SaveDataEntryInfo> entries = _query.GetInfos();
            foreach (SaveDataEntryInfo candidate in entries)
            {
                if (candidate.DataType != dataType) { continue; }

                entry = candidate;
                return true;
            }

            entry = default;
            return false;
        }

        #endregion
    }
}
