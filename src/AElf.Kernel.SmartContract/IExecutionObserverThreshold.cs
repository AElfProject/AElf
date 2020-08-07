namespace AElf.Kernel.SmartContract
{
    public interface IExecutionObserverThreshold
    {
        int ExecutionCallThreshold { get; set; }
        int ExecutionBranchThreshold { get; set; }
    }
    
    public class ExecutionObserverThreshold : IExecutionObserverThreshold
    {
        public int ExecutionCallThreshold { get; set; }
        public int ExecutionBranchThreshold { get; set; }

        public override bool Equals(object o)
        {
            return Equals(o as ExecutionObserverThreshold);
        }

        private bool Equals(ExecutionObserverThreshold executionObserverThreshold)
        {
            return executionObserverThreshold.ExecutionBranchThreshold == ExecutionBranchThreshold &&
                   executionObserverThreshold.ExecutionCallThreshold == ExecutionCallThreshold;
        }
    }
}