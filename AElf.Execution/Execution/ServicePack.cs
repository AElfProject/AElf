using AElf.ChainController;
using AElf.Kernel.Managers;
using AElf.SmartContract;

namespace AElf.Execution.Execution
{
    //TODO: maybe should change this class
    public class ServicePack
    {
        public IResourceUsageDetectionService ResourceDetectionService { get; set; }
        public ISmartContractService SmartContractService { get; set; }
        public IChainContextService ChainContextService { get; set; }
        public IStateManager StateManager { get; set; }
        public ITransactionTraceManager TransactionTraceManager { get; set; }
    }
}
