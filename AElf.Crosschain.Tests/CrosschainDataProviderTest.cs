using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Crosschain.EventMessage;
using AElf.Crosschain.Grpc.Client;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using Moq;
using Volo.Abp.EventBus.Local;
using Xunit;
using Xunit.Abstractions;

namespace AElf.Crosschain
{
    public class CrosschainDataProviderTest : CrosschainTestBase
    {
        private readonly ITestOutputHelper _testOutputHelper;

        private ICrossChainDataProducer FakeCLientBase(int chainId)
        {
            return new CrossChainDataProducer
            {
                BlockInfoCache = new BlockInfoCache()
            };
        }

        public CrosschainDataProviderTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        private ICrossChainDataProvider NewCrossChainDataProvider()
        {
            var mockContractReader = CreateFakeCrossChainContractReader();
            var crossChainDataProvider = new CrossChainDataProvider(mockContractReader);
            //crossChainDataProvider.LocalEventBus = _localEventBus;
            LocalEventBus.Subscribe<NewSideChainEvent>(
                e => crossChainDataProvider.HandleEventAsync(e));
            LocalEventBus.Subscribe<NewParentChainEvent>(e => crossChainDataProvider.HandleEventAsync(e));
            return crossChainDataProvider;
        }

        private ICrossChainContractReader CreateFakeCrossChainContractReader()
        {
            Mock<ICrossChainContractReader> mockObject = new Mock<ICrossChainContractReader>();
            mockObject.Setup(m => m.GetSideChainCurrentHeightAsync(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.FromResult<ulong>(1u));
            mockObject.Setup(m => m.GetParentChainCurrentHeightAsync(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.FromResult<ulong>(1u));
            return mockObject.Object;
        }

        private ICrossChainDataConsumer CreateFakeConsumer(int chainId, BlockInfoCache blockInfoCache)
        {
            return new CrossChainDataConsumer
            {
                BlockInfoCache = blockInfoCache,
                ChainId = chainId
            };
        }

        private ICrossChainDataProducer CreateFakeProducer(int chainId, BlockInfoCache blockInfoCache)
        {
            var mockObject = new Mock<ICrossChainDataProducer>();
            mockObject.Setup(m => m.ChainId).Returns(chainId);
            mockObject.Setup(m => m.BlockInfoCache).Returns(blockInfoCache);
            return mockObject.Object;
        }
        
        private ICrossChainDataProducerConsumerService CreateFakeClientService()
        {
            Mock<ICrossChainDataProducerConsumerService> mockObject = new Mock<ICrossChainDataProducerConsumerService>();
            return mockObject.Object;
        }

        private async Task PublishFakeSideChainConnectedEvent(int chainId, BlockInfoCache blockInfoCache)
        {
            var consumer = CreateFakeConsumer(chainId, blockInfoCache);
            var producer = CreateFakeProducer(chainId, blockInfoCache);
            await LocalEventBus.PublishAsync(new NewSideChainEvent
            {
                CrossChainDataConsumer = consumer,
                CrossChainDataProducer = producer,
                ChainId = chainId
            });
        }
        
        private async Task PublishFakeParentChainConnectedEvent(int chainId, BlockInfoCache blockInfoCache)
        {
            var consumer = CreateFakeConsumer(chainId, blockInfoCache);
            var producer = CreateFakeProducer(chainId, blockInfoCache);
            await LocalEventBus.PublishAsync(new NewParentChainEvent
            {
                CrossChainDataConsumer = consumer,
                CrossChainDataProducer = producer,
                ChainId = chainId
            });
        }
        
        
        [Fact]
        public async Task Arrange_NewSideChainConnectionEvent()
        {
            int chainId = 123;
            var blockInfoCache = new BlockInfoCache();
            var crossChainDataProvider = NewCrossChainDataProvider();
            await PublishFakeSideChainConnectedEvent(chainId, blockInfoCache);
            Assert.True(crossChainDataProvider.GetCachedChainCount() == 1);
        }

        #region Side chain

        [Fact]
        public async Task GetSideChainBlock_WithoutCache()
        {
            int chainId = 123;
            var blockInfoCache = new BlockInfoCache();
            var crossChainDataProvider = NewCrossChainDataProvider();
            await PublishFakeSideChainConnectedEvent(chainId, blockInfoCache);
            var list = new List<SideChainBlockData>();
            var res = await crossChainDataProvider.GetSideChainBlockDataAsync(chainId, list);
            Assert.False(res);
            Assert.True(list.Count == 0);
        }
        
        
        [Fact]
        public async Task GetSideChainBlock_WithoutEnoughCache()
        {
            int chainId = 123;
            var blockInfoCache = new BlockInfoCache();
            var crossChainDataProvider = NewCrossChainDataProvider();
            await PublishFakeSideChainConnectedEvent(chainId, blockInfoCache);
            var list = new List<SideChainBlockData>();
            var res = await crossChainDataProvider.GetSideChainBlockDataAsync(chainId, list);
            Assert.False(res);
            Assert.True(list.Count == 0);
        }
        
        [Fact]
        public async Task GetSideChainBlock_WithEnoughCache()
        {
            int chainId = 123;
            var blockInfoCache = new BlockInfoCache();
            var crossChainDataProvider = NewCrossChainDataProvider();
            await PublishFakeSideChainConnectedEvent(chainId, blockInfoCache);

            for (int i = 0; i <= CrossChainConsts.MinimalBlockInfoCacheThreshold; i++)
            {
                blockInfoCache.TryAdd(new SideChainBlockData
                {
                    Height = (ulong) (1 + i)
                });
            }
            
            var list = new List<SideChainBlockData>();
            var res = await crossChainDataProvider.GetSideChainBlockDataAsync(chainId, list);
            Assert.True(res);
            Assert.True(list.Count == 1);
        }
        
        [Fact]
        public async Task Validate_Without_ProvidedSideChainBlockData()
        {
            int chainId = 123;
            var blockInfoCache = new BlockInfoCache();
            var crossChainDataProvider = NewCrossChainDataProvider();
            await PublishFakeSideChainConnectedEvent(chainId, blockInfoCache);
            
            blockInfoCache.TryAdd(new SideChainBlockData
            {
                ChainId = chainId,
                Height = 1
            });
            
            var list = new List<SideChainBlockData>();
            var res = await crossChainDataProvider.GetSideChainBlockDataAsync(chainId, list, true);
            Assert.True(res);
        }
        
        
        [Fact]
        public async Task ValidateSideChainBlock_WithCaching()
        {
            int chainId = 123;
            var blockInfoCache = new BlockInfoCache();
            var crossChainDataProvider = NewCrossChainDataProvider();
            await PublishFakeSideChainConnectedEvent(chainId, blockInfoCache);
            
            blockInfoCache.TryAdd(new SideChainBlockData
            {
                ChainId = chainId,
                Height = 1
            });
            
            var list = new List<SideChainBlockData>{new SideChainBlockData
            {
                ChainId = chainId,
                Height = 1
            }};
            var res = await crossChainDataProvider.GetSideChainBlockDataAsync(chainId, list, true);
            Assert.True(res);
            Assert.True(list.Count == 1);
        }
        
        [Fact]
        public async Task ValidateSideChainBlock_WithoutCaching()
        {
            int chainId = 123;
            var crossChainDataProvider = NewCrossChainDataProvider();
            
            var list = new List<SideChainBlockData>{new SideChainBlockData
            {
                ChainId = 123,
                Height = 1
            }};
            var res = await crossChainDataProvider.GetSideChainBlockDataAsync(chainId, list, true);
            Assert.False(res);
        }
        
        [Fact]
        public async Task ValidateSideChainBlock_WithWrongBlockIndex()
        {
            int chainId = 123;
            var blockInfoCache = new BlockInfoCache();
            var crossChainDataProvider = NewCrossChainDataProvider();
            await PublishFakeSideChainConnectedEvent(chainId, blockInfoCache);
            
            blockInfoCache.TryAdd(new SideChainBlockData
            {
                ChainId = 123,
                Height = 1
            });
            
            var list = new List<SideChainBlockData>{new SideChainBlockData
            {
                ChainId = 123,
                Height = 2
            }};
            var res = await crossChainDataProvider.GetSideChainBlockDataAsync(chainId, list, true);
            Assert.False(res);
            Assert.True(list.Count == 1);
        }
        
        [Fact]
        public async Task ValidateSideChainBlock__NotEnoughCaching()
        {
            int chainId = 123;
            var blockInfoCache = new BlockInfoCache();
            var crossChainDataProvider = NewCrossChainDataProvider();
            await PublishFakeSideChainConnectedEvent(chainId, blockInfoCache);
            
            var list = new List<SideChainBlockData>{new SideChainBlockData
            {
                ChainId = 123,
                Height = 1
            }};
            var res = await crossChainDataProvider.GetSideChainBlockDataAsync(chainId, list, true);
            Assert.False(res);
            Assert.True(list.Count == 1);
        }

        [Fact]
        public async Task TryTwice_GetSideChainBlockData()
        {
            int chainId = 123;
            var blockInfoCache = new BlockInfoCache();
            var crossChainDataProvider = NewCrossChainDataProvider();
            await PublishFakeSideChainConnectedEvent(chainId, blockInfoCache);

            for (int i = 0; i <= CrossChainConsts.MinimalBlockInfoCacheThreshold; i++)
            {
                blockInfoCache.TryAdd(new SideChainBlockData
                {
                    Height = (ulong) (1 + i)
                });
            }
            
            var list = new List<SideChainBlockData>();
            await crossChainDataProvider.GetSideChainBlockDataAsync(chainId, list);
            list = new List<SideChainBlockData>();
            var res = await crossChainDataProvider.GetSideChainBlockDataAsync(chainId, list);
            Assert.True(res);
            Assert.True(list.Count == 1);
        }
        
        #endregion

        
        #region Parent chain

        [Fact]
        public async Task Arrange_NullParentChain()
        {
            int chainId = 123;
            var crossChainDataProvider = NewCrossChainDataProvider();

//            var mockClientBase = CreateFakeConsumer(chainId);
//            await LocalEventBus.PublishAsync(new NewParentChainEvent
//            {
//                ClientBase = mockClientBase
//            });

            var list = new List<ParentChainBlockData>();
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list);
            Assert.False(res);
        }

        [Fact]
        public async Task GetParentChainBLock_WithCache()
        {
            int chainId = 123;
            var blockInfoCache = new BlockInfoCache();
            var crossChainDataProvider = NewCrossChainDataProvider();
            await PublishFakeParentChainConnectedEvent(chainId, blockInfoCache);

            var list = new List<ParentChainBlockData>();
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list);
            Assert.True(res);
            Assert.True(list.Count == 0);
        }

        
        [Fact]
        public async Task ValidateParentChainBlock_WithoutProvidedData()
        {
            int chainId = 123;
            var blockInfoCache = new BlockInfoCache();
            var crossChainDataProvider = NewCrossChainDataProvider();
            await PublishFakeParentChainConnectedEvent(chainId, blockInfoCache);

            var list = new List<ParentChainBlockData>();
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list, true);
            Assert.True(res);
        }
        
        [Fact]
        public async Task ValidateParentChainBlock_WithTooManyProvidedData()
        {
            int chainId = 123;
            var blockInfoCache = new BlockInfoCache();
            var crossChainDataProvider = NewCrossChainDataProvider();
            await PublishFakeParentChainConnectedEvent(chainId, blockInfoCache);

            var list = new List<ParentChainBlockData>();
            for (int i = 0; i <= CrossChainConsts.MaximalCountForIndexingParentChainBlock; i++)
            {
                list.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ChainId = chainId,
                        Height = (ulong) (i +1)
                    }
                });
            }
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list, true);
            Assert.False(res);
        }

        [Fact]
        public async Task ValidateParentChainBlock__WithWrongIndex()
        {
            int chainId = 123;
            var blockInfoCache = new BlockInfoCache();
            var crossChainDataProvider = NewCrossChainDataProvider();
            await PublishFakeParentChainConnectedEvent(chainId, blockInfoCache);

            var cachingCount = 5;
            for (int i = 0; i < cachingCount; i++)
            {
                blockInfoCache.TryAdd(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ChainId = chainId,
                        Height = (ulong) (i +1)
                    }
                });
            }

            var list = new List<ParentChainBlockData>
            {
                new ParentChainBlockData {Root = new ParentChainBlockRootInfo {ChainId = chainId, Height = 2}}
            };
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list, true);
            Assert.False(res);
        }
        
        [Fact]
        public async Task GetParentChainBlock_WithProvidedData()
        {
            int chainId = 123;
            var blockInfoCache = new BlockInfoCache();
            var crossChainDataProvider = NewCrossChainDataProvider();
            await PublishFakeParentChainConnectedEvent(chainId, blockInfoCache);


            var list = new List<ParentChainBlockData>
            {
                new ParentChainBlockData {Root = new ParentChainBlockRootInfo {ChainId = chainId, Height = 1}}
            };
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list);
            Assert.False(res);
        }
        
        [Fact]
        public async Task GetSingleParentBlock_Single()
        {
            int chainId = 123;
            var blockInfoCache = new BlockInfoCache();
            var crossChainDataProvider = NewCrossChainDataProvider();
            await PublishFakeParentChainConnectedEvent(chainId, blockInfoCache);

            var cachingCount = 5;
            for (int i = 0; i < cachingCount; i++)
            {
                blockInfoCache.TryAdd(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ChainId = chainId,
                        Height = (ulong) (i +1)
                    }
                });
            }

            var list = new List<ParentChainBlockData>();
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list);
            Assert.True(res);
            var expectedResultCount = cachingCount - CrossChainConsts.MinimalBlockInfoCacheThreshold;
            Assert.True(list.Count == expectedResultCount);
        }

        [Fact]
        public async Task GetParentChainBlock_Success()
        {
            int chainId = 123;
            var blockInfoCache = new BlockInfoCache();
            var crossChainDataProvider = NewCrossChainDataProvider();
            await PublishFakeParentChainConnectedEvent(chainId, blockInfoCache);

            var cachingCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock;
            for (int i = 0; i < cachingCount; i++)
            {
                blockInfoCache.TryAdd(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ChainId = chainId,
                        Height = (ulong) (i +1)
                    }
                });
            }

            var list = new List<ParentChainBlockData>();
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list);
            Assert.True(res);
            var expectedResultCount = cachingCount - CrossChainConsts.MinimalBlockInfoCacheThreshold;
            Assert.True(list.Count == expectedResultCount);
        }
        
        [Fact]
        public async Task GetParentChainBlock_WithCountLimit()
        {
            int chainId = 123;
            var blockInfoCache = new BlockInfoCache();
            var crossChainDataProvider = NewCrossChainDataProvider();
            await PublishFakeParentChainConnectedEvent(chainId, blockInfoCache);

            var cachingCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock;
            for (int i = 0; i <= cachingCount + CrossChainConsts.MinimalBlockInfoCacheThreshold; i++)
            {
                blockInfoCache.TryAdd(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ChainId = chainId,
                        Height = (ulong) (i +1)
                    }
                });
            }

            var list = new List<ParentChainBlockData>();
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list);
            Assert.True(res);
            var expectedResultCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock;
            Assert.True(list.Count == expectedResultCount);
        }

        [Fact]
        public async Task TryTwice_GetParentChainBlock_WithCountLimit()
        {
            int chainId = 123;
            var blockInfoCache = new BlockInfoCache();
            var crossChainDataProvider = NewCrossChainDataProvider();
            await PublishFakeParentChainConnectedEvent(chainId, blockInfoCache);

            var cachingCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock;
            for (int i = 0; i <= cachingCount + CrossChainConsts.MinimalBlockInfoCacheThreshold; i++)
            {
                blockInfoCache.TryAdd(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ChainId = chainId,
                        Height = (ulong) (i +1)
                    }
                });
            }

            var list = new List<ParentChainBlockData>();
            await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list);
            list = new List<ParentChainBlockData>();
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list);
            Assert.True(res);
            var expectedResultCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock;
            Assert.True(list.Count == expectedResultCount);
        }

//        [Fact]
//        public async Task Act_Twice_GetParentChainBlock()
//        {
//            int chainId = 123;
//            var crossChainDataProvider = NewCrossChainDataProvider();
//
//            var mockClientBase = CreateFakeConsumer(chainId);
//            await LocalEventBus.PublishAsync(new NewParentChainEvent
//            {
//                ClientBase = mockClientBase
//            });
//            
//            var cachingCount = CrossChainConsts.MinimalBlockInfoCacheThreshold + 2;
//            for (int i = 0; i <= cachingCount + CrossChainConsts.MinimalBlockInfoCacheThreshold; i++)
//            {
//                mockClientBase.BlockInfoCache.TryAdd(new ParentChainBlockData
//                {
//                    Root = new ParentChainBlockRootInfo
//                    {
//                        ChainId = chainId,
//                        Height = (ulong) (i +1)
//                    }
//                });
//            }
//            
//            var list = new List<ParentChainBlockData>
//            {
//                new ParentChainBlockData
//                {
//                    Root = new ParentChainBlockRootInfo
//                    {
//                        ChainId = chainId,
//                        Height = 1
//                    }
//                }
//            };
//            await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list, true);
//            list = new List<ParentChainBlockData>();
//            var res =  await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list);
//            Assert.True(res);
//            Assert.True(list.Count == 1);
//        }
        
        #endregion
    }
}