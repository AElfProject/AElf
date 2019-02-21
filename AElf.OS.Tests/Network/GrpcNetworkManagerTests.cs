using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Account;
using AElf.Kernel.Services;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.Network;
using AElf.OS.Network.Events;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Temp;
using AElf.Synchronization.Tests;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
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
        private readonly IOptionsSnapshot<ChainOptions> _optionsMock;
        private LoggerFactory _loggerFactory;

        public GrpcNetworkManagerTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            
            var optionsMock = new Mock<IOptionsSnapshot<ChainOptions>>();
            optionsMock.Setup(m => m.Value).Returns(new ChainOptions { ChainId = ChainHelpers.DumpBase58(ChainHelpers.GetRandomChainId()) });
            _optionsMock = optionsMock.Object;
            
            _loggerFactory = new LoggerFactory();
            _loggerFactory.AddProvider(new XunitLoggerProvider(testOutputHelper));
        }
        
        public class XunitLoggerProvider : ILoggerProvider
        {
            private readonly ITestOutputHelper _testOutputHelper;

            public XunitLoggerProvider(ITestOutputHelper testOutputHelper)
            {
                _testOutputHelper = testOutputHelper;
            }

            public ILogger CreateLogger(string categoryName)
                => new XunitLogger(_testOutputHelper, categoryName);

            public void Dispose()
            { }
        }

        public class XunitLogger : ILogger
        {
            private readonly ITestOutputHelper _testOutputHelper;
            private readonly string _categoryName;

            public XunitLogger(ITestOutputHelper testOutputHelper, string categoryName)
            {
                _testOutputHelper = testOutputHelper;
                _categoryName = categoryName;
            }

            public IDisposable BeginScope<TState>(TState state)
                => NoopDisposable.Instance;

            public bool IsEnabled(LogLevel logLevel)
                => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                _testOutputHelper.WriteLine($"{_categoryName} [{eventId}] {formatter(state, exception)}");
                if (exception != null)
                    _testOutputHelper.WriteLine(exception.ToString());
            }

            private class NoopDisposable : IDisposable
            {
                public static NoopDisposable Instance = new NoopDisposable();
                public void Dispose()
                { }
            }
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

            var mockBlockService = new Mock<IFullBlockchainService>();
            if (blockList != null)
            {
                mockBlockService.Setup(bs => bs.GetBlockByHashAsync(It.IsAny<int>(), It.IsAny<Hash>()))
                    .Returns<int, Hash>((chainId, h) => Task.FromResult(blockList.FirstOrDefault(bl => bl.GetHash() == h)));
                
                mockBlockService.Setup(bs => bs.GetBlockByHeightAsync(It.IsAny<int>(), It.IsAny<ulong>()))
                    .Returns<int, ulong>((chainId, h) => Task.FromResult(blockList.FirstOrDefault(bl => bl.Height == h)));
            }
            
            var mockBlockChainService = new Mock<IFullBlockchainService>();
            mockBlockChainService.Setup(m => m.GetBestChainLastBlock(It.IsAny<int>()))
                .Returns(Task.FromResult(new BlockHeader()));

            GrpcPeerPool grpcPeerPool = new GrpcPeerPool(_optionsMock, optionsMock.Object, NetMockHelpers.MockAccountService().Object, mockBlockService.Object);
            grpcPeerPool.Logger = _loggerFactory.CreateLogger<GrpcPeerPool>();
            GrpcServerService serverService = new GrpcServerService(_optionsMock, grpcPeerPool, mockBlockService.Object);
            serverService.EventBus = mockLocalEventBus.Object;
            serverService.Logger = _loggerFactory.CreateLogger<GrpcServerService>();

            GrpcNetworkServer netServer = new GrpcNetworkServer(optionsMock.Object, serverService, grpcPeerPool);
            netServer.EventBus = mockLocalEventBus.Object;
            netServer.Logger = _loggerFactory.CreateLogger<GrpcNetworkServer>();

            return (netServer, grpcPeerPool);
        }

        [Fact]
        private async Task Multi_Connect()
        {
            var r = new List<(GrpcNetworkServer, IPeerPool)>();
            
            for (int i = 1; i <= 3; i++)
            {
                var s = BuildNetManager(new NetworkOptions { ListeningPort = 9800+i });
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

            var m1 = BuildNetManager(new NetworkOptions { ListeningPort = 9800 },
            null,
            new List<Block> { (Block) genesis });

            var m2 = BuildNetManager(new NetworkOptions
            {
                BootNodes = new List<string> {"127.0.0.1:9800"},
                ListeningPort = 9801
            });

            var m3 = BuildNetManager(new NetworkOptions
            {
                BootNodes = new List<string> {"127.0.0.1:9801", "127.0.0.1:9800"},
                ListeningPort = 9802
            });

            await m1.Item1.StartAsync();
            await m2.Item1.StartAsync();
            await m3.Item1.StartAsync();

            var service1 = new GrpcNetworkService(m1.Item2);
            service1.Logger = _loggerFactory.CreateLogger<GrpcNetworkService>();
            
            var service2 = new GrpcNetworkService(m2.Item2);
            service2.Logger = _loggerFactory.CreateLogger<GrpcNetworkService>();
            
            var service3 = new GrpcNetworkService(m3.Item2);
            service3.Logger = _loggerFactory.CreateLogger<GrpcNetworkService>();

            IBlock b = await service2.GetBlockByHashAsync(genesis.GetHash());
            IBlock bbh = await service3.GetBlockByHeightAsync(genesis.Height);
            IBlock bbh2 = await service3.GetBlockByHeightAsync((ulong)2);

            await m1.Item1.StopAsync();
            await m2.Item1.StopAsync();

            Assert.NotNull(b);
            Assert.NotNull(bbh);
            Assert.Equal(bbh2, null);

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

            var block21 = await service2.GetBlockByHeightAsync(2);
            var block22 = await service2.GetBlockByHashAsync(block.GetHash());

            await m1.Item1.StopAsync();
            await m2.Item1.StopAsync();

            block21.ShouldNotBeNull();
            block21.Height.ShouldBe((ulong)2);
            block22.ShouldNotBeNull();
            block21.ShouldBe(block22);
        }

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
            await servicem2.BroadcastAnnounceAsync(genesis.Header);
            
            await m1.Item1.StopAsync();
            await m2.Item1.StopAsync();

            Assert.True(receivedEventDatas.Count == 1);
            Assert.True(receivedEventDatas.First().Header.GetHash() == genesis.GetHash());
        }

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
            await servicem2.BroadcastTransactionAsync(new Transaction());

            await m1.Item1.StopAsync();
            await m2.Item1.StopAsync();

            Assert.True(receivedEventDatas.Count == 1);
        }

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
            await servicem2.BroadcastAnnounceAsync(genesis.Header);
            
            await m1.Item1.StopAsync();
            await m2.Item1.StopAsync();

            Assert.True(receivedEventDatas.Count == 1);
            Assert.True(receivedEventDatas.First().Header.GetHash() == genesis.GetHash());
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