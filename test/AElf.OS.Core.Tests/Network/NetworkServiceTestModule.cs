using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.SmartContract;
using AElf.Modularity;
using AElf.OS.Helpers;
using AElf.OS.Network.Application;
using AElf.OS.Network.Helpers;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Network.Protocol.Types;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.OS.Network
{
    [DependsOn(typeof(OSCoreTestAElfModule))]
    public class NetworkServiceTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<INetworkService, NetworkService>();
            
            context.Services.AddSingleton<IPeerPool, PeerPool>();
            context.Services.AddTransient(o => Mock.Of<IBroadcastPrivilegedPubkeyListProvider>());
            context.Services.AddTransient<IAElfNetworkServer>(o =>
            {
                var mockService = new Mock<IAElfNetworkServer>();
                mockService.Setup(s => s.ConnectAsync(It.IsAny<DnsEndPoint>())).ReturnsAsync(true);

                return mockService.Object;
            });
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var peerPool = context.ServiceProvider.GetRequiredService<IPeerPool>();
            var osTestHelper = context.ServiceProvider.GetRequiredService<OSTestHelper>();

            {
                var normalPeer = new Mock<IPeer>();
                normalPeer.Setup(m => m.RemoteEndpoint).Returns(new DnsEndPoint("192.168.100.200", 5000));
                var blockWithTransactions = osTestHelper.GenerateBlockWithTransactions(Hash.Empty, 10);
                normalPeer.Setup(p => p.Info).Returns(new PeerConnectionInfo
                    {Pubkey = "NormalPeer", ConnectionTime = TimestampHelper.GetUtcNow()});
                normalPeer.Setup(p => p.GetBlocksAsync(It.IsAny<Hash>(), It.IsAny<int>()))
                    .Returns<Hash, int>((h, cnt) => Task.FromResult(new List<BlockWithTransactions>()));
                normalPeer.Setup(p => p.IsReady).Returns(true);
                normalPeer.Setup(p => p.GetBlockByHashAsync(It.Is<Hash>(h => h == HashHelper.ComputeFrom("bHash1"))))
                    .Returns<Hash>(h => Task.FromResult(blockWithTransactions));
                normalPeer.Setup(m => m.GetNodesAsync(It.IsAny<int>()))
                    .Returns(Task.FromResult(new NodeList
                    {
                        Nodes =
                        {
                            new NodeInfo
                            {
                                Endpoint = "http://192.168.100.88:8000",
                                Pubkey = ByteString.CopyFromUtf8("p2")
                            }
                        }
                    }));
                
                peerPool.TryAddPeer(normalPeer.Object);
            }

            {
                var failedPeerPeer = new Mock<IPeer>();
                failedPeerPeer.Setup(p => p.RemoteEndpoint).Returns(new DnsEndPoint("192.168.100.400", 80));
                failedPeerPeer.Setup(p => p.Info).Returns(new PeerConnectionInfo
                    {Pubkey = "FailedPeer", ConnectionTime = TimestampHelper.GetUtcNow()});
                failedPeerPeer.Setup(p => p.IsInvalid).Returns(true);
                failedPeerPeer.Setup(p => p.GetBlockByHashAsync(It.IsAny<Hash>())).Throws(new NetworkException());
                failedPeerPeer.Setup(p => p.GetBlocksAsync(It.IsAny<Hash>(), It.IsAny<int>())).Throws(new NetworkException());
                peerPool.TryAddPeer(failedPeerPeer.Object);
            }
        }
    }

    [DependsOn(typeof(NetworkServiceTestModule))]
    public class NetworkServicePropagationTestModule : AElfModule
    {
        private class KnownHashContainer
        {
            private readonly List<Hash> _knownHashes = new List<Hash>();
            public bool TryAdd(Hash hash)
            {
                if (HasHash(hash)) 
                    return false;

                _knownHashes.Add(hash);

                return true;
            }

            public bool HasHash(Hash hash)
            {
                return _knownHashes.Contains(hash);
            }
        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var testContext = new NetworkServicePropagationTestContext();
            var aelfNetworkServer = new Mock<IAElfNetworkServer>();
            testContext.MockAElfNetworkServer = aelfNetworkServer;

            context.Services.AddSingleton<IAElfNetworkServer>(aelfNetworkServer.Object);
            context.Services.AddSingleton(testContext);
            context.Services.AddTransient(o => Mock.Of<IBroadcastPrivilegedPubkeyListProvider>());
        }
        
        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var peerPool = context.ServiceProvider.GetRequiredService<IPeerPool>();
            var testContext = context.ServiceProvider.GetRequiredService<NetworkServicePropagationTestContext>();
            for (var i = 0; i < 3; i++)
            {
                var peer = new Mock<IPeer>();
                var knownBlockHashes = new KnownHashContainer();
                var knownTransactionHashes = new KnownHashContainer();

                peer.Setup(p => p.Info).Returns(new PeerConnectionInfo {Pubkey = "Pubkey" + i.ToString()});
                peer.Setup(p => p.IsReady).Returns(true);
                peer.Setup(p => p.TryAddKnownBlock(It.IsAny<Hash>()))
                    .Returns<Hash>(blockHash => knownBlockHashes.TryAdd(blockHash));
                peer.Setup(p => p.KnowsBlock(It.IsAny<Hash>()))
                    .Returns<Hash>(blockHash => knownBlockHashes.HasHash(blockHash));

                peer.Setup(p => p.TryAddKnownTransaction(It.IsAny<Hash>()))
                    .Returns<Hash>(txHash => knownTransactionHashes.TryAdd(txHash));
                peer.Setup(p => p.KnowsTransaction(It.IsAny<Hash>()))
                    .Returns<Hash>(txHash => knownTransactionHashes.HasHash(txHash));
                SetupBroadcastCallbacks(peer);

                peerPool.TryAddPeer(peer.Object);
                testContext.MockedPeers.Add(peer);
            }
        }

        private void SetupBroadcastCallbacks(Mock<IPeer> peerMock)
        {
            // set up the mock to execute the broadcast callbacks

            peerMock
                .Setup(p => p.EnqueueBlock(It.IsAny<BlockWithTransactions>(), It.IsAny<Action<NetworkException>>()))
                .Callback<BlockWithTransactions, Action<NetworkException>>((block, action) => action.Invoke(null));
                
            peerMock
                .Setup(p => p.EnqueueAnnouncement(It.IsAny<BlockAnnouncement>(), It.IsAny<Action<NetworkException>>()))
                .Callback<BlockAnnouncement, Action<NetworkException>>((announce, action) => action.Invoke(null));
                
            peerMock
                .Setup(p => p.EnqueueTransaction(It.IsAny<Transaction>(), It.IsAny<Action<NetworkException>>()))
                .Callback<Transaction, Action<NetworkException>>((transaction, action) => action.Invoke(null));
        }
    }

    public class NetworkServicePropagationTestContext
    {
        public List<Mock<IPeer>> MockedPeers = new List<Mock<IPeer>>();

        public Mock<IAElfNetworkServer> MockAElfNetworkServer = new Mock<IAElfNetworkServer>();
    }
}