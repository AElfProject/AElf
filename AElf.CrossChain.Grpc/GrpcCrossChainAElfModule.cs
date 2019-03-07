using AElf.Common.Application;
using AElf.CrossChain.Grpc;
using AElf.CrossChain.Grpc.Server;
using AElf.Cryptography;
using AElf.Cryptography.Certificate;
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
            services.AddSingleton<CrossChainGrpcServer>();
            var configuration = context.Services.GetConfiguration();
            Configure<CrossChainGrpcConfigOption>(configuration.GetSection("CrossChainGrpc"));
            
            var certificateStore = new CertificateStore(ApplicationHelper.AppDataPath);
            context.Services.AddSingleton<ICertificateStore>(certificateStore);
        }
    }
}