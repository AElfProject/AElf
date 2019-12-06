using System.Collections.Generic;
using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Cache;
using AElf.CrossChain.Indexing.Application;
using AElf.Types;
using Xunit;

namespace AElf.CrossChain
{
    public class CrossChainIndexingDataServiceTests : CrossChainTestBase
    {
        private readonly ICrossChainIndexingDataService _crossChainIndexingDataService;
        private readonly CrossChainTestHelper _crossChainTestHelper;

        public CrossChainIndexingDataServiceTests()
        {
            _crossChainIndexingDataService = GetRequiredService<ICrossChainIndexingDataService>();
            _crossChainTestHelper = GetRequiredService<CrossChainTestHelper>();
        }

        #region Side chain
        
        [Fact]
        public async Task GetIndexedCrossChainBlockData_WithIndex_Test()
        {
            var chainId = _chainOptions.ChainId;
            var fakeMerkleTreeRoot1 = Hash.FromString("fakeMerkleTreeRoot1");
            var fakeSideChainBlockData = new SideChainBlockData
            {
                Height = 1,
                ChainId = chainId,
                TransactionStatusMerkleTreeRoot = fakeMerkleTreeRoot1
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
        public async Task GetIndexedCrossChainBlockData_WithoutIndex_Test()
        {
            var chainId = _chainOptions.ChainId;
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
        /*

   [Fact]
   public async Task GetCrossChainBlockDataForNextMining_Test()
   {
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
   public async Task GetCrossChainBlockDataForNextMining_WithoutCachingParentBlock_Test()
   {
       var chainId = _chainOptions.ChainId;
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
   public async Task GetCrossChainBlockDataForNextMining_WithoutCachingSideBlock_Test()
   {
       var chainId = _chainOptions.ChainId;
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
   public async Task GetCrossChainBlockDataForNextMining_WithoutCaching_Test()
   {
       var res = await _crossChainIndexingDataService.GetCrossChainBlockDataForNextMiningAsync(Hash.Empty, 1);
       Assert.True(res == null);
   }
*/
        #endregion
    }
}