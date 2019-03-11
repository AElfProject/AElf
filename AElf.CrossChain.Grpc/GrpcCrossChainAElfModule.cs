using System.Linq;
using AElf.Common.Application;
using AElf.CrossChain.Grpc.Server;
using AElf.Cryptography.Certificate;
using AElf.Kernel.Node.Infrastructure;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.CrossChain
{
    [DependsOn(typeof(CrossChainAElfModule))]
    public class GrpcCrossChainAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddTransient<IChainPlugin, GrpcCrossChainServerClient>();
            services.AddSingleton<ICrossChainServer, CrossChainGrpcServer>();
            var configuration = context.Services.GetConfiguration();
            Configure<GrpcCrossChainConfigOption>(configuration.GetSection("CrossChain").GetChildren()
                .FirstOrDefault(child => child.Key.Equals("Grpc")));
            var keyStore = new CertificateStore(ApplicationHelper.AppDataPath);
            context.Services.AddSingleton<ICertificateStore>(keyStore);
        }
    }
}