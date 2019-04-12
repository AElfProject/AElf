using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
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
             var services = context.Services;
             var configuration = services.GetConfiguration();
             var crossChainConfiguration =
                 configuration.GetChildren().FirstOrDefault(child => child.Key.Equals("CrossChain"));
             if (crossChainConfiguration == null)
                 return;
             Configure<CrossChainConfigOption>(option =>
             {
                 option.ParentChainId = ChainHelpers.ConvertBase58ToChainId(crossChainConfiguration["ParentChainId"]);
                 option.ExtraDataSymbols = crossChainConfiguration.GetSection("ExtraDataSymbols").Get<List<string>>();
             });
         }
     }
 }