using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.CrossChain
{
    public class CrossChainAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<IBlockExtraDataProvider, CrossChainBlockExtraDataProvider>();
            context.Services.AddTransient<ISystemTransactionGenerator, CrossChainIndexingTransactionGenerator>();
            context.Services.AddTransient<IBlockValidationProvider, CrossChainValidationProvider>();
            context.Services.AddTransient<ISmartContractAddressNameProvider, CrossChainSmartContractAddressNameProvider>();
            context.Services.AddSingleton<ICrossChainIndexingDataService, CrossChainIndexingDataService>();            
            context.Services
                .AddSingleton<ITransactionValidationProvider, ConstrainedCrossChainTransactionValidationProvider>();
            var crossChainConfiguration = context.Services.GetConfiguration().GetSection("CrossChain");
            Configure<CrossChainConfigOptions>(crossChainConfiguration);
        }
    }
}