using System;
using AElf.Common;
using AElf.Configuration.Config.Chain;
using AElf.Crosschain.Grpc;
using AElf.Crosschain.Grpc.Client;
using AElf.Kernel;
using AElf.Kernel.Txn;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
             
             var configuration = context.Services.GetConfiguration();
             Configure<GrpcConfigOption>(configuration.GetSection("Crosschain"));
             services.AddSingleton<IClientManager, GrpcClientManager>();
             services.AddSingleton<ICrossChainDataProvider, GrpcCrossChainDataProvider>();
             services.AddTransient<ISystemTransactionGenerator, CrossChainIndexingTransactionGenerator>();
         }
         
         public override void OnApplicationInitialization(ApplicationInitializationContext context)
         {
             context.ServiceProvider.GetService<SideChainBlockInfoRpcServer>()
                 .Init(ChainConfig.Instance.ChainId.ConvertBase58ToChainId());
             context.ServiceProvider.GetService<ParentChainBlockInfoRpcServer>()
                 .Init(ChainConfig.Instance.ChainId.ConvertBase58ToChainId());
             var opt = context.ServiceProvider.GetService<IOptionsSnapshot<GrpcConfigOption>>().Value;

             var clientManager = context.ServiceProvider.GetService<GrpcClientManager>();
             // Init client connected to parent chain if it exists.
             clientManager.Init(opt.CertificateDir);
             if (!string.IsNullOrEmpty(opt.ParentChainId) && !string.IsNullOrEmpty(opt.ParentChainNodeIp) &&
                 !string.IsNullOrEmpty(opt.ParentChainPort)) return;
             var blockInfoCache = new BlockInfoCache(opt.ParentChainId.ConvertBase58ToChainId());
             clientManager.CreateClient(new GrpcClientBase
             {
                 TargetIp = opt.ParentChainNodeIp,
                 TargetPort = uint.Parse(opt.ParentChainPort),
                 TargetChainId = opt.ParentChainId.ConvertBase58ToChainId(),
                 TargetIsSideChain = false,
                 BlockInfoCache = blockInfoCache
             });
             context.ServiceProvider.GetService<GrpcCrossChainDataProvider>().ParentChainBlockInfoCache =
                 blockInfoCache;
         }
     }
 }