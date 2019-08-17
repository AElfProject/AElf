using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel
{
    [DependsOn(
        typeof(KernelAElfModule)
    )]
    [Dependency(ServiceLifetime.Transient, ReplaceServices = true)]
    public class BlockTransactionLimitControllerModule : AElfModule
    {
    }
}