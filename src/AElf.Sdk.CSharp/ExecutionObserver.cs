using Org.BouncyCastle.Tsp;

namespace AElf.Sdk.CSharp
{
    // Instantiated for every transaction
    public sealed class ExecutionObserver : IExecutionObserver
    {
        private int _callCount;
        private int _branchCount;
        private readonly int _callThreshold;
        private readonly int _branchThreshold;

        public ExecutionObserver(int callThreshold, int branchThreshold)
        {
            _callCount = 0;
            _branchCount = 0;
            _callThreshold = callThreshold;
            _branchThreshold = branchThreshold;
        }

        public void CallCount()
        {
            if (_callThreshold != -1 && _callCount == _callThreshold)
            {
                throw new RuntimeCallThresholdExceededException();
            }

            _callCount++;
        }
        
        public void BranchCount()
        {
            if (_branchThreshold != -1 && _branchCount == _branchThreshold)
            {
                throw new RuntimeBranchThresholdExceededException();
            }

            _branchCount++;
        }

        public int GetCallCount()
        {
            return _callCount;
        }
        
        public int GetBranchCount()
        {
            return _branchCount;
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
