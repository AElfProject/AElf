using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs7;
using AElf.Contracts.CrossChain;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Xunit;

namespace AElf.CrossChain
{
    public class CrossChainBlockExtraDataProviderTest : CrossChainTestBase
    {
        private readonly IBlockExtraDataProvider _crossChainBlockExtraDataProvider;
        private readonly CrossChainTestHelper _crossChainTestHelper;

        public CrossChainBlockExtraDataProviderTest()
        {
            _crossChainBlockExtraDataProvider = GetRequiredService<IBlockExtraDataProvider>();
            _crossChainTestHelper = GetRequiredService<CrossChainTestHelper>();
        }

        [Fact]
        public async Task FillExtraData_GenesisHeight_Test()
        {
            var header = new BlockHeader
            {
                PreviousBlockHash = Hash.FromString("PreviousHash"),
                Height = 1
            };
            var bytes = await _crossChainBlockExtraDataProvider.GetExtraDataForFillingBlockHeaderAsync(header);
            Assert.Empty(bytes);
        }

        [Fact]
        public async Task FillExtraData__NoPendingProposal_Test()
        {
            var header = new BlockHeader
            {
                PreviousBlockHash = Hash.FromString("PreviousHash"),
                Height = 2
            };
            var bytes = await _crossChainBlockExtraDataProvider.GetExtraDataForFillingBlockHeaderAsync(header);
            Assert.Empty(bytes);
        }

        [Fact]
        public async Task FillExtraData__NotApproved_Test()
        {
            int sideChainId = _chainOptions.ChainId;
            var sideChainBlockData = new List<SideChainBlockData>();

            for (int i = 0; i < CrossChainConstants.MinimalBlockCacheEntityCount + 1; i++)
            {
                sideChainBlockData.Add(new SideChainBlockData()
                    {
                        Height = i + 1,
                        ChainId = sideChainId
                    }
                );
            }

            var header = new BlockHeader
            {
                PreviousBlockHash = Hash.FromString("PreviousHash"),
                Height = 2
            };
            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(
                new GetPendingCrossChainIndexingProposalOutput
                {
                    Proposer = SampleAddress.AddressList[0],
                    ProposalId = Hash.FromString("ProposalId"),
                    ProposedCrossChainBlockData = new CrossChainBlockData
                    {
                        SideChainBlockDataList = {sideChainBlockData}
                    },
                    ToBeReleased = false,
                    ExpiredTime = TimestampHelper.GetUtcNow().AddSeconds(10)
                });
            var bytes = await _crossChainBlockExtraDataProvider.GetExtraDataForFillingBlockHeaderAsync(header);
            Assert.Empty(bytes);
        }

        [Fact]
        public async Task FillExtraData_WithoutSideChainBlockData_Test()
        {
            int parentChainId = _chainOptions.ChainId;
            var parentChainBlockDataList = new List<ParentChainBlockData>();

            for (int i = 0; i < CrossChainConstants.MinimalBlockCacheEntityCount + 1; i++)
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
                PreviousBlockHash = Hash.FromString("PreviousHash"),
                Height = 2
            };
            _crossChainTestHelper.AddFakePendingCrossChainIndexingProposal(
                new GetPendingCrossChainIndexingProposalOutput
                {
                    Proposer = SampleAddress.AddressList[0],
                    ProposalId = Hash.FromString("ProposalId"),
                    ProposedCrossChainBlockData = new CrossChainBlockData
                    {
                        ParentChainBlockDataList = {parentChainBlockDataList}
                    },
                    ToBeReleased = true,
                    ExpiredTime = TimestampHelper.GetUtcNow().AddSeconds(10)
                });
            var bytes = await _crossChainBlockExtraDataProvider.GetExtraDataForFillingBlockHeaderAsync(header);
            Assert.Empty(bytes);
        }

        [Fact]
        public async Task FillExtraData_Test()
        {
            var fakeMerkleTreeRoot1 = Hash.FromString("fakeMerkleTreeRoot1");
            var fakeMerkleTreeRoot2 = Hash.FromString("fakeMerkleTreeRoot2");
            var fakeMerkleTreeRoot3 = Hash.FromString("fakeMerkleTreeRoot3");

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

            for (int i = 2; i < CrossChainConstants.MinimalBlockCacheEntityCount + 2; i++)
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
                    ProposalId = Hash.FromString("ProposalId"),
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
                PreviousBlockHash = Hash.FromString("PreviousHash"),
                Height = 2
            };

            var sideChainTxMerkleTreeRoot =
                await _crossChainBlockExtraDataProvider.GetExtraDataForFillingBlockHeaderAsync(header);
            var merkleTreeRoot = BinaryMerkleTree
                .FromLeafNodes(list1.Concat(list2).Concat(list3).Select(sideChainBlockData =>
                    sideChainBlockData.TransactionStatusMerkleTreeRoot)).Root;
            var expected = new CrossChainExtraData {TransactionStatusMerkleTreeRoot = merkleTreeRoot}.ToByteString();
            Assert.Equal(expected, sideChainTxMerkleTreeRoot);
        }
    }
}