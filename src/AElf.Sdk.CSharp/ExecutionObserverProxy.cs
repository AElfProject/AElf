using System;

namespace AElf.Sdk.CSharp
{
    // To be injected into contract, not used directly, used for authenticity validation
    public static class ExecutionObserverProxy
    {
        [ThreadStatic]
        private static IExecutionObserver _observer;

        public static void Initialize(IExecutionObserver observer)
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
    }

    // Instantiated for every call, threshold can be a CPU token later, may add GetRemaining method
    public class InstructionExecutionObserver : IExecutionObserver
    {
        private int _counter;

        public InstructionExecutionObserver(int threshold)
        {
            _counter = threshold;
        }

        public void Count()
        {
            if (--_counter == 0)
            {
                throw new RuntimeBranchingThresholdExceededException();
            }
        }
    }

    #if DEBUG
    public static class ExecutionObserverDebugger
    {
        public static void Test(IExecutionObserver observer)
        {
            // To observe the observer's condition during execution,
            // can place a breakpoint below
        }
    }
    #endif
}
