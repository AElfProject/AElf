using System.Runtime.CompilerServices;
using AElf.ChainController;
using AElf.ChainController.Rpc;
using AElf.Common;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
using AElf.Execution;
using AElf.Execution.Execution;
using AElf.Execution.Scheduling;
using AElf.Kernel.Tests.Concurrency.Execution;
using AElf.Kernel.Types.Transaction;
using AElf.Miner;
using AElf.Miner.Rpc;
using AElf.Miner.TxMemPool;
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
        typeof(SmartContractAElfModule),typeof(RpcChainControllerAElfModule),
        typeof(MinerAElfModule),
        typeof(MinerRpcAElfModule),
        typeof(CSharpRuntimeAElfModule),
        
        typeof(TestBaseAElfModule))]
    public class KernelTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            
            //should move out of this project
            services.AddSingleton<IActorEnvironment, ActorEnvironment>();
            services.AddSingleton<ServicePack>();
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            //init test data here
            NodeConfig.Instance.NodeAccount = Address.FromString("ELF_TestContractA").GetFormatted();
        }
    }
}