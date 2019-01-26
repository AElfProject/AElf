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
using AElf.OS.Network.Events;
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
            null, 
            new List<Block> { (Block) genesis });
            
            var m2 = BuildNetManager(new NetworkOptions
            {
                BootNodes = new List<string> {"127.0.0.1:6800"},
                ListeningPort = 6801
            });
            
            await m1.StartAsync();
            await m2.StartAsync();

            IBlock b = await m2.GetBlockByHash(genesis.GetHash());

            await m1.StopAsync();
            await m2.StopAsync();
            
            Assert.NotNull(b);
        }
        
        [Fact]
        private async Task Announcement_EventTest()
        {
            List<AnnoucementReceivedEventData> receivedEventDatas = new List<AnnoucementReceivedEventData>();

            void TransferEventCallbackAction(object eventData)
            {
                try
                {
                    if (eventData is AnnoucementReceivedEventData data)
                    {
                        receivedEventDatas.Add(data);
                    }
                }
                catch (Exception e)
                {
                    _testOutputHelper.WriteLine(e.ToString());
                }
            }

            var m1 = BuildNetManager(new NetworkOptions { ListeningPort = 6800 }, TransferEventCallbackAction);
            
            var m2 = BuildNetManager(new NetworkOptions
            {
                BootNodes = new List<string> {"127.0.0.1:6800"},
                ListeningPort = 6801
            });
            
            await m1.StartAsync();
            await m2.StartAsync();
            
            var genesis = (Block) ChainGenerationHelpers.GetGenesisBlock();

            await m2.BroadcastAnnounce(genesis);
            
            await m1.StopAsync();
            await m2.StopAsync();
            
            Assert.True(receivedEventDatas.Count == 1);
            Assert.True(receivedEventDatas.First().BlockId == genesis.GetHash());
        }
        
        [Fact]
        private async Task Transaction_EventTest()
        {
            List<TxReceivedEventData> receivedEventDatas = new List<TxReceivedEventData>();

            void TransferEventCallbackAction(object eventData)
            {
                try
                {
                    if (eventData is TxReceivedEventData data)
                    {
                        receivedEventDatas.Add(data);
                    }
                }
                catch (Exception e)
                {
                    _testOutputHelper.WriteLine(e.ToString());
                }
            }

            var m1 = BuildNetManager(new NetworkOptions { ListeningPort = 6800 }, TransferEventCallbackAction);
            
            var m2 = BuildNetManager(new NetworkOptions
            {
                BootNodes = new List<string> {"127.0.0.1:6800"},
                ListeningPort = 6801
            });
            
            await m1.StartAsync();
            await m2.StartAsync();
            
            var genesis = ChainGenerationHelpers.GetGenesisBlock();

            await m2.BroadcastTransaction(new Transaction());
            
            await m1.StopAsync();
            await m2.StopAsync();
            
            Assert.True(receivedEventDatas.Count == 1);
            //Assert.True(receivedEventDatas.First().BlockId == genesis.GetHash());
        }
    }
}