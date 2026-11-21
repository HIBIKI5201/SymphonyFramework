using System.Collections.Generic;
using System.Threading;

using SymphonyFramework.SaveData;

using UnityEngine;

namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     6.x の <c>SaveStore</c> を <see cref="Manuscript" /> へ転送する。
    /// </summary>
    /// <remarks>
    ///     7.0.0 の移行期間だけ残す。8.0.0 で削除する。
    ///     <b><see cref="GetEntries" /> は、6.x と違い未初期化のとき例外を投げる（6.19.0 の変更）。</b>
    /// </remarks>
    [global::System.Obsolete("SaveStore は Manuscript（名前空間 SymphonyFramework.SaveData）へ改名しました。Manuscript を使用してください。8.0.0 で削除します。")]
    public static class SaveStore
    {
        #region 外部向けAPI

        /// <summary> <see cref="Manuscript.Exists{T}" /> へ転送する。 </summary>
        public static bool Exists<T>() where T : SymphonyFramework.SaveData.SaveDataContent, new() =>
            Manuscript.Exists<T>();

        /// <summary> <see cref="Manuscript.Exists(global::System.Type)" /> へ転送する。 </summary>
        public static bool Exists(global::System.Type dataType) => Manuscript.Exists(dataType);

        /// <summary> <see cref="Manuscript.Get{T}" /> へ転送する。 </summary>
        public static T Get<T>() where T : SymphonyFramework.SaveData.SaveDataContent, new() => Manuscript.Get<T>();

        /// <summary> <see cref="Manuscript.Get(global::System.Type)" /> へ転送する。 </summary>
        /// <remarks> 戻り値は新しい <see cref="SymphonyFramework.SaveData.SaveDataContent" />。 </remarks>
        public static SymphonyFramework.SaveData.SaveDataContent Get(global::System.Type dataType) =>
            Manuscript.Get(dataType);

        /// <summary> <see cref="Manuscript.IsLoaded{T}" /> へ転送する。 </summary>
        public static bool IsLoaded<T>() where T : SymphonyFramework.SaveData.SaveDataContent, new() =>
            Manuscript.IsLoaded<T>();

        /// <summary> <see cref="Manuscript.IsLoaded(global::System.Type)" /> へ転送する。 </summary>
        public static bool IsLoaded(global::System.Type dataType) => Manuscript.IsLoaded(dataType);

        /// <summary> <see cref="Manuscript.LoadAsync{T}" /> へ転送する。 </summary>
        public static Awaitable<T> LoadAsync<T>(CancellationToken token = default)
            where T : SymphonyFramework.SaveData.SaveDataContent, new() =>
            Manuscript.LoadAsync<T>(token);

        /// <summary> <see cref="Manuscript.LoadAsync(global::System.Type, CancellationToken)" /> へ転送する。 </summary>
        public static Awaitable LoadAsync(global::System.Type dataType, CancellationToken token = default) =>
            Manuscript.LoadAsync(dataType, token);

        /// <summary> <see cref="Manuscript.SaveAsync{T}" /> へ転送する。 </summary>
        public static Awaitable SaveAsync<T>(CancellationToken token = default)
            where T : SymphonyFramework.SaveData.SaveDataContent, new() =>
            Manuscript.SaveAsync<T>(token);

        /// <summary> <see cref="Manuscript.SaveAsync(global::System.Type, CancellationToken)" /> へ転送する。 </summary>
        public static Awaitable SaveAsync(global::System.Type dataType, CancellationToken token = default) =>
            Manuscript.SaveAsync(dataType, token);

        /// <summary> <see cref="Manuscript.DeleteAsync{T}" /> へ転送する。 </summary>
        public static Awaitable DeleteAsync<T>(CancellationToken token = default)
            where T : SymphonyFramework.SaveData.SaveDataContent, new() =>
            Manuscript.DeleteAsync<T>(token);

        /// <summary> <see cref="Manuscript.DeleteAsync(global::System.Type, CancellationToken)" /> へ転送する。 </summary>
        public static Awaitable DeleteAsync(global::System.Type dataType, CancellationToken token = default) =>
            Manuscript.DeleteAsync(dataType, token);

        /// <summary> <see cref="Manuscript.GetEntries" /> へ転送する。 </summary>
        public static IReadOnlyList<SaveDataEntryInfo> GetEntries() => Manuscript.GetEntries();

        #endregion
    }
}
