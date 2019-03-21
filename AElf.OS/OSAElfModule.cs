using System;
using AElf.Common.Application;
using AElf.CrossChain.Grpc;
using AElf.Cryptography;
using AElf.Modularity;
using AElf.OS.Handlers;
using AElf.OS.Jobs;
using AElf.OS.Network.Grpc;
using AElf.OS.Rpc.ChainController;
using AElf.OS.Rpc.Net;
using AElf.OS.Rpc.Wallet;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.OS
{
    [DependsOn(
        typeof(CoreOSAElfModule),
        typeof(GrpcNetworkModule),
        typeof(GrpcCrossChainAElfModule)
    )]
    public class OSAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();

            context.Services.AddAssemblyOf<OSAElfModule>();

            Configure<AccountOptions>(configuration.GetSection("Account"));

            context.Services.AddSingleton<PeerConnectedEventHandler>();
            context.Services.AddTransient<BlockSyncJob>();

            //TODO: make ApplicationHelper as a provider, inject it into key store
            var keyStore = new AElfKeyStore(ApplicationHelper.AppDataPath);
            context.Services.AddSingleton<IKeyStore>(keyStore);
        }
    }
}