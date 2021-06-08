using AElf.Contracts.Genesis;
using AElf.CSharp.CodeOps;
using AElf.Kernel;
using AElf.Kernel.SmartContract.ExecutionPluginForCallThreshold;
using AElf.Kernel.SmartContract.ExecutionPluginForMethodFee;
using AElf.Kernel.SmartContract.ExecutionPluginForResourceFee;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Parallel.Orleans;
using AElf.Modularity;
using AElf.Runtime.CSharp;
using AElf.RuntimeSetup;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.ExecutionWorker
{
    [DependsOn(
        typeof(CoreKernelAElfModule),
        typeof(CSharpRuntimeAElfModule),
        typeof(CSharpCodeOpsAElfModule),
        typeof(RuntimeSetupAElfModule),
        typeof(OrleansParallelExecutionCoreModule),
        //plugin
        typeof(ExecutionPluginForMethodFeeModule),
        typeof(ExecutionPluginForResourceFeeModule),
        typeof(ExecutionPluginForCallThresholdModule)
    )]
    public class ExecutionWorkerModule: AElfModule
    {
        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var contractZeroCodeProvider =
                context.ServiceProvider.GetRequiredService<IDefaultContractZeroCodeProvider>();
            contractZeroCodeProvider.SetDefaultContractZeroRegistrationByType(typeof(BasicContractZero));
        }
    }
}