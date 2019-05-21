using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.CrossChain;
using AElf.CrossChain.Cache;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Google.Protobuf;
using Xunit;

namespace AElf.CrossChain
{
    public class CrossChainDataProviderTest : CrossChainTestBase
    {
        private readonly ICrossChainDataProvider _crossChainDataProvider;
        private readonly CrossChainTestHelper _crossChainTestHelper;

        public CrossChainDataProviderTest()
        {
            _crossChainDataProvider = GetRequiredService<ICrossChainDataProvider>();
            _crossChainTestHelper = GetRequiredService<CrossChainTestHelper>();
        }

        #region Side chain

        [Fact]
        public async Task GetSideChainBlock_WithoutCache()
        {
            var res = await _crossChainDataProvider.GetSideChainBlockDataAsync(Hash.Empty, 1);
            Assert.Empty(res);
        }

        [Fact]
        public async Task GetSideChainBlock_WithoutEnoughCache()
        {
            int chainId = 123;
            var fakeCache = new Dictionary<int, List<BlockCacheEntity>>
            {
                {
                    chainId, new List<BlockCacheEntity>
                    {
                        new BlockCacheEntity()
                        {
                            ChainId = 1
                        }
                    }
                }
            };
            AddFakeCacheData(fakeCache);
            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 0);
            var res = await _crossChainDataProvider.GetSideChainBlockDataAsync(Hash.Empty, 1);
            Assert.Empty(res);
        }

        [Fact]
        public async Task GetSideChainBlock_WithEnoughCache()
        {
            int chainId = 123;
            var blockInfoCache = new List<BlockCacheEntity>();
            for (int i = 0; i <= CrossChainConstants.MinimalBlockCacheEntityCount; i++)
            {
                var sideChainBlockData = new SideChainBlockData
                {
                    SideChainId = chainId,
                    SideChainHeight = i + 1,
                };
                blockInfoCache.Add(new BlockCacheEntity
                {
                    Height = sideChainBlockData.SideChainHeight,
                    ChainId = sideChainBlockData.SideChainId,
                    Payload = sideChainBlockData.ToByteString()
                });
            }
            var fakeCache = new Dictionary<int, List<BlockCacheEntity>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);
            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 0);
            var res = await _crossChainDataProvider.GetSideChainBlockDataAsync(Hash.Empty, 1);
            Assert.True(res.Count == 1);
            Assert.Equal(blockInfoCache[0].Height, res[0].SideChainHeight);
        }

//        [Fact]
//        public async Task Validate_Without_ProvidedSideChainBlockData()
//        {
//            int chainId = 123;
//            var blockInfoCache = new List<CrossChainCacheData>();
//            for (int i = 0; i <= CrossChainConstants.MinimalBlockInfoCacheThreshold; i++)
//            {
//                blockInfoCache.Add(new SideChainBlockData
//                {
//                    SideChainHeight = (1 + i),
//                    SideChainId = chainId
//                });
//            }
//
//            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 0);
//            var fakeCache = new Dictionary<int, List<CrossChainCacheData>> {{chainId, blockInfoCache}};
//            AddFakeCacheData(fakeCache);
//
//            var list = new List<SideChainBlockData>();
//            var res = await _crossChainDataProvider.ValidateSideChainBlockDataAsync(list, Hash.Empty, 1);
//            Assert.True(res);
//        }
//
//        [Fact]
//        public async Task ValidateSideChainBlock_WithCaching()
//        {
//            int chainId = 123;
//            var blockInfoCache = new List<CrossChainCacheData>
//            {
//                new SideChainBlockData {SideChainId = chainId, SideChainHeight = 1}
//            };
//            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 0);
//
//            var fakeCache = new Dictionary<int, List<CrossChainCacheData>> {{chainId, blockInfoCache}};
//            AddFakeCacheData(fakeCache);
//            
//            var list = new List<SideChainBlockData>
//            {
//                new SideChainBlockData
//                {
//                    SideChainId = chainId,
//                    SideChainHeight = 1
//                }
//            };
//            var res = await _crossChainDataProvider.ValidateSideChainBlockDataAsync(list, Hash.Empty, 1);
//            Assert.True(res);
//            Assert.True(list.Count == 1);
//        }
//
//        [Fact]
//        public async Task ValidateSideChainBlock_WithoutCaching()
//        {
//            int chainId = 123;
//
//            var list = new List<SideChainBlockData>
//            {
//                new SideChainBlockData
//                {
//                    SideChainId = chainId,
//                    SideChainHeight = 1
//                }
//            };
//            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 1);
//            var res = await _crossChainDataProvider.ValidateSideChainBlockDataAsync(list, Hash.Empty, 1);
//            Assert.False(res);
//        }
//
//        [Fact]
//        public async Task ValidateSideChainBlock_WithWrongBlockIndex()
//        {
//            int chainId = 123;
//            var blockInfoCache = new List<CrossChainCacheData>
//            {
//                new SideChainBlockData {SideChainId = chainId, SideChainHeight = 1}
//            };
//
//            var fakeCache = new Dictionary<int, List<CrossChainCacheData>> {{chainId, blockInfoCache}};
//            AddFakeCacheData(fakeCache);
//            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 1);
//            var list = new List<SideChainBlockData>
//            {
//                new SideChainBlockData
//                {
//                    SideChainId = chainId,
//                    SideChainHeight = 2
//                }
//            };
//            await Assert.ThrowsAsync<ValidateNextTimeBlockValidationException>(() =>
//                _crossChainDataProvider.ValidateSideChainBlockDataAsync(list, Hash.Empty, 1));
//        }
//
//        [Fact]
//        public async Task ValidateSideChainBlock__NotEnoughCaching()
//        {
//            int chainId = 123;
//            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 1);
//
//            var list = new List<SideChainBlockData>
//            {
//                new SideChainBlockData
//                {
//                    SideChainId = chainId,
//                    SideChainHeight = 1
//                }
//            };
//            var res = await _crossChainDataProvider.ValidateSideChainBlockDataAsync(list, Hash.Empty, 1);
//            Assert.False(res);
//            Assert.True(list.Count == 1);
//        }
//
//        [Fact]
//        public async Task TryTwice_GetSideChainBlockData()
//        {
//            int chainId = 123;
//            var blockInfoCache = new List<CrossChainCacheData>();
//
//            for (int i = 1; i <= CrossChainConstants.MinimalBlockInfoCacheThreshold + 1; i++)
//            {
//                blockInfoCache.Add(new SideChainBlockData
//                {
//                    SideChainId = chainId,
//                    SideChainHeight = 1 + i
//                });
//            }
//
//            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 1);
//            var fakeCache = new Dictionary<int, List<CrossChainCacheData>> {{chainId, blockInfoCache}};
//            AddFakeCacheData(fakeCache);
//            await _crossChainDataProvider.GetSideChainBlockDataAsync(Hash.Empty, 1);
//            var secondResult = await _crossChainDataProvider.GetSideChainBlockDataAsync(Hash.Empty, 1);
//            Assert.True(secondResult.Count == 1);
//        }
//
//        #endregion
//
//        #region Parent chain
//
//        [Fact]
//        public async Task GetParentChainBLock_WithEmptyCache()
//        {
//            int chainId = 123;
//            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 1);
//            var res = await _crossChainDataProvider.GetParentChainBlockDataAsync(Hash.Empty, 1);
//            Assert.Empty(res);
//        }
//
//        [Fact]
//        public async Task ValidateParentChainBlock_WithoutProvidedData()
//        {
//            int chainId = 123;
//            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 0);
//
//            var list = new List<ParentChainBlockData>();
//            var res = await _crossChainDataProvider.ValidateParentChainBlockDataAsync(list, Hash.Empty, 1);
//            Assert.True(res);
//        }
//
//        [Fact]
//        public async Task ValidateParentChainBlock_WithTooManyProvidedData()
//        {
//            int chainId = 123;
//            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 1);
//
//            var list = new List<ParentChainBlockData>();
//            for (int i = 0; i <= CrossChainConstants.MaximalCountForIndexingParentChainBlock; i++)
//            {
//                list.Add(new ParentChainBlockData
//                {
//                    Root = new ParentChainBlockRootInfo
//                    {
//                        ParentChainId = chainId,
//                        ParentChainHeight = (i + 1)
//                    }
//                });
//            }
//
//            var res = await _crossChainDataProvider.ValidateParentChainBlockDataAsync(list, Hash.Empty, 1);
//            Assert.False(res);
//        }
//
//        [Fact]
//        public async Task ValidateParentChainBlock_WithWrongIndex()
//        {
//            int chainId = 123;
//            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 0);
//            var blockInfoCache = new List<CrossChainCacheData>();
//            var cachingCount = 5;
//            for (int i = 0; i < cachingCount; i++)
//            {
//                blockInfoCache.Add(new ParentChainBlockData
//                {
//                    Root = new ParentChainBlockRootInfo
//                    {
//                        ParentChainId = chainId,
//                        ParentChainHeight = (i + 1)
//                    }
//                });
//            }
//            var fakeCache = new Dictionary<int, List<CrossChainCacheData>> {{chainId, blockInfoCache}};
//            AddFakeCacheData(fakeCache);
//
//            var list = new List<ParentChainBlockData>
//            {
//                new ParentChainBlockData
//                    {Root = new ParentChainBlockRootInfo {ParentChainId = chainId, ParentChainHeight = 2}}
//            };
//            var res = await _crossChainDataProvider.ValidateParentChainBlockDataAsync(list, Hash.Empty, 1);
//            Assert.False(res);
//        }
//
//        [Fact]
//        public async Task GetParentChainBlock_WithWrongIndex()
//        {
//            int chainId = 123;
//            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 1);
//            var blockInfoCache = new List<CrossChainCacheData>();
//            var cachingCount = CrossChainConstants.MinimalBlockInfoCacheThreshold;
//            for (int i = 0; i <= cachingCount; i++)
//            {
//                blockInfoCache.Add(new ParentChainBlockData
//                {
//                    Root = new ParentChainBlockRootInfo
//                    {
//                        ParentChainId = chainId,
//                        ParentChainHeight = (i + 1)
//                    }
//                });
//            }
//
//            var fakeCache = new Dictionary<int, List<CrossChainCacheData>> {{chainId, blockInfoCache}};
//            AddFakeCacheData(fakeCache);
//            var res = await _crossChainDataProvider.GetParentChainBlockDataAsync(Hash.Empty, 1);
//            Assert.Empty(res);
//        }
//
//        [Fact]
//        public async Task GetParentBlock_Single()
//        {
//            int chainId = 123;
//            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 1);
//            var blockInfoCache = new List<CrossChainCacheData>();
//            var cachingCount = CrossChainConstants.MinimalBlockInfoCacheThreshold + 1;
//            for (int i = 1; i <= cachingCount; i++)
//            {
//                blockInfoCache.Add(new ParentChainBlockData
//                {
//                    Root = new ParentChainBlockRootInfo
//                    {
//                        ParentChainId = chainId,
//                        ParentChainHeight = (i + 1)
//                    }
//                });
//            }
//
//            var fakeCache = new Dictionary<int, List<CrossChainCacheData>> {{chainId, blockInfoCache}};
//            AddFakeCacheData(fakeCache);
//
//            var res = await _crossChainDataProvider.GetParentChainBlockDataAsync(Hash.Empty, 1);
//            var expectedResultCount = cachingCount - CrossChainConstants.MinimalBlockInfoCacheThreshold;
//            Assert.True(res.Count == expectedResultCount);
//        }
//
//        [Fact]
//        public async Task GetParentChainBlock_Multiple()
//        {
//            int chainId = 123;
//            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 1);
//            var blockInfoCache = new List<CrossChainCacheData>();
//
//            var cachingCount = CrossChainConstants.MaximalCountForIndexingParentChainBlock;
//            for (int i = 1; i <= cachingCount; i++)
//            {
//                blockInfoCache.Add(new ParentChainBlockData
//                {
//                    Root = new ParentChainBlockRootInfo
//                    {
//                        ParentChainId = chainId,
//                        ParentChainHeight = (i + 1)
//                    }
//                });
//            }
//
//            var fakeCache = new Dictionary<int, List<CrossChainCacheData>> {{chainId, blockInfoCache}};
//            AddFakeCacheData(fakeCache);
//
//            var res = await _crossChainDataProvider.GetParentChainBlockDataAsync(Hash.Empty, 1);
//            var expectedResultCount = cachingCount - CrossChainConstants.MinimalBlockInfoCacheThreshold;
//            Assert.True(res.Count == expectedResultCount);
//        }
//
//        [Fact]
//        public async Task GetParentChainBlock_WithCountLimit()
//        {
//            int chainId = 123;
//            var blockInfoCache = new List<CrossChainCacheData>();
//            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 1);
//
//            var cachingCount = CrossChainConstants.MaximalCountForIndexingParentChainBlock +
//                               CrossChainConstants.MinimalBlockInfoCacheThreshold;
//            for (int i = 1; i <= cachingCount; i++)
//            {
//                blockInfoCache.Add(new ParentChainBlockData
//                {
//                    Root = new ParentChainBlockRootInfo
//                    {
//                        ParentChainId = chainId,
//                        ParentChainHeight = (i + 1)
//                    }
//                });
//            }
//
//            var fakeCache = new Dictionary<int, List<CrossChainCacheData>> {{chainId, blockInfoCache}};
//            AddFakeCacheData(fakeCache);
//
//            var res = await _crossChainDataProvider.GetParentChainBlockDataAsync(Hash.Empty, 1);
//            var expectedResultCount = CrossChainConstants.MaximalCountForIndexingParentChainBlock;
//            Assert.True(res.Count == expectedResultCount);
//        }
//
//        [Fact]
//        public async Task TryTwice_GetParentChainBlock_WithCountLimit()
//        {
//            int chainId = 123;
//            var blockInfoCache = new List<CrossChainCacheData>();
//            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 1);
//
//            var cachingCount = CrossChainConstants.MaximalCountForIndexingParentChainBlock +
//                               CrossChainConstants.MinimalBlockInfoCacheThreshold;
//            for (int i = 1; i <= cachingCount + 1; i++)
//            {
//                blockInfoCache.Add(new ParentChainBlockData
//                {
//                    Root = new ParentChainBlockRootInfo
//                    {
//                        ParentChainId = chainId,
//                        ParentChainHeight = (i + 1)
//                    }
//                });
//            }
//
//            var fakeCache = new Dictionary<int, List<CrossChainCacheData>> {{chainId, blockInfoCache}};
//            AddFakeCacheData(fakeCache);
//
//            await _crossChainDataProvider.GetParentChainBlockDataAsync(Hash.Empty, 1);
//            var secondResult = await _crossChainDataProvider.GetParentChainBlockDataAsync(Hash.Empty, 1);
//            var expectedResultCount = CrossChainConstants.MaximalCountForIndexingParentChainBlock;
//            Assert.True(secondResult.Count == expectedResultCount);
//        }
//
//        [Fact]
//        public async Task TryTwice_ValidateParentChainBlock()
//        {
//            int chainId = 123;
//            var blockInfoCache = new List<CrossChainCacheData>();            
//            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 0);
//
//            var cachingCount = CrossChainConstants.MaximalCountForIndexingParentChainBlock +
//                               CrossChainConstants.MinimalBlockInfoCacheThreshold;
//            for (int i = 0; i <= cachingCount; i++)
//            {
//                blockInfoCache.Add(new ParentChainBlockData
//                {
//                    Root = new ParentChainBlockRootInfo
//                    {
//                        ParentChainId = chainId,
//                        ParentChainHeight = (i + 1)
//                    }
//                });
//            }
//
//            var list = new List<ParentChainBlockData>();
//            for (int i = 0; i < CrossChainConstants.MaximalCountForIndexingParentChainBlock; i++)
//            {
//                list.Add(new ParentChainBlockData
//                {
//                    Root = new ParentChainBlockRootInfo
//                    {
//                        ParentChainId = chainId,
//                        ParentChainHeight = (i + 1)
//                    }
//                });
//            }
//
//            var fakeCache = new Dictionary<int, List<CrossChainCacheData>> {{chainId, blockInfoCache}};
//            AddFakeCacheData(fakeCache);
//
//            await _crossChainDataProvider.ValidateParentChainBlockDataAsync(list, Hash.Empty, 1);
//            list = new List<ParentChainBlockData>();
//            for (int i = 0; i < CrossChainConstants.MaximalCountForIndexingParentChainBlock; i++)
//            {
//                list.Add(new ParentChainBlockData
//                {
//                    Root = new ParentChainBlockRootInfo
//                    {
//                        ParentChainId = chainId,
//                        ParentChainHeight = i + 1
//                    }
//                });
//            }
//
//            var res = await _crossChainDataProvider.ValidateParentChainBlockDataAsync(list, Hash.Empty, 2);
//            Assert.True(res);
//            var expectedResultCount = CrossChainConstants.MaximalCountForIndexingParentChainBlock;
//            Assert.True(list.Count == expectedResultCount);
//        }

        #endregion
    }
}