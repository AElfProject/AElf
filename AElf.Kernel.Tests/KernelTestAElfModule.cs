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
using AElf.Miner.TxMemPool;
using AElf.Modularity;
using AElf.SmartContract;
using AElf.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Tests
{
    
    [DependsOn(typeof(KernelAElfModule),
        
        //TODO: only test kernel aelf module here
        typeof(ChainAElfModule),typeof(ExecutionAElfModule), 
        typeof(SmartContractAElfModule),typeof(ChainControllerRpcAElfModule),
        typeof(MinerAElfModule),

        typeof(TestBaseAElfModule))]
    public class KernelTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddTransient<MockSetup>();
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            //init test data here
            ChainConfig.Instance.ChainId = "kPBx";
            NodeConfig.Instance.NodeAccount = Address.FromString("ELF_kPBx_TestContractA").GetFormatted();
        }

    }
}