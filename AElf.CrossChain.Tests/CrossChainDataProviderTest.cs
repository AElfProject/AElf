using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.TestBase;
using AElf.CrossChain.Cache;
using Moq;
using Volo.Abp.Threading;
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
            var list = new List<SideChainBlockData>();
            var res = await _crossChainDataProvider.GetSideChainBlockDataAsync(list, Hash.Default, 1);
            Assert.False(res);
            Assert.True(list.Count == 0);
        }

        [Fact]
        public async Task GetSideChainBlock_WithoutEnoughCache()
        {
            int chainId = 123;
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
            _crossChainTestHelper.AddSideChainIdHeight(chainId, 0);
            var list = new List<SideChainBlockData>();
            var res = await _crossChainDataProvider.GetSideChainBlockDataAsync(list, Hash.Default, 1);
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
                    SideChainHeight = (1 + i),
                    SideChainId = chainId
                });
            }
            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);
            _crossChainTestHelper.AddSideChainIdHeight(chainId, 0);

            var list = new List<SideChainBlockData>();
            var res = await _crossChainDataProvider.GetSideChainBlockDataAsync(list, Hash.Default, 1);
            Assert.True(res);
            Assert.True(list.Count == 1);
        }

        [Fact]
        public async Task Validate_Without_ProvidedSideChainBlockData()
        {
            int chainId = 123;
            var blockInfoCache = new List<IBlockInfo>();
            for (int i = 0; i <= CrossChainConsts.MinimalBlockInfoCacheThreshold; i++)
            {
                blockInfoCache.Add(new SideChainBlockData
                {
                    SideChainHeight = (1 + i),
                    SideChainId = chainId
                });
            }

            _crossChainTestHelper.AddSideChainIdHeight(chainId, 0);
            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            var list = new List<SideChainBlockData>();
            var res = await _crossChainDataProvider.GetSideChainBlockDataAsync(list, Hash.Default, 1, true);
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

            _crossChainTestHelper.AddSideChainIdHeight(chainId, 1);

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
            var res = await _crossChainDataProvider.GetSideChainBlockDataAsync(list, Hash.Default, 1, true);
            Assert.True(res);
            Assert.True(list.Count == 1);
        }

        [Fact]
        public async Task ValidateSideChainBlock_WithoutCaching()
        {
            int chainId = 123;

            var list = new List<SideChainBlockData>
            {
                new SideChainBlockData
                {
                    SideChainId = chainId,
                    SideChainHeight = 1
                }
            };
            var res = await _crossChainDataProvider.GetSideChainBlockDataAsync(list, Hash.Default, 1, true);
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
            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);
            _crossChainTestHelper.AddSideChainIdHeight(chainId, 1);
            var list = new List<SideChainBlockData>
            {
                new SideChainBlockData
                {
                    SideChainId = chainId,
                    SideChainHeight = 2
                }
            };
            var res = await _crossChainDataProvider.GetSideChainBlockDataAsync(list, Hash.Default, 1, true);
            Assert.False(res);
            Assert.True(list.Count == 1);
        }

        [Fact]
        public async Task ValidateSideChainBlock__NotEnoughCaching()
        {
            int chainId = 123;
            _crossChainTestHelper.AddSideChainIdHeight(chainId, 1);

            var list = new List<SideChainBlockData>
            {
                new SideChainBlockData
                {
                    SideChainId = chainId,
                    SideChainHeight = 1
                }
            };
            var res = await _crossChainDataProvider.GetSideChainBlockDataAsync(list, Hash.Default, 1, true);
            Assert.False(res);
            Assert.True(list.Count == 1);
        }

        [Fact]
        public async Task TryTwice_GetSideChainBlockData()
        {
            int chainId = 123;
            var blockInfoCache = new List<IBlockInfo>();

            for (int i = 0; i <= CrossChainConsts.MinimalBlockInfoCacheThreshold; i++)
            {
                blockInfoCache.Add(new SideChainBlockData
                {
                    SideChainId = chainId,
                    SideChainHeight = (1 + i)
                });
            }

            _crossChainTestHelper.AddSideChainIdHeight(chainId, 0);
            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);
            var list = new List<SideChainBlockData>();
            await _crossChainDataProvider.GetSideChainBlockDataAsync(list, Hash.Default, 1);
            list = new List<SideChainBlockData>();
            var res = await _crossChainDataProvider.GetSideChainBlockDataAsync(list, Hash.Default, 1);
            Assert.True(res);
            Assert.True(list.Count == 1);
        }

        #endregion

        #region Parent chain

        [Fact]
        public async Task GetParentChainBLock_WithEmptyCache()
        {
            int chainId = 123;
            _crossChainTestHelper.AddParentChainIdHeight(chainId, 0);
            var list = new List<ParentChainBlockData>();
            var res = await _crossChainDataProvider.GetParentChainBlockDataAsync(list, Hash.Default, 1);
            Assert.True(res);
            Assert.True(list.Count == 0);
        }

        [Fact]
        public async Task ValidateParentChainBlock_WithoutProvidedData()
        {
            int chainId = 123;
            _crossChainTestHelper.AddParentChainIdHeight(chainId, 0);

            var list = new List<ParentChainBlockData>();
            var res = await _crossChainDataProvider.GetParentChainBlockDataAsync(list, Hash.Default, 1, true);
            Assert.True(res);
        }

        [Fact]
        public async Task ValidateParentChainBlock_WithTooManyProvidedData()
        {
            int chainId = 123;
            _crossChainTestHelper.AddParentChainIdHeight(chainId, 1);

            var list = new List<ParentChainBlockData>();
            for (int i = 0; i <= CrossChainConsts.MaximalCountForIndexingParentChainBlock; i++)
            {
                list.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId,
                        ParentChainHeight = (i + 1)
                    }
                });
            }

            var res = await _crossChainDataProvider.GetParentChainBlockDataAsync(list, Hash.Default, 1, true);
            Assert.False(res);
        }

        [Fact]
        public async Task ValidateParentChainBlock_WithWrongIndex()
        {
            int chainId = 123;
            _crossChainTestHelper.AddParentChainIdHeight(chainId, 1);
            var blockInfoCache = new List<IBlockInfo>();
            var cachingCount = 5;
            for (int i = 0; i < cachingCount; i++)
            {
                blockInfoCache.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId,
                        ParentChainHeight = (i + 1)
                    }
                });
            }
            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            var list = new List<ParentChainBlockData>
            {
                new ParentChainBlockData
                    {Root = new ParentChainBlockRootInfo {ParentChainId = chainId, ParentChainHeight = 2}}
            };
            var res = await _crossChainDataProvider.GetParentChainBlockDataAsync(list, Hash.Default, 1, true);
            Assert.False(res);
        }

        [Fact]
        public async Task GetParentChainBlock_WithWrongIndex()
        {
            int chainId = 123;
            _crossChainTestHelper.AddParentChainIdHeight(chainId, 1);
            var blockInfoCache = new List<IBlockInfo>();
            var cachingCount = CrossChainConsts.MinimalBlockInfoCacheThreshold;
            for (int i = 0; i < cachingCount + 1; i++)
            {
                blockInfoCache.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId,
                        ParentChainHeight = (i + 1)
                    }
                });
            }

            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            var list = new List<ParentChainBlockData>
            {
                new ParentChainBlockData
                    {Root = new ParentChainBlockRootInfo {ParentChainId = chainId, ParentChainHeight = 1}}
            };
            var res = await _crossChainDataProvider.GetParentChainBlockDataAsync(list, Hash.Default, 1);
            Assert.False(res);
        }

        [Fact]
        public async Task GetParentBlock_Single()
        {
            int chainId = 123;
            _crossChainTestHelper.AddParentChainIdHeight(chainId, 0);
            var blockInfoCache = new List<IBlockInfo>();
            var cachingCount = CrossChainConsts.MinimalBlockInfoCacheThreshold + 1;
            for (int i = 0; i < cachingCount; i++)
            {
                blockInfoCache.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId,
                        ParentChainHeight = (i + 1)
                    }
                });
            }

            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            var list = new List<ParentChainBlockData>();
            var res = await _crossChainDataProvider.GetParentChainBlockDataAsync(list, Hash.Default, 1);
            Assert.True(res);
            var expectedResultCount = cachingCount - CrossChainConsts.MinimalBlockInfoCacheThreshold;
            Assert.True(list.Count == expectedResultCount);
        }

        [Fact]
        public async Task GetParentChainBlock_Multiple()
        {
            int chainId = 123;
            _crossChainTestHelper.AddParentChainIdHeight(chainId, 0);
            var blockInfoCache = new List<IBlockInfo>();

            var cachingCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock;
            for (int i = 0; i < cachingCount; i++)
            {
                blockInfoCache.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId,
                        ParentChainHeight = (i + 1)
                    }
                });
            }

            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            var list = new List<ParentChainBlockData>();
            var res = await _crossChainDataProvider.GetParentChainBlockDataAsync(list, Hash.Default, 1);
            Assert.True(res);
            var expectedResultCount = cachingCount - CrossChainConsts.MinimalBlockInfoCacheThreshold;
            Assert.True(list.Count == expectedResultCount);
        }

        [Fact]
        public async Task GetParentChainBlock_WithCountLimit()
        {
            int chainId = 123;
            var blockInfoCache = new List<IBlockInfo>();
            _crossChainTestHelper.AddParentChainIdHeight(chainId, 0);

            var cachingCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock +
                               CrossChainConsts.MinimalBlockInfoCacheThreshold;
            for (int i = 0; i < cachingCount; i++)
            {
                blockInfoCache.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId,
                        ParentChainHeight = (i + 1)
                    }
                });
            }

            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            var list = new List<ParentChainBlockData>();
            var res = await _crossChainDataProvider.GetParentChainBlockDataAsync(list, Hash.Default, 1);
            Assert.True(res);
            var expectedResultCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock;
            Assert.True(list.Count == expectedResultCount);
        }

        [Fact]
        public async Task TryTwice_GetParentChainBlock_WithCountLimit()
        {
            int chainId = 123;
            var blockInfoCache = new List<IBlockInfo>();
            _crossChainTestHelper.AddParentChainIdHeight(chainId, 0);

            var cachingCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock +
                               CrossChainConsts.MinimalBlockInfoCacheThreshold;
            for (int i = 0; i <= cachingCount; i++)
            {
                blockInfoCache.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId,
                        ParentChainHeight = (i + 1)
                    }
                });
            }

            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            var list = new List<ParentChainBlockData>();
            await _crossChainDataProvider.GetParentChainBlockDataAsync(list, Hash.Default, 1);
            list = new List<ParentChainBlockData>();
            var res = await _crossChainDataProvider.GetParentChainBlockDataAsync(list, Hash.Default, 1);
            Assert.True(res);
            var expectedResultCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock;
            Assert.True(list.Count == expectedResultCount);
        }

        [Fact]
        public async Task TryTwice_ValidateParentChainBlock()
        {
            int chainId = 123;
            var blockInfoCache = new List<IBlockInfo>();            
            _crossChainTestHelper.AddParentChainIdHeight(chainId, 1);

            var cachingCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock +
                               CrossChainConsts.MinimalBlockInfoCacheThreshold;
            for (int i = 0; i <= cachingCount; i++)
            {
                blockInfoCache.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId,
                        ParentChainHeight = (i + 1)
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
                        ParentChainHeight = (i + 1)
                    }
                });
            }

            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            await _crossChainDataProvider.GetParentChainBlockDataAsync(list, Hash.Default, 1, true);
            list = new List<ParentChainBlockData>();
            for (int i = 0; i < CrossChainConsts.MaximalCountForIndexingParentChainBlock; i++)
            {
                list.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId,
                        ParentChainHeight = (i + 1)
                    }
                });
            }

            var res = await _crossChainDataProvider.GetParentChainBlockDataAsync(list, Hash.Default, 1, true);
            Assert.True(res);
            var expectedResultCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock;
            Assert.True(list.Count == expectedResultCount);
        }

        #endregion

        [Fact]
        public async Task Activate_Cache_WithChainExist()
        {
            int chainId = 123;
            var blockInfoCache = new List<IBlockInfo>
            {
                new SideChainBlockData {SideChainHeight = 1, SideChainId = chainId}
            };
            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);
            var res = await _crossChainDataProvider.ActivateCrossChainCacheAsync(Hash.Default, 1);
            Assert.False(res);
        }


        [Fact]
        public async Task Activate_Cache_WithoutChainExist()
        {
            var res = await _crossChainDataProvider.ActivateCrossChainCacheAsync(Hash.Default, 1);
            Assert.True(res);
        }
    }
}