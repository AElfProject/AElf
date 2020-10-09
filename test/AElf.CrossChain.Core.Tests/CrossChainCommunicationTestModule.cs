using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Standards.ACS7;
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
            context.Services.AddSingleton<CrossChainCommunicationTestHelper>();

            Configure<CrossChainConfigOptions>(option =>
            {
                option.ParentChainId = ChainHelper.ConvertChainIdToBase58(ChainHelper.GetChainId(1));
            });

            context.Services.AddTransient(provider =>
            {
                var kernelTestHelper = context.Services.GetRequiredServiceLazy<KernelTestHelper>();
                var crossChainCommunicationTestHelper =
                    context.Services.GetRequiredServiceLazy<CrossChainCommunicationTestHelper>().Value;
                var mockBlockChainService = new Mock<IBlockchainService>();
                mockBlockChainService.Setup(m => m.GetChainAsync()).Returns(() =>
                {
                    var chain = new Chain {LastIrreversibleBlockHeight = 10};
                    return Task.FromResult(chain);
                });
                mockBlockChainService.Setup(m =>
                        m.GetBlockHashByHeightAsync(It.IsAny<Chain>(), It.IsAny<long>(), It.IsAny<Hash>()))
                    .Returns<Chain, long, Hash>((chain, height, hash) =>
                        Task.FromResult(crossChainCommunicationTestHelper.GetBlockHashByHeight(height)));
                mockBlockChainService.Setup(m => m.GetBlockByHashAsync(It.IsAny<Hash>())).Returns<Hash>(hash =>
                {
                    if (!crossChainCommunicationTestHelper.TryGetHeightByHash(hash, out var height))
                        return Task.FromResult<Block>(null);
                    var block = kernelTestHelper.Value.GenerateBlock(height - 1,
                        crossChainCommunicationTestHelper.GetBlockHashByHeight(height - 1));
                    return Task.FromResult(block);
                });
                return mockBlockChainService.Object;
            });

            context.Services.AddTransient(provider =>
            {
                var crossChainCommunicationTestHelper =
                    context.Services.GetRequiredServiceLazy<CrossChainCommunicationTestHelper>().Value;
                var mockBlockExtraDataService = new Mock<IBlockExtraDataService>();
                mockBlockExtraDataService
                    .Setup(m => m.GetExtraDataFromBlockHeader(It.IsAny<string>(), It.IsAny<BlockHeader>())).Returns(
                        () =>
                        {
                            var crossExtraData = new CrossChainExtraData
                            {
                                TransactionStatusMerkleTreeRoot = BinaryMerkleTree
                                    .FromLeafNodes(
                                        crossChainCommunicationTestHelper.IndexedSideChainBlockData
                                            .SideChainBlockDataList.Select(scb => scb.TransactionStatusMerkleTreeRoot))
                                    .Root,
                            };
                            return ByteString.CopyFrom(crossExtraData.ToByteArray());
                        });
                return mockBlockExtraDataService.Object;
            });

            context.Services.AddTransient(provider =>
            {
                var crossChainCommunicationTestHelper =
                    context.Services.GetRequiredServiceLazy<CrossChainCommunicationTestHelper>().Value;

                var mockCrossChainIndexingDataService = new Mock<ICrossChainIndexingDataService>();
                var irreversibleBlockStateProvider =
                    context.Services.GetRequiredServiceLazy<IIrreversibleBlockStateProvider>();

                mockCrossChainIndexingDataService.Setup(service => service.GetNonIndexedBlockAsync(It.IsAny<long>()))
                    .Returns<long>(async height => await irreversibleBlockStateProvider.Value
                        .GetNotIndexedIrreversibleBlockByHeightAsync(height));

                mockCrossChainIndexingDataService
                    .Setup(m => m.GetIndexedSideChainBlockDataAsync(It.IsAny<Hash>(), It.IsAny<long>())).Returns(
                        () =>
                        {
                            var indexedSideChainBlockData = crossChainCommunicationTestHelper.IndexedSideChainBlockData;
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
                        crossChainClient = MockCrossChainClient(chainId, isConnected,
                            entity =>
                            {
                                crossChainCommunicationTestHelper.SetClientConnected(-1, true);
                                return true;
                            });
                    }))
                    .Returns<int, ICrossChainClient>((chainId, client) =>
                        crossChainCommunicationTestHelper.TryGetCrossChainClientCreationContext(chainId, out _));

                mockCrossChainClientProvider
                    .Setup(c => c.AddOrUpdateClient(It.IsAny<CrossChainClientCreationContext>()))
                    .Returns<CrossChainClientCreationContext>(
                        crossChainClientCreationContext => CreateAndAddClient(crossChainCommunicationTestHelper, crossChainClientCreationContext));

                mockCrossChainClientProvider.Setup(c =>
                    c.CreateChainInitializationDataClient(It.IsAny<CrossChainClientCreationContext>())).Returns<CrossChainClientCreationContext>(
                    crossChainClientCreationContext => CreateAndAddClient(crossChainCommunicationTestHelper, crossChainClientCreationContext));
                return mockCrossChainClientProvider.Object;
            });

            context.Services.AddSingleton<IConsensusExtraDataProvider, ConsensusExtraDataProvider>();
        }

        delegate void TryGetClientCallback(int chainId, out ICrossChainClient crossChainClient);

        public class MockConsensusExtraDataProvider : IConsensusExtraDataProvider
        {
            public Task<ByteString> GetBlockHeaderExtraDataAsync(BlockHeader blockHeader)
            {
                throw new NotImplementedException();
            }

            public string BlockHeaderExtraDataKey => "Consensus";
        }

        private ICrossChainClient MockCrossChainClient(int remoteChainId, bool isConnected,
            Func<ICrossChainBlockEntity, bool> func)
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
                        CreationHeightOnParentChain = 1,
                    };
                    return Task.FromResult(chainInitialization);
                });
            mockCrossChainClient.Setup(m =>
                    m.RequestCrossChainDataAsync(It.IsAny<long>(),
                        It.IsAny<Func<ICrossChainBlockEntity, bool>>()))
                .Returns(() =>
                {
                    func(new ParentChainBlockData());
                    return Task.CompletedTask;
                });
            mockCrossChainClient.Setup(c => c.ConnectAsync()).Returns(() => Task.CompletedTask);
            return mockCrossChainClient.Object;
        }

        private ICrossChainClient CreateAndAddClient(CrossChainCommunicationTestHelper crossChainCommunicationTestHelper, 
            CrossChainClientCreationContext crossChainClientCreationContext)
        {
            crossChainCommunicationTestHelper.AddNewCrossChainClient(crossChainClientCreationContext);
            bool isConnected =
                crossChainCommunicationTestHelper.CheckClientConnected(crossChainClientCreationContext
                    .RemoteChainId);
            return MockCrossChainClient(crossChainClientCreationContext.RemoteChainId, isConnected,
                entity =>
                {
                    crossChainCommunicationTestHelper.SetClientConnected(-1, true);
                    return true;
                });
        }
    }
}