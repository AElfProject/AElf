using AElf.Kernel.SmartContract;

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
                throw new RuntimeCallThresholdExceededException($"Contract call threshold {_callThreshold} exceeded.");
            }

            _callCount++;
        }
        
        public void BranchCount()
        {
            if (_branchThreshold != -1 && _branchCount == _branchThreshold)
            {
                throw new RuntimeBranchThresholdExceededException(
                    $"Contract branch threshold {_branchThreshold} exceeded.");
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
}
