using AElf.Contracts.Genesis;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Runtime.CSharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.Kernel.SmartContract.Orleans;

[DependsOn(typeof(CSharpRuntimeAElfModule), typeof(SmartContractAElfModule))]
public class SiloExecutionAElfModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        ConfigureOrleans(context, configuration); 
        context.Services.AddSingleton<IPlainTransactionExecutingGrain, PlainTransactionExecutingGrain>();
        context.Services.AddSingleton<ISmartContractExecutiveService, SmartContractExecutiveService>();
        context.Services.AddSingleton<IBlockStateSetCachedStateStore, BlockStateSetCachedStateStore>();
        context.Services.AddSingleton<INotModifiedCachedStateStore<BlockStateSet>, BlockStateSetCachedStateStore>(provider =>
            provider.GetRequiredService<IBlockStateSetCachedStateStore>() as BlockStateSetCachedStateStore);
        context.Services.AddTransient<IStateStore<BlockStateSet>, StateStore<BlockStateSet>>();
    }

    public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
    {
        var _defaultContractZeroCodeProvider = context.ServiceProvider.GetService<IDefaultContractZeroCodeProvider>();
        AsyncHelper.RunSync(async () => {_defaultContractZeroCodeProvider.SetDefaultContractZeroRegistrationByType(typeof(BasicContractZero));
        });
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
    }

    private static void ConfigureOrleans(ServiceConfigurationContext context, IConfiguration configuration)
    {
    }

}