namespace AElf.Sdk.CSharp
{
    // Instantiated for every call, threshold can be a CPU token later, may add GetRemaining method
    public sealed class ExecutionObserver : IExecutionObserver
    {
        private int _counter;

        public ExecutionObserver(int threshold)
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
    
    public static class ExecutionObserverDebugger
    {
        public static void Test(IExecutionObserver observer)
        {
            // To observe the observer's condition during execution,
            // can place a breakpoint below
        }
    }
}
