using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Configuration;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.OS.Network;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Temp;
using AElf.Synchronization.Tests;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp.EventBus.Local;
using Xunit;
using Xunit.Abstractions;

namespace AElf.OS.Tests.Network
{
    public class GrpcNetworkManagerTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public GrpcNetworkManagerTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        private GrpcNetworkManager BuildNetManager(NetworkOptions networkOptions, Action<object> eventCallBack = null, List<Block> blockList = null)
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

            var mockBlockService = new Mock<IBlockService>();
            if (blockList != null)
            {
                mockBlockService.Setup(bs => bs.GetBlockAsync(It.IsAny<Hash>()))
                    .Returns<Hash>(h => Task.FromResult(blockList.FirstOrDefault(bl => bl.GetHash() == h)));
            }
            
            GrpcNetworkManager manager1 = new GrpcNetworkManager(mock.Object, accountService.Object, mockBlockService.Object, mockLocalEventBus.Object);

            return manager1;
        }

        [Fact]
        private async Task RequestBlockTest()
        {
            var genesis = ChainGenerationHelpers.GetGenesisBlock();

            var m1 = BuildNetManager(new NetworkOptions { ListeningPort = 6800 },
            (object o) =>
            {
                _testOutputHelper.WriteLine("Announced");
            }, 
            new List<Block> { (Block) genesis });
            
            var m2 = BuildNetManager(new NetworkOptions
            {
                BootNodes = new List<string> {"127.0.0.1:6800"},
                ListeningPort = 6801
            });
            
            await m1.Start();
            await m2.Start();

            IBlock b = await m2.GetBlockByHash(genesis.GetHash());

            Assert.NotNull(b);
        }
        
        [Fact]
        private async Task EventTest()
        {
            var m1 = BuildNetManager(new NetworkOptions {
                ListeningPort = 6800 
            }, (object o) =>
            {
                _testOutputHelper.WriteLine("YES");
            });
            
            var m2 = BuildNetManager(new NetworkOptions
            {
                BootNodes = new List<string> {"127.0.0.1:6800"},
                ListeningPort = 6801
            });
            
            await m1.Start();
            await m2.Start();
            
            var genesis = ChainGenerationHelpers.GetGenesisBlock();

            await m2.BroadcastAnnounce(genesis);
            
            await m1.Stop();
            await m2.Stop();
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