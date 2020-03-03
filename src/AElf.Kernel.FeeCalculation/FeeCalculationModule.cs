using AElf.Kernel.FeeCalculation.Impl;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
namespace AElf.Kernel.FeeCalculation
{
    public class FeeCalculationModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddSingleton<ICalculateFunctionProvider, CalculateFunctionProvider>();
            services.AddSingleton<IPrimaryTokenFeeProvider, TxFeeProvider>();
            services.AddSingleton<IResourceTokenFeeProvider, ReadFeeProvider>();
            services.AddSingleton<IResourceTokenFeeProvider, StorageFeeProvider>();
            services.AddSingleton<IResourceTokenFeeProvider, TrafficFeeProvider>();
            services.AddSingleton<IResourceTokenFeeProvider, WriteFeeProvider>();
        }
    }
}