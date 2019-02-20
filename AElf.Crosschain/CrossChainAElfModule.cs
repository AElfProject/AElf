using AElf.Common;
using AElf.Crosschain.Grpc;
using AElf.Crosschain.Grpc.Client;
using AElf.Crosschain.Grpc.Server;
using AElf.Kernel;
using AElf.Kernel.Miner.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Crosschain
 {
     [DependsOn(typeof(KernelAElfModule))]
     public class CrossChainAElfModule : AElfModule
     {
         public override void ConfigureServices(ServiceConfigurationContext context)
         {
             var services = context.Services;
             services.AddSingleton<CrossChainBlockDataRpcServer>();
             
             var configuration = context.Services.GetConfiguration();
             Configure<GrpcConfigOption>(configuration.GetSection("Crosschain"));
             services.AddSingleton<IClientService, GrpcClientService>();
             services.AddSingleton<ICrossChainDataProvider, CrossChainDataProvider>();
             services.AddTransient<ISystemTransactionGenerator, CrossChainIndexingTransactionGenerator>();
             services.AddSingleton<ICrossChainService, CrossChainService>();
         }
         
         public override void OnApplicationInitialization(ApplicationInitializationContext context)
         {
             var opt = context.ServiceProvider.GetService<IOptionsSnapshot<GrpcConfigOption>>().Value;

             var clientService = context.ServiceProvider.GetService<GrpcClientService>();
             // Init client connected to parent chain if it exists.
             clientService.Init(opt.CertificateDir);
//             if (!string.IsNullOrEmpty(opt.ParentChainId) && !string.IsNullOrEmpty(opt.ParentChainNodeIp) &&
//                 !string.IsNullOrEmpty(opt.ParentChainPort)) return;
//             var blockInfoCache = new BlockInfoCache(opt.ParentChainId.ConvertBase58ToChainId());
//             clientService.CreateClient(new GrpcClientBase
//             {
//                 TargetIp = opt.ParentChainNodeIp,
//                 TargetPort = uint.Parse(opt.ParentChainPort),
//                 TargetChainId = opt.ParentChainId.ConvertBase58ToChainId(),
//                 TargetIsSideChain = false,
//                 BlockInfoCache = blockInfoCache
//             });
//             context.ServiceProvider.GetService<CrossChainDataProvider>().ParentChainBlockInfoCache =
//                 blockInfoCache;
         }
     }
 }