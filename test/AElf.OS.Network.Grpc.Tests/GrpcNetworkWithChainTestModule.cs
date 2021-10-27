using System;
using AElf.Kernel;
using AElf.Modularity;
using AElf.OS.Network.Application;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Helpers;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Network.Protocol.Types;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using QuickGraph;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.OS.Network.Grpc
{
    [DependsOn(
        typeof(OSCoreWithChainTestAElfModule),
        typeof(GrpcNetworkBaseTestModule))]
    public class GrpcNetworkWithChainTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var syncStateProvider = context.ServiceProvider.GetRequiredService<INodeSyncStateProvider>();
            syncStateProvider.SetSyncTarget(-1);
        }
    }
}