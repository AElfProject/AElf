using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs7;
using AElf.Contracts.CrossChain;
using AElf.CrossChain.Cache;
using AElf.CrossChain.Indexing.Application;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
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
            fakeIndexedCrossChainBlockData.SideChainBlockDataList.AddRange(new[] {fakeSideChainBlockData});

            _crossChainTestHelper.AddFakeIndexedCrossChainBlockData(fakeSideChainBlockData.Height,
                fakeIndexedCrossChainBlockData);
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
            Assert.True(res.SideChainBlockDataList[0].Height == fakeSideChainBlockData.Height);
            Assert.True(res.SideChainBlockDataList[0].ChainId == chainId);
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
            fakeIndexedCrossChainBlockData.SideChainBlockDataList.AddRange(new[] {fakeSideChainBlockData});

            var res = await _crossChainIndexingDataService.GetIndexedCrossChainBlockDataAsync(
                fakeSideChainBlockData.BlockHeaderHash, 1);
            Assert.True(res == null);
        }

        [Fact]
        public async Task PrepareExtraDataForNextMiningAsync_NoProposal_FirstTimeIndexing_Test()
        {
            var sideChainId = 123;
            var sideChainBlockInfoCache = new List<IBlockCacheEntity>();
            var cachingCount = 5;
            for (int i = 0; i < cachingCount + CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                sideChainBlockInfoCache.Add(new SideChainBlockData()
                {
                    ChainId = sideChainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = Hash.FromString((sideChainId + 1).ToString())
                });
            }

            _crossChainTestHelper.AddFakeSideChainIdHeight(sideChainId, 0);
            var fakeCache = new Dictionary<int, List<IBlockCacheEntity>> {{sideChainId, sideChainBlockInfoCache}};
            AddFakeCacheData(fakeCache);

            var res = await _crossChainIndexingDataService.PrepareExtraDataForNextMiningAsync(Hash.Empty, 1);
            Assert.Empty(res);
            var crossChainTransactionInput =
                await _crossChainIndexingDataService.GetCrossChainTransactionInputForNextMiningAsync(Hash.Empty, 1);
            Assert.NotNull(crossChainTransactionInput);
            var crossChainBlockData = CrossChainBlockData.Parser.ParseFrom(crossChainTransactionInput.Value);
            Assert.Single(crossChainBlockData.SideChainBlockDataList);
            Assert.Equal(sideChainBlockInfoCache.First().ToByteString(),
                crossChainBlockData.SideChainBlockDataList.First().ToByteString());
        }

        [Fact]
        public async Task PrepareExtraDataForNextMiningAsync_NoProposal_Test()
        {
            var sideChainId = 123;
            var sideChainBlockInfoCache = new List<IBlockCacheEntity>();
            var cachingCount = 5;
            for (int i = 1; i < cachingCount + CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                sideChainBlockInfoCache.Add(new SideChainBlockData()
                {
                    ChainId = sideChainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = Hash.FromString((sideChainId + 1).ToString())
                });
            }

            _crossChainTestHelper.AddFakeSideChainIdHeight(sideChainId, 1);
            var fakeCache = new Dictionary<int, List<IBlockCacheEntity>> {{sideChainId, sideChainBlockInfoCache}};
            AddFakeCacheData(fakeCache);

            var res = await _crossChainIndexingDataService.PrepareExtraDataForNextMiningAsync(Hash.Empty, 1);
            Assert.Empty(res);
            var crossChainTransactionInput =
                await _crossChainIndexingDataService.GetCrossChainTransactionInputForNextMiningAsync(Hash.Empty, 1);
            Assert.NotNull(crossChainTransactionInput);
            var crossChainBlockData = CrossChainBlockData.Parser.ParseFrom(crossChainTransactionInput.Value);

            Assert.Equal(CrossChainConstants.DefaultBlockCacheEntityCount,
                crossChainBlockData.SideChainBlockDataList.Count);
        }

        [Fact]
        public async Task PrepareExtraDataForNextMiningAsync_NotApproved_Test()
        {
            var sideChainId = 123;
            var sideChainBlockInfoCache = new List<SideChainBlockData>();
            var cachingCount = 5;
            for (int i = 0; i < cachingCount + CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                sideChainBlockInfoCache.Add(new SideChainBlockData()
                {
                    ChainId = sideChainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = Hash.FromString((sideChainId + 1).ToString())
                });
            }

            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(
                new GetPendingCrossChainIndexingProposalOutput
                {
                    Proposer = SampleAddress.AddressList[0],
                    ProposalId = Hash.FromString("ProposalId"),
                    ProposedCrossChainBlockData = new CrossChainBlockData
                    {
                        SideChainBlockDataList = {sideChainBlockInfoCache}
                    },
                    ToBeReleased = false,
                    ExpiredTime = TimestampHelper.GetUtcNow().AddSeconds(10)
                });
            var res = await _crossChainIndexingDataService.PrepareExtraDataForNextMiningAsync(Hash.Empty, 1);
            Assert.Empty(res);
        }

        [Fact]
        public async Task PrepareExtraDataForNextMiningAsync_Test()
        {
            var sideChainId = 123;
            var sideChainBlockInfoCache = new List<SideChainBlockData>();
            var cachingCount = 5;
            for (int i = 0; i < cachingCount + CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                sideChainBlockInfoCache.Add(new SideChainBlockData()
                {
                    ChainId = sideChainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = Hash.FromString((sideChainId + 1).ToString())
                });
            }

            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockDataList = {sideChainBlockInfoCache}
            };
            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(
                new GetPendingCrossChainIndexingProposalOutput
                {
                    Proposer = SampleAddress.AddressList[0],
                    ProposalId = Hash.FromString("ProposalId"),
                    ProposedCrossChainBlockData = crossChainBlockData,
                    ToBeReleased = true,
                    ExpiredTime = TimestampHelper.GetUtcNow().AddSeconds(10)
                });

            var res = await _crossChainIndexingDataService.PrepareExtraDataForNextMiningAsync(Hash.Empty, 1);
            var crossChainExtraData = CrossChainExtraData.Parser.ParseFrom(res);
            var expectedMerkleTreeRoot = BinaryMerkleTree
                .FromLeafNodes(sideChainBlockInfoCache.Select(s => s.TransactionStatusMerkleTreeRoot)).Root;
            Assert.Equal(expectedMerkleTreeRoot, crossChainExtraData.TransactionStatusMerkleTreeRoot);
            Assert.Equal(res,
                _crossChainIndexingDataService.ExtractCrossChainExtraDataFromCrossChainBlockData(crossChainBlockData));
        }

        [Fact]
        public async Task PrepareExtraDataForNextMiningAsync_Expired_Test()
        {
            var sideChainId = 123;
            var sideChainBlockInfoCache = new List<SideChainBlockData>();
            var cachingCount = 5;
            for (int i = 0; i < cachingCount + CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                sideChainBlockInfoCache.Add(new SideChainBlockData()
                {
                    ChainId = sideChainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = Hash.FromString((sideChainId + 1).ToString())
                });
            }

            _crossChainTestHelper.AddFakeSideChainIdHeight(sideChainId, 1);
            var fakeCache = new Dictionary<int, List<IBlockCacheEntity>>
                {{sideChainId, sideChainBlockInfoCache.ToList<IBlockCacheEntity>()}};
            AddFakeCacheData(fakeCache);

            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockDataList = {sideChainBlockInfoCache}
            };
            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(
                new GetPendingCrossChainIndexingProposalOutput
                {
                    Proposer = SampleAddress.AddressList[0],
                    ProposalId = Hash.FromString("ProposalId"),
                    ProposedCrossChainBlockData = crossChainBlockData,
                    ToBeReleased = true,
                    ExpiredTime = TimestampHelper.GetUtcNow().AddSeconds(-1)
                });

            var res = await _crossChainIndexingDataService.PrepareExtraDataForNextMiningAsync(Hash.Empty, 1);
            Assert.Empty(res);
            var crossChainTransactionInput =
                await _crossChainIndexingDataService.GetCrossChainTransactionInputForNextMiningAsync(Hash.Empty, 1);
            Assert.NotNull(crossChainTransactionInput);
            var crossChainBlockDataFromInput = CrossChainBlockData.Parser.ParseFrom(crossChainTransactionInput.Value);

            Assert.Equal(CrossChainConstants.DefaultBlockCacheEntityCount,
                crossChainBlockDataFromInput.SideChainBlockDataList.Count);
        }

        [Fact]
        public async Task PrepareExtraDataForNextMiningAsync_AlmostExpired_Test()
        {
            var sideChainId = 123;
            var sideChainBlockInfoCache = new List<SideChainBlockData>();
            var cachingCount = 5;
            for (int i = 0; i < cachingCount + CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                sideChainBlockInfoCache.Add(new SideChainBlockData()
                {
                    ChainId = sideChainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = Hash.FromString((sideChainId + 1).ToString())
                });
            }

            _crossChainTestHelper.AddFakeSideChainIdHeight(sideChainId, 1);
            var fakeCache = new Dictionary<int, List<IBlockCacheEntity>>
                {{sideChainId, sideChainBlockInfoCache.ToList<IBlockCacheEntity>()}};
            AddFakeCacheData(fakeCache);

            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockDataList = {sideChainBlockInfoCache}
            };
            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(
                new GetPendingCrossChainIndexingProposalOutput
                {
                    Proposer = SampleAddress.AddressList[0],
                    ProposalId = Hash.FromString("ProposalId"),
                    ProposedCrossChainBlockData = crossChainBlockData,
                    ToBeReleased = true,
                    ExpiredTime = TimestampHelper.GetUtcNow().AddMilliseconds(500)
                });

            var res = await _crossChainIndexingDataService.PrepareExtraDataForNextMiningAsync(Hash.Empty, 1);
            var crossChainExtraData = CrossChainExtraData.Parser.ParseFrom(res);
            var expectedMerkleTreeRoot = BinaryMerkleTree
                .FromLeafNodes(sideChainBlockInfoCache.Select(s => s.TransactionStatusMerkleTreeRoot)).Root;
            Assert.Equal(expectedMerkleTreeRoot, crossChainExtraData.TransactionStatusMerkleTreeRoot);
            Assert.Equal(res,
                _crossChainIndexingDataService.ExtractCrossChainExtraDataFromCrossChainBlockData(crossChainBlockData));
        }
        
        [Fact]
        public async Task PrepareExtraDataForNextMiningAsync_AlmostExpired_NotApproved_Test()
        {
            var sideChainId = 123;
            var sideChainBlockInfoCache = new List<SideChainBlockData>();
            var cachingCount = 5;
            for (int i = 0; i < cachingCount + CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                sideChainBlockInfoCache.Add(new SideChainBlockData()
                {
                    ChainId = sideChainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = Hash.FromString((sideChainId + 1).ToString())
                });
            }

            _crossChainTestHelper.AddFakeSideChainIdHeight(sideChainId, 1);
            var fakeCache = new Dictionary<int, List<IBlockCacheEntity>>
                {{sideChainId, sideChainBlockInfoCache.ToList<IBlockCacheEntity>()}};
            AddFakeCacheData(fakeCache);

            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockDataList = {sideChainBlockInfoCache}
            };
            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(
                new GetPendingCrossChainIndexingProposalOutput
                {
                    Proposer = SampleAddress.AddressList[0],
                    ProposalId = Hash.FromString("ProposalId"),
                    ProposedCrossChainBlockData = crossChainBlockData,
                    ToBeReleased = false,
                    ExpiredTime = TimestampHelper.GetUtcNow().AddMilliseconds(500)
                });

            var res = await _crossChainIndexingDataService.PrepareExtraDataForNextMiningAsync(Hash.Empty, 1);
            Assert.Empty(res);
        }

        [Fact]
        public async Task GetCrossChainBlockDataForNextMining_WithoutCachingParentBlock_Test()
        {
            var sideChainId = _chainOptions.ChainId;
            var blockInfoCache = new List<IBlockCacheEntity>();
            var cachingCount = 5;
            for (int i = 0; i < cachingCount + CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                blockInfoCache.Add(new SideChainBlockData()
                {
                    ChainId = sideChainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = Hash.FromString(i.ToString())
                });
            }

            _crossChainTestHelper.AddFakeSideChainIdHeight(sideChainId, 1);
            var fakeCache = new Dictionary<int, List<IBlockCacheEntity>> {{sideChainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            var res = await _crossChainIndexingDataService.PrepareExtraDataForNextMiningAsync(Hash.Empty, 1);
            Assert.Empty(res);
            var crossChainTransactionInput =
                await _crossChainIndexingDataService.GetCrossChainTransactionInputForNextMiningAsync(Hash.Empty, 1);
            Assert.NotNull(crossChainTransactionInput);
            var crossChainBlockData = CrossChainBlockData.Parser.ParseFrom(crossChainTransactionInput.Value);
            Assert.Equal(CrossChainConstants.DefaultBlockCacheEntityCount,
                crossChainBlockData.SideChainBlockDataList.Count);
            Assert.Empty(crossChainBlockData.ParentChainBlockDataList);
        }

        [Fact]
        public async Task GetCrossChainBlockDataForNextMining_WithoutCachingSideBlock_Test()
        {
            var parentChainId = _chainOptions.ChainId;
            var blockInfoCache = new List<IBlockCacheEntity>();
            var cachingCount = 5;
            for (int i = 0; i < cachingCount + CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                blockInfoCache.Add(new ParentChainBlockData()
                {
                    ChainId = parentChainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = Hash.FromString(i.ToString())
                });
            }

            _crossChainTestHelper.SetFakeLibHeight(2);
            _crossChainTestHelper.AddFakeParentChainIdHeight(parentChainId, 1);
            var fakeCache = new Dictionary<int, List<IBlockCacheEntity>> {{parentChainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            var res = await _crossChainIndexingDataService.PrepareExtraDataForNextMiningAsync(Hash.Empty, 1);
            Assert.Empty(res);
            var crossChainTransactionInput =
                await _crossChainIndexingDataService.GetCrossChainTransactionInputForNextMiningAsync(Hash.Empty, 1);
            Assert.NotNull(crossChainTransactionInput);
            var crossChainBlockData = CrossChainBlockData.Parser.ParseFrom(crossChainTransactionInput.Value);
            Assert.Equal(CrossChainConstants.DefaultBlockCacheEntityCount,
                crossChainBlockData.ParentChainBlockDataList.Count);
            Assert.Empty(crossChainBlockData.SideChainBlockDataList);
        }
        
        [Fact]
        public async Task GetCrossChainBlockDataForNextMining_FromGenesisBlock_Test()
        {
            var sideChainId = _chainOptions.ChainId;
            var blockInfoCache = new List<IBlockCacheEntity>();
            var cachingCount = 5;
            for (int i = 0; i < cachingCount + CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                blockInfoCache.Add(new SideChainBlockData()
                {
                    ChainId = sideChainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = Hash.FromString(i.ToString())
                });
            }

            _crossChainTestHelper.AddFakeSideChainIdHeight(sideChainId, 0);
            var fakeCache = new Dictionary<int, List<IBlockCacheEntity>> {{sideChainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            var res = await _crossChainIndexingDataService.PrepareExtraDataForNextMiningAsync(Hash.Empty, 1);
            Assert.Empty(res);
            var crossChainTransactionInput =
                await _crossChainIndexingDataService.GetCrossChainTransactionInputForNextMiningAsync(Hash.Empty, 1);
            Assert.NotNull(crossChainTransactionInput);
            var crossChainBlockData = CrossChainBlockData.Parser.ParseFrom(crossChainTransactionInput.Value);
            Assert.True(1 == crossChainBlockData.SideChainBlockDataList.Count);
            Assert.Empty(crossChainBlockData.ParentChainBlockDataList);
        }
        
        [Fact]
        public async Task GetCrossChainBlockDataForNextMining_WithoutLIB_Test()
        {
            var parentChainId = _chainOptions.ChainId;
            var blockInfoCache = new List<IBlockCacheEntity>();
            var cachingCount = 5;
            for (int i = 0; i < cachingCount + CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                blockInfoCache.Add(new ParentChainBlockData()
                {
                    ChainId = parentChainId,
                    Height = (i + 1),
                });
            }

            _crossChainTestHelper.AddFakeParentChainIdHeight(parentChainId, 1);
            var fakeCache = new Dictionary<int, List<IBlockCacheEntity>> {{parentChainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            var res = await _crossChainIndexingDataService.PrepareExtraDataForNextMiningAsync(Hash.Empty, 1);
            Assert.Empty(res);
            var crossChainTransactionInput =
                await _crossChainIndexingDataService.GetCrossChainTransactionInputForNextMiningAsync(Hash.Empty, 1);
            Assert.Null(crossChainTransactionInput);
        }

        #endregion
    }
}