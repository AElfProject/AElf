using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.CrossChain.Cache;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Types;
using AElf.Types.CSharp;
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
        public async Task FillExtraData_GenesisHeight()
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
        public async Task FillExtraData_WithoutCacheData()
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
        public async Task FillExtraData_WithoutSideChainCacheData()
        {
            int chainId1 = 123;
            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId1, 0);
            var fakeParentChainBlockDataList = new List<IBlockInfo>();

            for (int i = 0; i < CrossChainConsts.MinimalBlockInfoCacheThreshold + 1; i++)
            {
                fakeParentChainBlockDataList.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainHeight = i + 1,
                        ParentChainId = chainId1
                    }
                });
            }

            AddFakeCacheData(new Dictionary<int, List<IBlockInfo>>
            {
                {chainId1, fakeParentChainBlockDataList}
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
        public async Task FillExtraData()
        {
            var fakeMerkleTreeRoot1 = Hash.FromString("fakeMerkleTreeRoot1");
            var fakeMerkleTreeRoot2 = Hash.FromString("fakeMerkleTreeRoot2");
            var fakeMerkleTreeRoot3 = Hash.FromString("fakeMerkleTreeRoot3");

            int chainId1 = ChainHelpers.ConvertBase58ToChainId("2112");
            int chainId2 = ChainHelpers.ConvertBase58ToChainId("2113");
            int chainId3 = ChainHelpers.ConvertBase58ToChainId("2114");
            var fakeSideChainBlockDataList = new List<SideChainBlockData>
            {
                new SideChainBlockData
                {
                    SideChainHeight = 1,
                    TransactionMerkleTreeRoot = fakeMerkleTreeRoot1,
                    SideChainId = chainId1
                },
                new SideChainBlockData
                {
                    SideChainHeight = 1,
                    TransactionMerkleTreeRoot = fakeMerkleTreeRoot2,
                    SideChainId = chainId2
                },
                new SideChainBlockData
                {
                    SideChainHeight = 1,
                    TransactionMerkleTreeRoot = fakeMerkleTreeRoot3,
                    SideChainId = chainId3
                }
            };
            
            var list1 = new List<IBlockInfo>();
            var list2 = new List<IBlockInfo>();
            var list3 = new List<IBlockInfo>();
            
            list1.Add(fakeSideChainBlockDataList[0]);
            list2.Add(fakeSideChainBlockDataList[1]);          
            list3.Add(fakeSideChainBlockDataList[2]);

            for (int i = 2; i < CrossChainConsts.MinimalBlockInfoCacheThreshold + 2; i++)
            {
                list1.Add(new SideChainBlockData
                {
                    SideChainHeight = i,
                    TransactionMerkleTreeRoot = fakeMerkleTreeRoot1,
                    SideChainId = chainId1
                });
                list2.Add(new SideChainBlockData
                {
                    SideChainHeight = i,
                    TransactionMerkleTreeRoot = fakeMerkleTreeRoot2,
                    SideChainId = chainId2
                });
                list3.Add(new SideChainBlockData
                {
                    SideChainHeight = i,
                    TransactionMerkleTreeRoot = fakeMerkleTreeRoot3,
                    SideChainId = chainId3
                });
                
            }

            AddFakeCacheData(new Dictionary<int, List<IBlockInfo>>
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
            var merkleTreeRoot = new BinaryMerkleTree()
                .AddNodes(fakeSideChainBlockDataList.Select(sideChainBlockData => sideChainBlockData.TransactionMerkleTreeRoot))
                .ComputeRootHash();
            var expected = new CrossChainExtraData {SideChainTransactionsRoot = merkleTreeRoot}.ToByteString();
            Assert.Equal(expected, sideChainTxMerkleTreeRoot);
        }
    }
}