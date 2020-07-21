namespace AElf.Kernel.SmartContract
{
    public interface IExecutionObserverThreshold
    {
        int ExecutionCallThreshold { get; set; }
        int ExecutionBranchThreshold { get; set; }
    }
    
    internal class ExecutionObserverThreshold : IExecutionObserverThreshold
    {
        public int ExecutionCallThreshold { get; set; }
        public int ExecutionBranchThreshold { get; set; }
    }
}