using System.Collections.Generic;
using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Cache;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Xunit;

namespace AElf.CrossChain
{
    public class CrossChainIndexingDataServiceTest : CrossChainTestBase
    {
        private readonly ICrossChainIndexingDataService _crossChainIndexingDataService;
        private readonly CrossChainTestHelper _crossChainTestHelper;

        public CrossChainIndexingDataServiceTest()
        {
            _crossChainIndexingDataService = GetRequiredService<ICrossChainIndexingDataService>();
            _crossChainTestHelper = GetRequiredService<CrossChainTestHelper>();
        }

        #region Side chain

        [Fact]
        public async Task Validate_Without_ProvidedSideChainBlockData()
        {
            int chainId = 123;
            var blockInfoCache = new List<IBlockCacheEntity>();
            for (int i = 0; i <= CrossChainConstants.MinimalBlockCacheEntityCount; i++)
            {
                blockInfoCache.Add(new SideChainBlockData
                {
                    Height = (1 + i),
                    ChainId = chainId
                });
            }

            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 0);
            var fakeCache = new Dictionary<int, List<IBlockCacheEntity>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            var list = new List<SideChainBlockData>();
            var res = await _crossChainIndexingDataService.ValidateSideChainBlockDataAsync(list, Hash.Empty, 1);
            Assert.True(res);
        }

        [Fact]
        public async Task ValidateSideChainBlock_WithCaching()
        {
            int chainId = 123;
            var blockInfoCache = new List<IBlockCacheEntity>
            {
                new SideChainBlockData {ChainId = chainId, Height = 1}
            };
            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 0);

            var fakeCache = new Dictionary<int, List<IBlockCacheEntity>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);
            
            var list = new List<SideChainBlockData>
            {
                new SideChainBlockData
                {
                    ChainId = chainId,
                    Height = 1
                }
            };
            var res = await _crossChainIndexingDataService.ValidateSideChainBlockDataAsync(list, Hash.Empty, 1);
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
                    ChainId = chainId,
                    Height = 1
                }
            };
            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 1);
            var res = await _crossChainIndexingDataService.ValidateSideChainBlockDataAsync(list, Hash.Empty, 1);
            Assert.False(res);
        }

        [Fact]
        public async Task ValidateSideChainBlock_WithWrongBlockIndex()
        {
            int chainId = 123;
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
        public async Task ValidateSideChainBlock__NotEnoughCaching()
        {
            int chainId = 123;
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
        public async Task TryTwice_ValidateSideChainBlock()
        {
            int chainId = 123;
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
        public async Task ValidateParentChainBlock_WithoutProvidedData()
        {
            int chainId = 123;
            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 0);

            var list = new List<ParentChainBlockData>();
            var res = await _crossChainIndexingDataService.ValidateParentChainBlockDataAsync(list, Hash.Empty, 1);
            Assert.True(res);
            Assert.True(list.Count == 0);
        }

        [Fact]
        public async Task ValidateParentChainBlock_WithCaching()
        {
            int chainId = 123;
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
        public async Task ValidateParentChainBlock_WithoutCaching()
        {
            int chainId = 123;
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
        public async Task ValidateParentChainBlock_WithWrongIndex()
        {
            int chainId = 123;
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
        public async Task TryTwice_ValidateParentChainBlock()
        {
            int chainId = 123;
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
        
        [Fact]
        public async Task GetIndexedCrossChainBlockData_WithIndex()
        {
            var chainId = 123;
            var fakeMerkleTreeRoot1 = Hash.FromString("fakeMerkleTreeRoot1");
            var fakeSideChainBlockData = new SideChainBlockData
            {
                Height = 1,
                ChainId = chainId,
                TransactionMerkleTreeRoot = fakeMerkleTreeRoot1
            };
            
            var fakeIndexedCrossChainBlockData = new CrossChainBlockData();
            fakeIndexedCrossChainBlockData.SideChainBlockData.AddRange(new []{fakeSideChainBlockData});
            
            _crossChainTestHelper.AddFakeIndexedCrossChainBlockData(fakeSideChainBlockData.Height, fakeIndexedCrossChainBlockData);
            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 0);
            
            AddFakeCacheData(new Dictionary<int, List<IBlockCacheEntity>>
            {
                {
                    chainId,
                    new List<IBlockCacheEntity>
                    {
                        fakeSideChainBlockData
                    }
                }
            });

            var res = await _crossChainIndexingDataService.GetIndexedCrossChainBlockDataAsync(
                fakeSideChainBlockData.BlockHeaderHash, 1);
            Assert.True(res.SideChainBlockData[0].Height == fakeSideChainBlockData.Height);
            Assert.True(res.SideChainBlockData[0].ChainId == chainId);
        }

        [Fact]
        public async Task GetIndexedCrossChainBlockData_WithoutIndex()
        {
            var chainId = 123;
            var fakeSideChainBlockData = new SideChainBlockData
            {
                Height = 1,
                ChainId = chainId
            };
            
            var fakeIndexedCrossChainBlockData = new CrossChainBlockData();
            fakeIndexedCrossChainBlockData.SideChainBlockData.AddRange(new []{fakeSideChainBlockData});
            
            var res = await _crossChainIndexingDataService.GetIndexedCrossChainBlockDataAsync(
                fakeSideChainBlockData.BlockHeaderHash, 1);
            Assert.True(res == null);
        }
        
        [Fact]
        public async Task GetCrossChainBlockDataForNextMining()
        {
            var parentChainId = 123;
            var sideChainId = 456;
            var parentBlockInfoCache = new List<IBlockCacheEntity>();
            var cachingCount = 5;
            for (int i = 0; i < cachingCount + CrossChainConstants.MinimalBlockCacheEntityCount; i++)
            {
                parentBlockInfoCache.Add(new SideChainBlockData()
                {
                    ChainId = sideChainId, 
                    Height = (i + 1),
                });
            }
            _crossChainTestHelper.AddFakeSideChainIdHeight(sideChainId, 1);
            var parentFakeCache = new Dictionary<int, List<IBlockCacheEntity>> {{sideChainId, parentBlockInfoCache}};
            AddFakeCacheData(parentFakeCache);
           
            var sideBlockInfoCache = new List<IBlockCacheEntity>();
            for (int i = 0; i < cachingCount + CrossChainConstants.MinimalBlockCacheEntityCount; i++)
            {
                sideBlockInfoCache.Add(new ParentChainBlockData()
                {
                    ChainId = sideChainId, 
                    Height = (i + 1),
                });
            }
            _crossChainTestHelper.AddFakeParentChainIdHeight(sideChainId, 1);
            var sideFakeCache = new Dictionary<int, List<IBlockCacheEntity>> {{sideChainId, sideBlockInfoCache}};
            AddFakeCacheData(sideFakeCache);
            
            var res = await _crossChainIndexingDataService.GetCrossChainBlockDataForNextMiningAsync(Hash.Empty, 1);
            Assert.True(res.ParentChainBlockData.Count == 4);
            Assert.True(res.SideChainBlockData.Count == 4);
            Assert.True(res.PreviousBlockHeight == 1);
        }
        
        [Fact]
        public async Task GetCrossChainBlockDataForNextMining_WithoutCachingParentBlock()
        {
            var chainId = 123;
            var blockInfoCache = new List<IBlockCacheEntity>();
            var cachingCount = 5;
            for (int i = 0; i < cachingCount + CrossChainConstants.MinimalBlockCacheEntityCount; i++)
            {
                blockInfoCache.Add(new SideChainBlockData()
                {
                    ChainId = chainId, 
                    Height = (i + 1),
                });
            }
            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 1);
            var fakeCache = new Dictionary<int, List<IBlockCacheEntity>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);
           
            
            var res = await _crossChainIndexingDataService.GetCrossChainBlockDataForNextMiningAsync(Hash.Empty, 1);
            Assert.True(res.ParentChainBlockData.Count == 0);
            Assert.True(res.SideChainBlockData.Count == 4);
            Assert.True(res.PreviousBlockHeight == 1);
        }
        
        [Fact]
        public async Task GetCrossChainBlockDataForNextMining_WithoutCachingSideBlock()
        {
            var chainId = 123;
            var blockInfoCache = new List<IBlockCacheEntity>();
            var cachingCount = 5;
            for (int i = 0; i < cachingCount + CrossChainConstants.MinimalBlockCacheEntityCount; i++)
            {
                blockInfoCache.Add(new ParentChainBlockData()
                {
                    ChainId = chainId, 
                    Height = (i + 1),
                });
            }
            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 1);
            var fakeCache = new Dictionary<int, List<IBlockCacheEntity>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);
            
            var res = await _crossChainIndexingDataService.GetCrossChainBlockDataForNextMiningAsync(Hash.Empty, 1);
            Assert.True(res.ParentChainBlockData.Count == 4);
            Assert.True(res.SideChainBlockData.Count == 0);
            Assert.True(res.PreviousBlockHeight == 1);
        }
        
        [Fact]
        public async Task GetCrossChainBlockDataForNextMining_WithoutCaching()
        {
            var res = await _crossChainIndexingDataService.GetCrossChainBlockDataForNextMiningAsync(Hash.Empty, 1);
            Assert.True(res == null);
        }

        #endregion
    }
}