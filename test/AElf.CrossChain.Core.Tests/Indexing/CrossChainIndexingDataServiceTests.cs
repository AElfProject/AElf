using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs7;
using AElf.Contracts.CrossChain;
using AElf.CrossChain.Indexing.Application;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
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
        public async Task GetIndexedCrossChainBlockData_WithIndex_Test()
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
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom((sideChainId + 1).ToString())
                });
            }

            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(
                new GetPendingCrossChainIndexingProposalOutput
                {
                    Proposer = SampleAddress.AddressList[0],
                    ProposalId = HashHelper.ComputeFrom("ProposalId"),
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
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom((sideChainId + 1).ToString())
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
                    ProposalId = HashHelper.ComputeFrom("ProposalId"),
                    ProposedCrossChainBlockData = crossChainBlockData,
                    ToBeReleased = true,
                    ExpiredTime = TimestampHelper.GetUtcNow().AddSeconds(10)
                });

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
            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(
                new GetPendingCrossChainIndexingProposalOutput
                {
                    Proposer = SampleAddress.AddressList[0],
                    ProposalId = HashHelper.ComputeFrom("ProposalId"),
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
            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(
                new GetPendingCrossChainIndexingProposalOutput
                {
                    Proposer = SampleAddress.AddressList[0],
                    ProposalId = HashHelper.ComputeFrom("ProposalId"),
                    ProposedCrossChainBlockData = crossChainBlockData,
                    ToBeReleased = true,
                    ExpiredTime = TimestampHelper.GetUtcNow().AddMilliseconds(500)
                });

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
            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(
                new GetPendingCrossChainIndexingProposalOutput
                {
                    Proposer = SampleAddress.AddressList[0],
                    ProposalId = HashHelper.ComputeFrom("ProposalId"),
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

            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(
                new GetPendingCrossChainIndexingProposalOutput());
            _crossChainTestHelper.AddFakeSideChainIdHeight(sideChainId, 1);
            AddFakeCacheData(new Dictionary<int, List<ICrossChainBlockEntity>>
                {{sideChainId, sideChainBlockInfoCache}});

            var pendingProposal = new GetPendingCrossChainIndexingProposalOutput
            {
                Proposer = SampleAddress.AddressList[0],
                ProposalId = HashHelper.ComputeFrom("ProposalId"),
                ProposedCrossChainBlockData = new CrossChainBlockData(),
                ToBeReleased = true,
                ExpiredTime = TimestampHelper.GetUtcNow().AddSeconds(10)
            };
            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(pendingProposal);


            var res = await _crossChainIndexingDataService.PrepareExtraDataForNextMiningAsync(previousBlockHash,
                previousBlockHeight);
            Assert.Empty(res);

            var crossChainTransactionInput =
                await _crossChainIndexingDataService.GetCrossChainTransactionInputForNextMiningAsync(previousBlockHash,
                    previousBlockHeight);

            Assert.Equal(crossChainTransactionInput.MethodName,
                nameof(CrossChainContractContainer.CrossChainContractStub.ReleaseCrossChainIndexing));

            var proposalIdInParam = Hash.Parser.ParseFrom(crossChainTransactionInput.Value);
            Assert.Equal(pendingProposal.ProposalId, proposalIdInParam);
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

            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(
                new GetPendingCrossChainIndexingProposalOutput());
            _crossChainTestHelper.AddFakeSideChainIdHeight(sideChainId, 1);
            AddFakeCacheData(new Dictionary<int, List<ICrossChainBlockEntity>>
                {{sideChainId, sideChainBlockInfoCache}});

            var pendingProposal = new GetPendingCrossChainIndexingProposalOutput
            {
                Proposer = SampleAddress.AddressList[0],
                ProposalId = HashHelper.ComputeFrom("ProposalId"),
                ProposedCrossChainBlockData = new CrossChainBlockData(),
                ToBeReleased = false,
                ExpiredTime = TimestampHelper.GetUtcNow().AddSeconds(10)
            };
            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(pendingProposal);
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
            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(
                new GetPendingCrossChainIndexingProposalOutput
                {
                    Proposer = SampleAddress.AddressList[0],
                    ProposalId = HashHelper.ComputeFrom("ProposalId"),
                    ProposedCrossChainBlockData = new CrossChainBlockData
                    {
                        ParentChainBlockDataList = {parentChainBlockDataList}
                    },
                    ToBeReleased = true,
                    ExpiredTime = TimestampHelper.GetUtcNow().AddSeconds(10)
                });
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

            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(
                new GetPendingCrossChainIndexingProposalOutput
                {
                    Proposer = SampleAddress.AddressList[0],
                    ProposalId = HashHelper.ComputeFrom("ProposalId"),
                    ProposedCrossChainBlockData = new CrossChainBlockData
                    {
                        SideChainBlockDataList = {list1, list2, list3}
                    },
                    ToBeReleased = true,
                    ExpiredTime = TimestampHelper.GetUtcNow().AddSeconds(10)
                });

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

        #endregion
    }
}