using System.Collections.Generic;
using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Cache;
using AElf.CrossChain.Indexing.Application;
using AElf.Kernel.Blockchain.Application;
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
        public async Task Validate_WithoutProvidedSideChainBlockData_Test()
        {
            int chainId = _chainOptions.ChainId;
            var blockInfoCache = new List<IBlockCacheEntity>();
            for (int i = 0; i <= CrossChainConstants.MinimalBlockCacheEntityCount; i++)
            {
                blockInfoCache.Add(new SideChainBlockData
                {
                    Height = 1 + ),
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
            var res = await _crossChainIndexingDataValidationService.ValidateCrossChainIndexingData(crossChainBlockData, Hash.Empty, 1);
            Assert.True(res);
            Assert.True(list.Count == 1);
        }
        
        /*
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
            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 1);
            var res = await _crossChainIndexingDataService.ValidateSideChainBlockDataAsync(list, Hash.Empty, 1);
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
            await Assert.ThrowsAsync<ValidateNextTimeBlockValidationException>(() =>
                _crossChainIndexingDataService.ValidateSideChainBlockDataAsync(list, Hash.Empty, 1));
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
            var res = await _crossChainIndexingDataService.ValidateSideChainBlockDataAsync(list, Hash.Empty, 1);
            Assert.False(res);
            Assert.True(list.Count == 1);
        }
        
        [Fact]
        public async Task ValidateSideChainBlock__NotExcepted_Test()
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
            var res = await _crossChainIndexingDataService.ValidateSideChainBlockDataAsync(list, Hash.Empty, 1);
            Assert.False(res);
            Assert.True(list.Count == 1);
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

            var list = new List<SideChainBlockData>();
            for (int i = 0; i < _configOptions.MaximalCountForIndexingSideChainBlock; i++)
            {
                list.Add(new SideChainBlockData
                {
                    ChainId = chainId, 
                    Height = (i + 1)
                });
            }

            var fakeCache = new Dictionary<int, List<IBlockCacheEntity>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            await _crossChainIndexingDataService.ValidateSideChainBlockDataAsync(list, Hash.Empty, 1);
            list = new List<SideChainBlockData>();
            for (int i = 0; i < _configOptions.MaximalCountForIndexingSideChainBlock; i++)
            {
                list.Add(new SideChainBlockData
                {
                    ChainId = chainId, 
                    Height = (i + 1)
                });
            }

            var res = await _crossChainIndexingDataService.ValidateSideChainBlockDataAsync(list, Hash.Empty, 2);
            Assert.True(res);
            var expectedResultCount = _configOptions.MaximalCountForIndexingSideChainBlock;
            Assert.True(list.Count == expectedResultCount);
        }

        [Fact]
        public async Task ValidateParentChainBlock_WithoutProvidedData_Test()
        {
            int chainId = _chainOptions.ChainId;
            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 0);

            var list = new List<ParentChainBlockData>();
            var res = await _crossChainIndexingDataService.ValidateParentChainBlockDataAsync(list, Hash.Empty, 1);
            Assert.True(res);
            Assert.True(list.Count == 0);
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
            var res = await _crossChainIndexingDataService.ValidateParentChainBlockDataAsync(list, Hash.Empty, 1);
            Assert.True(res);
            Assert.True(list.Count == _configOptions.MaximalCountForIndexingParentChainBlock);
        }
        
        [Fact]
        public async Task ValidateParentChainBlock_WithoutCaching_Test()
        {
            int chainId = _chainOptions.ChainId;
            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 1);

            var list = new List<ParentChainBlockData>();
            for (int i = 0; i <= _configOptions.MaximalCountForIndexingParentChainBlock; i++)
            {
                list.Add(new ParentChainBlockData
                {
                    ChainId = chainId, 
                    Height = (i + 1)
                });
            }

            await Assert.ThrowsAsync<ValidateNextTimeBlockValidationException>(() =>
                _crossChainIndexingDataService.ValidateParentChainBlockDataAsync(list, Hash.Empty, 1));
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
            var res = await _crossChainIndexingDataService.ValidateParentChainBlockDataAsync(list, Hash.Empty, 1);
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

            await _crossChainIndexingDataService.ValidateParentChainBlockDataAsync(list, Hash.Empty, 1);
            list = new List<ParentChainBlockData>();
            for (int i = 0; i < _configOptions.MaximalCountForIndexingParentChainBlock; i++)
            {
                list.Add(new ParentChainBlockData
                {
                    ChainId = chainId, 
                    Height = (i + 1)
                });
            }

            var res = await _crossChainIndexingDataService.ValidateParentChainBlockDataAsync(list, Hash.Empty, 2);
            Assert.True(res);
            var expectedResultCount = _configOptions.MaximalCountForIndexingParentChainBlock;
            Assert.True(list.Count == expectedResultCount);
        }
        */
    }
}