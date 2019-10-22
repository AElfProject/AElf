using System.Collections.Generic;
using AElf.Kernel;
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

        public override void PostConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.RemoveAll(s=>s.ImplementationType == typeof(NewIrreversibleBlockFoundEventHandler));
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var parallelTestHelper = context.ServiceProvider.GetService<ParallelTestHelper>();
            AsyncHelper.RunSync(() => parallelTestHelper.DeployBasicFunctionWithParallelContract());
        }
    }
}