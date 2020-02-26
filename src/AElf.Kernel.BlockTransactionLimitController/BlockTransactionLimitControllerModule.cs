using AElf.Kernel.BlockTransactionLimitController;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel
{
    //TODO!! rename this module, AElf.Kernel.* cannot can reference AElf.Kernel 
    [DependsOn(
        typeof(KernelAElfModule)
    )]
    public class BlockTransactionLimitControllerModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IBlockAcceptedLogEventHandler, BlockTransactionLimitChangedLogEventHandler>();
        }
    }
}