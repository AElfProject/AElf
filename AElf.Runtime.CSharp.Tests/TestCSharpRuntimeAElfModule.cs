using AElf.Common;
using AElf.Database;
using AElf.Kernel;
using AElf.Kernel.Infrastructure;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Runtime.CSharp.Tests
{
    [DependsOn(
        typeof(AElf.ChainController.ChainControllerAElfModule),
        typeof(AElf.SmartContract.SmartContractAElfModule),
        typeof(AElf.Runtime.CSharp.CSharpRuntimeAElfModule),
        typeof(AElf.Miner.Rpc.MinerRpcAElfModule),
        typeof(CoreKernelAElfModule)
    )]
    public class TestCSharpRuntimeAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ChainOptions>(o => { o.ChainId = "AELF"; });
            
            context.Services.AddAssemblyOf<TestCSharpRuntimeAElfModule>();

            context.Services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(o => o.UseInMemoryDatabase());
            context.Services.AddKeyValueDbContext<StateKeyValueDbContext>(o => o.UseInMemoryDatabase());
        }


        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {

        }
    }
}