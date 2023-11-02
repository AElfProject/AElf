using AElf.Kernel.SmartContract.Grains;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.Silo;

[DependsOn(typeof(AbpAutofacModule),
    typeof(GrainsExecutionAElfModule),
    typeof(AbpAspNetCoreSerilogModule)
)]
public class AElfKernelSmartContractOrleansSiloModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        /*context.Services.AddHostedService<NFTMarketServerHostedService>();
        var configuration = context.Services.GetConfiguration();
        Configure<SynchronizeTransactionJobOptions>(configuration.GetSection("Synchronize"));
        Configure<ChainOptions>(configuration.GetSection("Chains"));*/
    }
}