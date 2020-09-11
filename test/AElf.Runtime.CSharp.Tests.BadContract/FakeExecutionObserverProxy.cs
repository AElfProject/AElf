using AElf.Kernel.SmartContract;

namespace AElf.Runtime.CSharp.Tests.BadContract
{
    public static class ExecutionObserverProxy
    {
        private static IExecutionObserver _observer;

        public static void SetObserver(IExecutionObserver observer)
        {
            _observer = observer;
        }

        public static void BranchCount()
        {
            _observer.BranchCount();
        }

        public static void CallCount()
        {
            _observer.CallCount();
        }
    }
}