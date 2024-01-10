using AElf.Contracts.Genesis;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Runtime.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.Grain;

[DependsOn(typeof(CSharpRuntimeAElfModule), typeof(SmartContractAElfModule))]
public class SiloExecutionAElfModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<ISmartContractExecutiveService, SmartContractExecutiveService>();
        context.Services.AddSingleton<IBlockStateSetCachedStateStore, BlockStateSetCachedStateStore>();
        context.Services.AddSingleton<INotModifiedCachedStateStore<BlockStateSet>, BlockStateSetCachedStateStore>(provider =>
            provider.GetRequiredService<IBlockStateSetCachedStateStore>() as BlockStateSetCachedStateStore);
        context.Services.AddTransient<IStateStore<BlockStateSet>, StateStore<BlockStateSet>>();
        context.Services.AddSingleton<IPlainTransactionExecutingService, PlainTransactionExecutingService>();
    }
    
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var defaultContractZeroCodeProvider = context.ServiceProvider.GetService<IDefaultContractZeroCodeProvider>();
        defaultContractZeroCodeProvider.SetDefaultContractZeroRegistrationByType(typeof(BasicContractZero));
    }
}