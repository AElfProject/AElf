using System.Collections.Generic;
using AElf.Contracts.TestBase;
using AElf.Database;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Infrastructure;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace AElf.CrossChain
{
    [DependsOn(typeof(AbpEventBusModule),
        typeof(ContractTestAElfModule),
        typeof(CrossChainAElfModule))]
    public class CrossChainTestModule : AElfModule
    {

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(o => o.UseInMemoryDatabase());
            services.AddKeyValueDbContext<StateKeyValueDbContext>(o => o.UseInMemoryDatabase());
            
            // todo: Remove this
            context.Services.AddTransient<ITransactionResultQueryService, NoBranchTransactionResultService>();
            context.Services.AddTransient<ITransactionResultService, NoBranchTransactionResultService>();
        }
    }
}