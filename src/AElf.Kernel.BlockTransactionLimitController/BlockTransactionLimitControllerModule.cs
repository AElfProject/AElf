using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.Kernel
{
    [DependsOn(
        typeof(KernelAElfModule)
    )]
    public class BlockTransactionLimitControllerModule : AElfModule
    {
    }
}