using AElf.ChainController;
using AElf.Kernel.SmartContractExecution;
using AElf.Kernel.SmartContractExecution.Execution;
using AElf.Kernel.Consensus;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Modularity;
using AElf.Runtime.CSharp;
using AElf.RuntimeSetup;
using AElf.SmartContract;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Concurrency.Worker
{
    [DependsOn(
        typeof(RuntimeSetupAElfModule),
        
        typeof(CSharpRuntimeAElfModule),
        typeof(SmartContractAElfModule),
        typeof(ChainControllerAElfModule)
        )]
    public class WorkerConcurrencyAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            Configure<ExecutionOptions>(configuration.GetSection("Execution"));
            
            context.Services.AddTransient<ServicePack>();
            context.Services.AddSingleton<ActorEnvironment>();
        }
    }
}