using System.Runtime.CompilerServices;
using AElf.ChainController;
using AElf.Common;
using AElf.Database;
using AElf.Kernel.SmartContractExecution;
using AElf.Kernel.SmartContractExecution.Execution;
using AElf.Kernel.SmartContractExecution.Scheduling;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.Tests.Concurrency.Execution;
using AElf.Miner;
using AElf.Miner.Rpc;
using AElf.Modularity;
using AElf.Runtime.CSharp;
using AElf.SmartContract;
using AElf.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Tests
{
    
    [DependsOn(typeof(CoreKernelAElfModule),
        
        //TODO: only test kernel aelf module here
        typeof(ChainControllerAElfModule),typeof(ExecutionAElfModule), 
        typeof(SmartContractAElfModule),
        typeof(MinerRpcAElfModule),
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
            Configure<ChainOptions>(o => { o.ChainId = "AELF"; });
            
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