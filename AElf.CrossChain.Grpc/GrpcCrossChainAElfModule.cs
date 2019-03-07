using AElf.CrossChain.Grpc;
using AElf.CrossChain.Grpc.Server;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.CrossChain
{
    [DependsOn(typeof(CrossChainAElfModule))]
    public class GrpcCrossChainAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddSingleton<CrossChainBlockDataRpcServer>();
             
            var configuration = context.Services.GetConfiguration();
            Configure<GrpcConfigOption>(configuration.GetSection("CrossChain"));
        }
        
        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
//             var opt = context.ServiceProvider.GetService<IOptionsSnapshot<GrpcConfigOption>>().Value;

//             var clientService = context.ServiceProvider.GetService<GrpcProducerConsumerService>();
//             // Init client connected to parent chain if it exists.
//             clientService.Init(opt.CertificateDir);
//             if (!string.IsNullOrEmpty(opt.ParentChainId) && !string.IsNullOrEmpty(opt.ParentChainNodeIp) &&
//                 !string.IsNullOrEmpty(opt.ParentChainPort)) return;
//             var blockInfoCache = new BlockInfoCache(opt.ParentChainId.ConvertBase58ToChainId());
//             clientService.CreateConsumerProducer(new CrossChainDataProducer
//             {
//                 TargetIp = opt.ParentChainNodeIp,
//                 TargetPort = uint.Parse(opt.ParentChainPort),
//                 SideChainId = opt.ParentChainId.ConvertBase58ToChainId(),
//                 TargetIsSideChain = false,
//                 BlockInfoCache = blockInfoCache
//             });
//             context.ServiceProvider.GetService<CrossChainDataProvider>().ParentChainBlockInfoCache =
//                 blockInfoCache;
        }

    }
}