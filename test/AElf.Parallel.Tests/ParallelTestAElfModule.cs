using AElf.Kernel.SmartContract.Application;
using System.Collections.Generic;
using AElf.Kernel;
using AElf.Kernel.FeeCalculation;
using AElf.Kernel.FeeCalculation.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.ExecutionPluginForMethodFee;
using AElf.Kernel.SmartContract.Parallel;
using AElf.Kernel.SmartContract.Parallel.Application;
using AElf.Modularity;
using AElf.OS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.Parallel.Tests
{
    [DependsOn(
        typeof(OSCoreWithChainTestAElfModule),
        typeof(ParallelExecutionModule),
        typeof(ExecutionPluginForMethodFeeModule)
    )]
    public class ParallelTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            base.ConfigureServices(context);
            context.Services.AddSingleton<ParallelTestHelper>();
            context.Services.AddSingleton<ITransactionExecutingService, LocalParallelTransactionExecutingService>();
            // context.Services.AddSingleton<ITransactionSizeFeeSymbolsProvider, TransactionSizeFeeSymbolsProvider>();
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