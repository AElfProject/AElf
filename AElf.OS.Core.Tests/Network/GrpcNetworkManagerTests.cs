using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS.Network.Application;
using AElf.OS.Network.Events;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using AElf.Synchronization.Tests;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;
using Xunit.Abstractions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace AElf.OS.Network
{
    public class GrpcNetworkManagerTests : OSCoreTestBase
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly IOptionsSnapshot<ChainOptions> _optionsMock;

        public GrpcNetworkManagerTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;

            var optionsMock = new Mock<IOptionsSnapshot<ChainOptions>>();
            optionsMock.Setup(m => m.Value).Returns(new ChainOptions {ChainId = ChainHelpers.GetRandomChainId()});
            _optionsMock = optionsMock.Object;
        }

        private (GrpcNetworkServer, IPeerPool) BuildNetManager(NetworkOptions networkOptions,
            Action<object> eventCallBack = null, List<Block> blockList = null)
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

            var mockBlockService = new Mock<IFullBlockchainService>();
            if (blockList != null)
            {
                mockBlockService.Setup(bs => bs.GetBlockByHashAsync(It.IsAny<Hash>()))
                    .Returns<Hash>((h) => Task.FromResult(blockList.FirstOrDefault(bl => bl.GetHash() == h)));

                mockBlockService.Setup(bs => bs.GetBlockByHeightAsync(It.IsAny<long>()))
                    .Returns<long>((h) => Task.FromResult(blockList.FirstOrDefault(bl => bl.Height == h)));
            }

            var mockBlockChainService = new Mock<IFullBlockchainService>();
            mockBlockChainService.Setup(m => m.GetBestChainLastBlock())
                .Returns(Task.FromResult(new BlockHeader()));

            GrpcPeerPool grpcPeerPool = new GrpcPeerPool(optionsMock.Object,
                NetMockHelpers.MockAccountService().Object, mockBlockService.Object);
            GrpcServerService serverService =
                new GrpcServerService(grpcPeerPool, mockBlockService.Object);
            serverService.EventBus = mockLocalEventBus.Object;

            GrpcNetworkServer netServer = new GrpcNetworkServer(optionsMock.Object, serverService, grpcPeerPool);
            netServer.EventBus = mockLocalEventBus.Object;

            return (netServer, grpcPeerPool);
        }

        [Fact]
        private async Task Multi_Connect()
        {
            var r = new List<(GrpcNetworkServer, IPeerPool)>();

            for (int i = 1; i <= 3; i++)
            {
                var s = BuildNetManager(new NetworkOptions {ListeningPort = 9800 + i});
                r.Add(s);
                await s.Item1.StartAsync();
            }

            var m3 = BuildNetManager(new NetworkOptions
            {
                BootNodes = new List<string> {"127.0.0.1:9801", "127.0.0.1:9802", "127.0.0.1:9803"},
                ListeningPort = 9800
            });

            await m3.Item1.StartAsync();

            var peer = m3.Item2.GetPeers();

            foreach (var server in r.Select(m => m.Item1))
            {
                await server.StopAsync();
            }

            await m3.Item1.StopAsync();

            Assert.Equal(3, peer.Count);
        }

        [Fact]
        private async Task Request_Block_Test()
        {
            var genesis = ChainGenerationHelpers.GetGenesisBlock();

            var m1 = BuildNetManager(new NetworkOptions {ListeningPort = 6800},
                null,
                new List<Block> {genesis});

            var m2 = BuildNetManager(new NetworkOptions
            {
                BootNodes = new List<string> {"127.0.0.1:6800"},
                ListeningPort = 6801
            });

            var m3 = BuildNetManager(new NetworkOptions
            {
                BootNodes = new List<string> {"127.0.0.1:6801", "127.0.0.1:6800"},
                ListeningPort = 6802
            });

            await m1.Item1.StartAsync();
            await m2.Item1.StartAsync();
            await m3.Item1.StartAsync();

            var service2 = new NetworkService(m2.Item2);

            IBlock b = await service2.GetBlockByHashAsync(genesis.GetHash());

            await m1.Item1.StopAsync();
            await m2.Item1.StopAsync();

            Assert.NotNull(b);

            await m3.Item1.StopAsync();
        }

        [Fact]
        private async Task Request_Block_With_MoreData_Test()
        {
            var genesis = ChainGenerationHelpers.GetGenesisBlock();
            var header = new BlockHeader()
            {
                PreviousBlockHash = genesis.GetHash(),
                Height = 2
            };
            var transactionItems = GenerateTransactionListInfo(10);
            var body = new BlockBody()
            {
                BlockHeader = header.GetHash(),
                TransactionList = {transactionItems.Item1},
                Transactions = {transactionItems.Item2}
            };
            var block = new Block()
            {
                Header = header,
                Body = body,
                Height =  2
            };

            var m1 = BuildNetManager(new NetworkOptions {ListeningPort = 6800},
                null,
                new List<Block> {genesis, block});

            var m2 = BuildNetManager(new NetworkOptions
            {
                BootNodes = new List<string> {"127.0.0.1:6800"},
                ListeningPort = 6801
            });

            await m1.Item1.StartAsync();
            await m2.Item1.StartAsync();
            
            var service2 = new NetworkService(m2.Item2);

            var block22 = await service2.GetBlockByHashAsync(block.GetHash());

            await m1.Item1.StopAsync();
            await m2.Item1.StopAsync();

            block22.ShouldNotBeNull();
        }

        [Fact]
        private async Task Announcement_Event_Test()
        {
            List<AnnouncementReceivedEventData> receivedEventDatas = new List<AnnouncementReceivedEventData>();

            void TransferEventCallbackAction(object eventData)
            {
                // todo use event bus
                try
                {
                    if (eventData is AnnouncementReceivedEventData data)
                    {
                        receivedEventDatas.Add(data);
                    }
                }
                catch (Exception e)
                {
                    _testOutputHelper.WriteLine(e.ToString());
                }
            }

            var m1 = BuildNetManager(new NetworkOptions {ListeningPort = 6800}, TransferEventCallbackAction);

            var m2 = BuildNetManager(new NetworkOptions
            {
                BootNodes = new List<string> {"127.0.0.1:6800"},
                ListeningPort = 6801
            });

            await m1.Item1.StartAsync();
            await m2.Item1.StartAsync();

            var genesis = ChainGenerationHelpers.GetGenesisBlock();

            var servicem2 = new NetworkService(m2.Item2);
            await servicem2.BroadcastAnnounceAsync(genesis.Header);

            await m1.Item1.StopAsync();
            await m2.Item1.StopAsync();

            Assert.True(receivedEventDatas.Count == 1);
            Assert.True(receivedEventDatas.First().Announce.BlockHash == genesis.GetHash());
        }

        [Fact]
        private async Task Transaction_Event_Test()
        {
            List<TransactionsReceivedEvent> receivedEventDatas = new List<TransactionsReceivedEvent>();

            void TransferEventCallbackAction(object eventData)
            {
                // todo use event bus
                try
                {
                    if (eventData is TransactionsReceivedEvent data)
                    {
                        receivedEventDatas.Add(data);
                    }
                }
                catch (Exception e)
                {
                    _testOutputHelper.WriteLine(e.ToString());
                }
            }

            var m1 = BuildNetManager(new NetworkOptions {ListeningPort = 6800}, TransferEventCallbackAction);

            var m2 = BuildNetManager(new NetworkOptions
            {
                BootNodes = new List<string> {"127.0.0.1:6800"},
                ListeningPort = 6801
            });

            await m1.Item1.StartAsync();
            await m2.Item1.StartAsync();

            var servicem2 = new NetworkService(m2.Item2);
            await servicem2.BroadcastTransactionAsync(new Transaction());

            await m1.Item1.StopAsync();
            await m2.Item1.StopAsync();

            Assert.True(receivedEventDatas.Count == 1);
        }

        [Fact]
        private async Task Announcement_Request_Test()
        {
            List<AnnouncementReceivedEventData> receivedEventDatas = new List<AnnouncementReceivedEventData>();

            void TransferEventCallbackAction(object eventData)
            {
                try
                {
                    if (eventData is AnnouncementReceivedEventData data)
                    {
                        receivedEventDatas.Add(data);
                    }
                }
                catch (Exception e)
                {
                    _testOutputHelper.WriteLine(e.ToString());
                }
            }

            var m1 = BuildNetManager(new NetworkOptions {ListeningPort = 6800}, TransferEventCallbackAction);

            var m2 = BuildNetManager(new NetworkOptions
            {
                BootNodes = new List<string> {"127.0.0.1:6800"},
                ListeningPort = 6801
            });

            await m1.Item1.StartAsync();
            await m2.Item1.StartAsync();

            var genesis = ChainGenerationHelpers.GetGenesisBlock();

            var servicem2 = new NetworkService(m2.Item2);
            await servicem2.BroadcastAnnounceAsync(genesis.Header);

            await m1.Item1.StopAsync();
            await m2.Item1.StopAsync();

            Assert.True(receivedEventDatas.Count == 1);
            Assert.True(receivedEventDatas.First().Announce.BlockHash == genesis.GetHash());
        }

        private (List<Transaction>, List<Hash>) GenerateTransactionListInfo(int count)
        {
            var transactionList = new List<Transaction>();
            var hashList = new List<Hash>();

            for (int i = 0; i < count; i++)
            {
                var transaction = new Transaction()
                {
                    From = Address.Generate(),
                    To = Address.Generate(),
                    MethodName = $"Test{i}",
                    Params = ByteString.CopyFromUtf8($"Test{i}"),
                    IncrementId = (ulong) i
                };
                var hash = transaction.GetHash();

                transactionList.Add(transaction);
                hashList.Add(hash);
            }

            return (transactionList, hashList);
        }
    }
}