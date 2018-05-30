using System;
using AElf.Kernel.Services;

namespace AElf.Kernel.Concurrency.Execution
{
    public class ServicePack
    {
        public ISmartContractService SmartContractService { get; set; }
        public IChainContextService ChainContextService { get; set; }
        public IAccountContextService AccountContextService { get; set; }
    }
}
