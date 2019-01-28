using AElf.Common;
using AElf.Configuration.Config.Chain;
using AElf.Crosschain.Grpc;
using AElf.Crosschain.Grpc.Server;
using AElf.Kernel;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Crosschain
 {
     [DependsOn(typeof(KernelAElfModule))]
     public class CrosschainAElfModule : AElfModule
     {
         public override void ConfigureServices(ServiceConfigurationContext context)
         {
             var services = context.Services;
             services.AddSingleton<SideChainBlockInfoRpcServer>();
             services.AddSingleton<ParentChainBlockInfoRpcServer>();
         }

         public override void OnApplicationInitialization(ApplicationInitializationContext context)
         {
             context.ServiceProvider.GetService<SideChainBlockInfoRpcServer>()
                 .Init(ChainConfig.Instance.ChainId.ConvertBase58ToChainId());
             context.ServiceProvider.GetService<ParentChainBlockInfoRpcServer>()
                 .Init(ChainConfig.Instance.ChainId.ConvertBase58ToChainId());

         }
     }
 }