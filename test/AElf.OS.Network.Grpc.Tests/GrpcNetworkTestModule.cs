using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Modularity;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.OS.Network
{
    [DependsOn(typeof(OSCoreWithChainTestAElfModule), typeof(GrpcNetworkModule))]
    public class GrpcNetworkTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<NetworkOptions>(o=>
            {
                o.ListeningPort = 2000;
                o.MaxPeers = 2;
            });
            
            Mock<IAEDPoSInformationProvider> aedPoSInformationProviderMock = new Mock<IAEDPoSInformationProvider>();

            ECKeyPair minerKeyPair = CryptoHelpers.GenerateKeyPair();
            ECKeyPair normalNodeKeyPair = CryptoHelpers.GenerateKeyPair();

            var minerKeyPairHex = minerKeyPair.PublicKey.ToHex();
            
            aedPoSInformationProviderMock
                .Setup(a => a.IsInMinerList(It.IsAny<ChainContext>(), It.IsAny<string>()))
                .Returns<ChainContext, string>((chainContext, pubKey) => Task.FromResult(minerKeyPairHex == pubKey));
            
            context.Services.AddSingleton<IAEDPoSInformationProvider>(aedPoSInformationProviderMock.Object);
            
            TestNodeCollection testNodeCollection = new TestNodeCollection();
            
            testNodeCollection.MinerNodes.Add(minerKeyPair);
            testNodeCollection.OtherNodes.Add(normalNodeKeyPair);

            context.Services.AddSingleton<TestNodeCollection>(testNodeCollection);
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            base.OnApplicationInitialization(context);
            
            var pool = context.ServiceProvider.GetRequiredService<IPeerPool>();
            var channel = new Channel(GrpcTestConstants.FakeListeningPort, ChannelCredentials.Insecure);
            
            var connectionInfo = new GrpcPeerInfo
            {
                PublicKey = GrpcTestConstants.FakePubKey2,
                PeerIpAddress = GrpcTestConstants.FakeListeningPort,
                ProtocolVersion = KernelConstants.ProtocolVersion,
                ConnectionTime = TimestampHelper.GetUtcNow().Seconds,
                StartHeight = 1,
                IsInbound = true
            };
            
            pool.AddPeer(new GrpcPeer(channel, new PeerService.PeerServiceClient(channel), connectionInfo));
        }
    }
}