using AElf.ChainController;
using AElf.Kernel.Managers;
using AElf.Kernel.Storages;
using AElf.SmartContract;

namespace AElf.Execution.Execution
{
    public class ServicePack
    {
        public IResourceUsageDetectionService ResourceDetectionService { get; set; }
        public ISmartContractService SmartContractService { get; set; }
        public IChainContextService ChainContextService { get; set; }
        public IStateStore StateStore { get; set; }
        public ITransactionTraceManager TransactionTraceManager { get; set; }
    }
}
