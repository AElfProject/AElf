using System;
using System.Linq;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract;
using AElf.Modularity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.CrossChain
{
    [DependsOn(typeof(SmartContractAElfModule))]
    public class CrossChainAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<IBlockExtraDataProvider, CrossChainBlockExtraDataProvider>();
            context.Services.AddTransient<ISystemTransactionGenerator, CrossChainIndexingTransactionGenerator>();
            context.Services.AddTransient<IBlockValidationProvider, CrossChainValidationProvider>();
            context.Services.AddTransient<ISmartContractAddressNameProvider, CrossChainSmartContractAddressNameProvider>();
            context.Services.AddSingleton<ICrossChainDataProvider, CrossChainDataProvider>();
            var services = context.Services;
            var configuration = services.GetConfiguration();
            var crossChainConfiguration =
                configuration.GetChildren().FirstOrDefault(child => child.Key.Equals("CrossChain"));
            if (crossChainConfiguration == null)
                return;
            Configure<CrossChainConfigOption>(option =>
            {
                var parentChainIdString = crossChainConfiguration["ParentChainId"];
                option.ParentChainId = parentChainIdString.IsNullOrEmpty() ? 0 : ChainHelpers.ConvertBase58ToChainId(parentChainIdString);
                var maximalCountForIndexingParentChainBlockConfiguration =
                    crossChainConfiguration.GetSection("MaximalCountForIndexingParentChainBlock");
                if (maximalCountForIndexingParentChainBlockConfiguration.Exists())
                {
                    option.MaximalCountForIndexingParentChainBlock = int.Parse(maximalCountForIndexingParentChainBlockConfiguration.Value);
                }

                var maximalCountForIndexingSideChainBlockConfiguration =
                    crossChainConfiguration.GetSection("MaximalCountForIndexingSideChainBlock");
                if (maximalCountForIndexingSideChainBlockConfiguration.Exists())
                {
                    option.MaximalCountForIndexingSideChainBlock =
                        int.Parse(maximalCountForIndexingSideChainBlockConfiguration.Value);
                }
            });
        }
    }
}