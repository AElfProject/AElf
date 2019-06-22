using System.Linq;
using AElf.Common.Application;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Modularity;
using AElf.OS.Consensus.DPos;
using AElf.OS.Handlers;
using AElf.OS.Network;
using AElf.OS.Network.Grpc;
using AElf.OS.Worker;
using AElf.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

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

            context.Services.AddSingleton<PeerConnectedEventHandler>();
            context.Services.AddSingleton<PeerDiscoveryWorker>();

            //TODO: make ApplicationHelper as a provider, inject it into key store
            var keyStore = new AElfKeyStore(ApplicationHelper.AppDataPath);
            context.Services.AddSingleton<IKeyStore>(keyStore);

            Configure<AccountOptions>(option =>
            {
                configuration.GetSection("Account").Bind(option);

                if (string.IsNullOrWhiteSpace(option.NodeAccount))
                {
                    AsyncHelper.RunSync(async () =>
                    {
                        var accountList = await keyStore.ListAccountsAsync();

                        option.NodeAccountPassword = string.Empty;
                        if (accountList.Count == 0)
                        {
                            var blockChainService = context.Services.GetRequiredServiceLazy<IBlockchainService>().Value;
                            var chainId = blockChainService.GetChainId();
                            var keyPair = await keyStore.CreateAsync(option.NodeAccountPassword, chainId.ToString());
                            option.NodeAccount = Address.FromPublicKey(keyPair.PublicKey).GetFormatted();
                        }
                        else
                        {
                            option.NodeAccount = accountList.First();
                        }

                    });
                }
            });
        }
        
        public override async void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var taskQueueManager = context.ServiceProvider.GetService<ITaskQueueManager>();

            taskQueueManager.CreateQueue(OSConsts.BlockSyncAttachQueueName);
            taskQueueManager.CreateQueue(OSConsts.BlockSyncQueueName);

            var networkOptions = context.ServiceProvider.GetService<IOptionsSnapshot<NetworkOptions>>().Value;

            if (networkOptions.EnablePeerDiscovery)
            {
                var peerDiscoveryWorker = context.ServiceProvider.GetService<PeerDiscoveryWorker>();
                await peerDiscoveryWorker.StartAsync();
            }
        }
    }
}