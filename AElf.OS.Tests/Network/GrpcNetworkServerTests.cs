using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.OS.Network;
using AElf.OS.Network.Grpc;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp.EventBus.Local;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace AElf.OS.Tests.Network
{
    public class GrpcNetworkConnectionTests
    {
        private GrpcNetworkServer BuildGrpcNetworkServer(NetworkOptions networkOptions, Action<object> eventCallBack = null)
        {
            var optionsMock = new Mock<IOptionsSnapshot<NetworkOptions>>();
            optionsMock.Setup(m => m.Value).Returns(networkOptions);
            
            var accountServiceMock = NetMockHelpers.MockAccountService();
            var mockLocalEventBus = new Mock<ILocalEventBus>();
            
            // Catch all events on the bus
            if (eventCallBack != null)
            {
                mockLocalEventBus
                    .Setup(m => m.PublishAsync(It.IsAny<object>()))
                    .Returns<object>(t => Task.CompletedTask)
                    .Callback<object>(m => eventCallBack(m));
            }
            
            GrpcNetworkServer manager1 = new GrpcNetworkServer(optionsMock.Object, accountServiceMock.Object, null, mockLocalEventBus.Object);

            return manager1;
        }

        [Fact]
        public async Task Basic_Net_Formation()
        {
            var m1 = BuildGrpcNetworkServer(new NetworkOptions { ListeningPort = 6800 });
            var m2 = BuildGrpcNetworkServer(new NetworkOptions { ListeningPort = 6801 });
            
            await m1.StartAsync();
            await m2.StartAsync();
            
            await m2.AddPeerAsync("127.0.0.1:6800");
            
            var p = m1.GetPeer("127.0.0.1:6801");
            var p2 = m2.GetPeer("127.0.0.1:6800");
            
            Assert.True(!string.IsNullOrWhiteSpace(p));
            Assert.True(!string.IsNullOrWhiteSpace(p2));
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
            
            await m1.StartAsync();
            await m2.StartAsync();
            
            var p = m2.GetPeer("127.0.0.1:6800");
            var p2 = m2.GetPeer("127.0.0.1:6801");

            await m1.StopAsync();
            await m2.StopAsync();
            
            Assert.True(!string.IsNullOrWhiteSpace(p));
        }
        
        [Fact]
        public async Task Basic_AddRemovePeer_Test()
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
            
            await m1.StartAsync();
            await m2.StartAsync();
            
            var p = await m2.RemovePeerAsync("127.0.0.1:6800");
            var p2 = await m2.AddPeerAsync("127.0.0.1:6800");

            await m1.StopAsync();
            await m2.StopAsync();
            
            //Assert.True(!string.IsNullOrWhiteSpace(p));
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
            
            await m1.StartAsync();
            await m2.StartAsync();
            await m3.StartAsync();

            var peers = m3.GetPeers();

            Assert.True(peers.Count == 2);
            
            Assert.True(peers.Select(p => p.PeerAddress).Contains("127.0.0.1:6800"));
            Assert.True(peers.Select(p => p.PeerAddress).Contains("127.0.0.1:6801"));
            
            await m1.StopAsync();
            await m2.StopAsync();
            await m3.StopAsync();
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
            
            await m1.StartAsync();
            await m2.StartAsync();
            
            var p = m2.GetPeer("127.0.0.1:6800");

            Assert.True(!string.IsNullOrWhiteSpace(p));

            await m2.RemovePeerAsync("127.0.0.1:6800");
            var p2 = m2.GetPeer("127.0.0.1:6800");
            
            Assert.True(string.IsNullOrWhiteSpace(p2));

            await m1.StopAsync();
            await m2.StopAsync();
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
            
            await m1.StartAsync();
            await m2.StartAsync();
            
            var p = m2.GetPeer("127.0.0.1:6800");

            Assert.True(!string.IsNullOrWhiteSpace(p));

            await m1.StopAsync();
           
            var p2 = m2.GetPeer("127.0.0.1:6800");
            
            Assert.True(string.IsNullOrWhiteSpace(p2));

            await m2.StopAsync();
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
            
            await m1.StartAsync();
            await m2.StartAsync();
            
            var p = m1.GetPeer("127.0.0.1:6801");

            Assert.True(!string.IsNullOrWhiteSpace(p));

            await m2.StopAsync();
           
            var p2 = m1.GetPeer("127.0.0.1:6801");
            
            Assert.True(string.IsNullOrWhiteSpace(p2));

            await m1.StopAsync();
        }
    }
}