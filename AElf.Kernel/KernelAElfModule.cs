using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.ChainController;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.Services;
using AElf.Kernel.SmartContract;
using AElf.Kernel.TransactionPool;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel
{
    [DependsOn(typeof(CoreKernelAElfModule), typeof(ChainControllerAElfModule), typeof(SmartContractAElfModule),
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