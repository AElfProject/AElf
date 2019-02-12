using System.Runtime.CompilerServices;
using AElf.ChainController;
using AElf.Common;
using AElf.Configuration;
using AElf.Crosschain;
using AElf.Database;
using AElf.Execution;
using AElf.Execution.Execution;
using AElf.Kernel.Storages;
using AElf.Miner;
using AElf.Modularity;
using AElf.Runtime.CSharp;
using AElf.SmartContract;
using AElf.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Tests
{
    
    [DependsOn(typeof(KernelAElfModule),
        
        //TODO: only test kernel aelf module here
        typeof(ChainControllerAElfModule),typeof(ExecutionAElfModule), 
        typeof(SmartContractAElfModule),
        typeof(MinerAElfModule),
        typeof(CrosschainAElfModule),
        typeof(CSharpRuntimeAElfModule),
        
        typeof(TestBaseAElfModule))]
    public class KernelTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            //TODO: should not be here.because execution tests in this test project.
            Configure<ExecutionOptions>(o =>
            {
                o.ActorCount = 8;
                o.ConcurrencyLevel = 8;
            });
            
            var services = context.Services;
            
            //should move out of this project
            services.AddSingleton<IActorEnvironment, ActorEnvironment>();
            services.AddSingleton<ServicePack>();

            services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(o => o.UseInMemoryDatabase());
            services.AddKeyValueDbContext<StateKeyValueDbContext>(o => o.UseInMemoryDatabase());

        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            //init test data here
        }
    }
}