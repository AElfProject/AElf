using Org.BouncyCastle.Tsp;

namespace AElf.Sdk.CSharp
{
    // Instantiated for every call, threshold can be a CPU token later, may add GetRemaining method
    public sealed class ExecutionObserver : IExecutionObserver
    {
        private int _usage;
        private readonly int _balance;

        public ExecutionObserver(int balance)
        {
            _usage = 0;
            _balance = balance;
        }

        public void Count()
        {
            if (_balance == -1)
                return;

            if (_usage == _balance)
            {
                throw new RuntimeBranchingThresholdExceededException();
            }

            _usage++;
        }

        public int GetUsage()
        {
            return _usage;
        }
    }
    
    public static class ExecutionObserverDebugger
    {
        public static void Test(IExecutionObserver observer)
        {
            // To observe the observer's condition during execution,
            // can place a breakpoint below
        }
    }
}
