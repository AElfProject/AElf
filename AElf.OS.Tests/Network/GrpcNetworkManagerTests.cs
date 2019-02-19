using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Account;
using AElf.OS.Network;
using AElf.OS.Network.Events;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Temp;
using AElf.Synchronization.Tests;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;
using Xunit.Abstractions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace AElf.OS.Tests.Network
{
    public class GrpcNetworkManagerTests : OSTestBase
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public GrpcNetworkManagerTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        private (GrpcNetworkServer, IPeerPool) BuildNetManager(NetworkOptions networkOptions, Action<object> eventCallBack = null, List<Block> blockList = null)
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

            var mockBlockService = new Mock<IBlockService>();
            if (blockList != null)
            {
                mockBlockService.Setup(bs => bs.GetBlockAsync(It.IsAny<Hash>()))
                    .Returns<Hash>(h => Task.FromResult(blockList.FirstOrDefault(bl => bl.GetHash() == h)));

                mockBlockService.Setup(bs => bs.GetBlockByHeight(It.IsAny<ulong>()))
                    .Returns<ulong>(h => Task.FromResult(blockList.FirstOrDefault(bl => bl.Height == h)));
            }

            GrpcPeerPool grpcPeerPool = new GrpcPeerPool(optionsMock.Object, NetMockHelpers.MockAccountService().Object);
            GrpcServerService serverService = new GrpcServerService(grpcPeerPool, mockBlockService.Object);
            serverService.EventBus = mockLocalEventBus.Object;

            GrpcNetworkServer netServer = new GrpcNetworkServer(optionsMock.Object, serverService, grpcPeerPool);
            netServer.EventBus = mockLocalEventBus.Object;

            return (netServer, grpcPeerPool);
        }

        /// <summary>
        /// Test: Verify network service request block by hash and hash.
        /// </summary>
        /// <returns></returns>
        [Fact]
        private async Task Request_Block_Test()
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

            var m3 = BuildNetManager(new NetworkOptions
            {
                BootNodes = new List<string> {"127.0.0.1:6801", "127.0.0.1:6800"},
                ListeningPort = 6802
            });

            await m1.Item1.StartAsync();
            await m2.Item1.StartAsync();
            await m3.Item1.StartAsync();

            var service1 = new GrpcNetworkService(m1.Item2);
            var service2 = new GrpcNetworkService(m2.Item2);
            var service3 = new GrpcNetworkService(m3.Item2);

            IBlock b = await service2.GetBlockByHash(genesis.GetHash());
            IBlock bbh = await service3.GetBlockByHeight(genesis.Height);
            IBlock bbh2 = await service3.GetBlockByHeight((ulong)2);

            await m1.Item1.StopAsync();
            await m2.Item1.StopAsync();

            Assert.NotNull(b);
            Assert.NotNull(bbh);
            Assert.Equal(bbh2, null);

            await m3.Item1.StopAsync();
        }

        /// <summary>
        /// Test: Verify other height or hash of blocks with more information.
        /// </summary>
        /// <returns></returns>
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
                TransactionList = { transactionItems.Item1 },
                Transactions = { transactionItems.Item2 }
            };
            var block = new Block()
            {
                Header = header,
                Body = body,
                Height = (ulong)2
            };

            var m1 = BuildNetManager(new NetworkOptions { ListeningPort = 6800 },
                null,
                new List<Block> { (Block) genesis, block });

            var m2 = BuildNetManager(new NetworkOptions
            {
                BootNodes = new List<string> {"127.0.0.1:6800"},
                ListeningPort = 6801
            });

            await m1.Item1.StartAsync();
            await m2.Item1.StartAsync();

            var service1 = new GrpcNetworkService(m1.Item2);
            var service2 = new GrpcNetworkService(m2.Item2);

            var block21 = await service2.GetBlockByHeight(2);
            var block22 = await service2.GetBlockByHash(block.GetHash());

            await m1.Item1.StopAsync();
            await m2.Item1.StopAsync();

            block21.ShouldNotBeNull();
            block21.Height.ShouldBe((ulong)2);
            block22.ShouldNotBeNull();
            block21.ShouldBe(block22);
        }

        /// <summary>
        /// Test: Verify network service of new announcement event.
        /// </summary>
        /// <returns></returns>
        [Fact]
        private async Task Announcement_Event_Test()
        {
            List<AnnoucementReceivedEventData> receivedEventDatas = new List<AnnoucementReceivedEventData>();

            void TransferEventCallbackAction(object eventData)
            {
                // todo use event bus
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

            await m1.Item1.StartAsync();
            await m2.Item1.StartAsync();

            var genesis = (Block) ChainGenerationHelpers.GetGenesisBlock();

            var servicem2 = new GrpcNetworkService(m2.Item2);
            await servicem2.BroadcastAnnounce(genesis.GetHash());

            await m1.Item1.StopAsync();
            await m2.Item1.StopAsync();

            Assert.True(receivedEventDatas.Count == 1);
            Assert.True(receivedEventDatas.First().BlockId == genesis.GetHash());
        }

        /// <summary>
        /// Test: Verify network service of broadcast transaction event.
        /// </summary>
        /// <returns></returns>
        [Fact]
        private async Task Transaction_Event_Test()
        {
            List<TxReceivedEventData> receivedEventDatas = new List<TxReceivedEventData>();

            void TransferEventCallbackAction(object eventData)
            {
                // todo use event bus
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

            await m1.Item1.StartAsync();
            await m2.Item1.StartAsync();

            var genesis = ChainGenerationHelpers.GetGenesisBlock();

            var servicem2 = new GrpcNetworkService(m2.Item2);
            await servicem2.BroadcastTransaction(new Transaction());

            await m1.Item1.StopAsync();
            await m2.Item1.StopAsync();

            Assert.True(receivedEventDatas.Count == 1);
        }

        /// <summary>
        /// Test: Verify network service of event receive without handling.x
        /// </summary>
        /// <returns></returns>
        [Fact]
        private async Task Announcement_Request_Test()
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

            await m1.Item1.StartAsync();
            await m2.Item1.StartAsync();

            var genesis = (Block) ChainGenerationHelpers.GetGenesisBlock();

            var servicem2 = new GrpcNetworkService(m2.Item2);
            await servicem2.BroadcastAnnounce(genesis.GetHash());

            await m1.Item1.StopAsync();
            await m2.Item1.StopAsync();

            Assert.True(receivedEventDatas.Count == 1);
            Assert.True(receivedEventDatas.First().BlockId == genesis.GetHash());
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
                    IncrementId = (ulong)i
                };
                var hash = transaction.GetHash();

                transactionList.Add(transaction);
                hashList.Add(hash);
            }

            return (transactionList, hashList);
        }
    }
}