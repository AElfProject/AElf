using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Grains;
using AElf.Kernel.SmartContract.Orleans;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.Orleans;

[DependsOn(
    typeof(AbpAutoMapperModule))]
public class SiloExecutionAElfModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IPlainTransactionExecutingService, SiloTransactionExecutingService>();
        context.Services.AddSingleton<IPlainTransactionExecutingGrain, PlainTransactionExecutingGrain>();

    }
}