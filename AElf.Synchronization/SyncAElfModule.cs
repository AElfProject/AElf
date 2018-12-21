using AElf.Modularity;
using AElf.Synchronization.BlockExecution;
using AElf.Synchronization.BlockSynchronization;
using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Synchronization
{
    public class SyncAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {

            var services = context.Services;
            services.AddSingleton<IBlockSynchronizer,BlockSynchronizer>();
            services.AddSingleton<IBlockExecutor,BlockExecutor>();
            services.AddSingleton<IBlockHeaderValidator,BlockHeaderValidator>();

        }

    }
}