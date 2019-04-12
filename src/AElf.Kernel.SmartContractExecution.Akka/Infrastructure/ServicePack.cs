using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Domain;

namespace AElf.Kernel.SmartContractExecution.Application
{
    //TODO: maybe should change this class
    public class ServicePack
    {
        public IResourceUsageDetectionService ResourceDetectionService { get; set; }
        public ISmartContractService SmartContractService { get; set; }
        public ITransactionTraceManager TransactionTraceManager { get; set; }
        
    }
}
