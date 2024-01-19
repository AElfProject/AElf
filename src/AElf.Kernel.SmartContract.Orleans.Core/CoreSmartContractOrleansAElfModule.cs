using AElf.Contracts.Genesis;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Orleans.Strategy;
using AElf.Runtime.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime;
using Orleans.Runtime.Placement;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.Orleans;

[DependsOn(typeof(CSharpRuntimeAElfModule), typeof(SmartContractAElfModule))]
public class CoreSmartContractOrleansAElfModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<ISmartContractExecutiveService, SmartContractExecutiveService>();
        context.Services.AddSingleton<IBlockStateSetCachedStateStore, BlockStateSetCachedStateStore>();
        context.Services.AddSingleton<INotModifiedCachedStateStore<BlockStateSet>, BlockStateSetCachedStateStore>(provider =>
            provider.GetRequiredService<IBlockStateSetCachedStateStore>() as BlockStateSetCachedStateStore);
        context.Services.AddTransient<IStateStore<BlockStateSet>, StateStore<BlockStateSet>>();
        context.Services.AddSingleton<IPlainTransactionExecutingService, PlainTransactionExecutingService>();
        context.Services.AddSingletonNamedService<PlacementStrategy, CleanCacheStrategy>(nameof(CleanCacheStrategy));
        context.Services.AddSingletonKeyedService<Type, IPlacementDirector, CleanCacheStrategyFixedSiloDirector>(
            typeof(CleanCacheStrategy));
    }
    
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var defaultContractZeroCodeProvider = context.ServiceProvider.GetService<IDefaultContractZeroCodeProvider>();
        defaultContractZeroCodeProvider.SetDefaultContractZeroRegistrationByType(typeof(BasicContractZero));
    }
}