using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.OS.Network;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Temp;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AElf.OS.Tests.Network
{
    public class GrpcNetworkConnectionTests
    {
        private GrpcNetworkManager BuildNetManager(NetworkOptions networkOptions, Action<object> eventCallBack = null)
        {
            var kp1 = new KeyPairGenerator().Generate();
            
            // mock IOptionsSnapshot
            var mock = new Mock<IOptionsSnapshot<NetworkOptions>>();
            mock.Setup(m => m.Value).Returns(networkOptions);
            
            // mock IAccountService
            var accountService = new Mock<IAccountService>();
            
            accountService.Setup(m => m.GetPublicKey()).Returns(Task.FromResult(kp1.PublicKey));
            
            accountService
                .Setup(m => m.Sign(It.IsAny<byte[]>()))
                .Returns<byte[]>(m => Task.FromResult(new ECSigner().Sign(kp1, m).SigBytes));
            
            accountService
                .Setup(m => m.VerifySignature(It.IsAny<byte[]>(), It.IsAny<byte[]>()))
                .Returns<byte[], byte[]>( (sig, data) => Task.FromResult(CryptoHelpers.Verify(sig, data, kp1.PublicKey)));
            
            var mockLocalEventBus = new Mock<ILocalEventBus>();
            
            // Catch all events on the bus
            if (eventCallBack != null)
            {
                mockLocalEventBus
                    .Setup(m => m.PublishAsync(It.IsAny<object>()))
                    .Returns<object>(t => Task.CompletedTask)
                    .Callback<object>(m => eventCallBack(m));
            }
            
            GrpcNetworkManager manager1 = new GrpcNetworkManager(mock.Object, accountService.Object, null, mockLocalEventBus.Object);

            return manager1;
        }
                
        [Fact]
        public async Task Basic_Connection_Test()
        {
            // setup 2 peers
            
            var m1 = BuildNetManager(new NetworkOptions {
                ListeningPort = 6800 
            });
            
            var m2 = BuildNetManager(new NetworkOptions
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
            
            var m1 = BuildNetManager(new NetworkOptions {
                ListeningPort = 6800 
            });
            
            var m2 = BuildNetManager(new NetworkOptions
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
            var m1 = BuildNetManager(new NetworkOptions
            {
                ListeningPort = 6800 
            });
            
            var m2 = BuildNetManager(new NetworkOptions
            {
                BootNodes = new List<string> {"127.0.0.1:6800"},
                ListeningPort = 6801
            });

            var m3 = BuildNetManager(new NetworkOptions
            {
                BootNodes = new List<string> {"127.0.0.1:6800", "127.0.0.1:6801"},
                ListeningPort = 6802
            });
            
            await m1.StartAsync();
            await m2.StartAsync();
            await m3.StartAsync();

            var peers = m3.GetPeers();

            Assert.True(peers.Count == 2);
            
            Assert.True(peers.Contains("127.0.0.1:6800"));
            Assert.True(peers.Contains("127.0.0.1:6801"));
            
            await m1.StopAsync();
            await m2.StopAsync();
            await m3.StopAsync();
        }
        
        [Fact]
        public async Task RemovePeer_Test()
        {
            // setup 2 peers
            
            var m1 = BuildNetManager(new NetworkOptions
            {
                ListeningPort = 6800 
            });
            
            var m2 = BuildNetManager(new NetworkOptions
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
            
            var m1 = BuildNetManager(new NetworkOptions
            {
                ListeningPort = 6800 
            });
            
            var m2 = BuildNetManager(new NetworkOptions
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
            
            var m1 = BuildNetManager(new NetworkOptions
            {
                ListeningPort = 6800 
            });
            
            var m2 = BuildNetManager(new NetworkOptions
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