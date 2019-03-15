using System.Linq;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.CrossChain
 {
     [DependsOn(typeof(SmartContractAElfModule))]
     public class CrossChainAElfModule : AElfModule
     {
         public override void ConfigureServices(ServiceConfigurationContext context)
         {
             var services = context.Services;
             var configuration = services.GetConfiguration();
             var crossChainConfiguration =
                 configuration.GetChildren().FirstOrDefault(child => child.Key.Equals("CrossChain"));
             if (crossChainConfiguration == null)
                 return;
             Configure<CrossChainConfigOption>(crossChainConfiguration);
             context.Services.AddTransient<IBlockExtraDataProvider, CrossChainBlockExtraDataProvider>();
             context.Services.AddTransient<ISystemTransactionGenerator, CrossChainIndexingTransactionGenerator>();
             context.Services.AddTransient<IBlockValidationProvider, CrossChainValidationProvider>();
             context.Services.AddTransient<ISmartContractAddressNameProvider, CrossChainSmartContractAddressNameProvider>();
         }
     }
 }