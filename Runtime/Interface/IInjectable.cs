using UnityEngine;

namespace SymphonyFrameWork
{
    public interface IInjectable<T0>
        where T0 : class
    {
        void Inject(T0 arg0);
    }

    public interface IInjectable<T0, T1>
        where T0 : class
        where T1 : class
    {
        void Inject(T0 arg0, T1 arg1);
    }

    public interface IInjectable<T0, T1, T2>
        where T0 : class
        where T1 : class
        where T2 : class
    {
        void Inject(T0 arg0, T1 arg1, T2 arg2);
    }

    public interface IInjectable<T0, T1, T2, T3>
        where T0 : class
        where T1 : class
        where T2 : class
        where T3 : class
    {
        void Inject(T0 arg0, T1 arg1, T2 arg2, T3 arg3);
    }
}
