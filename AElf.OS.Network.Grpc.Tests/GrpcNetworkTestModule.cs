using System;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Miner.Application;
using AElf.Modularity;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.OS.Network
{
    [DependsOn(typeof(OSCoreTestAElfModule), typeof(GrpcNetworkModule))]
    public class GrpcNetworkTestModule : AElfModule
    {
        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            base.OnApplicationInitialization(context);

            var minerService  = context.ServiceProvider.GetRequiredService<IMinerService>();
            var blockchainService = context.ServiceProvider.GetRequiredService<IBlockchainService>();
            
            for (int i = 0; i < 5; i++)
            {
                var chain = AsyncHelper.RunSync(() => blockchainService.GetChainAsync());
                AsyncHelper.RunSync(() => minerService.MineAsync(chain.BestChainHash, chain.BestChainHeight,
                    DateTime.UtcNow.AddMilliseconds(4000)));
            }

            var pool = context.ServiceProvider.GetRequiredService<IPeerPool>();
            pool.AddPeer(new GrpcPeer(new Channel("127.0.0.1:", ChannelCredentials.Insecure), null, "peerPubKey", "127.0.0.1:666"));
        }
    }
}