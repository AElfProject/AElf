using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.CrossChain;
using AElf.CrossChain.EventMessage;
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

        public CrosschainDataProviderTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        private ICrossChainDataProvider NewCrossChainDataProvider(Dictionary<int, List<IBlockInfo>> fakeCache)
        {
            var mockContractReader = CreateFakeCrossChainContractReader(fakeCache.Keys.ToList());
            var mockConsumer = FakeCrossChainDataConsumer(fakeCache);
            var crossChainDataProvider = new CrossChainDataProvider(mockContractReader, mockConsumer);
            return crossChainDataProvider;
        }

        private ICrossChainContractReader CreateFakeCrossChainContractReader(List<int> chainIdList)
        {
            Mock<ICrossChainContractReader> mockObject = new Mock<ICrossChainContractReader>();
            mockObject.Setup(m => m.GetSideChainCurrentHeightAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Hash>(), It.IsAny<ulong>()))
                .Returns(Task.FromResult<ulong>(1u));
            mockObject.Setup(m => m.GetParentChainCurrentHeightAsync(It.IsAny<int>(), It.IsAny<Hash>(), It.IsAny<ulong>()))
                .Returns(Task.FromResult<ulong>(1u));
            mockObject.Setup(m => m.GetSideChainIdAndHeightAsync(It.IsAny<int>(), It.IsAny<Hash>(), It.IsAny<ulong>()))
                .Returns(Task.FromResult(new Dictionary<int, ulong>(chainIdList.ToDictionary(id => id, id => 0ul))));
            return mockObject.Object;
        }
        
        private ICrossChainDataConsumer FakeCrossChainDataConsumer(Dictionary<int, List<IBlockInfo>> fakeCache)
        {
            Mock<ICrossChainDataConsumer> mockObject = new Mock<ICrossChainDataConsumer>();
            mockObject.Setup(m => m.TryTake(It.IsAny<int>(), It.IsAny<ulong>(), It.IsAny<bool>()))
                .Returns<int, ulong, bool>((chaiId, height, limit) =>
                {
                    if(fakeCache.ContainsKey(chaiId) &&  (!limit || height + (ulong) CrossChainConsts.MinimalBlockInfoCacheThreshold <= (fakeCache[chaiId].LastOrDefault()?.Height??0)) && height < (ulong) fakeCache[chaiId].Count)
                        return fakeCache[chaiId][(int) height - 1];
                    return null;
                });
            return mockObject.Object;
        }

//        private ICrossChainDataConsumer CreateFakeConsumer(int chainId, BlockInfoCache blockInfoCache)
//        {
//            return new CrossChainDataConsumer
//            {
//                BlockInfoCache = blockInfoCache,
//                ChainId = chainId
//            };
//        }

//        private ICrossChainDataProducer CreateFakeProducer(int chainId, BlockInfoCache blockInfoCache)
//        {
//            var mockObject = new Mock<ICrossChainDataProducer>();
//            mockObject.Setup(m => m.ChainId).Returns(chainId);
//            mockObject.Setup(m => m.BlockInfoCache).Returns(blockInfoCache);
//            return mockObject.Object;
//        }
        
//        private IProducerConsumerService CreateFakeClientService()
//        {
//            Mock<IProducerConsumerService> mockObject = new Mock<IProducerConsumerService>();
//            return mockObject.Object;
//        }

//        private async Task PublishFakeSideChainConnectedEvent(int chainId, BlockInfoCache blockInfoCache)
//        {
//            var consumer = CreateFakeConsumer(chainId, blockInfoCache);
//            var producer = CreateFakeProducer(chainId, blockInfoCache);
//            await LocalEventBus.PublishAsync(new NewChainEvent
//            {
//                CrossChainDataConsumer = consumer,
//                CrossChainDataProducer = producer,
//                RemoteChainId = chainId
//            });
//        }

        
        
//        private async Task PublishFakeParentChainConnectedEvent(int chainId, BlockInfoCache blockInfoCache)
//        {
//            var consumer = CreateFakeConsumer(chainId, blockInfoCache);
//            var producer = CreateFakeProducer(chainId, blockInfoCache);
//            await LocalEventBus.PublishAsync(new NewParentChainEvent
//            {
//                CrossChainDataConsumer = consumer,
//                CrossChainDataProducer = producer,
//                ChainId = chainId
//            });
//        }
        
        
//        [Fact]
//        public async Task Arrange_NewSideChainConnectionEvent()
//        {
//            int chainId = 123;
//            var blockInfoCache = new BlockInfoCache();
//            var crossChainDataProvider = NewCrossChainDataProvider();
//            await PublishFakeSideChainConnectedEvent(chainId, blockInfoCache);
//            Assert.True(crossChainDataProvider.GetCachedChainCount() == 1);
//        }

        //#region Side chain

        [Fact]
        public async Task GetSideChainBlock_WithoutCache()
        {
            int chainId = 123;
            var crossChainDataProvider = NewCrossChainDataProvider(new Dictionary<int, List<IBlockInfo>>());
            var list = new List<SideChainBlockData>();
            var res = await crossChainDataProvider.GetSideChainBlockDataAsync(chainId, list, Hash.Default, 1);
            Assert.False(res);
            Assert.True(list.Count == 0);
        }
        
        [Fact]
        public async Task GetSideChainBlock_WithoutEnoughCache()
        {
            int chainId = 123;
            var crossChainDataProvider = NewCrossChainDataProvider(new Dictionary<int, List<IBlockInfo>>{{chainId, new List<IBlockInfo>
            {
                new SideChainBlockData
                {
                    SideChainHeight = 1
                }
            }}});
            var list = new List<SideChainBlockData>();
            var res = await crossChainDataProvider.GetSideChainBlockDataAsync(chainId, list, Hash.Default, 1);
            Assert.False(res);
            Assert.True(list.Count == 0);
        }
        
        [Fact]
        public async Task GetSideChainBlock_WithEnoughCache()
        {
            int chainId = 123;
            var blockInfoCache = new List<IBlockInfo>();
            for (int i = 0; i <= CrossChainConsts.MinimalBlockInfoCacheThreshold; i++)
            {
                blockInfoCache.Add(new SideChainBlockData
                {
                    SideChainHeight = (ulong) (1 + i),
                    SideChainId = chainId
                });
            }
            
            var fakeCache = new Dictionary<int, List<IBlockInfo>>{{chainId, blockInfoCache}};
            var crossChainDataProvider = NewCrossChainDataProvider(fakeCache);

            var list = new List<SideChainBlockData>();
            var res = await crossChainDataProvider.GetSideChainBlockDataAsync(chainId, list, Hash.Default, 1);
            Assert.True(res);
            Assert.True(list.Count == 1);
        }
        
        [Fact]
        public async Task Validate_Without_ProvidedSideChainBlockData()
        {
            int chainId = 123;
            var blockInfoCache = new List<IBlockInfo>();
            var crossChainDataProvider = NewCrossChainDataProvider(new Dictionary<int, List<IBlockInfo>>());

            
            var list = new List<SideChainBlockData>();
            var res = await crossChainDataProvider.GetSideChainBlockDataAsync(chainId, list, Hash.Default, 1, true);
            Assert.True(res);
        }
        
        [Fact]
        public async Task ValidateSideChainBlock_WithCaching()
        {
            int chainId = 123;
            var blockInfoCache = new List<IBlockInfo>();
           
            blockInfoCache.Add(new SideChainBlockData
            {
                SideChainId = chainId,
                SideChainHeight = 1
            });
            var fakeCache = new Dictionary<int, List<IBlockInfo>>{{chainId, blockInfoCache}};
            var crossChainDataProvider = NewCrossChainDataProvider(fakeCache);
            
            var list = new List<SideChainBlockData>{new SideChainBlockData
            {
                SideChainId = chainId,
                SideChainHeight = 1
            }};
            var res = await crossChainDataProvider.GetSideChainBlockDataAsync(chainId, list, Hash.Default, 1, true);
            Assert.True(res);
            Assert.True(list.Count == 1);
        }
        
        [Fact]
        public async Task ValidateSideChainBlock_WithoutCaching()
        {
            int chainId = 123;
            var crossChainDataProvider = NewCrossChainDataProvider(new Dictionary<int, List<IBlockInfo>>());
            
            var list = new List<SideChainBlockData>{new SideChainBlockData
            {
                SideChainId = chainId,
                SideChainHeight = 1
            }};
            var res = await crossChainDataProvider.GetSideChainBlockDataAsync(chainId, list, Hash.Default, 1, true);
            Assert.False(res);
        }
        
        [Fact]
        public async Task ValidateSideChainBlock_WithWrongBlockIndex()
        {
            int chainId = 123;
            var blockInfoCache = new List<IBlockInfo>();
           
            blockInfoCache.Add(new SideChainBlockData
            {
                SideChainId = chainId,
                SideChainHeight = 1
            });
            var fakeCache = new Dictionary<int, List<IBlockInfo>>{{chainId, blockInfoCache}};
            var crossChainDataProvider = NewCrossChainDataProvider(fakeCache);
            
            var list = new List<SideChainBlockData>{new SideChainBlockData
            {
                SideChainId = chainId,
                SideChainHeight = 2
            }};
            var res = await crossChainDataProvider.GetSideChainBlockDataAsync(chainId, list, Hash.Default, 1, true);
            Assert.False(res);
            Assert.True(list.Count == 1);
        }
        
        [Fact]
        public async Task ValidateSideChainBlock__NotEnoughCaching()
        {
            int chainId = 123;
            var blockInfoCache = new List<IBlockInfo>();
            var fakeCache = new Dictionary<int, List<IBlockInfo>>{{chainId, blockInfoCache}};
            var crossChainDataProvider = NewCrossChainDataProvider(fakeCache);
            
            var list = new List<SideChainBlockData>{new SideChainBlockData
            {
                SideChainId = chainId,
                SideChainHeight = 1
            }};
            var res = await crossChainDataProvider.GetSideChainBlockDataAsync(chainId, list, Hash.Default, 1, true);
            Assert.False(res);
            Assert.True(list.Count == 1);
        }

        /*
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
            await crossChainDataProvider.GetSideChainBlockDataAsync(chainId, list, TODO, TODO);
            list = new List<SideChainBlockData>();
            var res = await crossChainDataProvider.GetSideChainBlockDataAsync(chainId, list, TODO, TODO);
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
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list, TODO, TODO);
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
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list, TODO, TODO);
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
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list, TODO, TODO, true);
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
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list, TODO, TODO, true);
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
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list, TODO, TODO, true);
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
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list, TODO, TODO);
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
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list, TODO, TODO);
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
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list, TODO, TODO);
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
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list, TODO, TODO);
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
            await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list, TODO, TODO);
            list = new List<ParentChainBlockData>();
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list, TODO, TODO);
            Assert.True(res);
            var expectedResultCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock;
            Assert.True(list.Count == expectedResultCount);
        }

        [Fact]
        public async Task Act_Twice_GetParentChainBlock()
        {
            int chainId = 123;
            var crossChainDataProvider = NewCrossChainDataProvider();

            var mockClientBase = CreateFakeConsumer(chainId);
            await LocalEventBus.PublishAsync(new NewParentChainEvent
            {
                ClientBase = mockClientBase
            });
            
            var cachingCount = CrossChainConsts.MinimalBlockInfoCacheThreshold + 2;
            for (int i = 0; i <= cachingCount + CrossChainConsts.MinimalBlockInfoCacheThreshold; i++)
            {
                mockClientBase.BlockInfoCache.TryAdd(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        SideChainId = chainId,
                        Height = (ulong) (i +1)
                    }
                });
            }
            
            var list = new List<ParentChainBlockData>
            {
                new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        SideChainId = chainId,
                        Height = 1
                    }
                }
            };
            await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list, true);
            list = new List<ParentChainBlockData>();
            var res =  await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list);
            Assert.True(res);
            Assert.True(list.Count == 1);
        }
       #endregion*/
    }
}