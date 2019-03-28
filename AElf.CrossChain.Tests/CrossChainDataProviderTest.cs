using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using Xunit;

namespace AElf.CrossChain
{
    public class CrossChainDataProviderTest : CrossChainTestBase
    {
        public CrossChainDataProviderTest()
        {
            _crossChainDataProvider = GetRequiredService<ICrossChainDataProvider>();
            _crossChainTestHelper = GetRequiredService<CrossChainTestHelper>();
        }

        private readonly ICrossChainDataProvider _crossChainDataProvider;
        private readonly CrossChainTestHelper _crossChainTestHelper;

        [Fact]
        public async Task Activate_Cache_Twice()
        {
            var chainId = 123;
            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 1);
            await _crossChainDataProvider.ActivateCrossChainCacheAsync(Hash.Empty, 1);
            await _crossChainDataProvider.ActivateCrossChainCacheAsync(Hash.Empty, 1);
            Assert.True(CrossChainDataConsumer.GetCachedChainCount() == 1);
        }

        [Fact]
        public async Task Activate_Cache_WithChainExist()
        {
            var chainId = 123;
            var blockInfoCache = new List<IBlockInfo>
            {
                new SideChainBlockData {SideChainHeight = 1, SideChainId = chainId}
            };
            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);
            var fakeChainId = 124;
            _crossChainTestHelper.AddFakeParentChainIdHeight(fakeChainId, 1);

            await _crossChainDataProvider.ActivateCrossChainCacheAsync(Hash.Empty, 1);
            Assert.True(CrossChainDataConsumer.GetCachedChainCount() == 2);
        }


        [Fact]
        public async Task Activate_Cache_WithoutChainExist()
        {
            var chainId = 123;
            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 1);
            await _crossChainDataProvider.ActivateCrossChainCacheAsync(Hash.Empty, 1);
            Assert.True(CrossChainDataConsumer.GetCachedChainCount() == 1);
        }

        [Fact]
        public async Task GetParentBlock_Single()
        {
            var chainId = 123;
            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 1);
            var blockInfoCache = new List<IBlockInfo>();
            var cachingCount = CrossChainConsts.MinimalBlockInfoCacheThreshold + 1;
            for (var i = 1; i <= cachingCount; i++)
                blockInfoCache.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId,
                        ParentChainHeight = i + 1
                    }
                });

            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            var res = await _crossChainDataProvider.GetParentChainBlockDataAsync(Hash.Empty, 1);
            var expectedResultCount = cachingCount - CrossChainConsts.MinimalBlockInfoCacheThreshold;
            Assert.True(res.Count == expectedResultCount);
        }

        [Fact]
        public async Task GetParentChainBlock_Multiple()
        {
            var chainId = 123;
            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 1);
            var blockInfoCache = new List<IBlockInfo>();

            var cachingCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock;
            for (var i = 1; i <= cachingCount; i++)
                blockInfoCache.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId,
                        ParentChainHeight = i + 1
                    }
                });

            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            var res = await _crossChainDataProvider.GetParentChainBlockDataAsync(Hash.Empty, 1);
            var expectedResultCount = cachingCount - CrossChainConsts.MinimalBlockInfoCacheThreshold;
            Assert.True(res.Count == expectedResultCount);
        }

        [Fact]
        public async Task GetParentChainBlock_WithCountLimit()
        {
            var chainId = 123;
            var blockInfoCache = new List<IBlockInfo>();
            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 1);

            var cachingCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock +
                               CrossChainConsts.MinimalBlockInfoCacheThreshold;
            for (var i = 1; i <= cachingCount; i++)
                blockInfoCache.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId,
                        ParentChainHeight = i + 1
                    }
                });

            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            var res = await _crossChainDataProvider.GetParentChainBlockDataAsync(Hash.Empty, 1);
            var expectedResultCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock;
            Assert.True(res.Count == expectedResultCount);
        }

        [Fact]
        public async Task GetParentChainBLock_WithEmptyCache()
        {
            var chainId = 123;
            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 1);
            var res = await _crossChainDataProvider.GetParentChainBlockDataAsync(Hash.Empty, 1);
            Assert.Empty(res);
        }

        [Fact]
        public async Task GetParentChainBlock_WithWrongIndex()
        {
            var chainId = 123;
            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 1);
            var blockInfoCache = new List<IBlockInfo>();
            var cachingCount = CrossChainConsts.MinimalBlockInfoCacheThreshold;
            for (var i = 0; i <= cachingCount; i++)
                blockInfoCache.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId,
                        ParentChainHeight = i + 1
                    }
                });

            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);
            var res = await _crossChainDataProvider.GetParentChainBlockDataAsync(Hash.Empty, 1);
            Assert.Empty(res);
        }

        [Fact]
        public async Task GetSideChainBlock_WithEnoughCache()
        {
            var chainId = 123;
            var blockInfoCache = new List<IBlockInfo>();
            for (var i = 0; i <= CrossChainConsts.MinimalBlockInfoCacheThreshold; i++)
                blockInfoCache.Add(new SideChainBlockData
                {
                    SideChainHeight = 1 + i,
                    SideChainId = chainId
                });
            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);
            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 0);
            var res = await _crossChainDataProvider.GetSideChainBlockDataAsync(Hash.Empty, 1);
            Assert.True(res.Count == 1);
            Assert.Equal(blockInfoCache[0], res[0]);
        }

        [Fact]
        public async Task GetSideChainBlock_WithoutCache()
        {
            var res = await _crossChainDataProvider.GetSideChainBlockDataAsync(Hash.Empty, 1);
            Assert.Empty(res);
        }

        [Fact]
        public async Task GetSideChainBlock_WithoutEnoughCache()
        {
            var chainId = 123;
            var fakeCache = new Dictionary<int, List<IBlockInfo>>
            {
                {
                    chainId, new List<IBlockInfo>
                    {
                        new SideChainBlockData
                        {
                            SideChainHeight = 1
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
        public async Task TryTwice_GetParentChainBlock_WithCountLimit()
        {
            var chainId = 123;
            var blockInfoCache = new List<IBlockInfo>();
            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 1);

            var cachingCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock +
                               CrossChainConsts.MinimalBlockInfoCacheThreshold;
            for (var i = 1; i <= cachingCount + 1; i++)
                blockInfoCache.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId,
                        ParentChainHeight = i + 1
                    }
                });

            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            await _crossChainDataProvider.GetParentChainBlockDataAsync(Hash.Empty, 1);
            var secondResult = await _crossChainDataProvider.GetParentChainBlockDataAsync(Hash.Empty, 1);
            var expectedResultCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock;
            Assert.True(secondResult.Count == expectedResultCount);
        }

        [Fact]
        public async Task TryTwice_GetSideChainBlockData()
        {
            var chainId = 123;
            var blockInfoCache = new List<IBlockInfo>();

            for (var i = 1; i <= CrossChainConsts.MinimalBlockInfoCacheThreshold + 1; i++)
                blockInfoCache.Add(new SideChainBlockData
                {
                    SideChainId = chainId,
                    SideChainHeight = 1 + i
                });

            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 1);
            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);
            await _crossChainDataProvider.GetSideChainBlockDataAsync(Hash.Empty, 1);
            var secondResult = await _crossChainDataProvider.GetSideChainBlockDataAsync(Hash.Empty, 1);
            Assert.True(secondResult.Count == 1);
        }

        [Fact]
        public async Task TryTwice_ValidateParentChainBlock()
        {
            var chainId = 123;
            var blockInfoCache = new List<IBlockInfo>();
            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 0);

            var cachingCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock +
                               CrossChainConsts.MinimalBlockInfoCacheThreshold;
            for (var i = 0; i <= cachingCount; i++)
                blockInfoCache.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId,
                        ParentChainHeight = i + 1
                    }
                });

            var list = new List<ParentChainBlockData>();
            for (var i = 0; i < CrossChainConsts.MaximalCountForIndexingParentChainBlock; i++)
                list.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId,
                        ParentChainHeight = i + 1
                    }
                });

            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            await _crossChainDataProvider.ValidateParentChainBlockDataAsync(list, Hash.Empty, 1);
            list = new List<ParentChainBlockData>();
            for (var i = 0; i < CrossChainConsts.MaximalCountForIndexingParentChainBlock; i++)
                list.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId,
                        ParentChainHeight = i + 1
                    }
                });

            var res = await _crossChainDataProvider.ValidateParentChainBlockDataAsync(list, Hash.Empty, 2);
            Assert.True(res);
            var expectedResultCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock;
            Assert.True(list.Count == expectedResultCount);
        }

        [Fact]
        public async Task Validate_Without_ProvidedSideChainBlockData()
        {
            var chainId = 123;
            var blockInfoCache = new List<IBlockInfo>();
            for (var i = 0; i <= CrossChainConsts.MinimalBlockInfoCacheThreshold; i++)
                blockInfoCache.Add(new SideChainBlockData
                {
                    SideChainHeight = 1 + i,
                    SideChainId = chainId
                });

            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 0);
            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            var list = new List<SideChainBlockData>();
            var res = await _crossChainDataProvider.ValidateSideChainBlockDataAsync(list, Hash.Empty, 1);
            Assert.True(res);
        }

        [Fact]
        public async Task ValidateParentChainBlock_WithoutProvidedData()
        {
            var chainId = 123;
            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 0);

            var list = new List<ParentChainBlockData>();
            var res = await _crossChainDataProvider.ValidateParentChainBlockDataAsync(list, Hash.Empty, 1);
            Assert.True(res);
        }

        [Fact]
        public async Task ValidateParentChainBlock_WithTooManyProvidedData()
        {
            var chainId = 123;
            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 1);

            var list = new List<ParentChainBlockData>();
            for (var i = 0; i <= CrossChainConsts.MaximalCountForIndexingParentChainBlock; i++)
                list.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId,
                        ParentChainHeight = i + 1
                    }
                });

            var res = await _crossChainDataProvider.ValidateParentChainBlockDataAsync(list, Hash.Empty, 1);
            Assert.False(res);
        }

        [Fact]
        public async Task ValidateParentChainBlock_WithWrongIndex()
        {
            var chainId = 123;
            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 0);
            var blockInfoCache = new List<IBlockInfo>();
            var cachingCount = 5;
            for (var i = 0; i < cachingCount; i++)
                blockInfoCache.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId,
                        ParentChainHeight = i + 1
                    }
                });
            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            var list = new List<ParentChainBlockData>
            {
                new ParentChainBlockData
                    {Root = new ParentChainBlockRootInfo {ParentChainId = chainId, ParentChainHeight = 2}}
            };
            var res = await _crossChainDataProvider.ValidateParentChainBlockDataAsync(list, Hash.Empty, 1);
            Assert.False(res);
        }

        [Fact]
        public async Task ValidateSideChainBlock__NotEnoughCaching()
        {
            var chainId = 123;
            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 1);

            var list = new List<SideChainBlockData>
            {
                new SideChainBlockData
                {
                    SideChainId = chainId,
                    SideChainHeight = 1
                }
            };
            var res = await _crossChainDataProvider.ValidateSideChainBlockDataAsync(list, Hash.Empty, 1);
            Assert.False(res);
            Assert.True(list.Count == 1);
        }

        [Fact]
        public async Task ValidateSideChainBlock_WithCaching()
        {
            var chainId = 123;
            var blockInfoCache = new List<IBlockInfo>
            {
                new SideChainBlockData {SideChainId = chainId, SideChainHeight = 1}
            };


            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 0);

            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            var list = new List<SideChainBlockData>
            {
                new SideChainBlockData
                {
                    SideChainId = chainId,
                    SideChainHeight = 1
                }
            };
            var res = await _crossChainDataProvider.ValidateSideChainBlockDataAsync(list, Hash.Empty, 1);
            Assert.True(res);
            Assert.True(list.Count == 1);
        }

        [Fact]
        public async Task ValidateSideChainBlock_WithoutCaching()
        {
            var chainId = 123;

            var list = new List<SideChainBlockData>
            {
                new SideChainBlockData
                {
                    SideChainId = chainId,
                    SideChainHeight = 1
                }
            };
            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 1);
            var res = await _crossChainDataProvider.ValidateSideChainBlockDataAsync(list, Hash.Empty, 1);
            Assert.False(res);
        }

        [Fact]
        public async Task ValidateSideChainBlock_WithWrongBlockIndex()
        {
            var chainId = 123;
            var blockInfoCache = new List<IBlockInfo>
            {
                new SideChainBlockData {SideChainId = chainId, SideChainHeight = 1}
            };

            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);
            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 1);
            var list = new List<SideChainBlockData>
            {
                new SideChainBlockData
                {
                    SideChainId = chainId,
                    SideChainHeight = 2
                }
            };
            var res = await _crossChainDataProvider.ValidateSideChainBlockDataAsync(list, Hash.Empty, 1);
            Assert.False(res);
            Assert.True(list.Count == 1);
        }
    }
}