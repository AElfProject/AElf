using AElf.Kernel;
using AElf.Modularity;
using AElf.OS.Consensus.DPos;
using AElf.OS.Network.Grpc;
using Microsoft.Extensions.DependencyInjection;
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

            Configure<AccountOptions>(configuration.GetSection("Account"));
        }
        
        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var taskQueueManager = context.ServiceProvider.GetService<ITaskQueueManager>();

            taskQueueManager.CreateQueue(OSConstants.BlockSyncAttachQueueName);
            taskQueueManager.CreateQueue(OSConstants.BlockDownloadQueueName);
            taskQueueManager.CreateQueue(OSConstants.BlockFetchQueueName, 4);
            taskQueueManager.CreateQueue(OSConstants.InitialSyncQueueName);
        }
    }
}