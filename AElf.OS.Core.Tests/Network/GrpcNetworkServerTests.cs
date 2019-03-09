using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.Network;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;
using Xunit.Abstractions;

namespace AElf.OS.Network
{
    public class GrpcNetworkConnectionTests : OSCoreTestBase
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly IOptionsSnapshot<ChainOptions> _optionsMock;

        public GrpcNetworkConnectionTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            
            var optionsMock = new Mock<IOptionsSnapshot<ChainOptions>>();
            optionsMock.Setup(m => m.Value).Returns(new ChainOptions { ChainId = ChainHelpers.GetRandomChainId() });
            _optionsMock = optionsMock.Object;
        }
        
        private (GrpcNetworkServer, IPeerPool) BuildGrpcNetworkServer(NetworkOptions networkOptions, Action<object> eventCallBack = null)
        {
            var optionsMock = new Mock<IOptionsSnapshot<NetworkOptions>>();
            optionsMock.Setup(m => m.Value).Returns(networkOptions);
            
            var mockLocalEventBus = new Mock<ILocalEventBus>();
            
            // Catch all events on the bus
            if (eventCallBack != null)
            {
                mockLocalEventBus
                    .Setup(m => m.PublishAsync(It.IsAny<object>()))
                    .Returns<object>(t => Task.CompletedTask)
                    .Callback<object>(m => eventCallBack(m));
            }
            
            var mockBlockChainService = new Mock<IFullBlockchainService>();
            mockBlockChainService.Setup(m => m.GetBestChainLastBlock())
                .Returns(Task.FromResult(new BlockHeader()));
            
            GrpcPeerPool grpcPeerPool = new GrpcPeerPool( optionsMock.Object, NetMockHelpers.MockAccountService().Object, mockBlockChainService.Object);
            
            GrpcServerService serverService = new GrpcServerService(grpcPeerPool, mockBlockChainService.Object);
            serverService.EventBus = mockLocalEventBus.Object;
            
            GrpcNetworkServer netServer = new GrpcNetworkServer(optionsMock.Object, serverService, grpcPeerPool);
            netServer.EventBus = mockLocalEventBus.Object;

            return (netServer, grpcPeerPool);
        }

        [Fact]
        public async Task SelfConnection_Test()
        {
            var server = BuildGrpcNetworkServer(new NetworkOptions
            {
                BootNodes = new List<string> {"127.0.0.1:6800"}, // Himself as a bootnode
                ListeningPort = 6800
            });

            await server.Item1.StartAsync();
            var peers1 = server.Item2.GetPeers();

            Assert.True(peers1.Count == 0);

            await server.Item2.AddPeerAsync("127.0.0.1:6800");

            var peers2 = server.Item2.GetPeers();
            Assert.True(peers2.Count == 0);

            await server.Item1.StopAsync();
        }

        [Fact]
        public async Task Basic_Net_Formation_Test()
        {
            var m1 = BuildGrpcNetworkServer(new NetworkOptions { ListeningPort = 6800 });
            var m2 = BuildGrpcNetworkServer(new NetworkOptions { ListeningPort = 6801 });
            
            await m1.Item1.StartAsync();
            await m2.Item1.StartAsync();
            
            await m2.Item2.AddPeerAsync("127.0.0.1:6800");
            
            var p = m1.Item2.FindPeerByAddress("127.0.0.1:6801");
            var p2 = m2.Item2.FindPeerByAddress("127.0.0.1:6800");
            
            Assert.NotNull(p);
            Assert.NotNull(p2);
            
            await m1.Item1.StopAsync();
            await m2.Item1.StopAsync();
        }
                
        [Fact]
        public async Task Basic_Connection_Test()
        {
            // setup 2 peers
            var m1 = BuildGrpcNetworkServer(new NetworkOptions {
                ListeningPort = 6800 
            });
            
            var m2 = BuildGrpcNetworkServer(new NetworkOptions
            {
                BootNodes = new List<string> {"127.0.0.1:6800"},
                ListeningPort = 6801
            });
            
            await m1.Item1.StartAsync();
            await m2.Item1.StartAsync();
            
            var p = m2.Item2.FindPeerByAddress("127.0.0.1:6800");
            var p2 = m1.Item2.FindPeerByAddress("127.0.0.1:6801");

            await m1.Item1.StopAsync();
            await m2.Item1.StopAsync();
            
            Assert.NotNull(p);
            Assert.NotNull(p2);
        }

        [Fact]
        public async Task GetPeers_Test()
        {
            var m1 = BuildGrpcNetworkServer(new NetworkOptions
            {
                ListeningPort = 6800 
            });
            
            var m2 = BuildGrpcNetworkServer(new NetworkOptions
            {
                BootNodes = new List<string> {"127.0.0.1:6800"},
                ListeningPort = 6801
            });

            var m3 = BuildGrpcNetworkServer(new NetworkOptions
            {
                BootNodes = new List<string> {"127.0.0.1:6800", "127.0.0.1:6801"},
                ListeningPort = 6802
            });
            
            await m1.Item1.StartAsync();
            await m2.Item1.StartAsync();
            await m3.Item1.StartAsync();

            var peers = m3.Item2.GetPeers();

            Assert.True(peers.Count == 2);
            
            Assert.Contains("127.0.0.1:6800", peers.Select(p => p.PeerAddress));
            Assert.Contains("127.0.0.1:6801", peers.Select(p => p.PeerAddress));
            
            await m1.Item1.StopAsync();
            await m2.Item1.StopAsync();
            await m3.Item1.StopAsync();
        }

        [Fact]
        public async Task Basic_Add_Remove_Peer_Test()
        {
            // setup 2 peers

            var m1 = BuildGrpcNetworkServer(new NetworkOptions {
                ListeningPort = 6800
            });

            var m2 = BuildGrpcNetworkServer(new NetworkOptions
            {
                BootNodes = new List<string> {"127.0.0.1:6800"},
                ListeningPort = 6801
            });

            await m1.Item1.StartAsync();
            await m2.Item1.StartAsync();

            var p = await m2.Item2.RemovePeerAsync("127.0.0.1:6800");
            var p2 = await m2.Item2.AddPeerAsync("127.0.0.1:6800");

            await m1.Item1.StopAsync();
            await m2.Item1.StopAsync();

            Assert.True(p);
            Assert.True(p2);
        }

        [Fact]
        public async Task Basic_Remove_NotExist_Peer_Test()
        {
            var m1 = BuildGrpcNetworkServer(new NetworkOptions { ListeningPort = 6800 });
            var m2 = BuildGrpcNetworkServer(new NetworkOptions { ListeningPort = 6801, BootNodes = new List<string> {"127.0.0.1:6800"}});

            await m1.Item1.StartAsync();
            await m2.Item1.StartAsync();

            var peers = m1.Item2.GetPeers();
            peers.Count.ShouldBe(1);

            await m1.Item2.RemovePeerAsync("127.0.0.1:7000");
            peers = m1.Item2.GetPeers();
            peers.Count.ShouldBe(1);

            await m1.Item2.RemovePeerAsync("127.0.0.1:6801");
            peers = m1.Item2.GetPeers();
            peers.Count.ShouldBe(0);

            await m1.Item1.StopAsync();
            await m2.Item1.StopAsync();
        }
        
        [Fact]
        public async Task GetPeers_NotExist_Test()
        {
            var m1 = BuildGrpcNetworkServer(new NetworkOptions { ListeningPort = 6800, BootNodes = new List<string> {"127.0.0.1:6801", "127.0.0.1:6802"}});
            var m2 = BuildGrpcNetworkServer(new NetworkOptions { ListeningPort = 4801 });

            await m1.Item1.StartAsync();
            await m2.Item1.StartAsync();

            var peers = m1.Item2.GetPeers();

            await m1.Item1.StopAsync();
            await m2.Item1.StopAsync();

            peers.Count.ShouldBe(0);
        }

        [Fact]
        public async Task RemovePeer_Test()
        {
            // setup 2 peers
            
            var m1 = BuildGrpcNetworkServer(new NetworkOptions
            {
                ListeningPort = 6800 
            });
            
            var m2 = BuildGrpcNetworkServer(new NetworkOptions
            {
                BootNodes = new List<string> {"127.0.0.1:6800"},
                ListeningPort = 6801
            });
            
            await m1.Item1.StartAsync();
            await m2.Item1.StartAsync();
            
            var p = m2.Item2.FindPeerByAddress("127.0.0.1:6800");

            Assert.NotNull(p);

            await m2.Item2.RemovePeerAsync("127.0.0.1:6800");
            var p2 = m2.Item2.FindPeerByAddress("127.0.0.1:6800");
            
            Assert.Null(p2);

            await m1.Item1.StopAsync();
            await m2.Item1.StopAsync();
        }
        
        [Fact]
        public async Task GetPeers_DisconnectionOfDialer_Test()
        {
            // setup 2 peers
            
            var m1 = BuildGrpcNetworkServer(new NetworkOptions
            {
                ListeningPort = 6800 
            });
            
            var m2 = BuildGrpcNetworkServer(new NetworkOptions
            {
                BootNodes = new List<string> {"127.0.0.1:6800"},
                ListeningPort = 6801
            });
            
            await m1.Item1.StartAsync();
            await m2.Item1.StartAsync();
            
            var p = m2.Item2.FindPeerByAddress("127.0.0.1:6800");

            Assert.NotNull(p);

            await m1.Item1.StopAsync();
           
            var p2 = m2.Item2.FindPeerByAddress("127.0.0.1:6800");
            
            Assert.Null(p2);

            await m2.Item1.StopAsync();
        }
        
        [Fact]
        public async Task GetPeers_DisconnectionOfDialee_Test()
        {
            // setup 2 peers
            
            var m1 = BuildGrpcNetworkServer(new NetworkOptions
            {
                ListeningPort = 6800 
            });
            
            var m2 = BuildGrpcNetworkServer(new NetworkOptions
            {
                BootNodes = new List<string> {"127.0.0.1:6800"},
                ListeningPort = 6801
            });
            
            await m1.Item1.StartAsync();
            await m2.Item1.StartAsync();
            
            var p = m1.Item2.FindPeerByAddress("127.0.0.1:6801");

            Assert.NotNull(p);

            await m2.Item1.StopAsync();
           
            var p2 = m1.Item2.FindPeerByAddress("127.0.0.1:6801");
            
            Assert.Null(p2);

            await m1.Item1.StopAsync();
        }
        
        [Fact]
        public async Task GetPeers_HardDisconnect_Test()
        {
            var m1 = BuildGrpcNetworkServer(new NetworkOptions
            {
                ListeningPort = 6800 
            });
            
            var m2 = BuildGrpcNetworkServer(new NetworkOptions
            {
                BootNodes = new List<string> {"127.0.0.1:6800"},
                ListeningPort = 6801
            });
            
            await m1.Item1.StartAsync();
            await m2.Item1.StartAsync();
            
            var p = m1.Item2.FindPeerByAddress("127.0.0.1:6801");

            Assert.NotNull(p);

            await m2.Item1.StopAsync(false); // stop 2 with hard disconnect
            
            // m1 tries to send an RPC to m2, will trigger the remove op
            await p.AnnounceAsync(new PeerNewBlockAnnouncement()); 

            // make sure we wait enough for disc
            await Task.Delay(TimeSpan.FromSeconds(NetworkConsts.DefaultPeerDialTimeout+2));
            
            // should be null
            var p2 = m1.Item2.FindPeerByAddress("127.0.0.1:6801");
            Assert.Null(p2);

            await m1.Item1.StopAsync();
        }
        
        [Fact]
        public async Task GetPeers_SoftDisconnect_Test()
        {
            var m1 = BuildGrpcNetworkServer(new NetworkOptions
            {
                ListeningPort = 6800 
            });
            
            var m2 = BuildGrpcNetworkServer(new NetworkOptions
            {
                BootNodes = new List<string> {"127.0.0.1:6800"},
                ListeningPort = 6801
            });
            
            await m1.Item1.StartAsync();
            await m2.Item1.StartAsync();
            
            var p = m1.Item2.FindPeerByAddress("127.0.0.1:6801");

            Assert.NotNull(p);

            await m2.Item1.StopAsync(); // stop 2
            
            // should be null
            var p2 = m1.Item2.FindPeerByAddress("127.0.0.1:6801");
            Assert.Null(p2);

            await m1.Item1.StopAsync();
        }
    }
}