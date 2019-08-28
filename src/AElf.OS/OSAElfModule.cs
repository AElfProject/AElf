using AElf.Kernel;
using AElf.Modularity;
using AElf.OS.BlockSync.Worker;
using AElf.OS.Consensus.DPos;
using AElf.OS.Network;
using AElf.OS.Network.Grpc;
using AElf.OS.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Modularity;

namespace AElf.OS
{
    [DependsOn(
        typeof(AbpBackgroundWorkersModule),
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

            context.Services.AddSingleton<PeerDiscoveryWorker>();

            Configure<AccountOptions>(configuration.GetSection("Account"));
        }
        
        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var taskQueueManager = context.ServiceProvider.GetService<ITaskQueueManager>();

            taskQueueManager.CreateQueue(OSConstants.BlockSyncAttachQueueName);
            taskQueueManager.CreateQueue(OSConstants.BlockFetchQueueName, 4);
            taskQueueManager.CreateQueue(OSConstants.InitialSyncQueueName);

            var backgroundWorkerManager = context.ServiceProvider.GetRequiredService<IBackgroundWorkerManager>();
            
            var networkOptions = context.ServiceProvider.GetService<IOptionsSnapshot<NetworkOptions>>().Value;
            if (networkOptions.EnablePeerDiscovery)
            {
                var peerDiscoveryWorker = context.ServiceProvider.GetService<PeerDiscoveryWorker>();
                backgroundWorkerManager.Add(peerDiscoveryWorker);
            }

            backgroundWorkerManager.Add(context.ServiceProvider.GetService<BlockDownloadWorker>());
        }
    }
}