using AElf.ChainController;
using AElf.Execution;
using AElf.Execution.Execution;
using AElf.Kernel.Consensus;
using AElf.Miner;
using AElf.Miner.Rpc;
using AElf.Modularity;
using AElf.Network;
using AElf.Runtime.CSharp;
using AElf.SmartContract;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Concurrency.Worker
{
    [DependsOn(
        typeof(CSharpRuntimeAElfModule),
        typeof(SmartContractAElfModule),
        typeof(ChainControllerAElfModule),
        typeof(MinerAElfModule)
    )]
    public class WorkerConcurrencyAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<ServicePack>();
            context.Services.AddSingleton<ActorEnvironment>();
        }
    }
}