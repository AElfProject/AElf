using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Cache;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
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
        public async Task FillExtraData_WithoutCacheData_Test()
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
        public async Task FillExtraData_WithoutSideChainCacheData_Test()
        {
            int chainId = _chainOptions.ChainId;
            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 0);
            var fakeParentChainBlockDataList = new List<IBlockCacheEntity>();

            for (int i = 0; i < CrossChainConstants.MinimalBlockCacheEntityCount + 1; i++)
            {
                fakeParentChainBlockDataList.Add(new ParentChainBlockData()
                    {
                        Height = i + 1,
                        ChainId = chainId
                    }
                );
            }

            AddFakeCacheData(new Dictionary<int, List<IBlockCacheEntity>>
            {
                {chainId, fakeParentChainBlockDataList}
            });
            _crossChainTestHelper.SetFakeLibHeight(1);

            var header = new BlockHeader
            {
                PreviousBlockHash = Hash.FromString("PreviousHash"),
                Height = 2
            };
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

            var list1 = new List<IBlockCacheEntity>();
            var list2 = new List<IBlockCacheEntity>();
            var list3 = new List<IBlockCacheEntity>();

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

            AddFakeCacheData(new Dictionary<int, List<IBlockCacheEntity>>
            {
                {chainId1, list1},
                {chainId2, list2},
                {chainId3, list3}
            });

            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId1, 0);
            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId2, 0);
            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId3, 0);
            _crossChainTestHelper.SetFakeLibHeight(1);
            var header = new BlockHeader
            {
                PreviousBlockHash = Hash.FromString("PreviousHash"),
                Height = 2
            };

            var sideChainTxMerkleTreeRoot =
                await _crossChainBlockExtraDataProvider.GetExtraDataForFillingBlockHeaderAsync(header);
            var merkleTreeRoot = BinaryMerkleTree
                .FromLeafNodes(fakeSideChainBlockDataList.Select(sideChainBlockData =>
                    sideChainBlockData.TransactionStatusMerkleTreeRoot)).Root;
            var expected = new CrossChainExtraData {TransactionStatusMerkleTreeRoot = merkleTreeRoot}.ToByteString();
            Assert.Equal(expected, sideChainTxMerkleTreeRoot);
        }
    }
}