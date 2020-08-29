namespace AElf.Kernel.SmartContract
{
    public class ForkHeightOptions
    {
        public long ExecutionObserverThresholdForkHeight { get; set; }
        
        public long BlockTransactionLimitValidationForkHeight { get; set; }
    }
}