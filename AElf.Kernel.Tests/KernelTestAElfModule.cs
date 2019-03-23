using AElf.Kernel.ChainController;
using AElf.Kernel.Consensus;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContractExecution;
using AElf.Kernel.TransactionPool;
using AElf.Modularity;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Modularity;

namespace AElf.Kernel
{
    [DependsOn(
         typeof(KernelAElfModule),
         typeof(ConsensusAElfModule),
         typeof(KernelCoreTestAElfModule),
         typeof(SmartContractTestAElfModule),
         typeof(SmartContractExecutionTestAElfModule),
         typeof(TransactionPoolTestAElfModule),
         typeof(ChainControllerTestAElfModule)
     )]
    public class KernelTestAElfModule : AElfModule
    {
    }
    
    [DependsOn(
        typeof(KernelTestAElfModule),
        typeof(KernelCoreWithChainTestAElfModule))]
    public class KernelWithChainTestAElfModule : AElfModule
    {
    }
}