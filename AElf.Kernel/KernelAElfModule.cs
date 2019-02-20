using AElf.Kernel.ChainController;
using AElf.Kernel.SmartContract;
using AElf.Kernel.TransactionPool;
using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.Kernel
{
    [DependsOn(typeof(CoreKernelAElfModule), typeof(ChainControllerAElfModule), typeof(SmartContractAElfModule),
        typeof(TransactionPoolAElfModule))]
    public class KernelAElfModule : AElfModule<KernelAElfModule>
    {
    }
}