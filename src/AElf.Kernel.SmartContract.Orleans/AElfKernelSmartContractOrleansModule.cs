using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Grains;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.Orleans;

[DependsOn(
    typeof(AbpAutoMapperModule),typeof(GrainsExecutionAElfModule))]
public class AElfKernelSmartContractOrleansModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IPlainTransactionExecutingService, SiloTransactionExecutingService>();
    }
}