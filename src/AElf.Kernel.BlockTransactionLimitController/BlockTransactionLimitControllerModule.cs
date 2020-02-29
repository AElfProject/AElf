using AElf.Kernel.SmartContract.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.BlockTransactionLimitController
{
    public class BlockTransactionLimitControllerModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IBlockAcceptedLogEventHandler, BlockTransactionLimitChangedLogEventHandler>();
        }
    }
}