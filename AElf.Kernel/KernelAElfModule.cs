using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.ChainController;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.Node;
using AElf.Kernel.Services;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContractExecution;
using AElf.Kernel.TransactionPool;
using AElf.Kernel.Types;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel
{
    [DependsOn(
        typeof(CoreKernelAElfModule), 
        typeof(ChainControllerAElfModule), 
        typeof(SmartContractAElfModule),
        typeof(NodeAElfModule),
        typeof(SmartContractExecutionAElfModule),
        typeof(TypesAElfModule),
        typeof(TransactionPoolAElfModule))]
    public class KernelAElfModule : AElfModule<KernelAElfModule>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
//            context.Services.AddSingleton<IMinerService, MinerService>();
//            context.Services.AddSingleton<BlockMiningEventHandler>();
        }
    }
}