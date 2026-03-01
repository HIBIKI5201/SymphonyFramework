namespace SymphonyFrameWork.System.ServiceLocate
{
    public static class ServiceInjector
    {
        public static void Inject<T0>(IInjectable<T0> target)
            where T0 : class
        {
            target.Inject(ServiceLocator.GetInstance<T0>());
        }

        public static void Inject<T0, T1>(IInjectable<T0, T1> target)
            where T0 : class
            where T1 : class
        {
            target.Inject(
                ServiceLocator.GetInstance<T0>(),
                ServiceLocator.GetInstance<T1>());
        }

        public static void Inject<T0, T1, T2>(IInjectable<T0, T1, T2> target)
            where T0 : class
            where T1 : class
            where T2 : class
        {
            target.Inject(
                ServiceLocator.GetInstance<T0>(),
                ServiceLocator.GetInstance<T1>(),
                ServiceLocator.GetInstance<T2>());
        }

        public static void Inject<T0, T1, T2, T3>(IInjectable<T0, T1, T2, T3> target)
            where T0 : class
            where T1 : class
            where T2 : class
            where T3 : class
        {
            target.Inject(
                ServiceLocator.GetInstance<T0>(),
                ServiceLocator.GetInstance<T1>(),
                ServiceLocator.GetInstance<T2>(),
                ServiceLocator.GetInstance<T3>());
        }
    }
}
