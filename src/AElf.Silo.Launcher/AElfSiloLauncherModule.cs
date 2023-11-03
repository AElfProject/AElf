using AElf.Kernel.SmartContract.Grains;
using AElf.Kernel.SmartContract.Orleans;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AElf.Silo.Launcher;

[DependsOn(typeof(AbpAutofacModule),
    typeof(SiloExecutionAElfModule)
)]
public class AElfSiloLauncherModule : AbpModule
{
    /*public override void ConfigureServices(ServiceConfigurationContext context)
    {
        /*context.Services.AddHostedService<NFTMarketServerHostedService>();
        var configuration = context.Services.GetConfiguration();
        Configure<SynchronizeTransactionJobOptions>(configuration.GetSection("Synchronize"));
        Configure<ChainOptions>(configuration.GetSection("Chains"));#1#
        
    }*/
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHostedService<SiloTransactionExecutingHost>();
        var configuration = context.Services.GetConfiguration();
    }
}