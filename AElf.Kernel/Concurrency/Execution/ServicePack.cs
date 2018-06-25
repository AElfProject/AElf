using System;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using NLog;

namespace AElf.Kernel.Concurrency.Execution
{
    public class ServicePack
    {
        public IResourceUsageDetectionService ResourceDetectionService { get; set; }
        public ISmartContractService SmartContractService { get; set; }
        public IChainContextService ChainContextService { get; set; }
        public IAccountContextService AccountContextService { get; set; }
        public IWorldStateManager WorldStateManager { get; set; }
        public ILogger Logger { get; set; }
    }
}
