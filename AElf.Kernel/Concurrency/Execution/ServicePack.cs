using System;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;

namespace AElf.Kernel.Concurrency.Execution
{
    public class ServicePack
    {
        public IResourceUsageDetectionService ResourceDetectionService { get; set; }
        public ISmartContractService SmartContractService { get; set; }
        public IChainContextService ChainContextService { get; set; }
        public IAccountContextService AccountContextService { get; set; }
        public IWorldStateConsole WorldStateConsole { get; set; }
    }
}
