namespace AElf.Kernel.SmartContract
{
    public class SmartContractConstants
    {
        public const int ExecutionCallThreshold = 15000;

        public const int ExecutionBranchThreshold = 15000;
        
        public const int OldExecutionCallThreshold = 5000;

        public const int OldExecutionBranchThreshold = 5000;
        
        public const int StateSizeLimit = 128 * 1024;
    }
}