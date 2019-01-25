using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Configuration;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.OS.Network;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Temp;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AElf.OS.Tests.Network
{
    public class GrpcNetworkManagerTests
    {
        private GrpcNetworkManager BuildNetManager(NetworkOptions networkOptions)
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
            
            GrpcNetworkManager manager1 = new GrpcNetworkManager(mock.Object, accountService.Object, null);

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
            
            await m1.Start();
            await m2.Start();
            
            var p = m2.GetPeer("127.0.0.1:6800");

            Assert.True(!string.IsNullOrWhiteSpace(p));

            await m1.Stop();
            await m2.Stop();
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
            
            await m1.Start();
            await m2.Start();
            
            var p = m2.GetPeer("127.0.0.1:6800");

            Assert.True(!string.IsNullOrWhiteSpace(p));

            await m2.RemovePeer("127.0.0.1:6800");
            var p2 = m2.GetPeer("127.0.0.1:6800");
            
            Assert.True(string.IsNullOrWhiteSpace(p2));

            await m1.Stop();
            await m2.Stop();
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
            
            await m1.Start();
            await m2.Start();
            await m3.Start();

            var peers = m3.GetPeers();

            Assert.True(peers.Count == 2);
            
            Assert.True(peers.Contains("127.0.0.1:6800"));
            Assert.True(peers.Contains("127.0.0.1:6801"));
            
            await m1.Stop();
            await m2.Stop();
            await m3.Stop();
        }
    }
}