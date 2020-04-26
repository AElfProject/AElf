using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Communication.Infrastructure;
using AElf.CrossChain.Indexing.Application;
using AElf.CrossChain.Indexing.Infrastructure;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.SmartContract;
using AElf.Modularity;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.CrossChain
{
    [DependsOn(typeof(CrossChainCoreModule),
        typeof(SmartContractAElfModule),
        typeof(KernelCoreTestAElfModule))]
    public class CrossChainCommunicationTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            base.ConfigureServices(context);

            var dictionary = new Dictionary<long, Hash>
            {
                {1, HashHelper.ComputeFrom("1")},
                {2, HashHelper.ComputeFrom("2")},
                {3, HashHelper.ComputeFrom("3")}
            };

            Configure<CrossChainConfigOptions>(option =>
            {
                option.ParentChainId = ChainHelper.ConvertChainIdToBase58(ChainHelper.GetChainId(1));
            });

            context.Services.AddTransient(provider =>
            {
                var kernelTestHelper = context.Services.GetRequiredServiceLazy<KernelTestHelper>();
                var mockBlockChainService = new Mock<IBlockchainService>();
                mockBlockChainService.Setup(m => m.GetChainAsync()).Returns(() =>
                {
                    var chain = new Chain {LastIrreversibleBlockHeight = 10};
                    return Task.FromResult(chain);
                });
                mockBlockChainService.Setup(m =>
                        m.GetBlockHashByHeightAsync(It.IsAny<Chain>(), It.IsAny<long>(), It.IsAny<Hash>()))
                    .Returns<Chain, long, Hash>((chain, height, hash) =>
                    {
                        if (height > 0 && height <= 3)
                            return Task.FromResult(dictionary[height]);
                        return Task.FromResult<Hash>(null);
                    });
                mockBlockChainService.Setup(m => m.GetBlockByHashAsync(It.IsAny<Hash>())).Returns<Hash>(hash =>
                {
                    foreach (var kv in dictionary)
                    {
                        if (kv.Value.Equals(hash))
                        {
                            var block = kernelTestHelper.Value.GenerateBlock(kv.Key - 1, dictionary[kv.Key - 1]);
                            return Task.FromResult(block);
                        }
                    }

                    return Task.FromResult<Block>(null);
                });
                return mockBlockChainService.Object;
            });

            context.Services.AddTransient(provider =>
            {
                var mockBlockExtraDataService = new Mock<IBlockExtraDataService>();
                mockBlockExtraDataService
                    .Setup(m => m.GetExtraDataFromBlockHeader(It.IsAny<string>(), It.IsAny<BlockHeader>())).Returns(
                        () =>
                        {
                            var crossExtraData = new CrossChainExtraData()
                            {
                                TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom("SideChainBlockHeadersRoot"),
                            };
                            return ByteString.CopyFrom(crossExtraData.ToByteArray());
                        });
                return mockBlockExtraDataService.Object;
            });

            context.Services.AddSingleton<CrossChainCommunicationTestHelper>();
            context.Services.AddTransient(provider =>
            {
                var mockCrossChainIndexingDataService = new Mock<ICrossChainIndexingDataService>();
                var irreversibleBlockStateProvider =
                    context.Services.GetRequiredServiceLazy<IIrreversibleBlockStateProvider>();

                mockCrossChainIndexingDataService.Setup(service => service.GetNonIndexedBlockAsync(It.IsAny<long>()))
                    .Returns<long>(async height => await irreversibleBlockStateProvider.Value
                        .GetNotIndexedIrreversibleBlockByHeightAsync(height));
                mockCrossChainIndexingDataService
                    .Setup(m => m.GetIndexedCrossChainBlockDataAsync(It.IsAny<Hash>(), It.IsAny<long>()))
                    .Returns(() =>
                    {
                        var crossChainBlockData = new CrossChainBlockData
                        {
                            SideChainBlockDataList =
                            {
                                new SideChainBlockData
                                {
                                    ChainId = 123, Height = 1,
                                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom("fakeTransactionMerkleTree")
                                }
                            }
                        };
                        return Task.FromResult(crossChainBlockData);
                    });
                mockCrossChainIndexingDataService
                    .Setup(m => m.GetIndexedSideChainBlockDataAsync(It.IsAny<Hash>(), It.IsAny<long>())).Returns(
                        () =>
                        {
                            var indexedSideChainBlockData = new IndexedSideChainBlockData
                            {
                                SideChainBlockDataList =
                                {
                                    new SideChainBlockData
                                    {
                                        ChainId = 123, Height = 1,
                                        TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom("fakeTransactionMerkleTree")
                                    }
                                }
                            };
                            return Task.FromResult(indexedSideChainBlockData);
                        });
                return mockCrossChainIndexingDataService.Object;
            });

            context.Services.AddTransient(provider =>
            {
                var crossChainCommunicationTestHelper =
                    context.Services.GetRequiredServiceLazy<CrossChainCommunicationTestHelper>().Value;
                var mockCrossChainClientProvider = new Mock<ICrossChainClientProvider>();
                ICrossChainClient client;
                mockCrossChainClientProvider
                    .Setup(m => m.TryGetClient(It.IsAny<int>(), out client))
                    .Callback(new TryGetClientCallback((int chainId, out ICrossChainClient crossChainClient) =>
                    {
                        if (!crossChainCommunicationTestHelper.TryGetCrossChainClientCreationContext(chainId, out _))
                        {
                            crossChainClient = null;
                            return;
                        }

                        bool isConnected = crossChainCommunicationTestHelper.CheckClientConnected(chainId);
                        crossChainClient = MockCrossChainClient(chainId, isConnected);
                    }))
                    .Returns<int, ICrossChainClient>((chainId, client) =>
                        crossChainCommunicationTestHelper.TryGetCrossChainClientCreationContext(chainId, out _));

                mockCrossChainClientProvider
                    .Setup(c => c.AddOrUpdateClient(It.IsAny<CrossChainClientCreationContext>()))
                    .Returns<CrossChainClientCreationContext>(
                        crossChainClientCreationContext =>
                        {
                            crossChainCommunicationTestHelper.AddNewCrossChainClient(crossChainClientCreationContext);
                            bool isConnected =
                                crossChainCommunicationTestHelper.CheckClientConnected(crossChainClientCreationContext
                                    .RemoteChainId);
                            return MockCrossChainClient(crossChainClientCreationContext.RemoteChainId, isConnected);
                        });
                return mockCrossChainClientProvider.Object;
            });

            context.Services.AddSingleton<IConsensusExtraDataKeyProvider, MockConsensusExtraDataProvider>();
        }

        delegate void TryGetClientCallback(int chainId, out ICrossChainClient crossChainClient);

        public class MockConsensusExtraDataProvider : IConsensusExtraDataKeyProvider
        {
            public Task<ByteString> GetBlockHeaderExtraDataAsync(BlockHeader blockHeader)
            {
                throw new NotImplementedException();
            }

            public string BlockHeaderExtraDataKey => "Consensus";
        }

        private ICrossChainClient MockCrossChainClient(int remoteChainId, bool isConnected)
        {
            var mockCrossChainClient = new Mock<ICrossChainClient>();
            mockCrossChainClient.Setup(c => c.RemoteChainId)
                .Returns(() => remoteChainId);
            mockCrossChainClient.Setup(c => c.IsConnected)
                .Returns(() => isConnected);
            mockCrossChainClient.Setup(m => m.RequestChainInitializationDataAsync(It.IsAny<int>())).Returns(
                () =>
                {
                    var chainInitialization = new ChainInitializationData
                    {
                        CreationHeightOnParentChain = 1
                    };
                    return Task.FromResult(chainInitialization);
                });
            mockCrossChainClient.Setup(m =>
                    m.RequestCrossChainDataAsync(It.IsAny<long>(),
                        It.IsAny<Func<ICrossChainBlockEntity, bool>>()))
                .Returns(() =>
                {
                    var chainInitialization = new ChainInitializationData
                    {
                        CreationHeightOnParentChain = 1
                    };
                    return Task.FromResult(chainInitialization);
                });
            mockCrossChainClient.Setup(c => c.ConnectAsync()).Returns(() => Task.CompletedTask);
            return mockCrossChainClient.Object;
        }
    }
}