using System;
using AElf.Sdk.CSharp;

namespace AElf.CSharp.CodeOps
{
    // Injected into contract, not used directly, kept as a template to compare IL codes
    public static class ExecutionObserverProxy
    {
        [ThreadStatic] private static ExecutionObserver _observer;

        public static void SetObserver(ExecutionObserver observer)
        {
            _observer = observer;
        }

        public static void BranchCount()
        {
            if (_observer != null)
                _observer.BranchCount();
        }

        public static void CallCount()
        {
            if (_observer != null)
                _observer.CallCount();
        }
    }
}