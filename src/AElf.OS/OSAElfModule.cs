using AElf.Kernel;
using AElf.Modularity;
using AElf.OS.Consensus.DPos;
using AElf.OS.Handlers;
using AElf.OS.Network;
using AElf.OS.Network.Grpc;
using AElf.OS.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.OS
{
    [DependsOn(
        typeof(KernelAElfModule),
        typeof(CoreOSAElfModule),
        typeof(GrpcNetworkModule),
        typeof(AElfConsensusOSAElfModule)
    )]
    public class OSAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();

            context.Services.AddAssemblyOf<OSAElfModule>();

            context.Services.AddSingleton<PeerDiscoveryWorker>();

            Configure<AccountOptions>(configuration.GetSection("Account"));
        }
        
        public override async void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var taskQueueManager = context.ServiceProvider.GetService<ITaskQueueManager>();

            taskQueueManager.CreateQueue(OSConstants.BlockSyncAttachQueueName);
            taskQueueManager.CreateQueue(OSConstants.BlockFetchQueueName, 4);
            taskQueueManager.CreateQueue(OSConstants.InitialSyncQueueName);

            var networkOptions = context.ServiceProvider.GetService<IOptionsSnapshot<NetworkOptions>>().Value;

            if (networkOptions.EnablePeerDiscovery)
            {
                var peerDiscoveryWorker = context.ServiceProvider.GetService<PeerDiscoveryWorker>();
                await peerDiscoveryWorker.StartAsync();
            }
        }
    }
}