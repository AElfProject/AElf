using System;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;

namespace AElf.CSharp.CodeOps
{
    // To be injected into contract, not used directly, used for authenticity validation
    public static class ExecutionObserverProxy
    {
        [ThreadStatic]
        private static IExecutionObserver _observer;

        public static void SetObserver(IExecutionObserver observer)
        {
            _observer = observer;
            #if DEBUG
            ExecutionObserverDebugger.Test(_observer);
            #endif
        }

        public static void Count()
        {
            #if DEBUG
            ExecutionObserverDebugger.Test(_observer);
            #endif
            if (_observer != null)
                _observer.Count();
        }

        public static int GetUsage()
        {
            #if DEBUG
            ExecutionObserverDebugger.Test(_observer);
            #endif
            if (_observer != null)
                return _observer.GetUsage();
            return 0;
        }
    }
}