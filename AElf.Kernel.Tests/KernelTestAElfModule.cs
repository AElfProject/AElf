using AElf.Kernel.ChainController;
using AElf.Kernel.Consensus;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContractExecution;
using AElf.Kernel.TransactionPool;
using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.Kernel
{
    [DependsOn(
        typeof(KernelAElfModule),
        typeof(SmartContractTestAElfModule),
        typeof(SmartContractExecutionTestAElfModule),
        typeof(TransactionPoolTestAElfModule),
        typeof(ChainControllerTestAElfModule),
        typeof(ConsensusAElfModule),
        typeof(KernelCoreTestAElfModule))]
    public class KernelTestAElfModule : AElfModule
    {
    }
}