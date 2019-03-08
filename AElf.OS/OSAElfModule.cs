using AElf.Common.Application;
using AElf.Cryptography;
using AElf.Modularity;
using AElf.OS.Handlers;
using AElf.OS.Jobs;
using AElf.OS.Network.Grpc;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.OS
{
    [DependsOn(
        typeof(CoreOSAElfModule),
        typeof(GrpcNetworkModule))]
    public class OSAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();

            context.Services.AddAssemblyOf<OSAElfModule>();

            Configure<AccountOptions>(configuration.GetSection("Account"));

            context.Services.AddSingleton<PeerConnectedEventHandler>();
            context.Services.AddTransient<ForkDownloadJob>();

            var keyStore = new AElfKeyStore(ApplicationHelper.AppDataPath);
            context.Services.AddSingleton<IKeyStore>(keyStore);
        }
    }
}