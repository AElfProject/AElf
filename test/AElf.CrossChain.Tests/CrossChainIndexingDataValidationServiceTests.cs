using System.Collections.Generic;
using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Cache;
using AElf.CrossChain.Indexing.Application;
using AElf.Types;
using Xunit;

namespace AElf.CrossChain
{
    public class CrossChainIndexingDataValidationServiceTests : CrossChainTestBase
    {
        private readonly CrossChainTestHelper _crossChainTestHelper;
        private readonly ICrossChainIndexingDataValidationService _crossChainIndexingDataValidationService;

        public CrossChainIndexingDataValidationServiceTests()
        {
            _crossChainTestHelper = GetRequiredService<CrossChainTestHelper>();
            _crossChainIndexingDataValidationService = GetRequiredService<ICrossChainIndexingDataValidationService>();
        }

        [Fact]
        public async Task Validate_WithoutEmptyInput_Test()
        {
            int chainId = _chainOptions.ChainId;
            var blockInfoCache = new List<IBlockCacheEntity>();
            for (int i = 0; i <= CrossChainConstants.MinimalBlockCacheEntityCount; i++)
            {
                blockInfoCache.Add(new SideChainBlockData
                {
                    Height = 1 + i,
                    ChainId = chainId
                });
            }

            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 0);
            var fakeCache = new Dictionary<int, List<IBlockCacheEntity>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            var crossChainBlockData = new CrossChainBlockData();
            var res = await _crossChainIndexingDataValidationService.ValidateCrossChainIndexingData(crossChainBlockData,
                Hash.Empty, 1);
            Assert.True(res);
        }


        [Fact]
        public async Task ValidateSideChainBlock_WithCaching_Test()
        {
            int chainId = _chainOptions.ChainId;
            var blockInfoCache = new List<IBlockCacheEntity>
            {
                new SideChainBlockData {ChainId = chainId, Height = 1}
            };
            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 0);

            var fakeCache = new Dictionary<int, List<IBlockCacheEntity>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockData =
                {
                    new SideChainBlockData
                    {
                        ChainId = chainId,
                        Height = 1
                    }
                }
            };
            var res = await _crossChainIndexingDataValidationService.ValidateCrossChainIndexingData(crossChainBlockData,
                Hash.Empty, 1);
            Assert.True(res);
        }

        [Fact]
        public async Task ValidateSideChainBlock_WithoutCaching_Test()
        {
            int chainId = _chainOptions.ChainId;

            var list = new List<SideChainBlockData>
            {
                new SideChainBlockData
                {
                    ChainId = chainId,
                    Height = 1
                }
            };
            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockData = {list}
            };
            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 1);
            var res = await _crossChainIndexingDataValidationService.ValidateCrossChainIndexingData(crossChainBlockData,
                Hash.Empty, 1);
            Assert.False(res);
        }


        [Fact]
        public async Task ValidateSideChainBlock_WithWrongBlockIndex_Test()
        {
            int chainId = _chainOptions.ChainId;
            var blockInfoCache = new List<IBlockCacheEntity>
            {
                new SideChainBlockData {ChainId = chainId, Height = 1}
            };

            var fakeCache = new Dictionary<int, List<IBlockCacheEntity>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);
            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 1);
            var list = new List<SideChainBlockData>
            {
                new SideChainBlockData
                {
                    ChainId = chainId,
                    Height = 2
                }
            };
            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockData = {list}
            };
            var res = await _crossChainIndexingDataValidationService.ValidateCrossChainIndexingData(crossChainBlockData,
                Hash.Empty, 1);
            Assert.False(res);
        }


        [Fact]
        public async Task ValidateSideChainBlock__NotEnoughCaching_Test()
        {
            int chainId = _chainOptions.ChainId;
            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 1);

            var list = new List<SideChainBlockData>
            {
                new SideChainBlockData
                {
                    ChainId = chainId,
                    Height = 1
                }
            };
            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockData = {list}
            };
            var res = await _crossChainIndexingDataValidationService.ValidateCrossChainIndexingData(crossChainBlockData,
                Hash.Empty, 1);
            Assert.False(res);
        }


        [Fact]
        public async Task ValidateSideChainBlock__NotExpected_Test()
        {
            int chainId = _chainOptions.ChainId;
            var blockInfoCache = new List<IBlockCacheEntity>
            {
                new SideChainBlockData
                {
                    ChainId = chainId,
                    Height = 1,
                    BlockHeaderHash = Hash.FromString("blockHash")
                }
            };
            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 0);
            var fakeCache = new Dictionary<int, List<IBlockCacheEntity>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            var list = new List<SideChainBlockData>
            {
                new SideChainBlockData
                {
                    ChainId = chainId,
                    Height = 1,
                    BlockHeaderHash = Hash.FromString("Block")
                }
            };
            var crossChainBlockData = FakeCrossChainBlockData(list, new ParentChainBlockData[0]);
            var res = await _crossChainIndexingDataValidationService.ValidateCrossChainIndexingData(crossChainBlockData,
                Hash.Empty, 1);
            Assert.False(res);
        }

        [Fact]
        public async Task TryTwice_ValidateSideChainBlock_Test()
        {
            int chainId = _chainOptions.ChainId;
            var blockInfoCache = new List<IBlockCacheEntity>();
            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 0);

            var cachingCount = _configOptions.MaximalCountForIndexingParentChainBlock +
                               CrossChainConstants.MinimalBlockCacheEntityCount;
            for (int i = 0; i <= cachingCount; i++)
            {
                blockInfoCache.Add(new SideChainBlockData()
                {
                    ChainId = chainId,
                    Height = (i + 1)
                });
            }

            var fakeCache = new Dictionary<int, List<IBlockCacheEntity>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);
            {
                var list = new List<SideChainBlockData>();
                for (int i = 0; i < _configOptions.MaximalCountForIndexingSideChainBlock; i++)
                {
                    list.Add(new SideChainBlockData
                    {
                        ChainId = chainId,
                        Height = (i + 1)
                    });
                }

                var crossChainBlockData = FakeCrossChainBlockData(list, new ParentChainBlockData[0]);
                var res = await _crossChainIndexingDataValidationService.ValidateCrossChainIndexingData(
                    crossChainBlockData,
                    Hash.Empty, 1);
                Assert.True(res);
            }
            {
                var list = new List<SideChainBlockData>();
                for (int i = 0; i < _configOptions.MaximalCountForIndexingSideChainBlock; i++)
                {
                    list.Add(new SideChainBlockData
                    {
                        ChainId = chainId,
                        Height = (i + 1)
                    });
                }

                var crossChainBlockData = FakeCrossChainBlockData(list, new ParentChainBlockData[0]);
                var res = await _crossChainIndexingDataValidationService.ValidateCrossChainIndexingData(
                    crossChainBlockData,
                    Hash.Empty, 2);
                Assert.True(res);
            }
        }

        [Fact]
        public async Task ValidateParentChainBlock_WithoutProvidedData_Test()
        {
            int chainId = _chainOptions.ChainId;
            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 0);

            var crossChainBlockData = FakeCrossChainBlockData(new SideChainBlockData[0], new ParentChainBlockData[0]);
            var res = await _crossChainIndexingDataValidationService.ValidateCrossChainIndexingData(crossChainBlockData,
                Hash.Empty, 1);
            Assert.True(res);
        }

        [Fact]
        public async Task ValidateParentChainBlock_WithCaching_Test()
        {
            int chainId = _chainOptions.ChainId;
            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 0);

            var blockInfoCache = new List<IBlockCacheEntity>();
            for (int i = 0; i < _configOptions.MaximalCountForIndexingParentChainBlock; i++)
            {
                blockInfoCache.Add(new ParentChainBlockData
                {
                    ChainId = chainId,
                    Height = (i + 1)
                });
            }

            var fakeCache = new Dictionary<int, List<IBlockCacheEntity>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            var list = new List<ParentChainBlockData>();
            for (int i = 0; i < _configOptions.MaximalCountForIndexingParentChainBlock; i++)
            {
                list.Add(new ParentChainBlockData
                {
                    ChainId = chainId,
                    Height = (i + 1)
                });
            }

            var crossChainBlockData = FakeCrossChainBlockData(new SideChainBlockData[0], list);
            var res = await _crossChainIndexingDataValidationService.ValidateCrossChainIndexingData(crossChainBlockData,
                Hash.Empty, 1);
            Assert.True(res);
        }

        [Fact]
        public async Task ValidateParentChainBlock_WithoutCaching_Test()
        {
            int parentChainId = _chainOptions.ChainId;
            _crossChainTestHelper.AddFakeParentChainIdHeight(parentChainId, 1);

            var list = new List<ParentChainBlockData>();
            for (int i = 0; i <= _configOptions.MaximalCountForIndexingParentChainBlock; i++)
            {
                list.Add(new ParentChainBlockData
                {
                    ChainId = parentChainId,
                    Height = (i + 1)
                });
            }

            var crossChainBlockData = FakeCrossChainBlockData(new SideChainBlockData[0], list);
            var res = await _crossChainIndexingDataValidationService.ValidateCrossChainIndexingData(crossChainBlockData,
                Hash.Empty, 1);
            Assert.False(res);
        }

        [Fact]
        public async Task ValidateParentChainBlock_WithWrongIndex_Test()
        {
            int chainId = _chainOptions.ChainId;
            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 0);
            var blockInfoCache = new List<IBlockCacheEntity>();
            var cachingCount = 5;
            for (int i = 0; i < cachingCount; i++)
            {
                blockInfoCache.Add(new ParentChainBlockData
                {
                    ChainId = chainId,
                    Height = (i + 1)
                });
            }

            var fakeCache = new Dictionary<int, List<IBlockCacheEntity>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            var list = new List<ParentChainBlockData>
            {
                new ParentChainBlockData
                    {ChainId = chainId, Height = 2}
            };
            var crossChainBlockData = FakeCrossChainBlockData(new SideChainBlockData[0], list);
            var res = await _crossChainIndexingDataValidationService.ValidateCrossChainIndexingData(crossChainBlockData,
                Hash.Empty, 1);
            Assert.False(res);
        }

        [Fact]
        public async Task TryTwice_ValidateParentChainBlock_Test()
        {
            int chainId = _chainOptions.ChainId;
            var blockInfoCache = new List<IBlockCacheEntity>();
            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 0);

            var cachingCount = _configOptions.MaximalCountForIndexingParentChainBlock +
                               CrossChainConstants.MinimalBlockCacheEntityCount;
            for (int i = 0; i <= cachingCount; i++)
            {
                blockInfoCache.Add(new ParentChainBlockData
                {
                    ChainId = chainId,
                    Height = (i + 1)
                });
            }

            var list = new List<ParentChainBlockData>();
            for (int i = 0; i < _configOptions.MaximalCountForIndexingParentChainBlock; i++)
            {
                list.Add(new ParentChainBlockData
                {
                    ChainId = chainId,
                    Height = (i + 1)
                });
            }

            var fakeCache = new Dictionary<int, List<IBlockCacheEntity>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);
            {
                var crossChainBlockData = FakeCrossChainBlockData(new SideChainBlockData[0], list);
                var res = await _crossChainIndexingDataValidationService.ValidateCrossChainIndexingData(
                    crossChainBlockData,
                    Hash.Empty, 1);
                list = new List<ParentChainBlockData>();
                for (int i = 0; i < _configOptions.MaximalCountForIndexingParentChainBlock; i++)
                {
                    list.Add(new ParentChainBlockData
                    {
                        ChainId = chainId,
                        Height = (i + 1)
                    });
                }

                Assert.True(res);
            }
            {
                var crossChainBlockData = FakeCrossChainBlockData(new SideChainBlockData[0], list);
                var res = await _crossChainIndexingDataValidationService.ValidateCrossChainIndexingData(
                    crossChainBlockData,
                    Hash.Empty, 2);
                Assert.True(res);
            }
        }

        private CrossChainBlockData FakeCrossChainBlockData(IEnumerable<SideChainBlockData> sideChainBlockDataList,
            IEnumerable<ParentChainBlockData> parentChainBlockDataList)
        {
            return new CrossChainBlockData
            {
                SideChainBlockData = {sideChainBlockDataList},
                ParentChainBlockData = {parentChainBlockDataList}
            };
        }
    }
}