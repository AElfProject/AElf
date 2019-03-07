using System;
using AElf.Kernel.ChainController;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContractExecution;
using AElf.Kernel.TransactionPool.Tests;
using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Tests
{
    [DependsOn(
        typeof(KernelAElfModule),
        typeof(SmartContractTestAElfModule),
        typeof(SmartContractExecutionTestAElfModule),
        typeof(TransactionPoolTestAElfModule),
        typeof(ChainControllerTestAElfModule),
        typeof(KernelCoreTestAElfModule))]
    public class KernelTestAElfModule : AElfModule
    {
    }
}