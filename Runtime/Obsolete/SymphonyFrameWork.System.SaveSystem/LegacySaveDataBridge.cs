namespace SymphonyFrameWork.System.SaveSystem
{
    /// <summary>
    ///     新しいセーブデータを、6.x の形の読み書き方式へ渡すための変換。
    /// </summary>
    /// <remarks> 7.0.0 の移行期間だけ残す。8.0.0 で削除する。 </remarks>
#pragma warning disable CS0618 // 旧型への橋渡しそのものが目的のため、旧型の参照を警告しない。
    internal static class LegacySaveDataBridge
    {
        #region 内部処理

        /// <summary>
        ///     新しいセーブデータを旧 <see cref="SaveDataContent" /> として取り出す。
        /// </summary>
        /// <param name="dataType"> セーブデータの型。 </param>
        /// <param name="data"> 新しい基底型で渡されたセーブデータ。 </param>
        /// <returns> 旧型として扱えるセーブデータ。null はそのまま返す。 </returns>
        /// <exception cref="global::System.InvalidOperationException"> 旧型を継承していないデータ型を渡した場合。 </exception>
        internal static SaveDataContent AsLegacy(
            global::System.Type dataType,
            SymphonyFramework.SaveData.SaveDataContent data)
        {
            // null の扱いは派生型の実装に任せ、橋渡しでは変えない。
            if (data == null) { return null; }

            // 旧型を継承したデータだけを旧シグネチャへ渡せる。新しい基底だけのデータは移行の途中と判断して止める。
            return data as SaveDataContent
                ?? throw new global::System.InvalidOperationException(
                    $"{dataType?.Name ?? data.GetType().Name} は旧 SymphonyFrameWork.System.SaveSystem.SaveDataContent を継承していないため、"
                    + "旧 SaveDataLoaderStrategy では読み書きできません。ローダーの using を SymphonyFramework.SaveData へ書き換えてください。");
        }

        #endregion
    }
#pragma warning restore CS0618
}
