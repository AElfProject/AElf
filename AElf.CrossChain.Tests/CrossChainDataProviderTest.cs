using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.CrossChain.Cache;
using Moq;
using Xunit;

namespace AElf.CrossChain
{
    public class CrossChainDataProviderTest : CrossChainTestBase
    {
        private ICrossChainDataProvider CreateNewCrossChainDataProvider(Dictionary<int, List<IBlockInfo>> fakeCache,
            Dictionary<int, ulong> sideChainIdHeights, Dictionary<int, ulong> parentCHainIdHeights)
        {
            var fakeMultiChainBlockInfoCacheProvider = CreateFakeMultiChainBlockInfoCacheProvider(fakeCache);
            var fakeConsumer = CreateFakeCrossChainDataConsumer(fakeMultiChainBlockInfoCacheProvider);
            var mockContractReader = CreateFakeCrossChainContractReader(sideChainIdHeights, parentCHainIdHeights);
            var crossChainDataProvider = new CrossChainDataProvider(mockContractReader, fakeConsumer);
            return crossChainDataProvider;
        }

        #region Side chain

        [Fact]
        public async Task GetSideChainBlock_WithoutCache()
        {
            var crossChainDataProvider = CreateNewCrossChainDataProvider(new Dictionary<int, List<IBlockInfo>>(),
                new Dictionary<int, ulong>(), new Dictionary<int, ulong>());
            var list = new List<SideChainBlockData>();
            var res = await crossChainDataProvider.GetSideChainBlockDataAsync(list, Hash.Default, 1);
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
            var sideChainIdHeights = new Dictionary<int, ulong>
            {
                {
                    chainId, 0
                }
            };
            var crossChainDataProvider =
                CreateNewCrossChainDataProvider(fakeCache, sideChainIdHeights, new Dictionary<int, ulong>());
            var list = new List<SideChainBlockData>();
            var res = await crossChainDataProvider.GetSideChainBlockDataAsync(list, Hash.Default, 1);
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

            var sideChainIdHeights = new Dictionary<int, ulong>
            {
                {
                    chainId, 0
                }
            };
            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            var crossChainDataProvider =
                CreateNewCrossChainDataProvider(fakeCache, sideChainIdHeights, new Dictionary<int, ulong>());

            var list = new List<SideChainBlockData>();
            var res = await crossChainDataProvider.GetSideChainBlockDataAsync(list, Hash.Default, 1);
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
                    SideChainHeight = (ulong) (1 + i),
                    SideChainId = chainId
                });
            }

            var sideChainIdHeights = new Dictionary<int, ulong>
            {
                {
                    chainId, 0
                }
            };
            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            var crossChainDataProvider =
                CreateNewCrossChainDataProvider(fakeCache, sideChainIdHeights, new Dictionary<int, ulong>());

            var list = new List<SideChainBlockData>();
            var res = await crossChainDataProvider.GetSideChainBlockDataAsync(list, Hash.Default, 1, true);
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

            var sideChainIdHeights = new Dictionary<int, ulong>
            {
                {
                    chainId, 1
                }
            };
            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            var crossChainDataProvider =
                CreateNewCrossChainDataProvider(fakeCache, sideChainIdHeights, new Dictionary<int, ulong>());

            var list = new List<SideChainBlockData>
            {
                new SideChainBlockData
                {
                    SideChainId = chainId,
                    SideChainHeight = 1
                }
            };
            var res = await crossChainDataProvider.GetSideChainBlockDataAsync(list, Hash.Default, 1, true);
            Assert.True(res);
            Assert.True(list.Count == 1);
        }

        [Fact]
        public async Task ValidateSideChainBlock_WithoutCaching()
        {
            int chainId = 123;
            var crossChainDataProvider = CreateNewCrossChainDataProvider(new Dictionary<int, List<IBlockInfo>>(),
                new Dictionary<int, ulong>(), new Dictionary<int, ulong>());

            var list = new List<SideChainBlockData>
            {
                new SideChainBlockData
                {
                    SideChainId = chainId,
                    SideChainHeight = 1
                }
            };
            var res = await crossChainDataProvider.GetSideChainBlockDataAsync(list, Hash.Default, 1, true);
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
            var sideChainIdHeights = new Dictionary<int, ulong>
            {
                {
                    chainId, 1
                }
            };
            var crossChainDataProvider =
                CreateNewCrossChainDataProvider(fakeCache, sideChainIdHeights, new Dictionary<int, ulong>());

            var list = new List<SideChainBlockData>
            {
                new SideChainBlockData
                {
                    SideChainId = chainId,
                    SideChainHeight = 2
                }
            };
            var res = await crossChainDataProvider.GetSideChainBlockDataAsync(list, Hash.Default, 1, true);
            Assert.False(res);
            Assert.True(list.Count == 1);
        }

        [Fact]
        public async Task ValidateSideChainBlock__NotEnoughCaching()
        {
            int chainId = 123;
            var sideChainIdHeights = new Dictionary<int, ulong>
            {
                {
                    chainId, 1
                }
            };
            var crossChainDataProvider = CreateNewCrossChainDataProvider(new Dictionary<int, List<IBlockInfo>>(),
                sideChainIdHeights, new Dictionary<int, ulong>());

            var list = new List<SideChainBlockData>
            {
                new SideChainBlockData
                {
                    SideChainId = chainId,
                    SideChainHeight = 1
                }
            };
            var res = await crossChainDataProvider.GetSideChainBlockDataAsync(list, Hash.Default, 1, true);
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
                    SideChainHeight = (ulong) (1 + i)
                });
            }

            var sideChainIdHeights = new Dictionary<int, ulong>
            {
                {
                    chainId, 0
                }
            };
            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            var crossChainDataProvider =
                CreateNewCrossChainDataProvider(fakeCache, sideChainIdHeights, new Dictionary<int, ulong>());

            var list = new List<SideChainBlockData>();
            await crossChainDataProvider.GetSideChainBlockDataAsync(list, Hash.Default, 1);
            list = new List<SideChainBlockData>();
            var res = await crossChainDataProvider.GetSideChainBlockDataAsync(list, Hash.Default, 1);
            Assert.True(res);
            Assert.True(list.Count == 1);
        }

        #endregion

        #region Parent chain

        [Fact]
        public async Task GetParentChainBLock_WithEmptyCache()
        {
            int chainId = 123;
            var parentIdHeight = new Dictionary<int, ulong> {{chainId, 0}};
            var crossChainDataProvider = CreateNewCrossChainDataProvider(new Dictionary<int, List<IBlockInfo>>(),
                new Dictionary<int, ulong>(), parentIdHeight);

            var list = new List<ParentChainBlockData>();
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(list, Hash.Default, 1);
            Assert.True(res);
            Assert.True(list.Count == 0);
        }

        [Fact]
        public async Task ValidateParentChainBlock_WithoutProvidedData()
        {
            int chainId = 123;
            var parentIdHeight = new Dictionary<int, ulong> {{chainId, 0}};
            var crossChainDataProvider = CreateNewCrossChainDataProvider(new Dictionary<int, List<IBlockInfo>>(),
                new Dictionary<int, ulong>(), parentIdHeight);

            var list = new List<ParentChainBlockData>();
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(list, Hash.Default, 1, true);
            Assert.True(res);
        }

        [Fact]
        public async Task ValidateParentChainBlock_WithTooManyProvidedData()
        {
            int chainId = 123;
            var parentIdHeight = new Dictionary<int, ulong> {{chainId, 1}};
            var crossChainDataProvider = CreateNewCrossChainDataProvider(new Dictionary<int, List<IBlockInfo>>(),
                new Dictionary<int, ulong>(), parentIdHeight);

            var list = new List<ParentChainBlockData>();
            for (int i = 0; i <= CrossChainConsts.MaximalCountForIndexingParentChainBlock; i++)
            {
                list.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId,
                        ParentChainHeight = (ulong) (i + 1)
                    }
                });
            }

            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(list, Hash.Default, 1, true);
            Assert.False(res);
        }

        [Fact]
        public async Task ValidateParentChainBlock__WithWrongIndex()
        {
            int chainId = 123;
            var parentIdHeight = new Dictionary<int, ulong> {{chainId, 1}};

            var blockInfoCache = new List<IBlockInfo>();
            var cachingCount = 5;
            for (int i = 0; i < cachingCount; i++)
            {
                blockInfoCache.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId,
                        ParentChainHeight = (ulong) (i + 1)
                    }
                });
            }

            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            var crossChainDataProvider = CreateNewCrossChainDataProvider(fakeCache,
                new Dictionary<int, ulong>(), parentIdHeight);

            var list = new List<ParentChainBlockData>
            {
                new ParentChainBlockData
                    {Root = new ParentChainBlockRootInfo {ParentChainId = chainId, ParentChainHeight = 2}}
            };
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(list, Hash.Default, 1, true);
            Assert.False(res);
        }

        [Fact]
        public async Task GetParentChainBlock_WithWrongIndex()
        {
            int chainId = 123;
            var parentIdHeight = new Dictionary<int, ulong> {{chainId, 1}};

            var blockInfoCache = new List<IBlockInfo>();
            var cachingCount = CrossChainConsts.MinimalBlockInfoCacheThreshold;
            for (int i = 0; i < cachingCount + 1; i++)
            {
                blockInfoCache.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId,
                        ParentChainHeight = (ulong) (i + 1)
                    }
                });
            }

            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            var crossChainDataProvider = CreateNewCrossChainDataProvider(fakeCache,
                new Dictionary<int, ulong>(), parentIdHeight);

            var list = new List<ParentChainBlockData>
            {
                new ParentChainBlockData
                    {Root = new ParentChainBlockRootInfo {ParentChainId = chainId, ParentChainHeight = 1}}
            };
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(list, Hash.Default, 1);
            Assert.False(res);
        }

        [Fact]
        public async Task GetParentBlock_Single()
        {
            int chainId = 123;
            var parentIdHeight = new Dictionary<int, ulong> {{chainId, 0}};

            var blockInfoCache = new List<IBlockInfo>();
            var cachingCount = CrossChainConsts.MinimalBlockInfoCacheThreshold + 1;
            for (int i = 0; i < cachingCount; i++)
            {
                blockInfoCache.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId,
                        ParentChainHeight = (ulong) (i + 1)
                    }
                });
            }

            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            var crossChainDataProvider = CreateNewCrossChainDataProvider(fakeCache,
                new Dictionary<int, ulong>(), parentIdHeight);

            var list = new List<ParentChainBlockData>();
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(list, Hash.Default, 1);
            Assert.True(res);
            var expectedResultCount = cachingCount - CrossChainConsts.MinimalBlockInfoCacheThreshold;
            Assert.True(list.Count == expectedResultCount);
        }

        [Fact]
        public async Task GetParentChainBlock_Multiple()
        {
            int chainId = 123;
            var parentIdHeight = new Dictionary<int, ulong> {{chainId, 0}};
            var blockInfoCache = new List<IBlockInfo>();

            var cachingCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock;
            for (int i = 0; i < cachingCount; i++)
            {
                blockInfoCache.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId,
                        ParentChainHeight = (ulong) (i + 1)
                    }
                });
            }

            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            var crossChainDataProvider = CreateNewCrossChainDataProvider(fakeCache,
                new Dictionary<int, ulong>(), parentIdHeight);

            var list = new List<ParentChainBlockData>();
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(list, Hash.Default, 1);
            Assert.True(res);
            var expectedResultCount = cachingCount - CrossChainConsts.MinimalBlockInfoCacheThreshold;
            Assert.True(list.Count == expectedResultCount);
        }

        [Fact]
        public async Task GetParentChainBlock_WithCountLimit()
        {
            int chainId = 123;
            var blockInfoCache = new List<IBlockInfo>();
            var parentIdHeight = new Dictionary<int, ulong> {{chainId, 0}};

            var cachingCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock +
                               CrossChainConsts.MinimalBlockInfoCacheThreshold;
            for (int i = 0; i < cachingCount; i++)
            {
                blockInfoCache.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId,
                        ParentChainHeight = (ulong) (i + 1)
                    }
                });
            }

            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            var crossChainDataProvider = CreateNewCrossChainDataProvider(fakeCache,
                new Dictionary<int, ulong>(), parentIdHeight);

            var list = new List<ParentChainBlockData>();
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(list, Hash.Default, 1);
            Assert.True(res);
            var expectedResultCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock;
            Assert.True(list.Count == expectedResultCount);
        }

        [Fact]
        public async Task TryTwice_GetParentChainBlock_WithCountLimit()
        {
            int chainId = 123;
            var blockInfoCache = new List<IBlockInfo>();
            var parentIdHeight = new Dictionary<int, ulong> {{chainId, 0}};

            var cachingCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock +
                               CrossChainConsts.MinimalBlockInfoCacheThreshold;
            for (int i = 0; i <= cachingCount; i++)
            {
                blockInfoCache.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId,
                        ParentChainHeight = (ulong) (i + 1)
                    }
                });
            }

            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            var crossChainDataProvider = CreateNewCrossChainDataProvider(fakeCache,
                new Dictionary<int, ulong>(), parentIdHeight);

            var list = new List<ParentChainBlockData>();
            await crossChainDataProvider.GetParentChainBlockDataAsync(list, Hash.Default, 1);
            list = new List<ParentChainBlockData>();
            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(list, Hash.Default, 1);
            Assert.True(res);
            var expectedResultCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock;
            Assert.True(list.Count == expectedResultCount);
        }

        [Fact]
        public async Task TryTwice_ValidateParentChainBlock()
        {
            int chainId = 123;
            var blockInfoCache = new List<IBlockInfo>();
            var parentIdHeight = new Dictionary<int, ulong> {{chainId, 1}};

            var cachingCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock +
                               CrossChainConsts.MinimalBlockInfoCacheThreshold;
            for (int i = 0; i <= cachingCount; i++)
            {
                blockInfoCache.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId,
                        ParentChainHeight = (ulong) (i + 1)
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
                        ParentChainHeight = (ulong) (i + 1)
                    }
                });
            }

            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            var crossChainDataProvider = CreateNewCrossChainDataProvider(fakeCache,
                new Dictionary<int, ulong>(), parentIdHeight);

            await crossChainDataProvider.GetParentChainBlockDataAsync(list, Hash.Default, 1, true);
            list = new List<ParentChainBlockData>();
            for (int i = 0; i < CrossChainConsts.MaximalCountForIndexingParentChainBlock; i++)
            {
                list.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = chainId,
                        ParentChainHeight = (ulong) (i + 1)
                    }
                });
            }

            var res = await crossChainDataProvider.GetParentChainBlockDataAsync(list, Hash.Default, 1, true);
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
            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{chainId, blockInfoCache}};
            var crossChainDataProvider =
                CreateNewCrossChainDataProvider(fakeCache, new Dictionary<int, ulong>(), new Dictionary<int, ulong>());
            var res = await crossChainDataProvider.ActivateCrossChainCacheAsync(Hash.Default, 1);

            Assert.False(res);
        }


        [Fact]
        public async Task Activate_Cache_WithoutChainExist()
        {
            var fakeCache = new Dictionary<int, List<IBlockInfo>>();
            var crossChainDataProvider =
                CreateNewCrossChainDataProvider(fakeCache, new Dictionary<int, ulong>(), new Dictionary<int, ulong>());
            var res = await crossChainDataProvider.ActivateCrossChainCacheAsync(Hash.Default, 1);

            Assert.True(res);
        }
    }
}