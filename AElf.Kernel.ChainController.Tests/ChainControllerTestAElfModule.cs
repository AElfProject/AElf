using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Database;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.ChainController.Application;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContractExecution;
using AElf.Modularity;
using AElf.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Kernel.ChainController
{
    [DependsOn(
        typeof(ChainControllerAElfModule),
        typeof(TestBaseAElfModule))]
    public class ChainControllerTestAElfModule : AElfModule
    {
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<ITransactionResultSettingService, NoBranchTransactionResultService>();
            context.Services.AddTransient<ITransactionResultGettingService, NoBranchTransactionResultService>();
        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(o => o.UseInMemoryDatabase());
            services.AddKeyValueDbContext<StateKeyValueDbContext>(o => o.UseInMemoryDatabase());


            services.AddTransient<ChainCreationService>();
        }
    }
}