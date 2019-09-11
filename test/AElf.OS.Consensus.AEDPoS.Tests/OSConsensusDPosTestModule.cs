using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Modularity;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Helpers;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.OS.Consensus.DPos
{
    [DependsOn(
        typeof(OSCoreWithChainTestAElfModule),
        typeof(AElfConsensusOSAElfModule)
    )]
    public class OSConsensusDPosTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var publicKeys = new[]
            {
                OSConsensusDPosTestConstants.Bp1PublicKey,
                OSConsensusDPosTestConstants.Bp2PublicKey,
                OSConsensusDPosTestConstants.Bp3PublicKey
            };
            
            var services = context.Services;
            services.AddSingleton<IPeerPool, PeerPool>();
            var peerList = new List<IPeer>();
            for (var i = 0; i < 3; i++)
            {
                var connectionInfo = new PeerInfo
                {
                    Pubkey = publicKeys[i],
                    ProtocolVersion = KernelConstants.ProtocolVersion,
                    ConnectionTime = TimestampHelper.GetUtcNow(),
                    IsInbound = true
                };
                peerList.Add(new GrpcPeer(new GrpcClient(null, null), IpEndPointHelper.Parse($"127.0.0.1:68{i + 1}0"), connectionInfo));
            }

            services.AddTransient(o =>
            {
                var mockService = new Mock<IPeerPool>();
                mockService.Setup(m => m.FindPeerByPublicKey(It.Is<string>(s => s.Length > 0)))
                    .Returns(peerList[2]);
                mockService.Setup(m => m.GetPeers(It.IsAny<bool>()))
                    .Returns(peerList);
                return mockService.Object;
            });

            services.AddTransient(o =>
            {
                var mockService = new Mock<IAEDPoSInformationProvider>();
                mockService.Setup(m => m.GetCurrentMinerList(It.IsAny<ChainContext>()))
                    .Returns(async () =>
                        await Task.FromResult(publicKeys));
                return mockService.Object;
            });
        }
    }
}