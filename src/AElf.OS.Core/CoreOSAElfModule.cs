using AElf.Kernel;
using AElf.Kernel.Token;
using AElf.Modularity;
using AElf.OS.Network;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.OS
{
    [DependsOn(typeof(CoreKernelAElfModule))]
    public class CoreOSAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();

            Configure<NetworkOptions>(configuration.GetSection("Network"));

            context.Services.AddSingleton<IPrimaryTokenSymbolProvider, PrimaryTokenSymbolProvider>();
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var taskQueueManager = context.ServiceProvider.GetService<ITaskQueueManager>();
            taskQueueManager.CreateQueue(NetworkConstants.PeerReconnectionQueueName);
            taskQueueManager.CreateQueue(NetworkConstants.BlockBroadcastQueueName);
            taskQueueManager.CreateQueue(NetworkConstants.AnnouncementBroadcastQueueName);
            taskQueueManager.CreateQueue(NetworkConstants.TransactionBroadcastQueueName);
        }
    }
}