using System;
using UnityEngine.Scripting.APIUpdating;

namespace SymphonyFramework.Services
{
    /// <summary>
    ///     必須サービスがPartituraへ登録されていない場合に発生する例外。
    /// </summary>
    // 6.x の名前（SymphonyFrameWork.System.ServiceLocate）を使う利用側スクリプトを、Unity の API Updater が書き換えるための対応。
    [MovedFrom(
        true,
        sourceNamespace: "SymphonyFrameWork.System.ServiceLocate",
        sourceAssembly: "SymphonyFrameWork")]
    public sealed class ServiceNotRegisteredException : Exception
    {
        #region 外部向けAPI

        /// <summary>
        ///     未登録だったサービス型を指定して例外を生成する。
        /// </summary>
        /// <param name="serviceType"> 取得を要求されたサービス型。 </param>
        public ServiceNotRegisteredException(Type serviceType)
            : base($"[{nameof(Partitura)}] 必須サービス {serviceType?.FullName ?? "(null)"} が登録されていません。")
        {
            // 取得に失敗した型を、メッセージとは別に呼び出し側が参照できる形で保持する。
            ServiceType = serviceType;
        }

        /// <summary> 取得を要求された未登録のサービス型。 </summary>
        public Type ServiceType { get; }

        #endregion
    }
}
