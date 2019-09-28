using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Parallel;
using AElf.Modularity;
using AElf.OS;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.Parallel.Tests
{
    [DependsOn(
        typeof(OSCoreWithChainTestAElfModule),
        typeof(ParallelExecutionModule)
    )]
    public class ParallelTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            base.ConfigureServices(context);
            context.Services.AddSingleton<ParallelTestHelper>();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var parallelTestHelper = context.ServiceProvider.GetService<ParallelTestHelper>();
            var blockchainService = context.ServiceProvider.GetService<IBlockchainService>();
            AsyncHelper.RunSync(() => parallelTestHelper.DeployBasicFunctionWithParallelContract());
            var chain = AsyncHelper.RunSync(() => blockchainService.GetChainAsync());
            AsyncHelper.RunSync(() =>
                blockchainService.SetIrreversibleBlockAsync(chain, chain.BestChainHeight, chain.BestChainHash));
        }
    }
}