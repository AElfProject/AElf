using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.CrossChain;
using AElf.CrossChain.EventMessage;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using Moq;
using NSubstitute;
using Volo.Abp.EventBus.Local;
using Xunit;
using Xunit.Abstractions;

namespace AElf.Crosschain
{
    public class CrossChainDataProviderTest : CrosschainTestBase
    {
//        private readonly ITestOutputHelper _testOutputHelper;
//
//        public CrossChainDataProviderTest(ITestOutputHelper testOutputHelper)
//        {
//            _testOutputHelper = testOutputHelper;
//        }
        private ICrossChainDataProvider CreateCrossChainDataProviderWithSingleChainCache(int chainId, List<IBlockInfo> blockInfoCache)
        {
            var fakeCache = new Dictionary<int, List<IBlockInfo>>{{chainId, blockInfoCache}};
            return NewCrossChainDataProvider(fakeCache);
        }
        
        private ICrossChainDataProvider NewCrossChainDataProvider(Dictionary<int, List<IBlockInfo>> fakeCache, List<int> idListInContract = null)
        {
            var mockContractReader = CreateFakeCrossChainContractReader(idListInContract ?? fakeCache.Keys.ToList());
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
            mockObject.Setup(m => m.GetAllChainsIdAndHeightAsync(It.IsAny<int>(), It.IsAny<Hash>(), It.IsAny<ulong>()))
                .Returns(Task.FromResult(new Dictionary<int, ulong>(chainIdList.ToDictionary(id => id, id => 0ul))));
            mockObject.Setup(m => m.GetParentChainIdAsync(It.IsAny<int>(), It.IsAny<Hash>(), It.IsAny<ulong>()))
                .Returns(Task.FromResult(123));
            return mockObject.Object;
        }
        
        private ICrossChainDataConsumer FakeCrossChainDataConsumer(Dictionary<int, List<IBlockInfo>> fakeCache)
        {
            Mock<ICrossChainDataConsumer> mockObject = new Mock<ICrossChainDataConsumer>();
            mockObject.Setup(m => m.TryTake(It.IsAny<int>(), It.IsAny<ulong>(), It.IsAny<bool>()))
                .Returns<int, ulong, bool>((chaiId, height, limit) =>
                {
                    if (fakeCache.ContainsKey(chaiId) &&
                        (!limit || height + (ulong) CrossChainConsts.MinimalBlockInfoCacheThreshold <=
                         (fakeCache[chaiId].LastOrDefault()?.Height ?? 0)) && height <= (ulong) fakeCache[chaiId].Count)
                        return fakeCache[chaiId][(int) height - 1];
                    return null;
                });
            mockObject.Setup(m => m.GetCachedChainCount()).Returns(fakeCache.Count);
            mockObject.Setup(m => m.RegisterNewChainCache(It.IsAny<int>(), It.IsAny<ulong>())).Callback<int, ulong>(
                (chainId, height) =>
                {
                    fakeCache.Add(chainId, new List<IBlockInfo>
                    {
                        new SideChainBlockData
                        {
                            SideChainHeight = height
                        }
                    });
                });
            return mockObject.Object;
        }

        #region Side chain

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
            var crossChainDataProvider = CreateCrossChainDataProviderWithSingleChainCache(chainId, blockInfoCache);
            
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
            var crossChainDataProvider = CreateCrossChainDataProviderWithSingleChainCache(chainId, blockInfoCache);
            
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
            var crossChainDataProvider = CreateCrossChainDataProviderWithSingleChainCache(chainId, blockInfoCache);
            
            var list = new List<SideChainBlockData>{new SideChainBlockData
            {
                SideChainId = chainId,
                SideChainHeight = 1
            }};
            var res = await crossChainDataProvider.GetSideChainBlockDataAsync(chainId, list, Hash.Default, 1, true);
            Assert.False(res);
            Assert.True(list.Count == 1);
        }

        [Fact]
        public async Task TryTwice_GetSideChainBlockData()
        {
            int chainId = 123;
            var blockInfoCache = new List<IBlockInfo>();
            var crossChainDataProvider = CreateCrossChainDataProviderWithSingleChainCache(chainId, blockInfoCache);

            for (int i = 0; i <= CrossChainConsts.MinimalBlockInfoCacheThreshold; i++)
            {
                blockInfoCache.Add(new SideChainBlockData
                {
                    SideChainHeight = (ulong) (1 + i)
                });
            }
            
            var list = new List<SideChainBlockData>();
            await crossChainDataProvider.GetSideChainBlockDataAsync(chainId, list, Hash.Default, 1);
            list = new List<SideChainBlockData>();
            var res = await crossChainDataProvider.GetSideChainBlockDataAsync(chainId, list, Hash.Default, 1);
            Assert.True(res);
            Assert.True(list.Count == 1);
        }
        
        #endregion
        
        #region Parent chain

        [Fact]
        public async Task GetParentChainBLock_WithCache()
        {
            int chainId = 123;
            var blockInfoCache = new List<IBlockInfo>();
            var crossChainDataProvider = CreateCrossChainDataProviderWithSingleChainCache(chainId, blockInfoCache);
          
            var list = new List<ParentChainBlockData>();
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list, Hash.Default, 1);
            Assert.True(res);
            Assert.True(list.Count == 0);
        }
        
        [Fact]
        public async Task ValidateParentChainBlock_WithoutProvidedData()
        {
            int chainId = 123;
            var blockInfoCache = new List<IBlockInfo>();
            var crossChainDataProvider = CreateCrossChainDataProviderWithSingleChainCache(chainId, blockInfoCache);

            var list = new List<ParentChainBlockData>();
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list, Hash.Default, 1,true);
            Assert.True(res);
        }
        
        [Fact]
        public async Task ValidateParentChainBlock_WithTooManyProvidedData()
        {
            int chainId = 123;
            var blockInfoCache = new List<IBlockInfo>();
            var crossChainDataProvider = CreateCrossChainDataProviderWithSingleChainCache(chainId, blockInfoCache);

            var list = new List<ParentChainBlockData>();
            for (int i = 0; i <= CrossChainConsts.MaximalCountForIndexingParentChainBlock; i++)
            {
                list.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId,
                        ParentChainHeight = (ulong) (i +1)
                    }
                });
            }
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list, Hash.Default, 1, true);
            Assert.False(res);
        }

        [Fact]
        public async Task ValidateParentChainBlock__WithWrongIndex()
        {
            int chainId = 123;
            var blockInfoCache = new List<IBlockInfo>();
            var crossChainDataProvider = CreateCrossChainDataProviderWithSingleChainCache(chainId, blockInfoCache);

            var cachingCount = 5;
            for (int i = 0; i < cachingCount; i++)
            {
                blockInfoCache.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId,
                        ParentChainHeight = (ulong) (i +1)
                    }
                });
            }

            var list = new List<ParentChainBlockData>
            {
                new ParentChainBlockData
                    {Root = new ParentChainBlockRootInfo {ParentChainId = chainId, ParentChainHeight = 2}}
            };
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list, Hash.Default, 1, true);
            Assert.False(res);
        }
        
        [Fact]
        public async Task GetParentChainBlock_WithProvidedData()
        {
            int chainId = 123;
            var blockInfoCache = new List<IBlockInfo>();
            var crossChainDataProvider = CreateCrossChainDataProviderWithSingleChainCache(chainId, blockInfoCache);

            var list = new List<ParentChainBlockData>
            {
                new ParentChainBlockData
                    {Root = new ParentChainBlockRootInfo {ParentChainId = chainId, ParentChainHeight = 1}}
            };
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list, Hash.Default, 1);
            Assert.False(res);
        }
        
        [Fact]
        public async Task GetSingleParentBlock_Single()
        {
            int chainId = 123;
            var blockInfoCache = new List<IBlockInfo>();
            var crossChainDataProvider = CreateCrossChainDataProviderWithSingleChainCache(chainId, blockInfoCache);

            var cachingCount = 5;
            for (int i = 0; i < cachingCount; i++)
            {
                blockInfoCache.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId, 
                        ParentChainHeight = (ulong) (i +1)
                    }
                });
            }

            var list = new List<ParentChainBlockData>();
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list, Hash.Default, 1);
            Assert.True(res);
            var expectedResultCount = cachingCount - CrossChainConsts.MinimalBlockInfoCacheThreshold;
            Assert.True(list.Count == expectedResultCount);
        }

        [Fact]
        public async Task GetParentChainBlock_Success()
        {
            int chainId = 123;
            var blockInfoCache = new List<IBlockInfo>();
            var crossChainDataProvider = CreateCrossChainDataProviderWithSingleChainCache(chainId, blockInfoCache);

            var cachingCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock;
            for (int i = 0; i < cachingCount; i++)
            {
                blockInfoCache.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId, 
                        ParentChainHeight = (ulong) (i +1)
                    }
                });
            }

            var list = new List<ParentChainBlockData>();
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list, Hash.Default, 1);
            Assert.True(res);
            var expectedResultCount = cachingCount - CrossChainConsts.MinimalBlockInfoCacheThreshold;
            Assert.True(list.Count == expectedResultCount);
        }
        
        [Fact]
        public async Task GetParentChainBlock_WithCountLimit()
        {
            int chainId = 123;
            var blockInfoCache = new List<IBlockInfo>();
            var crossChainDataProvider = CreateCrossChainDataProviderWithSingleChainCache(chainId, blockInfoCache);

            var cachingCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock;
            for (int i = 0; i <= cachingCount + CrossChainConsts.MinimalBlockInfoCacheThreshold; i++)
            {
                blockInfoCache.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId, 
                        ParentChainHeight = (ulong) (i +1)
                    }
                });
            }

            var list = new List<ParentChainBlockData>();
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list, Hash.Default, 1);
            Assert.True(res);
            var expectedResultCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock;
            Assert.True(list.Count == expectedResultCount);
        }

        [Fact]
        public async Task TryTwice_GetParentChainBlock_WithCountLimit()
        {
            int chainId = 123;
            var blockInfoCache = new List<IBlockInfo>();
            var crossChainDataProvider = CreateCrossChainDataProviderWithSingleChainCache(chainId, blockInfoCache);

            var cachingCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock;
            for (int i = 0; i <= cachingCount + CrossChainConsts.MinimalBlockInfoCacheThreshold; i++)
            {
                blockInfoCache.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId, 
                        ParentChainHeight = (ulong) (i +1)
                    }
                });
            }

            var list = new List<ParentChainBlockData>();
            await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list, Hash.Default, 1);
            list = new List<ParentChainBlockData>();
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list, Hash.Default, 1);
            Assert.True(res);
            var expectedResultCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock;
            Assert.True(list.Count == expectedResultCount);
        }

        [Fact]
        public async Task TryTwice_ValidateParentChainBlock()
        {
            int chainId = 123;
            var blockInfoCache = new List<IBlockInfo>();
            var crossChainDataProvider = CreateCrossChainDataProviderWithSingleChainCache(chainId, blockInfoCache);

            var cachingCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock;
            for (int i = 0; i <= cachingCount + CrossChainConsts.MinimalBlockInfoCacheThreshold; i++)
            {
                blockInfoCache.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId, 
                        ParentChainHeight = (ulong) (i +1)
                    }
                });
            }
            var list = new List<ParentChainBlockData>();
            for (int i = 0; i < CrossChainConsts.MaximalCountForIndexingParentChainBlock; i++)
            {
                list.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId,
                        ParentChainHeight = (ulong) (i +1)
                    }
                });
            }
            await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list, Hash.Default, 1, true);
            list = new List<ParentChainBlockData>();
            for (int i = 0; i < CrossChainConsts.MaximalCountForIndexingParentChainBlock; i++)
            {
                list.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId,
                        ParentChainHeight = (ulong) (i +1)
                    }
                });
            }
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(chainId, list, Hash.Default, 1, true);
            Assert.True(res);
            var expectedResultCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock;
            Assert.True(list.Count == expectedResultCount);
        }
        
       #endregion

       [Fact]
       public async Task Activate_Cache_WithChainExist()
       {
           int chainId = 123;
           var blockInfoCache = new List<IBlockInfo>();
           blockInfoCache.Add(new SideChainBlockData
           {
               SideChainHeight = 1,
               SideChainId = chainId
           });
           var fakeCache = new Dictionary<int, List<IBlockInfo>>{{chainId, blockInfoCache}};
           var crossChainDataProvider = NewCrossChainDataProvider(fakeCache);
           var res =await crossChainDataProvider.ActivateCrossChainCacheAsync(1, Hash.Default, 1);
           
           Assert.False(res);
       }
       
       [Fact]
       public async Task Activate_Cache_WithoutChainExist()
       {
           var fakeCache = new Dictionary<int, List<IBlockInfo>>();
           var crossChainDataProvider = NewCrossChainDataProvider(fakeCache, new List<int>{123});
           var res = await crossChainDataProvider.ActivateCrossChainCacheAsync(1, Hash.Default, 1);
           
           Assert.True(res);
       }
    }
}