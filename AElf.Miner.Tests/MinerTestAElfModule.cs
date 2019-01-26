using AElf.Common;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
using AElf.Crosschain;
using AElf.Database;
using AElf.Kernel;
using AElf.Kernel.Storages;
using AElf.Modularity;
using AElf.Runtime.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Miner.Tests
{
    [DependsOn(
        typeof(AElf.ChainController.ChainControllerAElfModule),
        typeof(AElf.SmartContract.SmartContractAElfModule),
        typeof(CSharpRuntimeAElfModule),
        typeof(MinerAElfModule),
        typeof(CrosschainAElfModule),
        typeof(KernelAElfModule)
    )]
    public class MinerTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            
            context.Services.AddAssemblyOf<MinerTestAElfModule>();
            
            context.Services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(o => o.UseInMemoryDatabase());
            context.Services.AddKeyValueDbContext<StateKeyValueDbContext>(o => o.UseInMemoryDatabase());
        }


        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            ChainConfig.Instance.ChainId = Hash.LoadByteArray(new byte[] {0x01, 0x02, 0x03}).DumpBase58();
            NodeConfig.Instance.NodeAccount = Address.Generate().GetFormatted();
        }

    }
}