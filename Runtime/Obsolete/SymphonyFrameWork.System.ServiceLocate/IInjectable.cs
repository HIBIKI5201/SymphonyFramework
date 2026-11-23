namespace SymphonyFrameWork.System.ServiceLocate
{
    /// <summary>
    ///     6.x の依存注入対象の目印。<see cref="SymphonyFramework.Services.IInjectable" /> を継承する。
    /// </summary>
    /// <remarks>
    ///     7.0.0 の移行期間だけ残す。8.0.0 で削除する。
    ///     <c>Inject</c> を明示的に実装している場合（<c>void IInjectable&lt;T&gt;.Inject(...)</c>）は、
    ///     宣言元が新しい型になるためコンパイルできない。名前空間を <c>SymphonyFramework.Services</c> へ書き換える。
    /// </remarks>
    [global::System.Obsolete("IInjectable は名前空間 SymphonyFramework.Services へ移りました。using を書き換えてください。8.0.0 で削除します。")]
    public interface IInjectable : SymphonyFramework.Services.IInjectable
    {
    }

    /// <summary> 6.x の1引数の依存注入契約。新しい型を継承する。 </summary>
    [global::System.Obsolete("IInjectable は名前空間 SymphonyFramework.Services へ移りました。using を書き換えてください。8.0.0 で削除します。")]
    public interface IInjectable<T0> : SymphonyFramework.Services.IInjectable<T0>, IInjectable
        where T0 : class
    {
    }

    /// <summary> 6.x の2引数の依存注入契約。新しい型を継承する。 </summary>
    [global::System.Obsolete("IInjectable は名前空間 SymphonyFramework.Services へ移りました。using を書き換えてください。8.0.0 で削除します。")]
    public interface IInjectable<T0, T1> : SymphonyFramework.Services.IInjectable<T0, T1>, IInjectable
        where T0 : class
        where T1 : class
    {
    }

    /// <summary> 6.x の3引数の依存注入契約。新しい型を継承する。 </summary>
    [global::System.Obsolete("IInjectable は名前空間 SymphonyFramework.Services へ移りました。using を書き換えてください。8.0.0 で削除します。")]
    public interface IInjectable<T0, T1, T2> : SymphonyFramework.Services.IInjectable<T0, T1, T2>, IInjectable
        where T0 : class
        where T1 : class
        where T2 : class
    {
    }

    /// <summary> 6.x の4引数の依存注入契約。新しい型を継承する。 </summary>
    [global::System.Obsolete("IInjectable は名前空間 SymphonyFramework.Services へ移りました。using を書き換えてください。8.0.0 で削除します。")]
    public interface IInjectable<T0, T1, T2, T3> : SymphonyFramework.Services.IInjectable<T0, T1, T2, T3>, IInjectable
        where T0 : class
        where T1 : class
        where T2 : class
        where T3 : class
    {
    }
}
