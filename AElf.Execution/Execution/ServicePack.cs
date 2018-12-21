using AElf.ChainController;
using AElf.Kernel.Manager.Interfaces;
using AElf.SmartContract;

namespace AElf.Execution.Execution
{
    public class ServicePack
    {
        public IResourceUsageDetectionService ResourceDetectionService { get; set; }
        public ISmartContractService SmartContractService { get; set; }
        public IChainContextService ChainContextService { get; set; }
        public IStateManager StateManager { get; set; }
        public ITransactionTraceManager TransactionTraceManager { get; set; }
    }
}
