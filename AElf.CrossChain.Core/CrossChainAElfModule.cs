using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Miner.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.CrossChain
 {
     [DependsOn(typeof(KernelAElfModule))]
     public class CrossChainAElfModule : AElfModule
     {
         public override void ConfigureServices(ServiceConfigurationContext context)
         {
             var services = context.Services;
             services.AddSingleton<ICrossChainDataProvider, CrossChainDataProvider>();
             services.AddScoped<ISystemTransactionGenerator, CrossChainIndexingTransactionGenerator>();
             services.AddScoped<IBlockExtraDataProvider, CrossChainBlockExtraDataProvider>();
             services.AddScoped<IBlockValidationProvider, CrossChainValidationProvider>();
             services.AddSingleton<ICrossChainService, CrossChainService>();
             services.AddSingleton<ICrossChainContractReader, CrossChainContractReader>();
         }        
     }
 }