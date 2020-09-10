using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Standards.ACS7;
using AElf.Contracts.CrossChain;
using AElf.CrossChain.Indexing.Application;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.CrossChain.Indexing
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
        public async Task GetIndexedSideChainBlockData_WithIndex_Test()
        {
            var chainId = _chainOptions.ChainId;
            var fakeMerkleTreeRoot1 = HashHelper.ComputeFrom("fakeMerkleTreeRoot1");
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

            AddFakeCacheData(new Dictionary<int, List<ICrossChainBlockEntity>>
            {
                {
                    chainId,
                    new List<ICrossChainBlockEntity>
                    {
                        fakeSideChainBlockData
                    }
                }
            });

            var res = await _crossChainIndexingDataService.GetIndexedSideChainBlockDataAsync(
                fakeSideChainBlockData.BlockHeaderHash, 1);
            Assert.True(res.SideChainBlockDataList[0].Height == fakeSideChainBlockData.Height);
            Assert.True(res.SideChainBlockDataList[0].ChainId == chainId);
        }

        [Fact]
        public async Task GetIndexedSideChainBlockData_WithoutIndex_Test()
        {
            var chainId = _chainOptions.ChainId;
            var fakeSideChainBlockData = new SideChainBlockData
            {
                Height = 1,
                ChainId = chainId
            };

            var fakeIndexedCrossChainBlockData = new CrossChainBlockData();
            fakeIndexedCrossChainBlockData.SideChainBlockDataList.AddRange(new[] {fakeSideChainBlockData});

            var res = await _crossChainIndexingDataService.GetIndexedSideChainBlockDataAsync(
                fakeSideChainBlockData.BlockHeaderHash, 1);
            Assert.True(res == null);
        }

        [Fact]
        public async Task PrepareExtraDataForNextMiningAsync_NoProposal_FirstTimeIndexing_Test()
        {
            var sideChainId = 123;
            var sideChainBlockInfoCache = new List<ICrossChainBlockEntity>();
            var cachingCount = 5;
            for (int i = 0; i < cachingCount + CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                sideChainBlockInfoCache.Add(new SideChainBlockData()
                {
                    ChainId = sideChainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom((sideChainId + 1).ToString())
                });
            }

            _crossChainTestHelper.AddFakeSideChainIdHeight(sideChainId, 0);
            var fakeCache = new Dictionary<int, List<ICrossChainBlockEntity>> {{sideChainId, sideChainBlockInfoCache}};
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
            var sideChainBlockInfoCache = new List<ICrossChainBlockEntity>();
            var cachingCount = 5;
            for (int i = 1; i < cachingCount + CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                sideChainBlockInfoCache.Add(new SideChainBlockData()
                {
                    ChainId = sideChainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom((sideChainId + 1).ToString())
                });
            }

            _crossChainTestHelper.AddFakeSideChainIdHeight(sideChainId, 1);
            var fakeCache = new Dictionary<int, List<ICrossChainBlockEntity>> {{sideChainId, sideChainBlockInfoCache}};
            AddFakeCacheData(fakeCache);

            var res = await _crossChainIndexingDataService.PrepareExtraDataForNextMiningAsync(Hash.Empty, 1);
            Assert.Empty(res);
            var crossChainTransactionInput =
                await _crossChainIndexingDataService.GetCrossChainTransactionInputForNextMiningAsync(Hash.Empty, 1);
            Assert.NotNull(crossChainTransactionInput);
            var crossChainBlockData = CrossChainBlockData.Parser.ParseFrom(crossChainTransactionInput.Value);
            crossChainTransactionInput.MethodName.ShouldBe(nameof(CrossChainContractImplContainer.CrossChainContractImplStub
                .ProposeCrossChainIndexing));
            Assert.Equal(CrossChainConstants.DefaultBlockCacheEntityCount,
                crossChainBlockData.SideChainBlockDataList.Count);
        }
        
        [Fact]
        public async Task PrepareExtraDataForNextMiningAsync_NoProposal_NoCache_Test()
        {
            var sideChainId = 123;
            _crossChainTestHelper.AddFakeSideChainIdHeight(sideChainId, 1);

            var res = await _crossChainIndexingDataService.PrepareExtraDataForNextMiningAsync(Hash.Empty, 1);
            res.ShouldBeEmpty();
            var crossChainTransactionInput =
                await _crossChainIndexingDataService.GetCrossChainTransactionInputForNextMiningAsync(Hash.Empty, 1);
            crossChainTransactionInput.ShouldBeNull();
        }

        [Fact]
        public async Task CheckExtraDataIsNeeded_ToBeReleased_Test()
        {
            var sideChainId = 123;
            var utcNow = TimestampHelper.GetUtcNow();
            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(sideChainId,
                CreatePendingChainIndexingProposalStatus(SampleAddress.AddressList[0],
                    HashHelper.ComputeFrom("ProposalId"),
                    new CrossChainBlockData(), true, utcNow.AddSeconds(1)));
            var res = await _crossChainIndexingDataService.CheckExtraDataIsNeededAsync(Hash.Empty, 1, utcNow);
            res.ShouldBe(true);
        }
        
        [Fact]
        public async Task CheckExtraDataIsNeeded_NotToBeReleased_Test()
        {
            var sideChainId = 123;

            var utcNow = TimestampHelper.GetUtcNow();
            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(sideChainId,
                CreatePendingChainIndexingProposalStatus(SampleAddress.AddressList[0],
                    HashHelper.ComputeFrom("ProposalId"),
                    new CrossChainBlockData(), false));
            var res = await _crossChainIndexingDataService.CheckExtraDataIsNeededAsync(Hash.Empty, 1, utcNow);
            res.ShouldBe(false);
        }
        
        [Fact]
        public async Task PrepareExtraDataForNextMiningAsync_NotApproved_Test()
        {
            var sideChainId = 123;
            var sideChainBlockInfoCache = new List<SideChainBlockData>();
            var cachingCount = 5;
            for (int i = 0; i < cachingCount + CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                sideChainBlockInfoCache.Add(new SideChainBlockData
                {
                    ChainId = sideChainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom((sideChainId + 1).ToString())
                });
            }

            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(sideChainId,
                CreatePendingChainIndexingProposalStatus(SampleAddress.AddressList[0],
                    HashHelper.ComputeFrom("ProposalId"),
                    new CrossChainBlockData
                    {
                        SideChainBlockDataList = {sideChainBlockInfoCache}
                    },
                    false)
            );
            var res = await _crossChainIndexingDataService.PrepareExtraDataForNextMiningAsync(Hash.Empty, 1);
            Assert.Empty(res);
            
            var crossChainTransactionInput =
                await _crossChainIndexingDataService.GetCrossChainTransactionInputForNextMiningAsync(Hash.Empty, 1);
            crossChainTransactionInput.ShouldBeNull();
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
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom((sideChainId + 1).ToString())
                });
            }

            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockDataList = {sideChainBlockInfoCache}
            };
            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(sideChainId,
                CreatePendingChainIndexingProposalStatus(SampleAddress.AddressList[0],
                    HashHelper.ComputeFrom("ProposalId"), crossChainBlockData)
            );

            var res = await _crossChainIndexingDataService.PrepareExtraDataForNextMiningAsync(Hash.Empty, 1);
            var crossChainExtraData = CrossChainExtraData.Parser.ParseFrom(res);
            var expectedMerkleTreeRoot = BinaryMerkleTree
                .FromLeafNodes(sideChainBlockInfoCache.Select(s => s.TransactionStatusMerkleTreeRoot)).Root;
            Assert.Equal(expectedMerkleTreeRoot, crossChainExtraData.TransactionStatusMerkleTreeRoot);
            Assert.Equal(res, crossChainBlockData.ExtractCrossChainExtraDataFromCrossChainBlockData());
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
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom((sideChainId + 1).ToString())
                });
            }

            _crossChainTestHelper.AddFakeSideChainIdHeight(sideChainId, 1);
            var fakeCache = new Dictionary<int, List<ICrossChainBlockEntity>>
                {{sideChainId, sideChainBlockInfoCache.ToList<ICrossChainBlockEntity>()}};
            AddFakeCacheData(fakeCache);

            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockDataList = {sideChainBlockInfoCache}
            };
            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(sideChainId, 
                CreatePendingChainIndexingProposalStatus(SampleAddress.AddressList[0],
                    HashHelper.ComputeFrom("ProposalId"), crossChainBlockData, true,
                    TimestampHelper.GetUtcNow().AddSeconds(-1)));

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
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom((sideChainId + 1).ToString())
                });
            }

            _crossChainTestHelper.AddFakeSideChainIdHeight(sideChainId, 1);
            var fakeCache = new Dictionary<int, List<ICrossChainBlockEntity>>
                {{sideChainId, sideChainBlockInfoCache.ToList<ICrossChainBlockEntity>()}};
            AddFakeCacheData(fakeCache);

            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockDataList = {sideChainBlockInfoCache}
            };
            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(sideChainId,
                CreatePendingChainIndexingProposalStatus(SampleAddress.AddressList[0],
                    HashHelper.ComputeFrom("ProposalId"), crossChainBlockData, true,
                    TimestampHelper.GetUtcNow().AddMilliseconds(500)));

            var res = await _crossChainIndexingDataService.PrepareExtraDataForNextMiningAsync(Hash.Empty, 1);
            var crossChainExtraData = CrossChainExtraData.Parser.ParseFrom(res);
            var expectedMerkleTreeRoot = BinaryMerkleTree
                .FromLeafNodes(sideChainBlockInfoCache.Select(s => s.TransactionStatusMerkleTreeRoot)).Root;
            Assert.Equal(expectedMerkleTreeRoot, crossChainExtraData.TransactionStatusMerkleTreeRoot);
            Assert.Equal(res, crossChainBlockData.ExtractCrossChainExtraDataFromCrossChainBlockData());
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
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom((sideChainId + 1).ToString())
                });
            }

            _crossChainTestHelper.AddFakeSideChainIdHeight(sideChainId, 1);
            var fakeCache = new Dictionary<int, List<ICrossChainBlockEntity>>
                {{sideChainId, sideChainBlockInfoCache.ToList<ICrossChainBlockEntity>()}};
            AddFakeCacheData(fakeCache);

            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockDataList = {sideChainBlockInfoCache}
            };
            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(sideChainId,
                CreatePendingChainIndexingProposalStatus(SampleAddress.AddressList[0],
                    HashHelper.ComputeFrom("ProposalId"), crossChainBlockData, false,
                    TimestampHelper.GetUtcNow().AddMilliseconds(500)
                ));

            var res = await _crossChainIndexingDataService.PrepareExtraDataForNextMiningAsync(Hash.Empty, 1);
            Assert.Empty(res);
        }

        [Fact]
        public async Task GetCrossChainBlockDataForNextMining_WithoutCachingParentBlock_Test()
        {
            var sideChainId = _chainOptions.ChainId;
            var blockInfoCache = new List<ICrossChainBlockEntity>();
            var cachingCount = 5;
            for (int i = 0; i < cachingCount + CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                blockInfoCache.Add(new SideChainBlockData()
                {
                    ChainId = sideChainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom(i.ToString())
                });
            }

            _crossChainTestHelper.AddFakeSideChainIdHeight(sideChainId, 1);
            var fakeCache = new Dictionary<int, List<ICrossChainBlockEntity>> {{sideChainId, blockInfoCache}};
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
            var blockInfoCache = new List<ICrossChainBlockEntity>();
            var cachingCount = 5;
            for (int i = 0; i < cachingCount + CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                blockInfoCache.Add(new ParentChainBlockData()
                {
                    ChainId = parentChainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom(i.ToString())
                });
            }

            _crossChainTestHelper.SetFakeLibHeight(2);
            _crossChainTestHelper.AddFakeParentChainIdHeight(parentChainId, 1);
            var fakeCache = new Dictionary<int, List<ICrossChainBlockEntity>> {{parentChainId, blockInfoCache}};
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
            var blockInfoCache = new List<ICrossChainBlockEntity>();
            var cachingCount = 5;
            for (int i = 0; i < cachingCount + CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                blockInfoCache.Add(new SideChainBlockData()
                {
                    ChainId = sideChainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom(i.ToString())
                });
            }

            _crossChainTestHelper.AddFakeSideChainIdHeight(sideChainId, 0);
            var fakeCache = new Dictionary<int, List<ICrossChainBlockEntity>> {{sideChainId, blockInfoCache}};
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
            var blockInfoCache = new List<ICrossChainBlockEntity>();
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
            var fakeCache = new Dictionary<int, List<ICrossChainBlockEntity>> {{parentChainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            var res = await _crossChainIndexingDataService.PrepareExtraDataForNextMiningAsync(Hash.Empty, 1);
            Assert.Empty(res);
            var crossChainTransactionInput =
                await _crossChainIndexingDataService.GetCrossChainTransactionInputForNextMiningAsync(Hash.Empty, 1);
            Assert.Null(crossChainTransactionInput);
        }

        [Fact]
        public async Task GetNonIndexedBlock_Test()
        {
            _crossChainTestHelper.SetFakeLibHeight(2);
            var res = await _crossChainIndexingDataService.GetNonIndexedBlockAsync(1);
            Assert.True(res.Height.Equals(1));
        }

        [Fact]
        public async Task GetNonIndexedBlock_NoBlock_Test()
        {
            _crossChainTestHelper.SetFakeLibHeight(1);
            var res = await _crossChainIndexingDataService.GetNonIndexedBlockAsync(2);
            Assert.Null(res);
        }

        [Fact]
        public async Task GenerateTransactionInput_PendingProposal_Test()
        {
            var sideChainId = 123;
            var sideChainBlockInfoCache = new List<ICrossChainBlockEntity>();
            var previousBlockHash = HashHelper.ComputeFrom("PreviousBlockHash");
            var previousBlockHeight = 1;

            var cachingCount = 5;
            for (int i = 1; i < cachingCount + CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                var sideChainBlockData = new SideChainBlockData()
                {
                    ChainId = sideChainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom((sideChainId + 1).ToString())
                };
                sideChainBlockInfoCache.Add(sideChainBlockData);
            }

            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(sideChainId,
                new PendingChainIndexingProposalStatus());
            _crossChainTestHelper.AddFakeSideChainIdHeight(sideChainId, 1);
            AddFakeCacheData(new Dictionary<int, List<ICrossChainBlockEntity>>
                {{sideChainId, sideChainBlockInfoCache}});

            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(sideChainId,
                CreatePendingChainIndexingProposalStatus(SampleAddress.AddressList[0],
                    HashHelper.ComputeFrom("ProposalId"), new CrossChainBlockData()));


            var res = await _crossChainIndexingDataService.PrepareExtraDataForNextMiningAsync(previousBlockHash,
                previousBlockHeight);
            Assert.Empty(res);

            var crossChainTransactionInput =
                await _crossChainIndexingDataService.GetCrossChainTransactionInputForNextMiningAsync(previousBlockHash,
                    previousBlockHeight);

            Assert.Equal(nameof(CrossChainContractImplContainer.CrossChainContractImplStub.ReleaseCrossChainIndexingProposal),
                crossChainTransactionInput.MethodName);

            var sideChainIdListInParam = ReleaseCrossChainIndexingProposalInput.Parser.ParseFrom(crossChainTransactionInput.Value).ChainIdList;
            sideChainIdListInParam.ShouldContain(sideChainId);
        }

        [Fact]
        public async Task GenerateTransaction_PendingProposal_NotApproved_Test()
        {
            var sideChainId = 123;
            var sideChainBlockInfoCache = new List<ICrossChainBlockEntity>();
            var previousBlockHash = HashHelper.ComputeFrom("PreviousBlockHash");
            var previousBlockHeight = 1;

            var cachingCount = 5;
            for (int i = 1; i < cachingCount + CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                var sideChainBlockData = new SideChainBlockData()
                {
                    ChainId = sideChainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom((sideChainId + 1).ToString())
                };
                sideChainBlockInfoCache.Add(sideChainBlockData);
            }

            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(sideChainId,
                new PendingChainIndexingProposalStatus());
            _crossChainTestHelper.AddFakeSideChainIdHeight(sideChainId, 1);
            AddFakeCacheData(new Dictionary<int, List<ICrossChainBlockEntity>>
                {{sideChainId, sideChainBlockInfoCache}});

            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(sideChainId,
                CreatePendingChainIndexingProposalStatus(SampleAddress.AddressList[0],
                    HashHelper.ComputeFrom("ProposalId"), new CrossChainBlockData(), false));
            var res = await _crossChainIndexingDataService.PrepareExtraDataForNextMiningAsync(previousBlockHash,
                previousBlockHeight);
            Assert.Empty(res);

            var crossChainTransactionInput =
                await _crossChainIndexingDataService.GetCrossChainTransactionInputForNextMiningAsync(previousBlockHash,
                    previousBlockHeight);
            Assert.Null(crossChainTransactionInput);
        }

        [Fact]
        public async Task FillExtraData_WithoutSideChainBlockData_Test()
        {
            int parentChainId = _chainOptions.ChainId;
            var parentChainBlockDataList = new List<ParentChainBlockData>();

            for (int i = 0; i < CrossChainConstants.DefaultBlockCacheEntityCount + 1; i++)
            {
                parentChainBlockDataList.Add(new ParentChainBlockData
                    {
                        Height = i + 1,
                        ChainId = parentChainId
                    }
                );
            }

            var header = new BlockHeader
            {
                PreviousBlockHash = HashHelper.ComputeFrom("PreviousHash"),
                Height = 2
            };
            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(parentChainId,
                CreatePendingChainIndexingProposalStatus(SampleAddress.AddressList[0],
                    HashHelper.ComputeFrom("ProposalId"), new CrossChainBlockData
                    {
                        ParentChainBlockDataList = {parentChainBlockDataList}
                    }
                ));
            var bytes = await _crossChainIndexingDataService.PrepareExtraDataForNextMiningAsync(header.PreviousBlockHash,
                header.Height - 1);
            Assert.Empty(bytes);
        }

        [Fact]
        public async Task FillExtraData_Test()
        {
            var fakeMerkleTreeRoot1 = HashHelper.ComputeFrom("fakeMerkleTreeRoot1");
            var fakeMerkleTreeRoot2 = HashHelper.ComputeFrom("fakeMerkleTreeRoot2");
            var fakeMerkleTreeRoot3 = HashHelper.ComputeFrom("fakeMerkleTreeRoot3");

            int chainId1 = ChainHelper.ConvertBase58ToChainId("2112");
            int chainId2 = ChainHelper.ConvertBase58ToChainId("2113");
            int chainId3 = ChainHelper.ConvertBase58ToChainId("2114");
            var fakeSideChainBlockDataList = new List<SideChainBlockData>
            {
                new SideChainBlockData
                {
                    Height = 1,
                    TransactionStatusMerkleTreeRoot = fakeMerkleTreeRoot1,
                    ChainId = chainId1
                },
                new SideChainBlockData
                {
                    Height = 1,
                    TransactionStatusMerkleTreeRoot = fakeMerkleTreeRoot2,
                    ChainId = chainId2
                },
                new SideChainBlockData
                {
                    Height = 1,
                    TransactionStatusMerkleTreeRoot = fakeMerkleTreeRoot3,
                    ChainId = chainId3
                }
            };

            var list1 = new List<SideChainBlockData>();
            var list2 = new List<SideChainBlockData>();
            var list3 = new List<SideChainBlockData>();

            list1.Add(fakeSideChainBlockDataList[0]);
            list2.Add(fakeSideChainBlockDataList[1]);
            list3.Add(fakeSideChainBlockDataList[2]);

            for (int i = 2; i < CrossChainConstants.DefaultBlockCacheEntityCount + 2; i++)
            {
                list1.Add(new SideChainBlockData
                {
                    Height = i,
                    TransactionStatusMerkleTreeRoot = fakeMerkleTreeRoot1,
                    ChainId = chainId1
                });
                list2.Add(new SideChainBlockData
                {
                    Height = i,
                    TransactionStatusMerkleTreeRoot = fakeMerkleTreeRoot2,
                    ChainId = chainId2
                });
                list3.Add(new SideChainBlockData
                {
                    Height = i,
                    TransactionStatusMerkleTreeRoot = fakeMerkleTreeRoot3,
                    ChainId = chainId3
                });
            }

            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(chainId1,
                CreatePendingChainIndexingProposalStatus(SampleAddress.AddressList[0],
                    HashHelper.ComputeFrom("ProposalId"), new CrossChainBlockData
                    {
                        SideChainBlockDataList = {list1}
                    }));
            
            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(chainId2,
                CreatePendingChainIndexingProposalStatus(SampleAddress.AddressList[0],
                    HashHelper.ComputeFrom("ProposalId"), new CrossChainBlockData
                    {
                        SideChainBlockDataList = {list2}
                    }));

            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(chainId3,
                CreatePendingChainIndexingProposalStatus(SampleAddress.AddressList[0],
                    HashHelper.ComputeFrom("ProposalId"), new CrossChainBlockData
                    {
                        SideChainBlockDataList = {list3}
                    })
            );

            _crossChainTestHelper.SetFakeLibHeight(1);
            var header = new BlockHeader
            {
                PreviousBlockHash = HashHelper.ComputeFrom("PreviousHash"),
                Height = 2
            };

            var sideChainTxMerkleTreeRoot =
                await _crossChainIndexingDataService.PrepareExtraDataForNextMiningAsync(header.PreviousBlockHash,
                    header.Height - 1);
            var merkleTreeRoot = BinaryMerkleTree
                .FromLeafNodes(list1.Concat(list2).Concat(list3).Select(sideChainBlockData =>
                    sideChainBlockData.TransactionStatusMerkleTreeRoot)).Root;
            var expected = new CrossChainExtraData {TransactionStatusMerkleTreeRoot = merkleTreeRoot}.ToByteString();
            Assert.Equal(expected, sideChainTxMerkleTreeRoot);
        }

        [Fact]
        public async Task GetAllChainIdHeightPairsAtLibAsync_Test()
        {
            {
                var allChainIdAndHeightResult =
                    await _crossChainIndexingDataService.GetAllChainIdHeightPairsAtLibAsync();
                allChainIdAndHeightResult.ShouldBe(new ChainIdAndHeightDict());
            }
            
            _crossChainTestHelper.SetFakeLibHeight(2);
            
            {
                var allChainIdAndHeightResult =
                    await _crossChainIndexingDataService.GetAllChainIdHeightPairsAtLibAsync();
                allChainIdAndHeightResult.ShouldBe(new ChainIdAndHeightDict());
            }
            
            var parentChainId = ChainHelper.GetChainId(10);
            _crossChainTestHelper.AddFakeParentChainIdHeight(parentChainId, 1);
            
            var sideChainId = ChainHelper.GetChainId(100);
            _crossChainTestHelper.AddFakeSideChainIdHeight(sideChainId, 0);
            
            {
                var allChainIdAndHeightResult =
                    await _crossChainIndexingDataService.GetAllChainIdHeightPairsAtLibAsync();
                allChainIdAndHeightResult.IdHeightDict[parentChainId].ShouldBe(1);
                allChainIdAndHeightResult.IdHeightDict[sideChainId].ShouldBe(0);
            }
        }

        #endregion

        private PendingChainIndexingProposalStatus CreatePendingChainIndexingProposalStatus(Address proposer,
            Hash proposalId, CrossChainBlockData proposedCrossChainBlockData, bool toBeReleased = true,
            Timestamp expiredTimeStamp = null)
        {
            return new PendingChainIndexingProposalStatus
            {
                Proposer = proposer,
                ProposalId = proposalId,
                ProposedCrossChainBlockData = proposedCrossChainBlockData,
                ToBeReleased = toBeReleased,
                ExpiredTime = expiredTimeStamp ?? TimestampHelper.GetUtcNow().AddSeconds(10)
            };
        }
    }
}