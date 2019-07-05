﻿using AElf.Kernel;
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

            Configure<AccountOptions>(configuration.GetSection("Account"));
        }
        
        public override async void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var taskQueueManager = context.ServiceProvider.GetService<ITaskQueueManager>();

            taskQueueManager.CreateQueue(OSConsts.BlockSyncAttachQueueName);
            taskQueueManager.CreateQueue(OSConsts.BlockSyncQueueName);
            taskQueueManager.CreateQueue(OSConsts.InitialSyncQueueName);

            var networkOptions = context.ServiceProvider.GetService<IOptionsSnapshot<NetworkOptions>>().Value;

            if (networkOptions.EnablePeerDiscovery)
            {
                var peerDiscoveryWorker = context.ServiceProvider.GetService<PeerDiscoveryWorker>();
                await peerDiscoveryWorker.StartAsync();
            }
        }
    }
}