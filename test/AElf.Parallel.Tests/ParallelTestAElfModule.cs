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
        private ParallelTestHelper _parallelTestHelper;

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            base.ConfigureServices(context);
            context.Services.AddSingleton<ParallelTestHelper>();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            _parallelTestHelper = context.ServiceProvider.GetService<ParallelTestHelper>();
            AsyncHelper.RunSync(() => _parallelTestHelper.MockChainAsync());
            AsyncHelper.RunSync(() => _parallelTestHelper.DeployBasicFunctionWithParallelContract());
        }

        public override void OnApplicationShutdown(ApplicationShutdownContext context)
        {
            AsyncHelper.RunSync(() => _parallelTestHelper.DisposeMock());
        }
    }
}